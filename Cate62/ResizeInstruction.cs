using System.Diagnostics;
using System.Reflection;

namespace Inu.Cate.Sc62015
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType, Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType) { }

        protected override void Reduce()
        {
            if (SourceOperand is VariableOperand variableOperand) {
                var sourceRegister = GetVariableRegister(variableOperand);
                if (Equals(sourceRegister, WordRegister.BA)) {
                    ByteRegister.A.Store(this, DestinationOperand);
                    return;
                }
                if (Equals(sourceRegister, WordRegister.I)) {
                    ByteRegister.IL.Store(this, DestinationOperand);
                    return;
                }
            }
            base.Reduce();
        }

        protected override void Expand()
        {
            if (DestinationOperand.Register is WordRegister wordRegister and not WordInternalRam) {
                using var reservation = WordOperation.ReserveAnyRegister(this, WordInternalRam.Registers, DestinationOperand);
                var destinationRegister = reservation.WordRegister;
                Debug.Assert(destinationRegister.Low != null);
                Debug.Assert(destinationRegister.High != null);
                destinationRegister.Low.Load(this, SourceOperand);
                destinationRegister.High.LoadConstant(this, 0);
                destinationRegister.Store(this, DestinationOperand);
                return;
            }
            base.Expand();
        }

        protected override void ExpandSigned()
        {
            using (ByteOperation.ReserveRegister(this, ByteRegister.A)) {
                ByteRegister.A.Load(this, SourceOperand);
                using (WordOperation.ReserveRegister(this, WordRegister.BA)) {
                    Compiler.CallExternal(this, "cate.ExpandSigned");
                    WordRegister.BA.Store(this, DestinationOperand);
                }
            }
        }
    }
}
