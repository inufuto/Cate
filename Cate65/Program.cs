using Inu.Language;

namespace Inu.Cate.Mos6502;

public class Program
{
    private enum CpuType
    {
        Mos6502,
        Wdc65C02,
    };

    public static int Main(string[] args)
    {
        var cpuType = CpuType.Mos6502;
        var normalArgument = new NormalArgument(args, (option, value) =>
        {
            cpuType = option switch
            {
                "6502" => CpuType.Mos6502,
                "65C02" => CpuType.Wdc65C02,
                _ => cpuType
            };
            return false;
        });
        var compiler = cpuType switch
        {
            CpuType.Wdc65C02 => new Wdc65c02.Compiler(),
            _ => new Compiler()
        };
        return compiler.Main(normalArgument);
    }
}