using System;

namespace Inu.Cate.I8086;

internal class MonomialInstruction(
    Function function,
    int operatorId,
    AssignableOperand destinationOperand,
    Operand sourceOperand)
    : Cate.MonomialInstruction(function, operatorId, destinationOperand, sourceOperand)
{
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
            void ViaRegister(Cate.ByteRegister r)
            {
                r.Load(this, SourceOperand);
                WriteLine("\t" + operation + r);
                AddChanged(r);
            }

            if (DestinationOperand.Register is ByteRegister byteRegister) {
                ViaRegister(byteRegister);
                return;
            }
            using var reservation = ByteOperation.ReserveAnyRegister(this, SourceOperand);
            ViaRegister(reservation.ByteRegister);
            reservation.ByteRegister.Store(this, DestinationOperand);
            return;
        }
        {
            void ViaRegister(Cate.WordRegister r)
            {
                WriteLine("\t" + operation + r);
                AddChanged(r);
            }

            if (DestinationOperand.Register is WordRegister wordRegister) {
                ViaRegister(wordRegister);
                return;
            }
            using var reservation = WordOperation.ReserveAnyRegister(this, SourceOperand);
            reservation.WordRegister.Load(this, SourceOperand);
            ViaRegister(reservation.WordRegister);
            reservation.WordRegister.Store(this, DestinationOperand);
        }
    }
}