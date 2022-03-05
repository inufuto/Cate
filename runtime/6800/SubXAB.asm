ext @Temp@Word
ext @Temp@Word2

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1
@Temp@Word2H equ @Temp@Word2
@Temp@Word2L equ @Temp@Word2+1

cseg
cate.SubXAB: public cate.SubXAB
    stx <@Temp@Word
    staa <@Temp@Word2H
    stab <@Temp@Word2L
    ldaa <@Temp@WordL
    suba <@Temp@Word2L
    staa <@Temp@WordL
    ldaa <@Temp@WordH
    sbca <@Temp@Word2H
    staa <@Temp@WordH
    ldx <@Temp@Word
rts
