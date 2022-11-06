ext @Temp@Word
ext @Temp@Byte

@Temp@WordH equ @Temp@Word
@Temp@WordL equ @Temp@Word+1

cseg
cate.SubXB: public cate.SubXB
    stx <@Temp@Word
    stab <@Temp@Byte
    ldab <@Temp@WordL    
    subb <@Temp@Byte
    stab <@Temp@WordL
    ldab <@Temp@WordH
    sbcb #0
    stab <@Temp@WordH
    ldx <@Temp@Word
rts
