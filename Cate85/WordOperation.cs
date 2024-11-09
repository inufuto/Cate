namespace Inu.Cate.Sm85;

internal class WordOperation:Cate.WordOperation
{
    public override List<Cate.WordRegister> Registers => WordRegister.Registers.Union(WordRegisterFile.Registers).ToList();
}