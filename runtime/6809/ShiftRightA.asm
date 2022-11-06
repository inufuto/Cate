cseg
cate.ShiftRightA: public cate.ShiftRightA
    pshs b
        tstb
        do
        while ne
            lsra
            decb
        wend
    puls b
rts