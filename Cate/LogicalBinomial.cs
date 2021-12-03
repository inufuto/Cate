namespace Inu.Cate
{
    class LogicalBinomial : BooleanValue
    {
        private readonly int operatorId;
        private readonly BooleanValue leftValue;
        private readonly BooleanValue rightValue;

        public LogicalBinomial(int operatorId, BooleanValue leftValue, BooleanValue rightValue)
        {
            this.operatorId = operatorId;
            this.leftValue = leftValue;
            this.rightValue = rightValue;
        }

        public override void BuildJump(Function function, Anchor? trueAnchor, Anchor? falseAnchor)
        {
            switch (operatorId) {
                case Keyword.LogicalOr: {
                        if (trueAnchor != null) {
                            leftValue.BuildJump(function, trueAnchor, null);
                            rightValue.BuildJump(function, trueAnchor, falseAnchor);
                        }
                        else {
                            var endAnchor = function.CreateAnchor();
                            leftValue.BuildJump(function, endAnchor, null);
                            rightValue.BuildJump(function, null, falseAnchor);
                            endAnchor.Address = function.NextAddress;
                        }
                        break;
                    }
                case Keyword.LogicalAnd: {
                        if (falseAnchor != null) {
                            leftValue.BuildJump(function, null, falseAnchor);
                            rightValue.BuildJump(function, trueAnchor, falseAnchor);
                        }
                        else {
                            var endAnchor = function.CreateAnchor();
                            leftValue.BuildJump(function, null, endAnchor);
                            rightValue.BuildJump(function, trueAnchor, null);
                            endAnchor.Address = function.NextAddress;
                        }
                        break;
                    }
            }
        }
    }
}
