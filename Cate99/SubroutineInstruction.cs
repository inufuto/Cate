using System.Collections.Generic;

namespace Inu.Cate.Tms99
{
    internal class SubroutineInstruction : Cate.SubroutineInstruction
    {
        public SubroutineInstruction(Function function, Function targetFunction, AssignableOperand? destinationOperand, List<Operand> sourceOperands) : base(function, targetFunction, destinationOperand, sourceOperands) { }

        protected override void Call()
        {
            WriteLine("\tbl\t@" + TargetFunction.Label);
        }

        protected override void StoreParameters()
        {
            StoreParametersDirect();
        }

        public override bool IsRegisterInUse(Register register)
        {
            foreach (var parameterAssignment in ParameterAssignments) {
                if (parameterAssignment.Done) {
                    var parameterAssignmentRegister = parameterAssignment.Register;
                    if (parameterAssignmentRegister != null) {
                        if (parameterAssignmentRegister.Conflicts(register)) return true;
                    }
                    var parameterRegister = parameterAssignment.Parameter.Register;
                    if (parameterRegister != null && parameterRegister.Conflicts(register)) return true;
                }
                else {
                    var operand = parameterAssignment.Operand;
                    if (operand.Register != null) {
                        if (register.Conflicts(operand.Register)) return true;
                    }
                    if (!(operand is IndirectOperand indirectOperand) ||
                        indirectOperand.Variable.Register == null) continue;
                    if (register.Conflicts(indirectOperand.Variable.Register)) return true;
                }
            }
            return base.IsRegisterInUse(register);
        }
    }
}
