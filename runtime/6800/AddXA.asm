ext @Temp@Word

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.AddXA: public cate.AddXA
    stx <@Temp@Word
    adda <@Temp@WordL
    staa <@Temp@WordL
    ldaa <@Temp@WordH
    adca #0
    staa <@Temp@WordH
    ldx <@Temp@Word
rts
