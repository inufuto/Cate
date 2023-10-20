namespace Inu.Cate.Hd61700
{
    internal class PointerOperation:Cate.PointerOperation
    {
        public override List<PointerRegister> Registers => WordPointerRegister.Registers;
    }
}
