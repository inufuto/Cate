using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Inu.Language
{
    public abstract class Tokenizer
    {
        public const char EndOfFile = SourceReader.EndOfFile;

        protected class Backup : IDisposable
        {
            private readonly Tokenizer tokenizer;
            private readonly List<char> chars = new List<char>();

            public Backup(Tokenizer tokenizer)
            {
                this.tokenizer = tokenizer;
                tokenizer.Add(this);
            }

            public void Dispose()
            {
                tokenizer.Remove(this);
            }

            public void Restore()
            {
                for (var i = chars.Count - 1; i >= 0; --i) {
                    tokenizer.ReturnChar(chars[i]);
                }
            }

            public void Add(char c)
            {
                chars.Add(c);
            }
        }


        public char LastChar { get; private set; } = EndOfFile;
        private SourcePosition lastPosition;
        private readonly Stack<char> lastChars = new Stack<char>();
        private readonly Stack<char> returnedChars = new Stack<char>();
        private readonly List<Backup> backups = new List<Backup>();

        public virtual void OpenSourceFile(string fileName)
        {
            string modifiedFileName;
            if (SourceReader.Current != null) {
                modifiedFileName = SourceReader.Current.Directory + Path.DirectorySeparatorChar + fileName;
            }
            else {
                modifiedFileName = fileName;
            }
            SourceReader.OpenFile(modifiedFileName);
            if (LastChar != 0) {
                lastChars.Push(LastChar);
            }
            NextChar();
        }

        public Token GetToken()
        {
            SkipSpaces();

            var position = lastPosition;
            var c = LastChar;
            if (IsQuotation(c)) {
                var s = ReadQuotedString();
                return new StringValue(position, s);
            }
            if (IsNumericValueHead(c)) {
                var value = ReadNumericValue();
                return new NumericValue(position, value);
            }
            if (IsIdentifierHead(c)) {
                var word = ReadWord();
                var id = ReservedWord.ToId(word);
                if (id > 0) {
                    return new ReservedWord(position, id);
                }
                return new Identifier(position, word);
            }
            if (IsSequenceHead(c)) {
                var id = ReadSequence();
                if (id > 0) {
                    return new ReservedWord(position, id);
                }
            }
            NextChar();
            return new ReservedWord(position, c);
        }

        private void Add(Backup backup)
        {
            backups.Add(backup);
        }

        private void Remove(Backup backup)
        {
            backups.Remove(backup);
        }

        protected void SkipChars(Func<char, bool> predicate)
        {
            while (LastChar > 0 && predicate(LastChar)) {
                NextChar();
            }
        }

        protected virtual void SkipSpaces()
        {
            SkipChars(IsSpace);
        }

        protected char NextChar()
        {
            foreach (var backup in backups) {
                backup.Add(LastChar);
            }
            if (returnedChars.Count > 0) {
                LastChar = returnedChars.Pop();
                return LastChar;
            }
            do {
                if (SourceReader.Current == null) {
                    return '\0';
                }
                lastPosition = SourceReader.Current.CurrentPosition;
                LastChar = SourceReader.Current.GetChar();
                if (LastChar == 0 && lastChars.Count > 0) {
                    LastChar = lastChars.Pop();
                }
            } while (LastChar == 0);
            return LastChar;
        }
        protected void ReturnChar(char c)
        {
            returnedChars.Push(LastChar);
            LastChar = c;
        }
        protected void ReturnChars(string s)
        {
            foreach (var c in s.Reverse()) {
                ReturnChar(c);
            }
        }

        protected string ReadString(Func<char, bool> isEnd)
        {
            var s = new StringBuilder();
            while (LastChar >= ' ' && !isEnd(LastChar)) {
                s.Append(ReadChar());
            }
            NextChar();
            return s.ToString();
        }

        protected virtual char ReadChar()
        {
            var c = LastChar;
            NextChar();
            return c;
        }

        private string ReadQuotedString()
        {
            var endChar = LastChar;
            NextChar();
            var s = ReadString(c => c == endChar);
            return s;
        }

        protected string ReadCharSequence(Func<char, bool> predicate)
        {
            var s = new StringBuilder();
            var c = ChangeCase(LastChar);
            do {
                s.Append(c);
                NextChar();
            } while (predicate((c = ChangeCase(LastChar))));
            return s.ToString();
        }

        protected string ReadWord(Func<char, bool> function)
        {
            var s = new StringBuilder();
            var c = ChangeCase(LastChar);
            do {
                s.Append(c);
                c = ChangeCase(NextChar());
            } while (function(c));
            return s.ToString();
        }

        protected string ReadWord()
        {
            return ReadWord(IsIdentifierElement);
        }

        protected virtual char ChangeCase(char c)
        {
            return char.ToUpper(c);
        }


        protected virtual bool IsSpace(char c)
        {
            return char.IsWhiteSpace(c);
        }
        protected virtual bool IsQuotation(char c)
        {
            return c == '\"';
        }
        protected virtual bool IsNumericValueHead(char c)
        {
            return char.IsDigit(c);
        }
        protected virtual bool IsIdentifierHead(char c)
        {
            char upper = char.ToUpper(c);
            return (upper >= 'A' && upper <= 'Z') || c == '_';
        }
        protected virtual bool IsIdentifierElement(char c)
        {
            return IsIdentifierHead(c) || char.IsDigit(c);
        }
        protected virtual bool IsSequenceHead(char c)
        {
            return false;
        }

        protected static bool IsHexDigit(char c)
        {
            return char.IsDigit(c) || (char.ToUpper(c) >= 'A' && char.ToUpper(c) <= 'F');
        }

        protected abstract int ReadNumericValue();

        protected int ReadDecValue()
        {
            string s = ReadWord(char.IsDigit);
            return int.Parse(s);
        }

        protected virtual int ReadSequence()
        {
            var c = LastChar;
            var nextChar = NextChar();
            var word = new string(new[] { c, nextChar });
            var id = ReservedWord.ToId(word);
            if (id > 0) return id;
            ReturnChar(nextChar);
            return id;
        }
    }
}
