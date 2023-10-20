cseg
cate.ShiftSignedByteRight: public cate.ShiftSignedByteRight
    phs $2
        ld $2,$0
        an $2,&h80
        sbc $1,$sx
        do
        while nz
            bid $0
            or $0,$2
            sb $1,$sy
        wend
    pps $2
rtn