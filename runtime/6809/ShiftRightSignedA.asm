cseg
cate.ShiftRightSignedA: public cate.ShiftRightSignedA
    pshs b
        tstb
        do
        while ne
            asra
            decb
        wend
    puls b
rts