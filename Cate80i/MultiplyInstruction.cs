using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate.I8080
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand, int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
        { }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                if (DestinationOperand.Register is WordRegister destinationRegister) {
                    destinationRegister.LoadConstant(this, 0);
                    return;
                }
                ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                {
                    ByteRegister.A.LoadConstant(this, 0);
                    ByteRegister.A.Store(this, Compiler.LowByteOperand(DestinationOperand));
                    ByteRegister.A.Store(this, Compiler.HighByteOperand(DestinationOperand));
                });
                return;
            }
            if (BitCount == 1) {
                WordOperation.UsingRegister(this, WordRegister.Hl,LeftOperand, () =>
                {
                    WordRegister.Hl.Load(this, LeftOperand);
                    Shift(() => WriteLine("\tdad\th"));
                    ChangedRegisters.Add(WordRegister.Hl);
                    RemoveRegisterAssignment(WordRegister.Hl);
                    WordRegister.Hl.Store(this, DestinationOperand);
                });
                return;
            }

            void OperateHl()
            {
                var candidates = new List<Cate.WordRegister> { WordRegister.Bc, WordRegister.De };
                WordOperation.UsingAnyRegister(this, candidates, addition =>
                {
                    addition.Load(this, LeftOperand);
                    WordRegister.Hl.LoadConstant(this, 0.ToString());
                    Operate(() => { WriteLine("\tdad\t" + addition.Name); }, () =>
                    {
                        ByteOperation.UsingRegister(this, ByteRegister.A, () =>
                        {
                            Debug.Assert(addition.Low != null && addition.High != null);
                            WriteLine("\tora\ta");
                            ByteRegister.A.CopyFrom(this, addition.Low);
                            WriteLine("\tral");
                            addition.Low.CopyFrom(this, ByteRegister.A);
                            ByteRegister.A.CopyFrom(this, addition.High);
                            WriteLine("\tral");
                            addition.High.CopyFrom(this, ByteRegister.A);
                        });
                    });
                    ChangedRegisters.Add(WordRegister.Hl);
                    RemoveRegisterAssignment(WordRegister.Hl);
                    ChangedRegisters.Add(addition);
                    RemoveRegisterAssignment(addition);
                });
            }

            if (DestinationOperand.Register == WordRegister.Hl)
            {
                OperateHl();
                return;
            }
            WordOperation.UsingRegister(this, WordRegister.Hl, () =>
            {
                OperateHl();
                WordRegister.Hl.Store(this, DestinationOperand);
            });
        }
    }
}
