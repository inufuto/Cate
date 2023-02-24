using System;

namespace Inu.Cate.I8086
{
    internal class MonomialInstruction : Cate.MonomialInstruction
    {
        public MonomialInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, operatorId, destinationOperand, sourceOperand)
        { }

        public override void BuildAssembly()
        {
            var operation = OperatorId switch
            {
                '-' => "neg ",
                '~' => "not ",
                _ => throw new NotImplementedException()
            };

            if (DestinationOperand.SameStorage(SourceOperand) &&
                DestinationOperand is VariableOperand { Register: null } destinationVariableOperand) {
                var size = DestinationOperand.Type.ByteCount == 1 ? "byte" : "word";
                var destinationAddress = destinationVariableOperand.MemoryAddress();
                WriteLine("\t" + operation + size + " ptr [" + destinationAddress + "]");
                return;
            }
            if (DestinationOperand.Type.ByteCount == 1) {
                ByteOperation.UsingAnyRegister(this, DestinationOperand, SourceOperand, temporaryRegister =>
                {
                    temporaryRegister.Load(this, SourceOperand);
                    WriteLine("\t" + operation + temporaryRegister);
                    temporaryRegister.Store(this, DestinationOperand);
                    ChangedRegisters.Add(temporaryRegister);
                });
                return;
            }
            WordOperation.UsingAnyRegister(this, DestinationOperand, SourceOperand, temporaryRegister =>
            {
                temporaryRegister.Load(this, SourceOperand);
                WriteLine("\t" + operation + temporaryRegister);
                temporaryRegister.Store(this, DestinationOperand);
                ChangedRegisters.Add(temporaryRegister);
            });
        }
    }
}
