ext @Temp@Word

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.AddXB: public cate.AddXB
    stx <@Temp@Word
    addb <@Temp@WordL
    stab <@Temp@WordL
    ldab <@Temp@WordH
    adcb #0
    stab <@Temp@WordH
    ldx <@Temp@Word
rts
