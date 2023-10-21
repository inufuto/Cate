cseg
cate.ShiftByteLeft: public cate.ShiftByteLeft
    sbc $1,$sx
    do
    while nz
        biu $0
        sb $1,$sy
    wend
rtn