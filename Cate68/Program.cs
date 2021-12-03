using Inu.Language;

namespace Inu.Cate.Mc6800
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var normalArgument = new NormalArgument(args);
            return new Compiler().Main(normalArgument);
        }
    }
}
