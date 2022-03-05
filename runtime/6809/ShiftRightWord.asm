ext  @Temp@Word

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.ShiftRightWord: public cate.ShiftRightWord
    pshs b
        tstb
        do
        while ne
            asr <@Temp@WordH
            ror <@Temp@WordL
            decb 
        wend
    puls b
rts