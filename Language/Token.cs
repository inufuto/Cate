using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inu.Language
{
    public enum TokenType
    {
        ReservedWord,
        Identifier,
        NumericValue,
        StringValue,
    }

    public abstract class Token
    {

        public SourcePosition Position { get; private set; }
        public TokenType Type { get; }
        protected Token(SourcePosition position, TokenType type)
        {
            Position = position;
            Type = type;
        }

        public virtual bool IsEof() => false;

        public virtual bool IsIdentifier() => false;


        public virtual bool IsReservedWord(int id) { return false; }

        protected static int AddWord(IDictionary<int, string> words, string word, ref int nextId)
        {
            var (id, _) = words.FirstOrDefault(p => p.Value == word);
            if (id > 0) return id;
            id = nextId++;
            words[id] = word;
            return id;
        }
    }

    public class ReservedWord : Token
    {
        public const int EndOfFile = 0;
        private const int MinId = 0x80;
        private static readonly IDictionary<int, string> Words = new Dictionary<int, string>();

        public static void AddWord(int id, string word)
        {
            Debug.Assert(!Words.ContainsKey(id) && Words.All(p => p.Value != word));
            Words[id] = word;
        }

        public static void AddWords(IDictionary<int, string> words)
        {
            foreach (var (id, word) in words) {
                AddWord(id, word);
            }
        }

        public ReservedWord(SourcePosition position, int id) : base(position, TokenType.ReservedWord)
        {
            Id = id;
        }

        public static string FromId(int id)
        {
            if (id < MinId) return "" + (char)id;
            if (Words.TryGetValue(id, out var word))
                return word;
            throw new NullReferenceException();
        }

        public readonly int Id;

        public override string ToString()
        {
            return Id == SourceReader.EndOfLine ? "end of line" : FromId(Id);
        }

        public override bool IsReservedWord(int id) => Id == id;

        public override bool IsIdentifier() => true;
        public override bool IsEof() => Id == EndOfFile;

        public static int ToId(string word)
        {
            return Words.FirstOrDefault(p => p.Value == word).Key;
        }
    }

    public class Identifier : Token
    {
        public const int MinId = 0x100;
        private static int nextId = MinId;
        private static readonly IDictionary<int, string> Words = new Dictionary<int, string>();

        public static int Add(string word)
        {
            return AddWord(Words, word, ref nextId);
        }

        public readonly int Id;

        protected Identifier(SourcePosition position, int id) : base(position, TokenType.Identifier)
        {
            Id = id;
        }

        public Identifier(SourcePosition position, string word) : this(position, Add(word))
        { }


        public override string ToString()
        {
            return Words.TryGetValue(Id, out var value) ? value : string.Empty;
        }

        public static string FromId(int id)
        {
            if (Words.TryGetValue(id, out var word))
                return word;
            throw new NullReferenceException();
        }

        public static bool IsIdentifierId(in int id)
        {
            return Words.ContainsKey(id);
        }
    }

    public class NumericValue : Token
    {
        public readonly int Value;

        public NumericValue(SourcePosition position, int value) : base(position, TokenType.NumericValue)
        {
            Value = value;
        }

        public override string ToString() => Value.ToString();
    }

    public class StringValue : Token
    {
        private const int MinId = 0x4000;
        private static int nextId = MinId;
        private static readonly IDictionary<int, string> Words = new Dictionary<int, string>();

        public readonly int Id;
        protected StringValue(SourcePosition position, int id) : base(position, TokenType.StringValue)
        {
            Id = id;
        }

        public StringValue(SourcePosition position, string word) : this(position, AddWord(Words, word, ref nextId))
        { }

        public override string ToString()
        {
            return Words.TryGetValue(Id, out var value) ? value : string.Empty;
        }

        public static string FromId(int id)
        {
            if (Words.TryGetValue(id, out var word))
                return word;
            throw new NullReferenceException();
        }
    }
}
