using System.Collections.Generic;

namespace Inu.Cate.Mc6800
{
    internal class WordOperation : Cate.WordOperation
    {
        public virtual List<Cate.WordRegister> AddableRegisters => new List<Cate.WordRegister>();

        public override List<Cate.WordRegister> Registers => WordRegister.Registers;


        public static void OperatePair(
            Instruction instruction,
            Operand leftOperand, Operand rightOperand, AssignableOperand destinationOperand,
            string lowOperation, string highOperation
        )
        {
            Cate.Compiler.Instance.ByteOperation.UsingAnyRegister(instruction, register =>
            {
                var leftTemporary = leftOperand is IndirectOperand leftIndirectOperand &&
                                    !WordRegister.X.IsOffsetInRange(leftIndirectOperand.Offset);
                var rightTemporary = rightOperand is IndirectOperand rightIndirectOperand &&
                                     !WordRegister.X.IsOffsetInRange(rightIndirectOperand.Offset);
                var destinationTemporary = destinationOperand is IndirectOperand destinationIndirectOperand &&
                                           !WordRegister.X.IsOffsetInRange(destinationIndirectOperand.Offset);

                if (leftTemporary) {
                    ZeroPage.Word.From(instruction, leftOperand);
                }
                if (rightTemporary) {
                    ZeroPage.Word2.From(instruction, rightOperand);
                }
                if (leftTemporary) {
                    register.LoadFromMemory(instruction, ZeroPage.Word.Low.Name);
                }
                else {
                    register.Load(instruction, Cate.Compiler.Instance.LowByteOperand(leftOperand));
                }

                if (rightTemporary) {
                    register.Operate(instruction, lowOperation, true, ZeroPage.Word2.Low.Name);
                }
                else {
                    register.Operate(instruction, lowOperation, true, Cate.Compiler.Instance.LowByteOperand(rightOperand));
                }

                if (destinationTemporary) {
                    register.StoreToMemory(instruction, ZeroPage.Word.High.Name);
                }
                else {
                    register.Store(instruction, Cate.Compiler.Instance.LowByteOperand(destinationOperand));
                }

                if (leftTemporary) {
                    register.LoadFromMemory(instruction, ZeroPage.Word.High.Name);
                }
                else {
                    register.Load(instruction, Cate.Compiler.Instance.HighByteOperand(leftOperand));
                }

                if (rightTemporary) {
                    register.Operate(instruction, highOperation, true, ZeroPage.Word2.High.Name);
                }
                else {
                    register.Operate(instruction, highOperation, true, Cate.Compiler.Instance.HighByteOperand(rightOperand));
                }

                if (destinationTemporary) {
                    register.StoreToMemory(instruction, ZeroPage.Word.High.Name);
                }
                else {
                    register.Store(instruction, Cate.Compiler.Instance.HighByteOperand(destinationOperand));
                }

                if (destinationTemporary) {
                    WordRegister.X.StoreToMemory(instruction, ZeroPage.Word.Name);
                    WordRegister.X.Store(instruction, destinationOperand);
                }
                else {
                    instruction.RemoveVariableRegister(destinationOperand);
                }
            });
        }
    }
}
