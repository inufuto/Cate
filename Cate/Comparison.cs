using System;
using System.Diagnostics;
using Inu.Language;

namespace Inu.Cate
{
    class Comparison : BooleanValue
    {
        private readonly int operatorId;
        private readonly Value leftValue;
        private readonly Value rightValue;

        public Comparison(int operatorId, Value leftValue, Value rightValue)
        {
            Debug.Assert(leftValue != null && rightValue != null);
            this.operatorId = operatorId;
            this.leftValue = leftValue;
            this.rightValue = rightValue;
        }

        public override string ToString()
        {
            return leftValue + ReservedWord.FromId(operatorId) + rightValue;
        }


        public override void BuildJump(Function function, Anchor? trueAnchor, Anchor? falseAnchor)
        {
            var leftOperand = leftValue.ToOperand(function);
            var rightOperand = rightValue.ToOperand(function);

            if (falseAnchor != null) {
                var instruction = Compiler.Instance.CreateCompareInstruction(function, ReverseOperator(operatorId), leftOperand, rightOperand, falseAnchor);
                function.Instructions.Add(instruction);
                if (trueAnchor == null) return;
                function.Instructions.Add(Compiler.Instance.CreateJumpInstruction(function, trueAnchor));
            }
            else {
                Debug.Assert(trueAnchor != null);
                var instruction = Compiler.Instance.CreateCompareInstruction(function, operatorId, leftOperand, rightOperand, trueAnchor);
                function.Instructions.Add(instruction);
            }
        }

        private static int ReverseOperator(int operatorId)
        {
            return operatorId switch
            {
                Keyword.Equal => Keyword.NotEqual,
                Keyword.NotEqual => Keyword.Equal,
                '<' => Keyword.GreaterEqual,
                '>' => Keyword.LessEqual,
                Keyword.LessEqual => '>',
                Keyword.GreaterEqual => '<',
                _ => throw new NotImplementedException()
            };
        }
    }
}