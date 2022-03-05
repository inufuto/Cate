cseg
cate.ShiftLeftA: public cate.ShiftLeftA
    pshs b
        tstb
        do
        while ne
            asla
            decb
        wend
    puls b
rts