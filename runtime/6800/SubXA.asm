ext @Temp@Word
ext @Temp@Byte

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.SubXA: public cate.SubXA
    stx <@Temp@Word
    staa <@Temp@Byte
    ldaa <@Temp@WordL    
    suba <@Temp@Byte
    staa <@Temp@WordL
    ldaa <@Temp@WordH
    sbca #0
    staa <@Temp@WordH
    ldx <@Temp@Word
rts
