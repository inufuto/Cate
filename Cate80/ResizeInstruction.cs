using System;
using System.Diagnostics;

namespace Inu.Cate.Z80
{
    internal class ResizeInstruction : Cate.ResizeInstruction
    {
        public ResizeInstruction(Function function, AssignableOperand destinationOperand, IntegerType destinationType,
            Operand sourceOperand, IntegerType sourceType) : base(function, destinationOperand, destinationType, sourceOperand, sourceType)
        { }


        protected override void Reduce()
        {
            if (SourceOperand.Register is WordRegister sourceRegister) {
                if (sourceRegister.IsPair()) {
                    Debug.Assert(sourceRegister.Low != null);
                    sourceRegister.Low.Store(this, DestinationOperand);
                    return;
                }
            }
            if (DestinationOperand.Register is ByteRegister destinationRegister) {
                var pairRegister = destinationRegister.PairRegister;
                if (pairRegister != null && Equals(pairRegister.Low, destinationRegister)) {
                    Debug.Assert(pairRegister.High != null);
                    if (!IsRegisterInUse(pairRegister.High)) {
                        pairRegister.Load(this, SourceOperand);
                        return;
                    }
                }
            }
            WordRegister.UsingAny(this, WordRegister.PairRegisters, temporaryRegister =>
            {
                Debug.Assert(temporaryRegister.Low != null);
                temporaryRegister.Load(this, SourceOperand);
                temporaryRegister.Low.Store(this, DestinationOperand);
            });
        }


        protected override void ExpandSigned()
        {
            void ToWord(Cate.WordRegister wordRegister)
            {
                Debug.Assert(wordRegister.Low != null && wordRegister.High != null);
                WriteLine("\tld\t" + wordRegister.Low.Name + ",a");
                WriteLine("\tadd\ta,a");
                WriteLine("\tsbc\ta,a");
                WriteLine("\tld\t" + wordRegister.High.Name + ",a");
                ChangedRegisters.Add(ByteRegister.A);
                RemoveRegisterAssignment(ByteRegister.A);
            }

            void ExpandA()
            {
                if (DestinationOperand.Register is WordRegister destinationRegister) {
                    if (destinationRegister.IsPair()) {
                        ToWord(destinationRegister);
                        return;
                    }
                }
                WordRegister.UsingAny(this, WordRegister.PairRegisters, temporaryRegister =>
                {
                    ToWord(temporaryRegister);
                    temporaryRegister.Store(this, DestinationOperand);
                });
            }

            if (Equals(SourceOperand.Register, ByteRegister.A)) {
                ExpandA();
                return;
            }
            ByteRegister.UsingAccumulator(this, () =>
            {
                ByteRegister.A.Load(this, SourceOperand);
                ExpandA();
            });
        }

        //protected override void Expand()
        //{
        //    void ToWord(Cate.ByteRegister byteRegister, Cate.WordRegister wordRegister)
        //    {
        //        Debug.Assert(wordRegister.Low != null && wordRegister.High != null);
        //        if (!Equals(wordRegister.Low, byteRegister)) {
        //            wordRegister.Low.CopyFrom(this, byteRegister);
        //        }
        //        wordRegister.High.LoadConstant(this,0);
        //    }

        //    if (SourceOperand is VariableOperand sourceVariableOperand) {
        //        var sourceVariable = sourceVariableOperand.Variable;
        //        var sourceOffset = sourceVariableOperand.Offset;
        //        if (sourceVariable.Register is ByteRegister sourceRegister) {
        //            Debug.Assert(sourceOffset == 0);
        //            var wordRegister = sourceRegister.PairRegister;
        //            if (wordRegister != null && sourceRegister.IsLow()) {
        //                Debug.Assert(wordRegister.High != null);
        //                if (!IsRegisterInUse(wordRegister.High)) {
        //                    BeginRegister(wordRegister.High);
        //                    ToWord(sourceRegister, wordRegister);
        //                    wordRegister.Store(this, DestinationOperand);
        //                    EndRegister(wordRegister.High);
        //                    return;
        //                }
        //            }
        //        }
        //    }
        //    if (DestinationOperand is VariableOperand destinationVariableOperand) {
        //        var destinationVariable = destinationVariableOperand.Variable;
        //        var destinationOffset = destinationVariableOperand.Offset;
        //        if (destinationVariable.Register is WordRegister destinationRegister) {
        //            Debug.Assert(destinationOffset == 0);
        //            if (destinationRegister.Low != null) {
        //                destinationRegister.Low.Load(this, SourceOperand);
        //                ToWord(destinationRegister.Low, destinationRegister);
        //                return;
        //            }
        //        }
        //    }
        //    WordRegister.UsingAny(this, WordRegister.PairRegisters, temporaryRegister =>
        //    {
        //        Debug.Assert(temporaryRegister.Low != null);
        //        temporaryRegister.Low.Load(this, SourceOperand);
        //        ToWord(temporaryRegister.Low, temporaryRegister);
        //        temporaryRegister.Store(this, DestinationOperand);
        //    });
        //    throw new NotImplementedException();
        //}
    }
}
