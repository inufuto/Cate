using System.Diagnostics;

namespace Inu.Cate.Wdc65816;

internal class Compiler() : Cate.Compiler(new ByteOperation(), new WordOperation())
{
    public const string ZeroPageLabel = "@zp";
    public const string TemporaryWordLabel = "cate.Temp";
    public const string TemporaryCountLabel = "cate.Count";

    protected override void WriteAssembly(StreamWriter writer)
    {
        writer.WriteLine("extrn " + ZeroPageLabel);
        base.WriteAssembly(writer);
    }

    public override void SaveRegisters(StreamWriter writer, ISet<Register> registers, Function function)
    {
        //if (function.Name.Contains("FindLift")) {
        //    var aaa = 111;
        //}
        var distinctList = SavingRegisters(registers);
        using var evacuation = new RegisterEvacuation(writer, distinctList, function);
        foreach (var register in distinctList.OrderByDescending(RegisterOrder)) {
            evacuation.ChangeMode(register);
            register.Save(writer, null, null, 0);
        }
    }

    public override void SaveRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction, int tabCount)
    {
        if (instruction == null) {
            base.SaveRegisters(writer, variables, instruction, tabCount);
            return;
        }
        //if (instruction.ToString().Contains("CanMove_(pMonster,dx,dy)")) {
        //    var aaa = 111;
        //}
        var distinct = DistinctRegisters(variables);
        Debug.Assert(distinct.Keys.ToList() != null);
        using var evacuation = new RegisterEvacuation(writer, distinct.Keys.ToList(), instruction, tabCount);
        var savedFlags = new Dictionary<ModeFlag, int>(evacuation.Flags);
        foreach (var (register, list) in distinct.OrderByDescending(p => RegisterOrder(p.Key))) {
            var comment = "\t; " + string.Join(',', list.Select(v => v.Name).ToArray());
            evacuation.ChangeMode(register);
            register.Save(writer, comment, instruction, tabCount);
        }
        evacuation.RestoreFlags(savedFlags);
    }

    public override void RestoreRegisters(StreamWriter writer, ISet<Register> registers, int byteCount)
    {
        var distinctList = SavingRegisters(registers);
        distinctList.Reverse();
        Register? modeFlag = null;
        using (var evacuation = new RegisterEvacuation(writer, distinctList, byteCount)) {
            foreach (var register in distinctList.OrderBy(RegisterOrder)) {
                if (register is ModeFlag) {
                    modeFlag = register;
                }
                else {
                    evacuation.ChangeMode(register);
                    RestoreRegister(writer, register, byteCount);
                }
            }
        }
        if (modeFlag != null) {
            RestoreRegister(writer, modeFlag, byteCount);
        }
    }

    public override void RestoreRegisters(StreamWriter writer, IEnumerable<Variable> variables, Instruction? instruction, int tabCount)
    {
        if (instruction == null) {
            base.RestoreRegisters(writer, variables, instruction, tabCount);
            return;
        }
        var distinct = DistinctRegisters(variables);
        using var evacuation = new RegisterEvacuation(writer, distinct.Keys.ToList(), instruction, tabCount);
        var savedFlags = new Dictionary<ModeFlag, int>(evacuation.PrepareFlags(instruction));
        foreach (var (register, list) in distinct.OrderBy(p => RegisterOrder(p.Key))) {
            var comment = "\t; " + string.Join(',', list.Select(v => v.Name).ToArray());
            evacuation.ChangeMode(register);
            register.Restore(writer, comment, instruction, tabCount);
        }
        evacuation.RestoreFlags(savedFlags);
    }


    private static int RegisterOrder(Register register)
    {
        if (register is ModeFlag) {
            return int.MaxValue;
        }
        var order = register.ByteCount * 100 + register.Id;
        if (register is not (ByteZeroPage or WordZeroPage)) {
            order += 1000;
        }
        return order;
    }

    private static List<Register> SavingRegisters(ISet<Register> registers)
    {
        var list = new List<Register>(registers);
        list.Sort((register1, register2) => RegisterOrder(register2) - RegisterOrder(register1));
        var distinctList = new List<Register>();
        var modeFlagAdded = false;
        foreach (var register in list) {
            switch (register) {
                case ModeFlag when modeFlagAdded:
                    continue;
                case ModeFlag:
                    distinctList.Add(register);
                    modeFlagAdded = true;
                    break;
                default: {
                        if (!distinctList.Any(r => r.Conflicts(register))) {
                            distinctList.Add(register);
                        }
                        break;
                    }
            }
        }
        return distinctList;
    }

    public override void AllocateRegisters(List<Variable> variables, Function function)
    {
        foreach (var variable in variables) {
            if (variable is { Static: false, Parameter.Register: not null }) {
                variable.Register = variable.Parameter.Register;
            }
        }
        //if (function.Name.Contains("FallMovable")) {
        //    var aaa = 111;
        //}
        var rangeOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderBy(v => v.Range)
            .ThenBy(v => v.Usages.Count).ToList();
        List<Cate.ByteRegister> byteRegisters = new List<Cate.ByteRegister> { ByteRegister.A }.Union(ByteZeroPage.Registers).ToList();
        List<Cate.WordRegister> wordRegisters = new List<Cate.WordRegister> { WordRegister.A }.Union(WordZeroPage.Registers).ToList();
        var pointerRegisters = WordRegister.PointerRegisters;
        Allocate(rangeOrdered);
        var usageOrdered = variables.Where(v => v.Register == null && v is { Static: false, Parameter: null }).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
        Allocate(usageOrdered);

        return;

        void Allocate(List<Variable> list)
        {
            foreach (var variable in list) {
                var variableType = variable.Type;
                var register = variableType.ByteCount switch
                {
                    1 => AllocatableRegister(variable, byteRegisters, function),
                    _ => AllocatableRegister(variable, variableType switch
                    {
                        PointerType => pointerRegisters,
                        _ => wordRegisters
                    }, function)
                };
                if (register == null)
                    continue;
                variable.Register = register;
            }
        }
    }

    public override Register? ParameterRegister(int index, ParameterizableType type)
    {
        if (index < 4) {
            return type.ByteCount switch
            {
                1 => ByteZeroPage.Registers[index * 2],
                2 => WordZeroPage.Registers[index],
                _ => null
            };
        }
        return null;
    }

    public override Register? ReturnRegister(ParameterizableType type)
    {
        return type.ByteCount switch
        {
            1 => ByteZeroPage.Registers[0],
            2 => WordZeroPage.Registers[0],
            _ => null
        };
    }

    protected override LoadInstruction CreateByteLoadInstruction(Function function, AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        return new ByteLoadInstruction(function, destinationOperand, sourceOperand);
    }

    public override BinomialInstruction CreateBinomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand leftOperand, Operand rightOperand)
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
            '|' or '^' or '&' => new WordBitInstruction(function, operatorId, destinationOperand, leftOperand,
                rightOperand),
            '+' or '-' => new WordAddOrSubtractInstruction(function, operatorId, destinationOperand, leftOperand,
                rightOperand),
            Keyword.ShiftLeft or Keyword.ShiftRight => new WordShiftInstruction(function, operatorId,
                destinationOperand, leftOperand, rightOperand),
            _ => throw new NotImplementedException()
        };
    }

    public override MonomialInstruction CreateMonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand,
        Operand sourceOperand)
    {
        return new MonomialInstruction(function, operatorId, destinationOperand, sourceOperand);
    }

    public override ResizeInstruction CreateResizeInstruction(Function function, AssignableOperand destinationOperand,
        IntegerType destinationType, Operand sourceOperand, IntegerType sourceType)
    {
        return new ResizeInstruction(function, destinationOperand, destinationType, sourceOperand, sourceType);
    }

    public override CompareInstruction CreateCompareInstruction(Function function, int operatorId, Operand leftOperand,
        Operand rightOperand, Anchor anchor)
    {
        return new CompareInstruction(function, operatorId, leftOperand, rightOperand, anchor);
    }

    public override JumpInstruction CreateJumpInstruction(Function function, Anchor anchor)
    {
        return new JumpInstruction(function, anchor);
    }

    public override SubroutineInstruction CreateSubroutineInstruction(Function function, Function targetFunction,
        AssignableOperand? destinationOperand, List<Operand> sourceOperands)
    {
        return new SubroutineInstruction(function, targetFunction, destinationOperand, sourceOperands);
    }

    public override ReturnInstruction CreateReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor)
    {
        return new ReturnInstruction(function, sourceOperand, anchor);
    }

    public override DecrementJumpInstruction CreateDecrementJumpInstruction(Function function, AssignableOperand operand, Anchor anchor)
    {
        return new DecrementJumpInstruction(function, operand, anchor);
    }

    public override ReadOnlySpan<char> EndOfFunction => "\trts";

    public override MultiplyInstruction CreateMultiplyInstruction(Function function, AssignableOperand destinationOperand,
        Operand leftOperand, int rightValue)
    {
        return new MultiplyInstruction(function, destinationOperand, leftOperand, rightValue);
    }

    public override IEnumerable<Register> IncludedRegisters(Register register)
    {
        var list = new List<Register>() { register };
        if (register is WordAccumulator wordAccumulator) {
            list.Add(wordAccumulator.ByteRegister);
        }
        return list;
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
                    default:
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset);
                }
            case IndirectOperand indirectOperand:
                return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset);
        }
        throw new NotImplementedException();
    }

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
                    default:
                        return new VariableOperand(variableOperand.Variable, newType, variableOperand.Offset + 1);
                }
            case IndirectOperand indirectOperand:
                return new IndirectOperand(indirectOperand.Variable, newType, indirectOperand.Offset + 1);
        }
        throw new NotImplementedException();
    }

    public override void CallExternal(Instruction instruction, string externalName)
    {
        instruction.WriteLine("\tjsr\t" + externalName);
        Instance.AddExternalName(externalName);
    }

    public override void RemoveSavingRegister(ISet<Register> savedRegisters, Register returnRegister)
    {
        if (returnRegister is ByteZeroPage byteZeroPage) {
            var wordZeroPage = byteZeroPage.WordRegister as WordZeroPage;
            Debug.Assert(wordZeroPage != null);
            if (savedRegisters.Remove(wordZeroPage)) {
                Debug.Assert(wordZeroPage.High != null);
                savedRegisters.Add(wordZeroPage.High);
            }
        }
        else if (returnRegister is WordZeroPage wordZeroPage) {
            Debug.Assert(wordZeroPage is { Low: not null, High: not null });
            savedRegisters.Remove(wordZeroPage.Low);
            savedRegisters.Remove(wordZeroPage.High);
        }
        base.RemoveSavingRegister(savedRegisters, returnRegister);
    }

    protected override int? RegisterAdaptability(Variable variable, Register register)
    {
        if (variable.Intersections.Any()) {
            if (register.Equals(ByteRegister.A) || register.Equals(WordRegister.A)) {
                return null;
            }
        }
        return base.RegisterAdaptability(variable, register);
    }

    public static void MakeSize(Register register, Instruction instruction)
    {
        switch (register) {
            case ByteRegister byteRegister:
                byteRegister.MakeSize(instruction);
                return;
            case WordRegister wordRegister:
                wordRegister.MakeSize(instruction);
                return;
        }
        //throw new NotImplementedException();
    }

    public static void Save(Register register, StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tph" + register + comment);
    }

    public static void Restore(Register register, StreamWriter writer, string? comment, Instruction? instruction, int tabCount)
    {
        Instruction.WriteTabs(writer, tabCount);
        writer.WriteLine("\tpl" + register + comment);
    }

    public static void Save(Register register, Instruction instruction)
    {
        MakeSize(register, instruction);
        instruction.WriteLine("\tph" + register);
    }

    public static void Restore(Register register, Instruction instruction)
    {
        MakeSize(register, instruction);
        instruction.WriteLine("\tpl" + register);
    }

    public static void LoadConstant(Register register, Instruction instruction, string value)
    {
        MakeSize(register, instruction);
        instruction.WriteLine("\tld" + register + "\t#" + value);
        instruction.AddChanged(register);
        instruction.RemoveRegisterAssignment(register);
        //instruction.ResultFlags |= Instruction.Flag.Z;
    }

    public static void LoadFromMemory(Register register, Instruction instruction, string label)
    {
        MakeSize(register, instruction);
        instruction.WriteLine("\tld" + register + "\t" + label);
        instruction.AddChanged(register);
        instruction.RemoveRegisterAssignment(register);
        //instruction.ResultFlags |= Instruction.Flag.Z;
    }

    public static void StoreToMemory(Register register, Instruction instruction, string label)
    {
        MakeSize(register, instruction);
        instruction.WriteLine("\tst" + register + "\t" + label);
    }

    public static void LoadFromMemory(Register register, Instruction instruction, Variable variable, int offset)
    {
        MakeSize(register, instruction);
        instruction.WriteLine("\tld" + register + "\t" + variable.MemoryAddress(offset));
        instruction.RemoveRegisterAssignment(register);
        instruction.SetVariableRegister(variable, offset, register);
        instruction.AddChanged(register);
        //instruction.ResultFlags |= Instruction.Flag.Z;
    }

    public static void StoreToMemory(Register register, Instruction instruction, Variable variable, int offset)
    {
        MakeSize(register, instruction);
        instruction.WriteLine("\tst" + register + "\t" + variable.MemoryAddress(offset));
        instruction.SetVariableRegister(variable, offset, register);
    }

    public static void LoadIndirect(Register register, Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        if (offset < 0) {
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                LoadIndirect(register, instruction, pointerRegister, 0);
            });
            return;
        }
        switch (pointerRegister) {
            case WordIndexRegister indexRegister:
                if (!register.Conflicts(indexRegister)) {
                    MakeSize(register, instruction);
                    indexRegister.MakeSize(instruction);
                    instruction.WriteLine("\tld" + register + "\t>" + offset + "," + indexRegister);
                    goto exit;
                }
                break;
            case WordZeroPage zeroPage:
                if (offset == 0) {
                    MakeSize(register, instruction);
                    instruction.WriteLine("\tld" + register + "\t(" + zeroPage.Name + ")");
                    goto exit;
                }
                ViaWordIndex();
                goto exit;
            default:
                ViaWordIndex();
                goto exit;
        }

        switch (register) {
            case ByteRegister byteRegister: {
                    Debug.Assert(!byteRegister.Equals(ByteRegister.A));
                    using (Instance.ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                        ByteRegister.A.LoadIndirect(instruction, pointerRegister, offset);
                        byteRegister.CopyFrom(instruction, ByteRegister.A);
                    }
                    break;
                }
            case WordRegister wordRegister: {
                    Debug.Assert(!wordRegister.Equals(WordRegister.A));
                    using (Instance.WordOperation.ReserveRegister(instruction, WordRegister.A)) {
                        WordRegister.A.LoadIndirect(instruction, pointerRegister, offset);
                        wordRegister.CopyFrom(instruction, WordRegister.A);
                    }

                    break;
                }
            default:
                throw new NotImplementedException();
        }
    exit:
        instruction.AddChanged(register);
        instruction.RemoveRegisterAssignment(register);
        //instruction.ResultFlags |= Instruction.Flag.Z;
        return;

        void ViaWordIndex()
        {
            var candidates = ((List<Cate.WordRegister>)[WordRegister.X, WordRegister.Y]);
            using var reservation = Instance.WordOperation.ReserveAnyRegister(instruction, candidates);
            reservation.WordRegister.CopyFrom(instruction, pointerRegister);
            LoadIndirect(register, instruction, reservation.WordRegister, offset);
        }
    }

    public static void StoreIndirect(Register register, Instruction instruction, Cate.WordRegister pointerRegister, int offset)
    {
        if (offset < 0) {
            pointerRegister.TemporaryOffset(instruction, offset, () =>
            {
                StoreIndirect(register, instruction, pointerRegister, 0);
            });
            return;
        }
        switch (pointerRegister) {
            case WordIndexRegister indexRegister:
                if (!register.Conflicts(indexRegister)) {
                    MakeSize(register, instruction);
                    indexRegister.MakeSize(instruction);
                    instruction.WriteLine("\tst" + register + "\t>" + offset + "," + indexRegister);
                    goto exit;
                }
                break;
            case WordZeroPage zeroPage:
                if (offset == 0) {
                    MakeSize(register, instruction);
                    instruction.WriteLine("\tst" + register + "\t(" + zeroPage.Name + ")");
                    goto exit;
                }
                ViaWordIndex();
                goto exit;
            default:
                ViaWordIndex();
                goto exit;
        }

        switch (register) {
            case ByteRegister byteRegister: {
                    Debug.Assert(!byteRegister.Equals(ByteRegister.A));
                    using (Instance.ByteOperation.ReserveRegister(instruction, ByteRegister.A)) {
                        ByteRegister.A.CopyFrom(instruction, byteRegister);
                        ByteRegister.A.StoreIndirect(instruction, pointerRegister, offset);
                    }
                    break;
                }
            case WordRegister wordRegister: {
                    Debug.Assert(!wordRegister.Equals(WordRegister.A));
                    using (Instance.WordOperation.ReserveRegister(instruction, WordRegister.A)) {
                        WordRegister.A.CopyFrom(instruction, wordRegister);
                        WordRegister.A.StoreIndirect(instruction, pointerRegister, offset);
                    }
                    break;
                }
            default:
                throw new NotImplementedException();
        }
    exit:;
        return;

        void ViaWordIndex()
        {
            var candidates = ((List<Cate.WordRegister>)[WordRegister.X, WordRegister.Y]);
            using var reservation = Instance.WordOperation.ReserveAnyRegister(instruction, candidates);
            reservation.WordRegister.CopyFrom(instruction, pointerRegister);
            StoreIndirect(register, instruction, reservation.WordRegister, offset);
        }
    }

    public static void CopyFrom(Register register, Instruction instruction, Register sourceRegister)
    {
        if (sourceRegister.Equals(register)) {
            return;
        }
        switch (sourceRegister) {
            case ByteRegister:
            case WordRegister:
                MakeSize(register, instruction);
                MakeSize(sourceRegister, instruction);
                instruction.WriteLine("\tt" + sourceRegister + register);
                instruction.AddChanged(register);
                instruction.RemoveRegisterAssignment(register);
                //instruction.ResultFlags |= Instruction.Flag.Z;
                return;
            case ByteZeroPage:
            case WordZeroPage:
                LoadFromMemory(register, instruction, sourceRegister.Name);
                return;
        }
        throw new NotImplementedException();
    }
}
