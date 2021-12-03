namespace Inu.Cate
{
    internal class IfStatement : Statement
    {
        private readonly BooleanValue booleanValue;
        private readonly Statement trueStatement;
        private readonly Statement? falseStatement;

        public IfStatement(BooleanValue booleanValue, Statement trueStatement, Statement? falseStatement)
        {
            this.booleanValue = booleanValue;
            this.trueStatement = trueStatement;
            this.falseStatement = falseStatement;
        }

        public override void BuildInstructions(Function function)
        {
            if (trueStatement is JumpStatement trueJumpStatement && !trueJumpStatement.HasOperand()) {
                if (falseStatement is JumpStatement falseJumpStatement && !falseJumpStatement.HasOperand()) {
                    booleanValue.BuildJump(function, trueJumpStatement.Anchor, falseJumpStatement.Anchor);
                    return;
                }
                booleanValue.BuildJump(function, trueJumpStatement.Anchor, null);
                falseStatement?.BuildInstructions(function);
            }
            else {
                if (falseStatement is JumpStatement falseJumpStatement && !falseJumpStatement.HasOperand()) {
                    booleanValue.BuildJump(function, null, falseJumpStatement.Anchor);
                    trueStatement.BuildInstructions(function);
                    return;
                }
                var falseAnchor = function.CreateAnchor();
                booleanValue.BuildJump(function, null, falseAnchor);
                trueStatement.BuildInstructions(function);
                if (falseStatement != null) {
                    var endAnchor = function.CreateAnchor();
                    function.Instructions.Add(Compiler.Instance.CreateJumpInstruction(function, endAnchor));
                    falseAnchor.Address = function.NextAddress;
                    falseStatement.BuildInstructions(function);
                    endAnchor.Address = function.NextAddress;
                }
                else {
                    falseAnchor.Address = function.NextAddress;
                }
            }
        }
    }
}