cseg
cate.ShiftRightHl: public cate.ShiftRightHl
    push bc
        inc b
        dec b
        if nz
            do
                srl h
                rr l
            dwnz
        endif
    pop bc
ret