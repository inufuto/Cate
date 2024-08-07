using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Inu.Cate.Mos6502;

internal class Compiler : Cate.Compiler
{
    public const string ZeroPageLabel = "@zp";

    public new static Compiler Instance => (Compiler)Cate.Compiler.Instance;

    public Compiler() : base(new ByteOperation(), new WordOperation(), new PointerOperation()) { }

    protected override void WriteAssembly(StreamWriter writer)
    {
        //writer.WriteLine("\tinclude\t'Cate6502.inc'");
        writer.WriteLine("extrn " + ZeroPageLabel);
        writer.WriteLine("extrn ZB0");
        base.WriteAssembly(writer);
    }

    public override void AddSavingRegister(ISet<Register> registers, Register register)
    {
        if (register is WordZeroPage wordZeroPage) {
            Debug.Assert(wordZeroPage.Low != null);
            Debug.Assert(wordZeroPage.High != null);
            base.AddSavingRegister(registers, wordZeroPage.Low);
            base.AddSavingRegister(registers, wordZeroPage.High);
        }
        if (register is WordZeroPage pointerZeroPage) {
            Debug.Assert(pointerZeroPage.Low != null);
            Debug.Assert(pointerZeroPage.High != null);
            base.AddSavingRegister(registers, pointerZeroPage.Low);
            base.AddSavingRegister(registers, pointerZeroPage.High);
        }
        else {
            base.AddSavingRegister(registers, register);
        }
    }


    public override void AllocateRegisters(List<Variable> variables, Function function)
    {
        var shortRange = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null && v.Range <= 1).OrderBy(v => v.Usages.Count).ToList();
        foreach (var variable in shortRange.Where(variable => variable.Type.ByteCount == 1 && !Conflict(variable.Intersections, ByteRegister.A) && CanAllocate(variable, ByteRegister.A))) {
            variable.Register = ByteRegister.A;
        }
        var rangeOrdered = variables.Where(v => v.Register == null && !v.Static && v.Parameter == null).OrderBy(v => v.Range)
            .ThenBy(v => v.Usages.Count);
        foreach (var variable in rangeOrdered.Where(v => v.Range <= 1)) {
            if (variable.Type.ByteCount != 1 || Conflict(variable.Intersections, ByteRegister.X) ||
                !CanAllocate(variable, ByteRegister.X))
                continue;
            variable.Register = ByteRegister.X;
        }

        var usageOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
        foreach (var variable in usageOrdered) {
            var variableType = variable.Type;
            var register = variableType.ByteCount switch
            {
                1 => AllocatableRegister(variable, ByteZeroPage.Registers, function),
                _ => variableType is PointerType ?
                    AllocatableRegister(variable, PointerZeroPage.Registers, function)
                    :
                    AllocatableRegister(variable, WordZeroPage.Registers, function)
            };
            if (register == null)
                continue;
            variable.Register = register;
        }

        foreach (var variable in variables.Where(v => v.Register == null && !v.Static)) {
            if (variable.Parameter?.Register == null)
                continue;
            var register = variable.Parameter.Register;
            if (register is ByteRegister byteRegister && !Conflict(variable.Intersections, byteRegister)) {
                variable.Register = byteRegister;
            }
            else if (register is ByteRegister) {
                register = AllocatableRegister(variable, ByteZeroPage.Registers, function);
                if (register != null) {
                    variable.Register = register;
                }
            }
            else if (register is WordRegister wordRegister) {
                if ((variable.Type is PointerType { ElementType: StructureType _ }) || Conflict(variable.Intersections, wordRegister)) {
                    var candidates = WordZeroPage.Registers;
                    register = AllocatableRegister(variable, candidates, function);
                    if (register != null) {
                        variable.Register = register;
                    }
                }
                else {
                    variable.Register = wordRegister;
                    break;
                }
            }
        }
    }


    //private static Register? AllocatableRegister<T>(Variable variable, IEnumerable<T> registers, Function function) where T : Register
    //{
    //    return registers.FirstOrDefault(register => !Conflict(variable.Intersections, register) && CanAllocate(variable, register));
    //}

    private static bool CanAllocate(Variable variable, Register register)
    {
        var function = variable.Block.Function;
        Debug.Assert(function != null);
        var first = variable.Usages.First().Key;
        var last = variable.Usages.Last().Key;
        for (var address = first; address <= last; ++address) {
            var instruction = function.Instructions[address];
            if (instruction.RegisterAdaptability(variable, register) == null) {
                return false;
            }
        }
        return true;
    }

    //private static bool Conflict<T>(IEnumerable<Variable> variables, T register) where T : Register
    //{
    //    return variables.Any(v =>
    //        v.Register != null && register.Conflicts(v.Register));
    //}

    public override Register? ParameterRegister(int index, ParameterizableType type)
    {
        //if (index >= WordZeroPage.Count) return null;
        //switch (type.ByteCount) {
        //    case 1:
        //        return ByteZeroPage.FromOffset(index * 2);
        //    case 2:
        //        var wordRegister = WordZeroPage.FromOffset(index);
        //        if (type is PointerType && wordRegister != null)
        //            return wordRegister.ToPointer();
        //        return wordRegister;
        //}
        return null;
    }

    public override Register? ReturnRegister(ParameterizableType type)
    {
        return type.ByteCount switch
        {
            1 => ByteRegister.Y,
            2 => (type is PointerType ? PairPointerRegister.Xy : PairWordRegister.Xy),
            _ => null
        };
    }

    protected override LoadInstruction CreateByteLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    {
        return new ByteLoadInstruction(function, destinationOperand, sourceOperand);
    }

    protected override LoadInstruction CreateWordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    {
        return new WordLoadInstruction(function, destinationOperand, sourceOperand);
    }

    protected override LoadInstruction CreatePointerLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand)
    {
        return new PointerLoadInstruction(function, destinationOperand, sourceOperand);
    }

    public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId,
        AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand)
    {
        if (destinationOperand.Type.ByteCount == 1) {
            return operatorId switch
            {
                '|' or '^' or '&' => new ByteBitInstruction(function, operatorId, destinationOperand, leftOperand,
                    rightOperand),
                '+' or '-' => new ByteAddOrSubtractInstruction(function, operatorId, destinationOperand,
                    leftOperand, rightOperand),
                Keyword.ShiftLeft or Keyword.ShiftRight => new ByteShiftInstruction(function, operatorId,
                    destinationOperand, leftOperand, rightOperand),
                _ => throw new NotImplementedException()
            };
        }
        return operatorId switch
        {
            '|' or '^' or '&' => new WordBinomialInstruction(function, operatorId, destinationOperand, leftOperand,
                rightOperand),
            '+' or '-' => new WordBinomialInstruction(function, operatorId, destinationOperand, leftOperand,
                rightOperand),
            Keyword.ShiftLeft or Keyword.ShiftRight => new WordShiftInstruction(function, operatorId,
                destinationOperand, leftOperand, rightOperand),
            _ => throw new NotImplementedException()
        };
    }

    public override Cate.MonomialInstruction CreateMonomialInstruction(Function function, int operatorId,
        AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        return new MonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
    }

    public override Cate.ResizeInstruction CreateResizeInstruction(Function function,
        AssignableOperand destinationOperand,
        IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
    {
        return new ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType);
    }

    public override Cate.CompareInstruction CreateCompareInstruction(Function function, int operatorId,
        Operand leftOperand, Operand rightOperand,
        Anchor anchor)
    {
        return new CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor);
    }

    public override Cate.JumpInstruction CreateJumpInstruction(Function function, Anchor anchor)
    {
        return new JumpInstruction(function, anchor);
    }

    public override Cate.SubroutineInstruction CreateSubroutineInstruction(Function function,
        Function targetFunction,
        AssignableOperand? destinationOperand,
        List<Operand> sourceOperands)
    {
        return new SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands);
    }

    public override Cate.ReturnInstruction CreateReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor)
    {
        return new ReturnInstruction(function, sourceOperand, anchor);
    }

    public override Cate.DecrementJumpInstruction CreateDecrementJumpInstruction(Function function,
        AssignableOperand operand, Anchor anchor)
    {
        return new DecrementJumpInstruction(function, operand, anchor);
    }

    public override ReadOnlySpan<char> EndOfFunction => "\trts";

    public override Cate.MultiplyInstruction CreateMultiplyInstruction(Function function,
        AssignableOperand destinationOperand, Operand leftOperand,
        int rightValue)
    {
        return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
    }

    public override IEnumerable<Register> IncludedRegisters(Register register)
    {
        return new List<Register>() { register };
    }

    public override bool IsAssignedRegisterPrior() => true;


    public override Operand HighByteOperand(Operand operand)
    {
        var newType = ToByteType(operand);
        switch (operand) {
            case IntegerOperand integerOperand:
                return new StringOperand(newType, "high " + integerOperand.IntegerValue);
            case PointerOperand pointerOperand:
                return new StringOperand(newType, "high(" + pointerOperand.MemoryAddress() + ")");
            case VariableOperand variableOperand:
                switch (variableOperand.Register) {
                    case WordRegister wordRegister:
                        Debug.Assert(wordRegister.High != null);
                        return new ByteRegisterOperand(newType, wordRegister.High);
                    case WordPointerRegister pointerRegister:
                        Debug.Assert(pointerRegister.High != null);
                        return new ByteRegisterOperand(newType, pointerRegister.High);
                    default:
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset + 1);
                }
            case IndirectOperand indirectOperand:
                return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset + 1);
            default:
                throw new NotImplementedException();
        }
    }

    public override Operand LowByteOperand(Operand operand)
    {
        var newType = ToByteType(operand);
        switch (operand) {
            case IntegerOperand integerOperand:
                return new StringOperand(newType, "low " + integerOperand.IntegerValue);
            case PointerOperand pointerOperand:
                return new StringOperand(newType, "low(" + pointerOperand.MemoryAddress() + ")");
            case VariableOperand variableOperand:
                switch (variableOperand.Register) {
                    case WordRegister wordRegister:
                        Debug.Assert(wordRegister.Low != null);
                        return new ByteRegisterOperand(newType, wordRegister.Low);
                    case WordPointerRegister pointerRegister:
                        Debug.Assert(pointerRegister.Low != null);
                        return new ByteRegisterOperand(newType, pointerRegister.Low);
                    default:
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset);
                }
            case IndirectOperand indirectOperand:
                return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset);
        }
        throw new NotImplementedException();
    }

    public override void CallExternal(Instruction instruction, string externalName)
    {
        instruction.WriteLine("\tjsr\t" + externalName);
        Instance.AddExternalName(externalName);
    }

    public virtual void LoadIndirect(Instruction instruction, ByteRegister byteRegister, PointerZeroPage zeroPage, int offset)
    {
        Debug.Assert(!Equals(byteRegister, ByteRegister.Y));
        ByteRegister.Y.LoadConstant(instruction, offset);
        instruction.WriteLine("\tld" + byteRegister.AsmName + "\t(" + zeroPage.Name + "),y");
    }

    public virtual void StoreIndirect(Instruction instruction, ByteRegister byteRegister, PointerZeroPage zeroPage, int offset)
    {
        Debug.Assert(!Equals(byteRegister, ByteRegister.Y));
        ByteRegister.Y.LoadConstant(instruction, offset);
        instruction.WriteLine("\tst" + byteRegister.AsmName + "\t(" + zeroPage.AsmName + "),y");
    }

    public virtual void OperateIndirect(Instruction instruction, string operation, PointerZeroPage zeroPage, int offset, int count)
    {
        ByteRegister.Y.LoadConstant(instruction, offset);
        for (var i = 0; i < count; ++i) {
            instruction.WriteLine("\t" + operation + "\t(" + zeroPage.Name + "),y");
        }
    }

    public virtual void ClearByte(Instruction instruction, string label)
    {
        using var reservation = ByteOperation.ReserveAnyRegister(instruction);
        var register = reservation.ByteRegister;
        register.LoadConstant(instruction, 0);
        register.StoreToMemory(instruction, label);
    }
}