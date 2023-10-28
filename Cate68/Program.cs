using Inu.Language;

namespace Inu.Cate.Mc6800
{
    public class Program
    {
        private enum CpuType
        {
            Mc6800,
            Mc6801,
        };
        public static int Main(string[] args)
        {

            var cpuType = CpuType.Mc6800;
            var normalArgument = new NormalArgument(args, (option, value) =>
            {
                if (option == "6801") cpuType = CpuType.Mc6801;
                return false;
            });
            var compiler = cpuType switch
            {
                CpuType.Mc6801 => new Mc6801.Compiler(),
                _ => new Mc6800.Compiler()
            };
            return compiler.Main(normalArgument);
        }
    }
}
