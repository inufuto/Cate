cseg
cate.ShiftLeftWord: public cate.ShiftLeftWord
    cmp cl,0
    do
    while nz
        shl (px)
        rol (px+1)
        dec cl
    wend
ret