ext @Temp@Word

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.ShiftRightSignedWord: public cate.ShiftRightSignedWord
    pshb
        stx <@Temp@Word
        tstb
        do
        while ne
            lsr <@Temp@WordH
            ror <@Temp@WordL
            decb 
        wend
        ldx <@Temp@Word
    pulb
rts