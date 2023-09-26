cseg
cate.ShiftSignedWordRight: public cate.ShiftSignedWordRight
    phs $2
        ld $2,$11
        an $2,&h80
        sbc $1,$sx
        do
        while nz
            bidw $11
            or $11,$2
            sb $1,$sy
        wend
    pps $2
rtn