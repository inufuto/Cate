    ext ZB0
    sign equ ZB0

cseg
cate.ShiftRightSignedWord: public cate.ShiftRightSignedWord
    pha
        lda 1,x
        and #$80
        sta <sign
        lda 1,x
        cpy #0
        do
        while ne
            lsr a
            ora <sign
            ror 0,x
            dey 
        wend
        sta 1,x
    pla    
rts