using System;
using Inu.Language;

namespace Inu.Cate
{
    internal class Error : Exception
    {
        public Error(SourcePosition position, string message) : base(message)
        {
            Position = position;
        }

        public readonly SourcePosition Position;
    }

    internal class SyntaxError : Error
    {
        public SyntaxError(Token token) : base(token.Position, "Syntax error: " + token.ToString())
        { }
    }

    internal class UndefinedIdentifierError : Error
    {
        public UndefinedIdentifierError(Token identifier) : base(identifier.Position,
            "Undefined: " + identifier.ToString())
        { }
    }

    internal class MultipleIdentifierError : Error
    {
        public MultipleIdentifierError(Token identifier) : base(identifier.Position,
            "Multiple identifier: " + identifier.ToString())
        { }
    }

    internal class TypeMismatchError : Error
    {
        public TypeMismatchError(Token token) : base(token.Position, "Type mismatch")
        { }
    }

    internal class InvalidOperatorError : Error
    {
        public InvalidOperatorError(SourcePosition position, int operatorId) : base(position,
            "Invalid operator: " + ReservedWord.FromId(operatorId))
        { }

        public InvalidOperatorError(ReservedWord operatorToken) : this(operatorToken.Position, operatorToken.Id)
        { }
    }

    internal class MustBeConstantError : Error
    {
        public MustBeConstantError(SourcePosition position) : base(position, "Must be a constant.")
        { }
    }

    internal class MustBeBooleanError : Error
    {
        public MustBeBooleanError(SourcePosition position) : base(position, "Must be a boolean expression.")
        { }
    }
}
