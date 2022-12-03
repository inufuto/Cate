using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.Z80
{
    internal class MultiplyInstruction : Cate.MultiplyInstruction
    {
        public MultiplyInstruction(Function function, AssignableOperand destinationOperand, Operand leftOperand,
            int rightValue) : base(function, destinationOperand, leftOperand, rightValue)
        { }

        public override void BuildAssembly()
        {
            if (RightValue == 0) {
                WordRegister.UsingAny(this, WordRegister.Registers, DestinationOperand, register =>
                {
                    register.LoadConstant(this, 0);
                    register.Store(this, DestinationOperand);
                });
                return;
            }
            if (BitCount == 1) {
                WordRegister.UsingAny(this, WordRegister.AddableRegisters, DestinationOperand, register =>
                {
                    register.Load(this, LeftOperand);
                    Shift(() => WriteLine("\tadd\t" + register.Name + "," + register.Name));
                    ChangedRegisters.Add(register);
                    RemoveRegisterAssignment(register);
                    register.Store(this, DestinationOperand);
                });
                return;
            }

            WordRegister.UsingAny(this, WordRegister.AddableRegisters, DestinationOperand, addedRegister =>
            {
                var candidates = WordRegister.Registers.Where(r => r != addedRegister && !r.IsAddable()).ToList();
                WordRegister.UsingAny(this, candidates, addition =>
                {
                    addition.Load(this, LeftOperand);
                    addedRegister.LoadConstant(this, 0.ToString());
                    Operate(() =>
                    {
                        WriteLine("\tadd\t" + addedRegister.Name + "," + addition.Name);
                    }, () =>
                    {
                        Debug.Assert(addition.Low != null && addition.High != null);
                        WriteLine("\tsla\t" + addition.Low.Name);
                        WriteLine("\trl\t" + addition.High.Name);
                    });
                    ChangedRegisters.Add(addedRegister);
                    RemoveRegisterAssignment(addedRegister);
                    ChangedRegisters.Add(addition);
                    RemoveRegisterAssignment(addition);
                });
                addedRegister.Store(this, DestinationOperand);
            });
        }
    }
}