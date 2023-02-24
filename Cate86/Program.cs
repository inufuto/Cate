using Inu.Cate;
using Inu.Language;

namespace Inu.Cate.I8086
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            var normalArgument = new NormalArgument(args);
            return new Compiler().Main(normalArgument);
        }
    }
}
