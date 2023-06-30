ext ZB0
cate.signByte equ ZB0

cseg
cate.ShiftRightSignedA: public cate.ShiftRightSignedA
    mv (<cate.signByte),a
    and (<cate.signByte),80h
    cmp cl,0
    do
    while nz
        shr a
        or a,(<cate.signByte)
        dec cl
    wend
ret
