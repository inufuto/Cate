using System;
using Inu.Language;

namespace Inu.Cate.Mos6502
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
