using Inu.Language;

namespace Inu.Cate.Sm83;

internal class Program
{
    public static int Main(string[] args)
    {
        var normalArgument = new NormalArgument(args);
        return new Compiler().Main(normalArgument);
    }
}