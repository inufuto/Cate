using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Inu.Cate
{
    public class Variable : NamedValue
    {
        [Flags]
        public enum Usage
        {
            Read = 1, Write = 2
        }

        public readonly Visibility Visibility;
        private readonly Constant? value;
        private readonly SortedDictionary<int, Usage> usages = new SortedDictionary<int, Usage>();
        private bool @static;
        public readonly List<Variable> Intersections = new List<Variable>();
        public Function.Parameter? Parameter;
        private Register? register;
        private int? localVariableId;

        public Variable(Block block, int id, Type type, Visibility visibility, bool @static, Constant? value)
            : base(block, id, type)
        {
            Visibility = visibility;
            this.@static = @static;
            this.value = value;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool Static
        {
            get => @static;
            set
            {
                Debug.Assert(register == null);
                Debug.Assert(localVariableId == null);
                @static = value;
                Usages.Clear();
            }
        }

        public Register? Register
        {
            get => register;
            set
            {
                Debug.Assert(localVariableId == null);
                Debug.Assert(!@static);
                register = value;
            }
        }

        public int? LocalVariableId
        {
            get => localVariableId;
            set
            {
                Debug.Assert(register == null);
                Debug.Assert(!@static);
                localVariableId = value;
            }
        }
        //public bool IsWriteOnly() => Usages.All(u => u.Value);

        public override string Label
        {
            get
            {
                if (localVariableId != null) {
                    Debug.Assert(Block.Function != null);
                    return Block.Function.Label + LocalVariableName(localVariableId.Value);
                }

                return base.Label;
            }
        }

        public static string LocalVariableName(int id)
        {
            return Compiler.Instance.LabelPrefix + "Local" + id.ToString();
        }

        public int Range
        {
            get
            {
                if (Usages.Count > 0) {
                    return Usages.Last().Key - Usages.First().Key;
                }

                return int.MinValue;
            }
        }

        public ImmutableSortedDictionary<int, Usage> Usages => usages.ToImmutableSortedDictionary();

        public bool IsConstant() => value != null;

        public void WriteAssembly(StreamWriter writer, ref int offset)
        {
            Compiler.Instance.MakeAlignment(writer, Type.MaxElementSize, ref offset);
            if (value != null) {
                writer.WriteLine(Label + ": ;" + Name);
                if (Visibility == Visibility.Public) {
                    writer.WriteLine("\tpublic\t" + Label);
                }
                value.WriteAssembly(writer);
                offset += Type.ByteCount;
            }
            else {
                if (register != null) {
                    if (!IsTemporary()) {
                        writer.WriteLine("\t;" + Name + " => register " + register.Name);
                    }
                }
                else if (localVariableId != null) {
                    if (!IsTemporary()) {
                        writer.WriteLine("\t;" + Name + " => " + LocalVariableName(localVariableId.Value));
                    }
                }
                else {
                    if (Visibility == Visibility.External) {
                        writer.WriteLine("\textrn\t" + Label);
                    }
                    else {
                        var size = Type.ByteCount;
                        offset += size;
                        Compiler.Instance.MakeAlignment(writer, Type.MaxElementSize, ref offset);
                        writer.WriteLine(Label + ":\tdefs " + Type.ByteCount);
                        if (Visibility == Visibility.Public) {
                            writer.WriteLine("\tpublic\t" + Label);
                        }
                    }
                }
            }
        }

        public bool IsTemporary()
        {
            return Name.StartsWith(Compiler.Instance.LabelPrefix);
        }


        public void AddUsage(int address, Usage usage)
        {
            if (usages.ContainsKey(address)) {
                usages[address] |= usage;
            }
            else {
                usages[address] = usage;
            }
        }

        public AssignableOperand ToAssignableOperand(Type type, int offset = 0)
        {
            return new VariableOperand(this, type, offset);
        }

        public AssignableOperand ToAssignableOperand()
        {
            return ToAssignableOperand(Type);
        }

        public Operand ToOperand()
        {
            return ToAssignableOperand(Type);
        }

        public void MakeIntersection(Variable other)
        {
            Debug.Assert(Usages.Count > 0 && other.Usages.Count > 0);

            var first = Usages.First();
            var last = Usages.Last();
            var otherFirst = other.Usages.First();
            var otherLast = other.Usages.Last();

            if (otherFirst.Key >= last.Key || otherLast.Key <= first.Key)
                return;
            Intersections.Add(other);
        }

        public void FillSavings(Function function)
        {
            Debug.Assert(!@static && register != null && Usages.Count > 0);
            var pairs = Usages.ToList();
            for (var i = 0; i < pairs.Count - 1; ++i) {
                var last = i == pairs.Count - 2;
                var (key, usage) = pairs[i];
                var (nextKey, nextUsage) = pairs[i + 1];
                var from = key + (usage == Usage.Read ? 0 : 1);
                var modify = (last && !function.Instructions[nextKey].IsJump()) || nextUsage != Usage.Read ? 1 : 0;
                var to = nextKey - modify;
                for (var address = from; address <= to; ++address) {
                    var instruction = function.Instructions[address];
                    if (instruction.TemporaryRegisters.Any(r =>
                            register.Conflicts(r))) {
                        instruction.AddSavingVariable(this);
                    }
                }
            }
        }

        public void TestAnchors(List<Anchor> anchors)
        {
            if (Usages.Count == 0)
                return;

            var first = Usages.First().Key;
            var last = Usages.Last().Key;

            foreach (var anchor in anchors) {
                if (anchor.Address == null)
                    continue;
                if (anchor.Address.Value > first && anchor.Address.Value < last) {
                    if (anchor.OriginAddresses.All(address => address >= first && address <= last))
                        continue;
                    Static = true;
                    return;
                }
            }
        }

        public bool SameStorage(Variable variable)
        {
            if (register != null) {
                return variable.register != null && Equals(register, variable.register);
            }

            if (localVariableId != null) {
                return variable.localVariableId != null && localVariableId == variable.localVariableId;
            }

            return Equals(variable);
        }

        public string MemoryAddress(int offset)
        {
            var s = new StringBuilder();
            s.Append(Label);
            if (offset > 0) {
                s.Append('+');
                s.Append(offset.ToString());
            }
            else if (offset < 0) {
                s.Append('-');
                s.Append((-offset).ToString());
            }
            return s.ToString();
        }
    }
}