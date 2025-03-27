using Inu.Language;

namespace Inu.Cate.Tlcs900;

internal class Program
{
    public static int Main(string[] args)
    {
        var normalArgument = new NormalArgument(args);
        return new Compiler().Main(normalArgument);
    }
}