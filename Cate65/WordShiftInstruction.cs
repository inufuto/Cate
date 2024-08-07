using System;
using System.Diagnostics;

namespace Inu.Cate.Mos6502;

class WordShiftInstruction : Cate.WordShiftInstruction
{
    public WordShiftInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand)
    { }

    protected override void ShiftConstant(int count)
    {
        if (OperatorId == Keyword.ShiftRight && ((IntegerType)LeftOperand.Type).Signed) {
            CallExternal(() => ByteRegister.Y.LoadConstant(this, count));
            return;
        }

        Action<Action<string>, Action<string>> byteAction = OperatorId switch
        {
            Keyword.ShiftLeft => (low, high) =>
            {
                low("asl");
                high("rol");
            }
            ,
            Keyword.ShiftRight => (low, high) =>
            {
                high("\tlsr");
                low("\tror");
            }
            ,
            _ => throw new NotImplementedException()
        };
        if (LeftOperand.SameStorage(DestinationOperand)) {
            for (var i = 0; i < count; ++i) {
                byteAction(operation =>
                {
                    ByteOperation.Operate(this, operation, true, Compiler.LowByteOperand(DestinationOperand));
                }, operation =>
                {
                    ByteOperation.Operate(this, operation, true, Compiler.HighByteOperand(DestinationOperand));
                });
            }
            return;
        }
        {
            void ViaRegister(WordRegister r)
            {
                r.Load(this, LeftOperand);
                for (var i = 0; i < count; ++i) {
                    byteAction(operation =>
                    {
                        Debug.Assert(r.Low != null);
                        r.Low.Operate(this, operation, true, 1);
                    }, operation =>
                    {
                        Debug.Assert(r.High != null);
                        r.High.Operate(this, operation, true, 1);
                    });
                }
            }

            if (DestinationOperand.Register is WordRegister wordRegister && !RightOperand.Conflicts(wordRegister)) {
                ViaRegister(wordRegister);
                return;
            }

            using var reservation = WordOperation.ReserveAnyRegister(this, WordZeroPage.Registers, LeftOperand);
            ViaRegister(reservation.WordRegister);
            reservation.WordRegister.Store(this, DestinationOperand);
        }
    }

    protected override void ShiftVariable(Operand counterOperand)
    {
        CallExternal(() => ByteRegister.Y.Load(this, counterOperand));
    }

    private void CallExternal(Action loadY)
    {
        var functionName = OperatorId switch
        {
            Keyword.ShiftLeft => "cate.ShiftLeftWord",
            Keyword.ShiftRight => ((IntegerType)LeftOperand.Type).Signed
                ? "cate.ShiftRightSignedWord"
                : "cate.ShiftRightWord",
            _ => throw new NotImplementedException()
        };
        {
            void ViaRegister(WordRegister r)
            {
                r.Load(this, LeftOperand);
                using (ByteOperation.ReserveRegister(this, ByteRegister.X)) {
                    var wordZeroPage = (WordZeroPage)r;
                    ByteRegister.X.LoadConstant(this, wordZeroPage.Label);
                    using (ByteOperation.ReserveRegister(this, ByteRegister.Y)) {
                        loadY();
                        Compiler.CallExternal(this, functionName);
                    }
                }
            }

            if (DestinationOperand.Register is WordRegister wordRegister && !RightOperand.Conflicts(wordRegister)) {
                ViaRegister(wordRegister);
                return;
            }
            using var reservation =
                WordOperation.ReserveAnyRegister(this, WordZeroPage.Registers, LeftOperand);
            ViaRegister(reservation.WordRegister);
            reservation.WordRegister.Store(this, DestinationOperand);
        }
    }
}