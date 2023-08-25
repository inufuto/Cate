cseg
cate.ShiftLeftA: public cate.ShiftLeftA
    cmp cl,0
    do
    while nz
        shl a
        dec cl
    wend
ret