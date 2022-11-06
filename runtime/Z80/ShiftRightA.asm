cseg
cate.ShiftRightA: public cate.ShiftRightA
    inc b
    dec b
    if nz
        do
            srl a
        dwnz
    endif
ret