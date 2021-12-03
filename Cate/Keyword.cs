using System.Collections.Generic;
using Inu.Language;

namespace Inu.Cate
{
    public static class Keyword
    {
        private const int MinId = Identifier.MinId;

        public const int Break = MinId + 0;
        public const int Case = MinId + 1;
        public const int Const = MinId + 2;
        public const int Continue = MinId + 3;
        public const int Default = MinId + 4;
        public const int Do = MinId + 5;
        public const int Else = MinId + 6;
        public const int Extern = MinId + 7;
        public const int For = MinId + 8;
        public const int Goto = MinId + 9;
        public const int If = MinId + 10;
        public const int Return = MinId + 11;
        public const int Sizeof = MinId + 12;
        public const int Static = MinId + 13;
        public const int Struct = MinId + 14;
        public const int Switch = MinId + 15;
        public const int Void = MinId + 16;
        public const int While = MinId + 17;
        public const int Bool = MinId + 18;
        public const int ConstExpr = MinId + 19;
        public const int False = MinId + 20;
        public const int NullPtr = MinId + 21;
        public const int True = MinId + 22;
        public const int Byte = MinId + 23;
        public const int Ptr = MinId + 24;
        public const int Repeat = MinId + 25;
        public const int SByte = MinId + 26;
        public const int SWord = MinId + 27;
        public const int Word = MinId + 28;
        public const int NotEqual = MinId + 29;
        public const int Decrement = MinId + 30;
        public const int ModuloAssign = MinId + 31;
        public const int LogicalAnd = MinId + 32;
        public const int LogicalAndAssign = MinId + 33;
        public const int AndAssign = MinId + 34;
        public const int MultiplyAssign = MinId + 35;
        public const int DivideAssign = MinId + 36;
        public const int XorAssign = MinId + 37;
        public const int LogicalOr = MinId + 38;
        public const int LogicalOrAssign = MinId + 39;
        public const int OrAssign = MinId + 40;
        public const int Increment = MinId + 41;
        public const int AddAssign = MinId + 42;
        public const int ShiftLeft = MinId + 43;
        public const int ShiftLeftAssign = MinId + 44;
        public const int LessEqual = MinId + 45;
        public const int SubtractAssign = MinId + 46;
        public const int Equal = MinId + 47;
        public const int Arrow = MinId + 48;
        public const int GreaterEqual = MinId + 49;
        public const int ShiftRight = MinId + 50;
        public const int ShiftRightAssign = MinId + 51;

        public static readonly Dictionary<int, string> Words = new Dictionary<int, string>()
        {
            { Break,"break"},
            { Case,"case"},
            { Const,"const"},
            { Continue,"continue"},
            { Default,"default"},
            { Do,"do"},
            { Else,"else"},
            { Extern,"extern"},
            { For,"for"},
            { Goto,"goto"},
            { If,"if"},
            { Return,"return"},
            { Sizeof,"sizeof"},
            { Static,"static"},
            { Struct,"struct"},
            { Switch,"switch"},
            { Void,"void"},
            { While,"while"},
            { Bool,"bool"},
            { ConstExpr,"constexpr"},
            { False,"false"},
            { NullPtr,"nullptr"},
            { True,"true"},
            { Byte,"byte"},
            { Ptr,"ptr"},
            { Repeat,"repeat"},
            { SByte,"sbyte"},
            { SWord,"sword"},
            { Word,"word"},
            { NotEqual,"!="},
            { Decrement,"--"},
            { ModuloAssign,"%="},
            { LogicalAnd,"&&"},
            { LogicalAndAssign,"&&="},
            { AndAssign,"&="},
            { MultiplyAssign,"*="},
            { DivideAssign,"/="},
            { XorAssign,"^="},
            { LogicalOr,"||"},
            { LogicalOrAssign,"||="},
            { OrAssign,"|="},
            { Increment,"++"},
            { AddAssign,"+="},
            { ShiftLeft,"<<"},
            { ShiftLeftAssign,"<<="},
            { LessEqual,"<="},
            { SubtractAssign,"-="},
            { Equal,"=="},
            { Arrow,"->"},
            { GreaterEqual,">="},
            { ShiftRight,">>"},
            { ShiftRightAssign,">>="},
        };
    }
}
