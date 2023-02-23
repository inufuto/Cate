using System;
using System.Diagnostics;
using System.Text;

namespace Inu.Cate
{
    public abstract class Operand
    {
        public virtual Register? Register => null;
        public virtual bool Conflicts(Register register) => false;
        public virtual bool Matches(Register register) => false;
        public abstract Type Type { get; }
        public abstract Operand Cast(ParameterizableType type);
        public abstract void AddUsage(int address, Variable.Usage usage);

        public virtual bool SameStorage(Operand operand)
        {
            return Equals(operand);
        }
    }

    public abstract class ConstantOperand : Operand
    {
        public abstract string MemoryAddress();

        public override string ToString()
        {
            return MemoryAddress();
        }

        public override void AddUsage(int address, Variable.Usage usage) { }
    }

    public class IntegerOperand : ConstantOperand
    {
        public readonly int IntegerValue;

        public IntegerOperand(Type type, int integerValue)
        {
            Type = type;
            IntegerValue = integerValue;
        }

        public override bool Equals(object? obj) => obj is IntegerOperand integerOperand && IntegerValue == integerOperand.IntegerValue;

        public override int GetHashCode() => IntegerValue;

        public override string MemoryAddress()
        {
            return IntegerValue.ToString();
        }

        public override Type Type { get; }
        public override Operand Cast(ParameterizableType type)
        {
            return new IntegerOperand(type, IntegerValue);
        }
    }


    class BooleanOperand : IntegerOperand
    {
        public BooleanOperand(int integerValue) : base(BooleanType.Type, integerValue)
        { }
        public override Type Type => BooleanType.Type;
    }


    public class PointerOperand : ConstantOperand
    {
        public readonly Variable Variable;
        public readonly int Offset;

        public PointerOperand(Type type, Variable variable, int offset)
        {
            this.Type = type is ArrayType arrayType ? arrayType.ElementType : type;
            Variable = variable;
            Offset = offset;
        }

        public override Type Type { get; }

        public override Operand Cast(ParameterizableType type)
        {
            return new PointerOperand(type, Variable, Offset);
        }

        public override bool SameStorage(Operand operand)
        {
            return operand is PointerOperand pointerOperand && Variable.SameStorage(pointerOperand.Variable) &&
                Offset == pointerOperand.Offset;
        }

        public override bool Equals(object? obj) =>
            obj is PointerOperand pointerOperand && Variable.Equals(pointerOperand.Variable) &&
            Offset == pointerOperand.Offset;

        public override int GetHashCode() => Variable.GetHashCode() + Offset.GetHashCode();

        public override string MemoryAddress() => Variable.MemoryAddress(Offset);
    }

    public class StringOperand : ConstantOperand
    {
        public readonly string StringValue;

        public StringOperand(Type type, string value)
        {
            Type = type;
            StringValue = value;
        }

        public override bool Equals(object? obj) => obj is StringOperand stringOperand && StringValue == stringOperand.StringValue;

        public override int GetHashCode() => StringValue.GetHashCode();

        public override string MemoryAddress()
        {
            return StringValue;
        }

        public override Type Type { get; }
        public override Operand Cast(ParameterizableType type)
        {
            return new StringOperand(type, StringValue);
        }
    }

    public abstract class AssignableOperand : Operand
    {
        public abstract AssignableOperand ToMember(Type type, int offset);
    }

    public class VariableOperand : AssignableOperand
    {
        public readonly Variable Variable;
        public override Type Type { get; }
        public override Operand Cast(ParameterizableType type)
        {
            return new VariableOperand(Variable, type, Offset);
        }

        public override void AddUsage(int address, Variable.Usage usage)
        {
            Variable.AddUsage(address, usage);
        }

        public override bool SameStorage(Operand operand)
        {
            return operand is VariableOperand variableOperand && Variable.SameStorage(variableOperand.Variable) &&
                   Offset == variableOperand.Offset;
        }

        public readonly int Offset;

        public VariableOperand(Variable variable, Type type, int offset)
        {
            Variable = variable;
            Type = type;
            Offset = offset;
        }


        public override bool Equals(object? obj)
        {
            return obj is VariableOperand variableOperand && Variable.Equals(variableOperand.Variable) &&
                   Offset == variableOperand.Offset;
        }

        public override int GetHashCode() => Variable.GetHashCode() + Offset.GetHashCode();

        public override Register? Register => Variable.Register;

        public override bool Conflicts(Register register)
        {
            return Variable.Register != null && Variable.Register.Conflicts(register);
        }
        public override bool Matches(Register register)
        {
            return Variable.Register != null && Variable.Register.Matches(register);
        }

        public virtual bool IsMemory() => Register == null;
        public override AssignableOperand ToMember(Type type, int offset)
        {
            return new VariableOperand(Variable, type, Offset + offset);
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder(Variable.ToString());
            if (Offset != 0) {
                stringBuilder.Append('[');
                stringBuilder.Append(Offset.ToString());
                stringBuilder.Append(']');
            }
            return stringBuilder.ToString();
        }

        public string MemoryAddress()
        {
            return Variable.MemoryAddress(Offset);
        }
    }

    public class IndirectOperand : AssignableOperand
    {
        public readonly Variable Variable;
        public override bool Conflicts(Register register)
        {
            return Variable.Register != null && Variable.Register.Conflicts(register);
        }
        public override bool Matches(Register register)
        {
            return Variable.Register != null && Variable.Register.Matches(register);
        }

        public override Type Type { get; }
        public readonly int Offset;

        public IndirectOperand(Variable variable, Type type, int offset)
        {
            Debug.Assert(variable.Type is PointerType);
            Variable = variable;
            Type = type;
            Offset = offset;
        }

        public IndirectOperand(Variable variable, int offset = 0) : this(variable, ((PointerType)variable.Type).ElementType, offset) { }

        public virtual bool IsMemory() => true;
        public override Operand Cast(ParameterizableType type)
        {
            return new IndirectOperand(Variable, type, Offset);
        }

        public override void AddUsage(int address, Variable.Usage usage)
        {
            Variable.AddUsage(address, Variable.Usage.Read);
        }

        public override bool SameStorage(Operand operand)
        {
            return operand is IndirectOperand indirectOperand && Variable.SameStorage(indirectOperand.Variable) &&
                   Offset == indirectOperand.Offset;
        }

        public override AssignableOperand ToMember(Type type, int offset)
        {
            return new IndirectOperand(Variable, type, Offset + offset);
        }

        public override bool Equals(object? obj)
        {
            return obj is IndirectOperand indirectOperand && Variable.Equals(indirectOperand.Variable) &&
                   Offset == indirectOperand.Offset;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Variable, Offset);
        }

        public override string ToString()
        {
            if (Offset != 0) {
                return Variable + "[" + Offset + "]";
            }
            return "*" + Variable;
        }
    }

    public class ByteRegisterOperand : Operand
    {
        public override Type Type { get; }
        public new readonly ByteRegister Register;

        public ByteRegisterOperand(Type type, ByteRegister register)
        {
            Type = type;
            this.Register = register;
        }

        public override Operand Cast(ParameterizableType type)
        {
            return new ByteRegisterOperand(type, Register);
        }

        public override void AddUsage(int address, Variable.Usage usage) { }

        public void CopyFrom(Instruction instruction, ByteRegister register)
        {
            Register.CopyFrom(instruction, register);
            instruction.ChangedRegisters.Add(this.Register);
            instruction.RemoveRegisterAssignment(this.Register);
        }

        public void CopyTo(Instruction instruction, ByteRegister register)
        {
            register.CopyFrom(instruction, Register);
        }
    }
}