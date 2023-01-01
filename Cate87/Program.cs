using Inu.Language;

namespace Inu.Cate.MuCom87
{
    public class Program
    {
        private enum CpuType
        {
            MuPd7800,
            MuPd7805,
        };

        public static int Main(string[] args)
        {
            var cpuType = CpuType.MuPd7800;
            var normalArgument = new NormalArgument(args, (option, value) =>
            {
                cpuType = option switch
                {
                    "7801" => CpuType.MuPd7800,
                    "7805" => CpuType.MuPd7805,
                    _ => cpuType
                };
                return false;
            });
            Inu.Cate.MuCom87.Compiler compiler;
            if (cpuType == CpuType.MuPd7805)
                compiler = new Inu.Cate.MuCom87.MuPd7805.Compiler();
            else
                compiler = new Inu.Cate.MuCom87.MuPD7800.Compiler();
            return compiler.Main(normalArgument);
        }
    }
}
