using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Cate.I8086
{
    internal class PointerRegister : WordPointerRegister
    {
        public static List<Cate.PointerRegister> Registers = new();

        public static PointerRegister Bx = new(I8086.WordRegister.Bx);
        public static PointerRegister Si = new(I8086.WordRegister.Si);
        public static PointerRegister Di = new(I8086.WordRegister.Di);
        public static PointerRegister Ax = new(I8086.WordRegister.Ax);
        public static PointerRegister Cx = new(I8086.WordRegister.Cx);
        public static PointerRegister Dx = new(I8086.WordRegister.Dx);
        public static PointerRegister Bp = new(I8086.WordRegister.Bp, SegmentRegister.Ss);

        public static string AsPointer(Cate.PointerRegister pointerRegister)
        {
            var defaultSegmentRegister = ((PointerRegister)pointerRegister).DefaultSegmentRegister;
            return defaultSegmentRegister is null or SegmentRegister.Ds
                ? pointerRegister.ToString()
                : "ds:" + pointerRegister;
        }

        public readonly SegmentRegister? DefaultSegmentRegister;

        public PointerRegister(Cate.WordRegister wordRegister, SegmentRegister? defaultSegmentRegister = null) : base(2, wordRegister)
        {
            DefaultSegmentRegister = defaultSegmentRegister;
            if (wordRegister is IndexRegister || Equals(wordRegister, I8086.WordRegister.Bx)) {
                Registers.Add(this);
            }
        }

        public override bool IsOffsetInRange(int offset) => true;

        public override void Add(Instruction instruction, int offset)
        {
            switch (offset) {
                case 0:
                    return;
                case 1:
                    instruction.WriteLine("\tinc " + this);
                    return;
                case -1:
                    instruction.WriteLine("\tdec " + this);
                    return;
            }
            if (offset > 0) {
                instruction.WriteLine("\tadd " + this + "," + offset);
                return;
            }
            instruction.WriteLine("\tsub " + this + "," + (-offset));
        }

        public override void Operate(Instruction instruction, string operation, bool change, Operand operand)
        {
            switch (operand) {
                case ConstantOperand constantOperand:
                    instruction.WriteLine("\t" + operation + this + "," + constantOperand.MemoryAddress());
                    instruction.AddChanged(this);
                    instruction.RemoveRegisterAssignment(this);
                    return;
                case VariableOperand variableOperand: {
                        var sourceVariable = variableOperand.Variable;
                        var sourceOffset = variableOperand.Offset;
                        if (sourceVariable.Register is WordRegister sourceRegister) {
                            Debug.Assert(sourceOffset == 0);
                            instruction.WriteLine("\t" + operation + this + "," + sourceRegister);
                            instruction.AddChanged(this);
                            instruction.RemoveRegisterAssignment(this);
                            return;
                        }
                        instruction.WriteLine("\t" + operation + this + ",[" + variableOperand.MemoryAddress() + "]");
                        instruction.AddChanged(this);
                        instruction.RemoveRegisterAssignment(this);
                        return;
                    }
            }
            {
                if (operand is not IndirectOperand indirectOperand) throw new NotImplementedException();
                var pointer = indirectOperand.Variable;
                var offset = indirectOperand.Offset;
                if (pointer.Register is PointerRegister pointerRegister) {
                    var addition = offset >= 0 ? "+" + offset : "-" + (-offset);
                    instruction.WriteLine("\t" + operation + this + ",[" + PointerRegister.AsPointer(pointerRegister) + addition + "]");
                    return;
                }
                using var reservation = PointerOperation.ReserveAnyRegister(instruction, PointerRegister.Registers.Where(r => !r.Conflicts(this)).ToList());
                var temporaryRegister = reservation.PointerRegister;
                temporaryRegister.Load(instruction, operand);
                instruction.WriteLine("\t" + operation + this + "," + temporaryRegister);
            }
        }
    }
}
