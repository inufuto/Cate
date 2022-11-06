using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate.Tms99
{
    internal class ReturnInstruction : Cate.ReturnInstruction
    {
        public ReturnInstruction(Function function, Operand? sourceOperand, Anchor anchor) : base(function, sourceOperand, anchor) { }

        public override void BuildAssembly()
        {
            LoadResult();
            if (SourceOperand != null) {
                var register = Compiler.ReturnRegister(SourceOperand.Type.ByteCount);
                ISet<Register> registers = new HashSet<Register>();
                foreach (var changedRegister in ChangedRegisters) {
                    if (changedRegister.Conflicts(register)) {
                        registers.Add(changedRegister);
                    }
                }

                foreach (var register1 in registers) {
                    ChangedRegisters.Remove(register1);
                }
            }
            if (!Equals(Function.Instructions.Last())) {
                WriteLine("\tjmp\t" + Anchor.Label);
            }
        }
    }
}
