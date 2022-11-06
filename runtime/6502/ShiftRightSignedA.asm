    ext ZB0
    sign equ ZB0

cseg
cate.ShiftRightSignedA: public cate.ShiftRightSignedA
    pha
        and #$80
        sta <sign
    pla
    cpy #0
    do
    while ne
        lsr a
        ora <sign
        dey
    wend
rts