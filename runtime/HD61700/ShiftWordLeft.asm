cseg
cate.ShiftWordLeft: public cate.ShiftWordLeft
    sbc $1,$sx
    do
    while nz
        biuw $10
        sb $1,$sy
    wend
rtn