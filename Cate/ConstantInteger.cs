using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Inu.Language;

namespace Inu.Cate
{
    public class ConstantInteger : Constant
    {
        public readonly int IntegerValue;

        public ConstantInteger(IntegerType type, int integerValue) : base(type)
        {
            IntegerValue = integerValue;
        }
        public ConstantInteger(int integerValue) : this(IntegerType.IntegerTypeOf(integerValue), integerValue)
        { }

        public new IntegerType Type => (IntegerType)base.Type;

        public override bool Equals(object? obj)
        {
            return obj is ConstantInteger constantInteger && IntegerValue == constantInteger.IntegerValue;
        }

        public override int GetHashCode()
        {
            return IntegerValue.GetHashCode();
        }

        private static readonly Dictionary<int, Func<int, int, int>> BinaryOperations =
            new Dictionary<int, Func<int, int, int>>
            {
                { '|', (left, right) => left | right },
                { '^', (left, right) => left ^ right },
                { '&', (left, right) =>  left & right },
                { Keyword.ShiftLeft, (left, right) =>  left << right },
                { Keyword.ShiftRight, (left, right) => left >> right },
                { '+', (left, right) => left + right },
                { '-', (left, right) => left - right },
                { '*', (left, right) => left* right },
                { '/', (left, right) => left / right },
                { '%', (left, right) => left % right },
            };
        public override Value? BinomialResult(SourcePosition position, int operatorId, Value rightValue)
        {
            if (!(rightValue is ConstantInteger rightConstant)) {
                return base.BinomialResult(position, operatorId, rightValue);
            }

            if (!BinaryOperations.TryGetValue(operatorId, out var operation))
                throw new InvalidOperatorError(position, operatorId);
            var result = operation(IntegerValue, rightConstant.IntegerValue);
            Debug.Assert(Compiler.Instance != null);
            return new ConstantInteger(result);
        }

        private static readonly Dictionary<int, Func<int, int>> UnaryOperations = new Dictionary<int, Func<int, int>>() {
            { '+', (value)=> value},
            { '-', (value)=> -value },
            { '~', (value)=> ~value  },
        };


        public override Value? MonomialResult(SourcePosition position, int operatorId)
        {
            if (!UnaryOperations.TryGetValue(operatorId, out var operation)) {
                throw new InvalidOperatorError(position, operatorId);
            }
            var result = operation(IntegerValue);
            return new ConstantInteger(result);
        }

        public override Value? ConvertTypeTo(Type type)
        {
            if (type is IntegerType integerType) {
                return Type.Equals(integerType) ? this : new ConstantInteger(integerType, IntegerValue);
            }
            return base.ConvertTypeTo(type); ;
        }

        public override Value? CastTo(Type type)
        {
            if (type is PointerType pointerType) {
                return new CastedConstantPointer(pointerType, IntegerValue);
            }
            return base.CastTo(type);
        }


        public override void WriteAssembly(StreamWriter writer)
        {
            if (Type.ByteCount > 1) {
                writer.WriteLine("\tdefw " + IntegerValue);
            }
            else {
                writer.WriteLine("\tdefb " + IntegerValue);
            }
        }

        public override Operand ToOperand()
        {
            return new IntegerOperand(Type, IntegerValue);
        }
    }
}