cseg
cate.ShiftRightSignedA: public cate.ShiftRightSignedA
    inc b
    dec b
    if nz
        do
            sra a
        dwnz
    endif
ret