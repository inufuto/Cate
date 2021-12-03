using System.IO;

namespace Inu.Cate
{
    interface ICode
    {
        void WriteTo(StreamWriter writer);
    }

    class CodeString : ICode
    {
        public string String { get; }

        public CodeString(string s)
        {
            String = s;
        }

        public override string ToString() => String;
        public void WriteTo(StreamWriter writer)
        {
            writer.WriteLine(String);
        }
    }
}