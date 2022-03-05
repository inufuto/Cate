cseg
cate.ShiftRightHl: public cate.ShiftRightHl
    inc b
    dec b
    if nz
        do
            srl h
            rr l
        dwnz
    endif
ret