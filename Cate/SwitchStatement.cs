using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate
{
    internal class SwitchStatement : BreakableStatement
    {
        private readonly Value value;
        private readonly List<CaseStatement> caseStatements = new List<CaseStatement>();
        public DefaultStatement? DefaultStatement;
        public Statement? Statement;

        public SwitchStatement(Value value, Function function) : base(function)
        {
            this.value = value;
        }

        public IntegerType Type => (IntegerType)value.Type;

        public void Add(CaseStatement caseStatement)
        {
            caseStatements.Add(caseStatement);
        }

        public override void BuildInstructions(Function function)
        {
            var compiler = Compiler.Instance;
            var leftOperand = value.ToOperand(function);
            foreach (var caseStatement in caseStatements) {
                var rightOperand = caseStatement.Value.ToOperand(function);
                var instruction = compiler.CreateCompareInstruction(function,
                    Keyword.Equal,
                    leftOperand,
                    rightOperand,
                    caseStatement.Anchor);
                function.Instructions.Add(instruction);
            }
            if (DefaultStatement != null) {
                var instruction = compiler.CreateJumpInstruction(function, DefaultStatement.Anchor);
                function.Instructions.Add(instruction);
            }
            else {
                var instruction = compiler.CreateJumpInstruction(function, BreakAnchor);
                function.Instructions.Add(instruction);
            }
            Debug.Assert(Statement != null);
            Statement.BuildInstructions(function);
            BreakAnchor.Address = function.NextAddress;
        }
    }
}