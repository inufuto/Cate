ext @Temp@Word

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.AddXAB: public cate.AddXAB
    stx <@Temp@Word
    addb <@Temp@WordL
    stab <@Temp@WordL
    adca <@Temp@WordH
    staa <@Temp@WordH
    ldx <@Temp@Word
rts
