using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Inu.Cate
{
    public abstract class SubroutineInstruction : Instruction
    {
        protected class ParameterAssignment
        {
            public readonly Function.Parameter Parameter;
            public readonly Operand Operand;
            private bool began;
            public Register? Register { get; private set; }
            public bool Done { get; private set; }

            public ParameterAssignment(Function.Parameter parameter, Operand operand)
            {
                Parameter = parameter;
                Operand = operand;
            }

            public override string ToString()
            {
                return "{" + Parameter.Register + "," + Operand + "," + Register + "," + Done + "}";
            }

            public void SetDone(SubroutineInstruction instruction, Register? register)
            {
                Register = register;
                Done = true;
                instruction.RemoveSourceRegister(Operand);
                if (register != null) {
                    instruction.BeginRegister(register);
                    began = true;
                }
            }

            public void Exchange(SubroutineInstruction instruction, ParameterAssignment other)
            {
                Debug.Assert(other.Parameter.Register != null);
                if (Parameter.Register is ByteRegister byteRegister) {
                    byteRegister.Exchange(instruction, (ByteRegister)other.Parameter.Register);
                }
                if (Parameter.Register is WordRegister wordRegister) {
                    wordRegister.CopyFrom(instruction, (WordRegister)other.Parameter.Register);
                }
                SetDone(instruction, other.Operand.Register);
                other.SetDone(instruction, Operand.Register);
            }

            public void Close(SubroutineInstruction instruction)
            {
                Debug.Assert(Done);
                if (Register == null)
                    return;
                if (!Equals(Register, Parameter.Register)) {
                    Debug.Assert(Parameter.Register != null);
                    if (Parameter.Register is ByteRegister byteRegister) {
                        var br = (ByteRegister)Register;
                        instruction.CopyByte(instruction, byteRegister, br);
                    }
                    if (Parameter.Register is WordRegister wordRegister) {
                        wordRegister.CopyFrom(instruction, (WordRegister)Register);
                    }
                }
                if (began) {
                    instruction.EndRegister(Register);
                }
                Done = false;
            }
        }

        protected virtual void CopyByte(Instruction instruction, ByteRegister destination, ByteRegister source)
        {
            destination.CopyFrom(instruction, source);
        }

        public readonly Function TargetFunction;
        public readonly AssignableOperand? DestinationOperand;
        public readonly List<Operand> SourceOperands;

        protected readonly List<ParameterAssignment> ParameterAssignments = new List<ParameterAssignment>();

        public override bool IsSourceOperand(Variable variable)
        {
            foreach (var sourceOperand in SourceOperands) {
                switch (sourceOperand) {
                    case VariableOperand variableOperand when variableOperand.Variable == variable:
                    case IndirectOperand indirectOperand when indirectOperand.Variable == variable:
                        return true;
                }
            }
            return base.IsSourceOperand(variable);
        }

        public override bool IsResultChanged()
        {
            if (ResultOperand == null) {
                return false;
            }
            var returnRegister = Compiler.Instance.ReturnRegister(TargetFunction.Type.ByteCount);
            return TargetFunction.Parameters.Any(parameter => parameter.Register != null && parameter.Register.Conflicts(returnRegister));
        }

        protected SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand,
            List<Operand> sourceOperands) : base(function)
        {
            TargetFunction = targetFunction;
            DestinationOperand = destinationOperand;
            SourceOperands = sourceOperands;

            DestinationOperand?.AddUsage(function.NextAddress, Variable.Usage.Write);
            for (var i = 0; i < SourceOperands.Count; i++) {
                var sourceOperand = SourceOperands[i];
                sourceOperand.AddUsage(function.NextAddress, Variable.Usage.Read);
                ParameterAssignments.Add(new ParameterAssignment(targetFunction.Parameters[i], sourceOperands[i]));
            }
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            if (DestinationOperand != null) {
                s.Append(DestinationOperand.ToString());
                s.Append(" = ");
            }
            s.Append(TargetFunction.Label);
            s.Append('(');
            var index = 0;
            foreach (var sourceOperand in SourceOperands) {
                s.Append(sourceOperand.ToString());
                if (++index < TargetFunction.Parameters.Count) {
                    s.Append(',');
                }
            }

            s.Append(')');
            return s.ToString();
        }

        public override void BuildAssembly()
        {
            FillParameters();
            ResultFlags = 0;
            Call();

            if (DestinationOperand == null)
                return;
            var returnRegister = Compiler.Instance.ReturnRegister(TargetFunction.Type.ByteCount);
            Debug.Assert(returnRegister != null);
            RemoveVariableRegister(returnRegister);
            //if (!Equals(returnRegister, DestinationOperand.Register)) {
            BeginRegister(returnRegister);
            EndRegister(returnRegister);
            //}
            ChangedRegisters.Add(returnRegister);
            RemoveVariableRegister(returnRegister);
            switch (returnRegister) {
                case ByteRegister byteRegister:
                    byteRegister.Store(this, DestinationOperand);
                    break;
                case WordRegister wordRegister:
                    wordRegister.Store(this, DestinationOperand);
                    break;
            }
        }


        public override void AddSourceRegisters()
        {
            foreach (var sourceOperand in SourceOperands) {
                AddSourceRegister(sourceOperand);
            }
        }

        //public override void RemoveDestinationRegister()
        //{
        //    if (DestinationOperand != null) {
        //        RemoveChangedRegisters(DestinationOperand);
        //    }
        //}

        protected Dictionary<Function.Parameter, Operand> OperandPairs()
        {
            Dictionary<Function.Parameter, Operand> pairs = new Dictionary<Function.Parameter, Operand>();
            for (var index = SourceOperands.Count - 1; index >= 0; --index) {
                pairs[TargetFunction.Parameters[index]] = SourceOperands[index];
            }
            return pairs;
        }

        public override Operand? ResultOperand => DestinationOperand;

        protected abstract void Call();

        protected void FillParameters()
        {
            StoreParameters();

            var changed = true;
            while (changed) {
                changed = false;
                for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                    var assignment = ParameterAssignments[i];
                    if (assignment.Done)
                        continue;
                    var parameter = assignment.Parameter;
                    var operand = assignment.Operand;
                    Debug.Assert(parameter.Register != null);
                    if (operand is VariableOperand variableOperand) {
                        var register = variableOperand.Variable.Register
                                       ?? GetVariableRegister(variableOperand.Variable, variableOperand.Offset);
                        if (Equals(register, parameter.Register)) {
                            assignment.SetDone(this, SaveAccumulator(register, assignment));
                            changed = true;
                        }
                    }
                }
                if (changed)
                    continue;
                for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                    var assignment = ParameterAssignments[i];
                    if (assignment.Done)
                        continue;
                    var parameter = assignment.Parameter;
                    var operand = assignment.Operand;
                    Debug.Assert(parameter.Register != null);
                    if (!IsRegisterInUse(parameter.Register)) {
                        Load(parameter.Register, operand);
                        assignment.SetDone(this, SaveAccumulator(parameter.Register, assignment));
                        changed = true;
                    }
                }
                if (changed)
                    continue;
                for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                    var assignment = ParameterAssignments[i];
                    if (assignment.Done)
                        continue;
                    var parameter = assignment.Parameter;
                    Debug.Assert(parameter.Register != null);
                    var operand = assignment.Operand;
                    {
                        Register? register;
                        if (parameter.Type.ByteCount == 1) {
                            register = ByteOperation.Registers.Find(r => !IsRegisterInUse(r));
                        }
                        else {
                            register = WordOperation.Registers.Find(r => !IsRegisterInUse(r));
                        }
                        if (register == null)
                            continue;
                        Load(register, operand);
                        assignment.SetDone(this, SaveAccumulator(register, assignment));
                        changed = true;
                        break;
                    }
                }
                if (changed)
                    continue;
                for (var i = ParameterAssignments.Count - 1; i >= 0; i--) {
                    var assignment = ParameterAssignments[i];
                    if (assignment.Done)
                        continue;
                    var parameter = assignment.Parameter;
                    Debug.Assert(parameter.Register != null);
                    var operand = assignment.Operand;
                    var other = ParameterAssignments.Find(a => !a.Done && Equals(a.Operand.Register, parameter.Register));
                    if (other != null && Equals(other.Parameter.Register, operand.Register)) {
                        assignment.Exchange(this, other);
                    }
                }
            }

            foreach (var assignment in ParameterAssignments) {
                assignment.Close(this);
            }
        }

        protected virtual Register? SaveAccumulator(Register register, ParameterAssignment assignment)
        {
            return register;
        }

        private void Load(Register register, Operand operand)
        {
            switch (register) {
                case ByteRegister byteRegister:
                    byteRegister.Load(this, operand);
                    break;
                case WordRegister wordRegister:
                    wordRegister.Load(this, operand);
                    break;
            }
        }


        protected abstract void StoreParameters();

        protected virtual List<ByteRegister> Candidates(Operand operand)
        {
            return ByteOperation.Registers;
        }

        protected void StoreParametersDirect()
        {
            ParameterAssignments.Sort((a1, a2) =>
            {
                var id1 = a1.Operand.Register?.Id ?? int.MaxValue;
                var id2 = a2.Operand.Register?.Id ?? int.MaxValue;
                return id1 - id2;
            });
            foreach (var assignment in ParameterAssignments) {
                if (assignment.Done || assignment.Parameter.Register != null)
                    continue;
                var parameter = assignment.Parameter;
                var label = TargetFunction.ParameterLabel(parameter);
                var operand = assignment.Operand;
                if (parameter.Type.ByteCount == 1) {
                    if (operand is IntegerOperand { IntegerValue: 0 }) {
                        ByteOperation.ClearByte(this, label);
                    }
                    else {
                        var candidates = Candidates(operand);
                        ByteOperation.UsingAnyRegister(this, candidates, null, operand, register =>
                        {
                            register.Load(this, operand);
                            register.StoreToMemory(this, label);
                        });
                    }
                }
                else {
                    StoreWord(operand, label);
                }
                assignment.SetDone(this, null);
            }
        }

        protected virtual void StoreWord(Operand operand, string label)
        {
            WordOperation.UsingAnyRegister(this, register =>
            {
                register.Load(this, operand);
                register.StoreToMemory(this, label);
            });
        }


        protected void StoreParametersViaPointer()
        {
            WordOperation.UsingAnyRegister(this, WordOperation.PointerRegisters(0), pointerRegister =>
            {
                var index = 0;
                var count = ParameterAssignments.Count(a => a.Parameter.Register == null);
                foreach (var assignment in ParameterAssignments.Where(assignment => assignment.Parameter.Register == null)) {
                    Debug.Assert(!assignment.Done);
                    var parameter = assignment.Parameter;
                    var last = index >= count - 1;

                    void StoreByte(ByteRegister register)
                    {
                        register.StoreIndirect(this, pointerRegister, 0);
                        if (!last) {
                            pointerRegister.Add(this, 1);
                        }
                    }

                    void StoreWord(WordRegister register)
                    {
                        Debug.Assert(register.Low != null);
                        Debug.Assert(register.High != null);
                        register.Low.StoreIndirect(this, pointerRegister, 0);
                        pointerRegister.Add(this, 1);
                        register.High.StoreIndirect(this, pointerRegister, 0);
                        if (!last) {
                            pointerRegister.Add(this, 1);
                        }
                    }

                    var operand = assignment.Operand;
                    if (index == 0) {
                        //pointerRegister.LoadFromMemory(this, TargetFunction.ParameterLabel(parameter));
                        pointerRegister.LoadConstant(this, TargetFunction.ParameterLabel(parameter));
                    }

                    if (parameter.Type.ByteCount == 1) {
                        switch (operand) {
                            case IntegerOperand integerOperand:
                                ByteOperation.StoreConstantIndirect(this, pointerRegister, 0,
                                    integerOperand.IntegerValue);
                                if (!last) {
                                    pointerRegister.Add(this, 1);
                                }
                                break;
                            case VariableOperand variableOperand when variableOperand.Variable.Register is ByteRegister register: {
                                    Debug.Assert(variableOperand.Offset == 0);
                                    StoreByte(register);
                                    break;
                                }
                            case VariableOperand _:
                                ByteOperation.UsingAnyRegister(this, ByteOperation.Accumulators, register =>
                                {
                                    register.Load(this, operand);
                                    StoreByte(register);
                                });
                                break;
                            default:
                                ByteOperation.UsingAnyRegister(this, register =>
                                {
                                    register.Load(this, operand);
                                    StoreByte(register);
                                });
                                break;
                        }
                    }
                    else {
                        if (operand.Register is WordRegister wordRegister && wordRegister.IsPair()) {
                            wordRegister.Load(this, operand);
                            StoreWord(wordRegister);
                        }
                        else {
                            WordOperation.UsingAnyRegister(this, WordOperation.PairRegisters, register =>
                            {
                                register.Load(this, operand);
                                StoreWord(register);
                            });
                        }
                    }
                    assignment.SetDone(this, null);
                    ++index;
                }
            });
        }
    }
}