namespace Inu.Cate
{
    public abstract class AddOrSubtractInstruction : BinomialInstruction
    {
        protected AddOrSubtractInstruction(Function function, int operatorId, AssignableOperand destinationOperand, Operand leftOperand, Operand rightOperand) : base(function, operatorId, destinationOperand, leftOperand, rightOperand) { }

        protected bool IncrementOrDecrement()
        {
            if (!(RightOperand is IntegerOperand rightIntegerOperand))
                return false;

            var mask = rightIntegerOperand.Type.ByteCount == 1 ? 0xff : 0xffff;
            var count = (rightIntegerOperand.IntegerValue & mask);
            var threshold = Threshold();
            switch (OperatorId) {
                case '+': {
                    if (count <= threshold) {
                        Increment(count);
                        return true;
                    }
                    if (count >= (mask + 1) - threshold) {
                        Decrement((mask + 1) - count);
                        return true;
                    }
                    break;
                }
                case '-': {
                    if (count <= threshold) {
                        Decrement(count);
                        return true;
                    }
                    if (count >= (mask + 1) - threshold) {
                        Increment((mask + 1) - count);
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        protected abstract int Threshold();
        protected abstract void Increment(int count);
        protected abstract void Decrement(int count);
    }
}