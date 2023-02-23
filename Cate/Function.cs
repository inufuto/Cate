using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Inu.Language;

namespace Inu.Cate
{
    public class Function : NamedValue
    {
        public class Parameter
        {
            public readonly ParameterizableType Type;
            public readonly int? Id;
            public readonly Register? Register;

            public Parameter(ParameterizableType type, int? id, Register? register)
            {
                Type = type;
                Id = id;
                Register = register;
            }

            public Variable? Variable { get; set; }
        }

        public readonly Visibility Visibility;
        public readonly List<Parameter> Parameters = new List<Parameter>();
        private FunctionBlock? functionBlock = null;
        public readonly List<Instruction> Instructions = new List<Instruction>();
        private int lastTemporaryVariableIndex = 0;
        private readonly Dictionary<int, int> localVariableIds = new Dictionary<int, int>();
        public readonly List<Anchor> Anchors = new List<Anchor>();
        public readonly Anchor ExitAnchor;
        public readonly Dictionary<int, NamedLabel> NamedLabels = new Dictionary<int, NamedLabel>();

        public Function(GlobalBlock block, int id, Visibility visibility, Type type) : base(block, id, type)
        {
            Visibility = visibility;
            functionBlock = new FunctionBlock(Block, this);
            ExitAnchor = CreateAnchor();
        }

        public FunctionBlock CreateBlock()
        {
            functionBlock = new FunctionBlock(Block, this);
            foreach (var parameter in Parameters) {
                Debug.Assert(parameter.Id != null);
                var variable = functionBlock.AddVariable(parameter.Id.Value, parameter.Type, Visibility.Private, false, null);
                if (parameter.Register != null) {
                    var temporaryVariable = CreateTemporaryVariable(variable.Type);
                    temporaryVariable.Register = parameter.Register;
                    var instruction = Compiler.Instance.CreateLoadInstruction(this, variable.ToAssignableOperand(), temporaryVariable.ToOperand());
                    Instructions.Add(instruction);
                }
                else {
                    variable.Static = true;
                }
                variable.Parameter = parameter;
                parameter.Variable = variable;
            }
            return functionBlock;
        }

        public int NextAddress => Instructions.Count;
        public List<Variable> AllVariables => functionBlock != null ? functionBlock.AllVariables : new List<Variable>();

        public bool AddParameter(ParameterizableType type, int? id)
        {
            if (Parameters.Find(p => p.Id != null && p.Id == id) != null)
                return false;
            var register = Compiler.Instance.ParameterRegister(Parameters.Count, type);
            Parameters.Add(new Parameter(type, id, register));
            return true;
        }

        public bool IsSameSignature(Function function)
        {
            if (!Type.Equals(function.Type) || Parameters.Count != function.Parameters.Count) { return false; }
            return !Parameters.Where((p, i) => !p.Type.Equals(function.Parameters[i].Type)).Any();
        }

        public Variable CreateTemporaryVariable(Type type)
        {
            var name = Compiler.Instance.LabelPrefix + (++lastTemporaryVariableIndex).ToString();
            var id = Identifier.Add(name);
            Debug.Assert(functionBlock != null);
            return functionBlock.AddVariable(id, type, Visibility.Private, false, null);
        }


        public void WriteAssembly(StreamWriter writer, ref int codeOffset, ref int dataOffset)
        {
            ExitAnchor.Address = NextAddress;

            if (Visibility == Visibility.External) {
                writer.WriteLine("\textrn\t" + Label);
                foreach (var parameter in Parameters.Where(parameter => parameter.Register == null)) {
                    writer.WriteLine("\textrn\t" + ParameterLabel(parameter));
                }
                return;
            }

            writer.WriteLine(";");
            writer.WriteLine(";\tfunction " + Name);
            writer.WriteLine(";");

            if (functionBlock == null) return;

            FillFlow();
            OptimizeVariables();

            WriteLocalVariables(writer, ref dataOffset);

            Compiler.Instance.MakeAlignment(writer, "cseg", ref codeOffset);
            Compiler.Instance.MakeAlignment(writer, "dseg", ref dataOffset);
            functionBlock.WriteAssembly(writer, ref codeOffset, ref dataOffset);
            writer.WriteLine("\tcseg");
            writer.WriteLine(Label + ":");
            if (Visibility == Visibility.Public) {
                writer.WriteLine("\tpublic\t" + Label);
            }

            var compiler = Compiler.Instance;
            compiler.WriteBeginningOfFunction(writer, this);

            ISet<Register> savedRegisters = new HashSet<Register>();
            foreach (var instruction in Instructions.Where(i => !i.IsEmpty())) {
                foreach (var changedRegister in instruction.ChangedRegisters) {
                    var savingRegisters = Compiler.Instance.SavingRegisters(changedRegister).Where(r => !savedRegisters.Contains(r));
                    foreach (var savingRegister in savingRegisters) {
                        var saved = false;
                        foreach (var variable in instruction.SavingVariables) {
                            if (variable.Register != null && Compiler.Instance.SavingRegisters(variable.Register).Contains(savingRegister)) {
                                saved = true;
                            }
                        }
                        if (!saved) {
                            savedRegisters.Add(savingRegister);
                        }
                    }
                }
            }

            var returnRegisterId = compiler.ReturnRegister(Type.ByteCount);
            if (returnRegisterId != null) {
                savedRegisters.Remove(returnRegisterId);
                compiler.RemoveSavingRegister(savedRegisters, Type.ByteCount);
                foreach (var includedIds in compiler.IncludedRegisterIds(returnRegisterId)) {
                    savedRegisters.Remove(includedIds);
                }
            }
            compiler.SaveRegisters(writer, savedRegisters);

            Instruction? prevInstruction = null;
            var tabCount = 0;
            var lastAddress = 0;

            bool SavingChanged(Instruction instruction1, Instruction instruction2)
            {
                return instruction1.IsJump() || instruction2.IsJump() || instruction1.IsResultChanged() || instruction2.IsResultChanged() ||
                       instruction1.SavingVariables.Any(instruction2.IsSourceOperand) ||
                       !instruction1.SavingVariables.SetEquals(instruction2.SavingVariables) || Anchors.Any(a => a.Address == instruction2.Address);
            }

            for (var address = 0; address < Instructions.Count; ++address) {
                var instruction = Instructions[address];
                var nextInstruction = address + 1 < Instructions.Count ? Instructions[address + 1] : null;
                lastAddress = address;
                foreach (var anchor in Anchors.Where(anchor => anchor.Address == address)) {
                    writer.WriteLine(anchor.Label + ":");
                }
                Instruction.WriteTabs(writer, tabCount);
                writer.WriteLine("\t;\t" + instruction + "\t[" + address + "]");

                if (prevInstruction == null || SavingChanged(prevInstruction, instruction)) {
                    if (instruction.SavingVariables.Count > 0) {
                        compiler.SaveRegisters(writer, instruction.SavingVariables, instruction.IsJump(), tabCount++);
                    }
                }
                instruction.WriteAssembly(writer, tabCount);
                if (nextInstruction == null || SavingChanged(instruction, nextInstruction)) {
                    if (instruction.SavingVariables.Count > 0) {
                        compiler.RestoreRegisters(writer, instruction.SavingVariables, instruction.IsJump(), --tabCount);
                    }
                }
                instruction.WriteAssemblyAfterRestoring(writer, tabCount);

                prevInstruction = instruction;
            }
            foreach (var anchor in Anchors.Where(anchor => anchor.Address > lastAddress)) {
                writer.WriteLine(anchor.Label + ":");
            }

            //writer.WriteLine(ExitLabel + ":");
            compiler.RestoreRegisters(writer, savedRegisters, Type.ByteCount);
            compiler.WriteEndOfFunction(writer, this);
        }

        private void WriteLocalVariables(StreamWriter writer, ref int offset)
        {
            if (localVariableIds.Count > 0) {
                writer.WriteLine("\tdseg");
            }
            //var offset = 0;
            foreach (var (id, size) in localVariableIds) {
                if (size >= Compiler.Instance.Alignment) {
                    Compiler.Instance.MakeAlignment(writer, ref offset);
                }
                writer.WriteLine(Label + Variable.LocalVariableName(id) + ":\tdefs " + size);
                offset += size;
            }
        }

        public void WriteParameterLabel(StreamWriter writer, int index, Parameter? parameter)
        {
            //if (index >= Parameters.Count) return;
            //var parameter = Parameters[index];
            if (parameter == null)
                return;
            if (parameter.Register == null) {
                var parameterLabel = ParameterLabel(parameter);
                writer.WriteLine(parameterLabel + ":");
                if (Visibility == Visibility.Public) {
                    writer.WriteLine("\tpublic\t" + parameterLabel);
                }
            }
            //   && parameter.Variable.Static
            //parameter.Variable?.WriteAssembly(writer);
        }


        public string ParameterLabel(Parameter parameter)
        {
            return Label + Compiler.Instance.ParameterPrefix + "Param" + Parameters.IndexOf(parameter);
        }
        private void FillFlow()
        {
            foreach (var anchor in Anchors) {
                Debug.Assert(anchor.Address != null);
                if (anchor.Address.Value >= Instructions.Count)
                    continue;
                var instruction = Instructions[anchor.Address.Value];
                foreach (var originAddress in anchor.OriginAddresses) {
                    instruction.PreviousInstructions.Add(Instructions[originAddress]);
                }
            }

            Debug.Assert(functionBlock != null);
            foreach (var variable in functionBlock.AllVariables.Where(v => !v.IsTemporary())) {
                if (variable.Usages.IsEmpty)
                    continue;
                int? lastJump = null;
                //var first = variable.Usages.First().Key;
                var last = variable.Usages.Last().Key;
                for (var address = 0; address <= last; ++address) {
                    var instruction = Instructions[address];
                    foreach (var previousInstruction in instruction.PreviousInstructions) {
                        if (previousInstruction.Address > last && previousInstruction.IsJump()) {
                            lastJump = previousInstruction.Address;
                        }
                    }
                }
                if (lastJump != null) {
                    variable.AddUsage(lastJump.Value, Variable.Usage.Read);
                }
            }
        }

        private void OptimizeVariables()
        {
            static List<Variable> TargetVariables(IEnumerable<Variable> sourceVariables)
            {
                return sourceVariables.Where(v => v.Type is ParameterizableType && v.Usages.Any() && !v.Static).ToList();
            }

            Debug.Assert(functionBlock != null);
            var variables = TargetVariables(functionBlock.AllVariables);
            foreach (var variable1 in variables) {
                foreach (var variable2 in variables.Where(variable2 => variable2 != variable1)) {
                    variable1.MakeIntersection(variable2);
                }
            }

            foreach (var variable in variables) {
                variable.TestAnchors(Anchors);
            }

            Compiler.Instance.AllocateRegisters(variables, this);
            var sortedVariables = TargetVariables(variables).OrderByDescending(v => v.Usages.Count).ThenBy(v => v.Range).ToList();
            AllocateLocalVariables(sortedVariables);

            foreach (var instruction in Instructions) {
                instruction.AddSourceRegisters();
                instruction.BuildResultVariables();
                if (instruction.ToString().Contains("pBullet[6] = 70")) {
                    var aaa = 111;
                }
                instruction.BuildAssembly();
            }

            foreach (var variable in variables.Where(v => v.Register != null).OrderBy(v => v.Range)) {
                variable.FillSavings(this);
            }
        }

        private void AllocateLocalVariables(IReadOnlyCollection<Variable> variables)
        {
            var changed = true;
            while (changed) {
                changed = false;
                foreach (var variable in variables) {
                    if (variable.Register != null || variable.LocalVariableId != null || variable.Static)
                        continue;
                    var localVariableId = AllocateLocalVariable(variable);
                    variable.LocalVariableId = localVariableId;
                    changed = true;
                    break;
                }
            }
        }

        private int AllocateLocalVariable(Variable variable)
        {
            int localVariableId;
            foreach (var pair in from pair in localVariableIds let byteCount = pair.Value where byteCount == variable.Type.ByteCount select pair) {
                localVariableId = pair.Key;
                var conflict = Conflict(variable.Intersections, localVariableId);
                if (!conflict) {
                    return localVariableId;
                }
            }
            localVariableId = ++lastTemporaryVariableIndex;
            localVariableIds[localVariableId] = variable.Type.ByteCount;
            return localVariableId;
        }

        private static bool Conflict(IEnumerable<Variable> variables, int localVariableId)
        {
            return variables.Any(v => v.LocalVariableId == localVariableId);
        }

        public Anchor CreateAnchor()
        {
            var anchor = new Anchor(this);
            Anchors.Add(anchor);
            return anchor;
        }
        public NamedLabel AddNamedLabel(Identifier identifier)
        {
            if (NamedLabels.TryGetValue(identifier.Id, out var namedLabel)) {
                if (namedLabel.Anchor != null)
                    throw new MultipleIdentifierError(identifier);
                namedLabel.Anchor = CreateAnchor();
                return namedLabel;
            }
            namedLabel = new NamedLabel(identifier);
            NamedLabels[identifier.Id] = namedLabel;
            namedLabel.Anchor = CreateAnchor();
            return namedLabel;
        }

        public NamedLabel FindNamedLabel(Identifier identifier)
        {
            if (NamedLabels.TryGetValue(identifier.Id, out var namedLabel))
                return namedLabel;
            namedLabel = new NamedLabel(identifier);
            NamedLabels[identifier.Id] = namedLabel;
            return namedLabel;
        }

        public bool IsRegisterAssigned(Register register)
        {
            return AllVariables.Any(v => Equals(v.Register, register));
        }
    }
}