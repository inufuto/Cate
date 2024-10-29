using Inu.Cate.Sm85;
using Inu.Language;

namespace Cate85;

internal class Program
{
    public static int Main(string[] args)
    {
        var normalArgument = new NormalArgument(args);
        return new Compiler().Main(normalArgument);
    }
}