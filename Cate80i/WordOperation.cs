using System;
using System.Collections.Generic;
using System.Linq;

namespace Inu.Cate.I8080
{
    internal class WordOperation : Cate.WordOperation
    {
        public override List<Cate.WordRegister> Registers => WordRegister.Registers;

        public override void UsingRegister(Instruction instruction, Cate.WordRegister register, Action action)
        {
            if (instruction.IsRegisterInUse(register)) {
                var changed = instruction.ChangedRegisters.Contains(register);
                register.Save(instruction);
                action();
                register.Restore(instruction);
                if (!changed) {
                    instruction.ChangedRegisters.Remove(register);
                }
            }
            else {
                instruction.BeginRegister(register);
                action();
                instruction.EndRegister(register);
            }
        }
    }
}
