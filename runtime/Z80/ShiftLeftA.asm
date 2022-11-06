cseg
cate.ShiftLeftA: public cate.ShiftLeftA
    inc b
    dec b
    if nz
        do
            sla a
        dwnz
    endif
ret