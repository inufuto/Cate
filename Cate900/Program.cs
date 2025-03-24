using Inu.Language;

namespace Inu.Cate.Tlcs900;

internal class Program
{
    public static int Main(string[] args)
    {
        var constantData = false;
        var normalArgument = new NormalArgument(args, (option, value) =>
        {
            if (option == "DSEG") {
                constantData = true;
            }
            return false;
        });
        return new Compiler(constantData).Main(normalArgument);
    }
}