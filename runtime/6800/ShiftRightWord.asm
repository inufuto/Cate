ext @Temp@Word

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.ShiftRightWord: public cate.ShiftRightWord
    pshb
        stx <@Temp@Word
        tstb
        do
        while ne
            asr <@Temp@WordH
            ror <@Temp@WordL
            decb 
        wend
        ldx <@Temp@Word
    pulb
rts