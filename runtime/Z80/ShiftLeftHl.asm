cseg
cate.ShiftLeftHl: public cate.ShiftLeftHl
    inc b
    dec b
    if nz
        do
            sla l
            rl h
        dwnz
    endif
ret