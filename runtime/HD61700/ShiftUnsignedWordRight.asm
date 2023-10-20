cseg
cate.ShiftUnsignedWordRight: public cate.ShiftUnsignedWordRight
    sbc $1,$sx
    do
    while nz
        bidw $11
        sb $1,$sy
    wend
rtn
