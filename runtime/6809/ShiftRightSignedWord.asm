ext  @Temp@Word

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.ShiftRightSignedWord: public cate.ShiftRightSignedWord
    pshs b
        tstb
        do
        while ne
            lsr <@Temp@WordH
            ror <@Temp@WordL
            decb 
        wend
    puls b
rts