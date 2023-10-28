﻿namespace Inu.Cate.Mc6800
{
    internal abstract class ZeroPage
    {
        public readonly string Label;

        // zero page area names
        public static readonly ZeroPageByte Byte = new("@Temp@Byte");
        public static readonly ZeroPageWord Word = new("@Temp@Word");
        public static readonly ZeroPageByte WordHigh = Word.High;
        public static readonly ZeroPageByte WordLow = Word.Low;
        public static readonly ZeroPageWord Word2 = new ZeroPageWord("@Temp@Word2");
        public static readonly ZeroPageByte Word2High = Word2.High;
        public static readonly ZeroPageByte Word2Low = Word2.Low;

        protected ZeroPage(string label)
        {
            Label = label;
        }

        public override string ToString()
        {
            return Name;
        }

        public string Name => "<" + Label;
    }

    internal class ZeroPageByte : ZeroPage
    {
        public ZeroPageByte(string name) : base(name) { }

        public void Clear(Instruction instruction)
        {
            instruction.WriteLine("\tclr\t" + Name);
        }

        public void Operate(Instruction instruction, string operation, bool change, string operand)
        {
            using var reservation = Cate.Compiler.Instance.ByteOperation.ReserveAnyRegister(instruction);
            var register = reservation.ByteRegister;
            register.LoadFromMemory(instruction, Name);
            register.Operate(instruction, operation, change, operand);
            register.StoreToMemory(instruction, Name);
        }

        public void Operate(Instruction instruction, string operation)
        {
            instruction.WriteLine("\t" + operation + "\t" + Name);
        }
    }

    class ZeroPageWord : ZeroPage
    {
        public readonly ZeroPageByte High;
        public readonly ZeroPageByte Low;

        public ZeroPageWord(string name) : base(name)
        {
            High = new ZeroPageByte(name + "+0");
            Low = new ZeroPageByte(name + "+1");
        }

        public void From(Instruction instruction, Operand operand)
        {
            if (operand is IntegerOperand { IntegerValue: 0 }) {
                Clear(instruction);
                return;
            }
            IndexRegister.X.Load(instruction, operand);
            IndexRegister.X.StoreToMemory(instruction, Name);
        }

        public void Clear(Instruction instruction)
        {
            High.Clear(instruction);
            Low.Clear(instruction);
        }

        public void To(Instruction instruction, AssignableOperand operand)
        {
            IndexRegister.X.LoadFromMemory(instruction, Name);
            IndexRegister.X.Store(instruction, operand);
        }



        public void Operate(Instruction instruction, string lowOperation, string highOperation, ZeroPageWord operand)
        {
            Low.Operate(instruction, lowOperation, true, operand.Low.Name);
            High.Operate(instruction, highOperation, true, operand.High.Name);
        }


        public void Operate(Instruction instruction, string lowOperation, string highOperation)
        {
            Low.Operate(instruction, lowOperation);
            High.Operate(instruction, highOperation);
        }


        public void Add(Instruction instruction, ZeroPageWord operand)
        {
            Operate(instruction, "add", "adc", operand);
        }
    }
}