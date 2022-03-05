cseg
cate.ShiftRightSignedHl: public cate.ShiftRightSignedHl
    inc b
    dec b
    if nz
        do
            sra h
            rr l
        dwnz
    endif
ret