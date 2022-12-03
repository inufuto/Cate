using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Inu.Language;

namespace Inu.Cate
{
    public abstract class Block
    {
        public const char LabelSeparator = '.';
        public readonly Block? Parent;
        private readonly Dictionary<int, NamedType> namedTypes = new Dictionary<int, NamedType>();
        private readonly Dictionary<int, NamedConstant> constants = new Dictionary<int, NamedConstant>();
        public readonly Dictionary<int, Variable> Variables = new Dictionary<int, Variable>();

        protected Block(Block? parent)
        {
            Parent = parent;
        }

        public string LabelPrefix => Parent != null ? Parent.LabelPrefix + Label + LabelSeparator : "";
        public abstract string Label { get; }
        public abstract Function? Function { get; }

        public bool IsGlobal()
        {
            return Parent == null;
        }

        public NamedType AddType(Identifier identifier, StructureType type)
        {
            var id = identifier.Id;
            if (namedTypes.TryGetValue(id, out var namedType)) {
                var structureType = (StructureType)namedType.Type;
                if (structureType.Members.Count == 0) {
                    return namedType;
                }
                throw new MultipleIdentifierError(identifier);
            }
            namedType = new NamedType(this, id, type);
            namedTypes[id] = namedType;
            return namedType;
        }

        public Type? FindNamedType(int id)
        {
            return namedTypes.TryGetValue(id, out var namedType) ? namedType.Type : Parent?.FindNamedType(id);
        }


        public NamedConstant AddConstant(Identifier identifier, Type type, Constant value)
        {
            var id = identifier.Id;
            var namedValue = FindNamedValue(id);
            if (namedValue != null) {
                if (namedValue is NamedConstant namedConstant && namedConstant.Type.Equals(type) && namedConstant.Value.Equals(value)) {
                    return namedConstant;
                }
                throw new MultipleIdentifierError(identifier);
            }
            else {
                var namedConstant = new NamedConstant(this, id, type, value);
                constants[id] = namedConstant;
                return namedConstant;
            }
        }

        public NamedConstant? FindConstant(int id)
        {
            return constants.TryGetValue(id, out var constant) ? constant : Parent?.FindConstant(id);
        }


        public Variable AddVariable(Identifier identifier, Type type, Visibility visibility, bool @static,
            Constant? value)
        {
            var namedValue = FindNamedValue(identifier.Id);
            if (namedValue != null && (!(namedValue is Variable variable) || !variable.Type.Equals(type) ||
                                       (variable.Visibility != Visibility.External))) {
                throw new MultipleIdentifierError(identifier);
            }
            return AddVariable(identifier.Id, type, visibility, @static, value);
        }

        public Variable AddVariable(int id, Type type, Visibility visibility, bool @static, Constant? value)
        {
            var variable = new Variable(this, id, type, visibility, @static, value);
            Variables[id] = variable;
            return variable;
        }

        public Variable? FindVariable(int id)
        {
            return Variables.TryGetValue(id, out var variable) ? variable : Parent?.FindVariable(id);
        }


        protected virtual NamedValue? FindNamedValue(int id)
        {
            if (constants.TryGetValue(id, out var constant)) {
                return constant;
            }
            if (Variables.TryGetValue(id, out var variable)) {
                return variable;
            }
            return null;
        }

        public NamedValue? FindNamedValueIncludingAncestors(int id)
        {
            var namedValue = FindNamedValue(id);
            return namedValue ?? Parent?.FindNamedValueIncludingAncestors(id);
        }

        public virtual void Clear()
        { }

        public virtual void WriteAssembly(StreamWriter writer, ref int codeOffset, ref int dataOffset)
        {
            var codeSegmentVariables = Variables.Values.Where(v => v.IsConstant()).ToList();
            if (codeSegmentVariables.Any()) {
                writer.WriteLine("\tcseg");
                foreach (var variable in codeSegmentVariables) {
                    variable.WriteAssembly(writer, ref codeOffset);
                }
                Compiler.Instance.MakeAlignment(writer, ref codeOffset);
            }
            var dataSegmentVariables = Variables.Values.Where(v => !v.IsConstant()).ToList();
            if (!dataSegmentVariables.Any()) return;
            {
                var index = 0;
                //var variables = dataSegmentVariables.Where(variable => variable.Parameter == null).ToList();
                if (dataSegmentVariables.Any()) {
                    writer.WriteLine("\tdseg");
                }

                foreach (var variable in dataSegmentVariables) {
                    WriteVariableAssembly(writer, variable, index++, ref dataOffset);
                }
            }
        }

        protected virtual void WriteVariableAssembly(StreamWriter writer, Variable variable, int index, ref int offset)
        {
            variable.WriteAssembly(writer, ref offset);
        }

        public void End(Function function)
        {
        }
    }

    public class GlobalBlock : Block
    {
        private readonly List<Function> functions = new List<Function>();

        public GlobalBlock() : base(null) { }
        public override string Label => "";
        public override Function? Function => null;

        public Function AddFunction(Token identifier, Function function)
        {
            var namedValue = FindNamedValue(function.Id);
            if (namedValue != null) {
                if (!(namedValue is Function foundFunction)) {
                    throw new MultipleIdentifierError(identifier);
                }
                if (foundFunction != null) {
                    if (foundFunction.IsSameSignature(function)) {
                        if (foundFunction.Visibility != Visibility.External) {
                            if (function.Visibility == Visibility.External) {
                                return foundFunction;
                            }

                            throw new MultipleIdentifierError(identifier);
                        }
                    }
                    else {
                        throw new MultipleIdentifierError(identifier);
                    }
                    functions.Remove(foundFunction);
                }
            }
            {
                functions.Add(function);
                return function;
            }
        }

        protected override NamedValue? FindNamedValue(int id)
        {
            var namedValue = base.FindNamedValue(id);
            if (namedValue != null) {
                return namedValue;
            }
            return functions.Find(f => f.Id == id);
        }

        public override void WriteAssembly(StreamWriter writer, ref int codeOffset, ref int dataOffset)
        {
            base.WriteAssembly(writer, ref codeOffset, ref dataOffset);
            foreach (var function in functions) {
                function.WriteAssembly(writer, ref codeOffset, ref dataOffset);
            }
        }
    }

    public class LocalBlock : Block
    {
        public readonly List<LocalBlock> Children = new List<LocalBlock>();
        private static int lastId = 0;
        private readonly int id;

        public LocalBlock(Block? parent) : base(parent)
        {
            id = ++lastId;
        }

        public override void Clear()
        {
            base.Clear();
            Children.Clear();
        }

        public override void WriteAssembly(StreamWriter writer, ref int codeOffset, ref int dataOffset)
        {
            base.WriteAssembly(writer, ref codeOffset, ref dataOffset);
            foreach (var child in Children) {
                child.WriteAssembly(writer, ref codeOffset, ref dataOffset);
            }
        }

        public override string Label => id.ToString();
        public override Function? Function => Parent?.Function;

        public void AddBlock(LocalBlock block)
        {
            Children.Add(block);
        }

        public List<Variable> AllVariables
        {
            get
            {
                var allVariables = new List<Variable>(this.Variables.Values);
                foreach (var child in Children) {
                    allVariables.AddRange(child.AllVariables);
                }
                return allVariables;
            }
        }
    }

    public class FunctionBlock : LocalBlock
    {
        private readonly Function function;

        public FunctionBlock(Block? parent, Function function) : base(parent)
        {
            this.function = function;
        }

        public override string Label => function.Name + "_";
        public override Function? Function => function;


        protected override void WriteVariableAssembly(StreamWriter writer, Variable variable, int index, ref int offset)
        {
            function.WriteParameterLabel(writer, index, variable.Parameter);
            base.WriteVariableAssembly(writer, variable, index, ref offset);
        }
    }
}
