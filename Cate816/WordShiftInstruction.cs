using System.Diagnostics;

namespace Inu.Cate.Wdc65816;

internal class WordShiftInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand leftOperand,
    Operand rightOperand)
    : Cate.WordShiftInstruction(function, operatorId, destinationOperand, leftOperand, rightOperand)
{
    protected override void ShiftConstant(int count)
    {
        if (((IntegerType)LeftOperand.Type).Signed) {
            ShiftVariable(RightOperand);
            return;
        }
        var operation = OperatorId switch
        {
            Keyword.ShiftLeft => "asl",
            Keyword.ShiftRight => "lsr",
            _ => throw new NotImplementedException()
        };
        if (count <= 2 && DestinationOperand.SameStorage(LeftOperand) && DestinationOperand is VariableOperand variableOperand) {
            ModeFlag.Memory.ResetBit(this);
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + operation + "\t" + variableOperand.MemoryAddress());
            }
            RemoveVariableRegister(variableOperand);
            return;
        }
        if (Equals(DestinationOperand.Register, WordRegister.A)) {
            ViaA();
        }
        else {
            using (WordOperation.ReserveRegister(this, WordRegister.A)) {
                ViaA();
            }
        }
        return;

        void ViaA()
        {
            WordRegister.A.Load(this, LeftOperand);
            WordRegister.A.MakeSize(this);
            for (var i = 0; i < count; ++i) {
                WriteLine("\t" + operation + "\ta");
            }
            AddChanged(WordRegister.A);
            RemoveRegisterAssignment(WordRegister.A);
            WordRegister.A.Store(this, DestinationOperand);
        }
    }

    protected override void ShiftVariable(Operand counterOperand)
    {
        var functionName = OperatorId switch
        {
            Keyword.ShiftLeft => "cate.ShiftLeftWord",
            Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                ? "cate.ShiftRightSignedWord"
                : "cate.ShiftRightWord",
            _ => throw new NotImplementedException()
        };
        using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
            StoreCount();
        }
        if (Equals(DestinationOperand.Register, WordRegister.A)) {
            Call();
            return;
        }
        if (Equals(RightOperand.Register, ByteRegister.A)) {
            StoreCount();
        }
        else {
            using (WordOperation.ReserveRegister(this, WordRegister.A)) {
                Call();
            }
        }
        return;

        void Call()
        {
            WordRegister.A.Load(this, LeftOperand);
            Compiler.CallExternal(this, functionName);
            RemoveRegisterAssignment(WordRegister.A);
            AddChanged(WordRegister.A);
            WordRegister.A.Store(this, DestinationOperand);
        }

        void StoreCount()
        {
            ByteRegister.A.Load(this, RightOperand);
            ByteRegister.A.MakeSize(this);
            WriteLine("\tsta\t<" + Wdc65816.Compiler.TemporaryCountLabel);
        }
    }
}