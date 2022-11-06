ext @Temp@Word

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.ShiftLeftWord: public cate.ShiftLeftWord
    pshb
        stx <@Temp@Word
        tstb
        do
        while ne
            asl <@Temp@WordL
            rol <@Temp@WordH
            decb 
        wend
        ldx <@Temp@Word
    pulb
rts