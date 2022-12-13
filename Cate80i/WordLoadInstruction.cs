namespace Inu.Cate.I8080
{
    internal class WordLoadInstruction : Cate.WordLoadInstruction
    {
        public WordLoadInstruction(Function function, AssignableOperand destinationOperand, Operand sourceOperand) : base(function, destinationOperand, sourceOperand) { }

        public override void BuildAssembly()
        {
            if (SourceOperand is ConstantOperand constantOperand) {
                if (DestinationOperand is IndirectOperand destinationIndirectOperand) {
                    void ViaHl()
                    {
                        var low = Compiler.LowByteOperand(constantOperand);
                        var high = Compiler.HighByteOperand(constantOperand);
                        WriteLine("\tmvi\tm," + low);
                        WriteLine("\tinx\th");
                        WriteLine("\tmvi\tm," + high);
                        ChangedRegisters.Add(WordRegister.Hl);
                        RemoveRegisterAssignment(WordRegister.Hl);
                    }

                    var pointer = destinationIndirectOperand.Variable;
                    var offset = destinationIndirectOperand.Offset;
                    var pointerRegister = GetVariableRegister(pointer, 0);
                    if (Equals(pointerRegister, WordRegister.Hl)) {
                        if (offset == 0) {
                            ViaHl();
                        }
                        else {
                            WordRegister.Hl.TemporaryOffset(this, offset, ViaHl);
                        }
                        return;
                    }
                    WordOperation.UsingRegister(this, WordRegister.Hl, () =>
                    {
                        if (pointerRegister != null) {
                            WordRegister.Hl.CopyFrom(this, (Cate.WordRegister)pointerRegister);
                        }
                        else {
                            WordRegister.Hl.LoadFromMemory(this, pointer, 0);
                        }
                        if (offset == 0) {
                            ViaHl();
                            return;
                        }
                        WordRegister.Hl.TemporaryOffset(this, offset, ViaHl);
                    });
                    return;
                }
            }
            base.BuildAssembly();
        }
    }
}
