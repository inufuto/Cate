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
        var parameterRegister = false;
        var normalArgument = new NormalArgument(args, (option, value) =>
        {
            switch (option) {
                case "6502":
                    cpuType = CpuType.Mos6502;
                    break;
                case "65C02":
                    cpuType = CpuType.Wdc65C02;
                    break;
                case "V2":
                    parameterRegister = true;
                    break;
            }

            return false;
        });
        var compiler = cpuType switch
        {
            CpuType.Wdc65C02 => new Wdc65c02.Compiler(parameterRegister),
            _ => new Compiler(parameterRegister)
        };
        return compiler.Main(normalArgument);
    }
}