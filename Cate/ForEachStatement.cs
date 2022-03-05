using System.Diagnostics;

namespace Inu.Cate
{
    internal class ForEachStatement : LoopStatement
    {
        private readonly AssignableValue pointer;
        private readonly ConstantPointer array;
        public Statement? Statement;

        public ForEachStatement(AssignableValue pointer, ConstantPointer array, Function function) : base(function)
        {
            this.pointer = pointer;
            this.array = array;
            Debug.Assert(pointer.Type is PointerType);
        }

        public override void BuildInstructions(Function function)
        {
            Debug.Assert(array.ElementCount != null, "array.ElementCount != null");
            var compiler = Compiler.Instance;
            var counterType = compiler.CounterType;
            var counter = function.CreateTemporaryVariable(counterType);

            function.Instructions.Add(
                compiler.CreateLoadInstruction(function, counter.ToAssignableOperand(), new IntegerOperand(counterType, array.ElementCount.Value)));
            function.Instructions.Add(
                compiler.CreateLoadInstruction(function, pointer.ToAssignableOperand(function), array.ToOperand()));
            var repeatAnchor = function.CreateAnchor();
            repeatAnchor.Address = function.NextAddress;

            Debug.Assert(Statement != null);
            Statement.BuildInstructions(function);

            ContinueAnchor.Address = function.NextAddress;
            var byteCount = ((PointerType)pointer.Type).ElementType.ByteCount;
            function.Instructions.Add(
                compiler.CreateBinomialInstruction(function, '+', pointer.ToAssignableOperand(function), pointer.ToOperand(function), new IntegerOperand(IntegerType.WordType, byteCount)));
            function.Instructions.Add(
                compiler.CreateDecrementJumpInstruction(function, counter.ToAssignableOperand(), repeatAnchor));

            BreakAnchor.Address = function.NextAddress;
        }
    }
}