ext  @Temp@Word

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.ShiftLeftWord: public cate.ShiftLeftWord
    pshs b
        tstb
        do
        while ne
            asl <@Temp@WordL
            rol <@Temp@WordH
            decb 
        wend
    puls b
rts