using Inu.Language;

namespace Inu.Cate.Wdc65816;

internal class Program
{
    static int Main(string[] args)
    {
        var normalArgument = new NormalArgument(args);
        return new Compiler().Main(normalArgument);
    }
}