using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Inu.Language;

namespace Inu.Cate
{
    class Tokenizer : Language.Tokenizer
    {
        private const char EscapeChar = '\\';
        private const char DoubleQuotation = '\"';
        private const char SingleQuotation = '\'';
        private const char EndOfLine = '\n';

        private readonly List<string> fileNames = new List<string>();

        public List<Token> GetTokens(string fileName)
        {
            List<Token> tokens = new List<Token>();
            OpenSourceFile(fileName);
            Token token;
            do {
                token = GetToken();
                tokens.Add(token);
            } while (!token.IsEof());
            return tokens;
        }

        protected override void SkipSpaces()
        {
            base.SkipSpaces();
            while (SkipComment() || DoInclude()) {
                base.SkipSpaces();
            }
        }

        private bool SkipComment()
        {
            if (LastChar != '/') return false;
            using var backup = new Backup(this);
            NextChar();
            switch (LastChar) {
                case '*': {
                        do {
                            NextChar();
                            if (LastChar == '*') {
                                NextChar();
                                if (LastChar == '/') {
                                    NextChar();
                                    break;
                                }
                            }
                        } while (LastChar != EndOfFile);

                        base.SkipSpaces();
                        return true;
                    }
                case '/': {
                        do {
                            NextChar();
                        } while (LastChar != EndOfLine && LastChar != EndOfFile);
                        base.SkipSpaces();
                        return true;
                    }
                default:
                    backup.Restore();
                    return false;
            }
        }

        private bool DoInclude()
        {
            const string directive = "include";
            static bool IsSpace(char c) => c != EndOfLine && char.IsWhiteSpace(c);
            static bool IsQuotation(char c) => c == DoubleQuotation || c == EndOfLine;

            SkipChars(IsSpace);
            if (LastChar != '#') return false;
            using var backup = new Backup(this);
            NextChar();
            SkipChars(IsSpace);
            var word = ReadWord();
            if (word == directive) {
                SkipChars(IsSpace);
                if (LastChar == DoubleQuotation) {
                    NextChar();
                    var fileName = ReadString(IsQuotation);
                    try {
                        OpenSourceFile(fileName);
                    }
                    catch (IOException e) {
                        Debug.Assert(SourceReader.Current != null);
                        string s = $"{SourceReader.Current.CurrentPosition}: {e.Message}";
                        Console.Error.WriteLine(s);
                    }
                    return true;
                }
            }
            backup.Restore();
            return false;
        }

        private int ReadHexValue() { return ReadNumericValue(16, IsHexDigit); }
        private int ReadOctValue() { return ReadNumericValue(8, IsOctDigit); }

        private int ReadNumericValue(int fromBase, Func<char, bool> predicate)
        {
            var s = new StringBuilder();
            while (predicate(LastChar)) {
                s.Append(LastChar);
                NextChar();
            }
            return Convert.ToInt32(s.ToString(), fromBase);
        }

        protected override bool IsNumericValueHead(char c)
        {
            return c == SingleQuotation || base.IsNumericValueHead(c);
        }

        private static bool IsHexHead(char c) { return char.ToUpper(c) == 'X'; }
        private static bool IsOctDigit(char c) { return c >= '0' && c <= '7'; }
        private static bool IsBinDigit(char c) { return c == '0' || c == '1'; }
        static bool IsOperatorChar(char c) { return "!=-&*/|+<>^".Contains(c); }

        protected override int ReadNumericValue()
        {
            switch (LastChar) {
                case '0':
                    NextChar();
                    switch (char.ToUpper(LastChar)) {
                        case 'X':
                            NextChar();
                            return ReadNumericValue(16, IsHexDigit);
                        case 'B':
                            NextChar();
                            return ReadNumericValue(2, IsBinDigit);
                        default:
                            ReturnChar('0');
                            return ReadNumericValue(8, IsOctDigit);
                    }
                case SingleQuotation: {
                        NextChar();
                        var s = ReadString(c => c == SingleQuotation);
                        return s.Length > 0 ? Encoding.ASCII.GetBytes(s)[0] : 0;
                    }
                default:
                    return ReadNumericValue(10, char.IsDigit);
            }
        }

        protected override char ReadChar()
        {
            if (LastChar != EscapeChar) {
                return base.ReadChar();
            }
            NextChar();
            if (IsHexHead(LastChar)) {
                NextChar();
                return (char)ReadHexValue();
            }
            if (IsOctDigit(LastChar)) {
                return (char)(ReadOctValue());
            }

            var c = LastChar;
            NextChar();
            return c;
        }

        protected override int ReadSequence()
        {
            var backup = new Backup(this);
            var word = ReadCharSequence(IsOperatorChar);
            var id = ReservedWord.ToId(word);
            if (id <= 0) {
                backup.Restore();
            }
            return id;
        }

        protected override bool IsSequenceHead(char c)
        {
            return IsOperatorChar(c);
        }

        protected override char ChangeCase(char c)
        {
            return c;
        }

        public override void OpenSourceFile(string fileName)
        {
            if (fileNames.Contains(fileName)) return;
            base.OpenSourceFile(fileName);
            fileNames.Add(fileName);
        }
    }
}
