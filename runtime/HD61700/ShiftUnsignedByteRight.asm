cseg
cate.ShiftUnsignedByteRight: public cate.ShiftUnsignedByteRight
    sbc $1,$sx
    do
    while nz
        bid $0
        sb $1,$sy
    wend
rtn
