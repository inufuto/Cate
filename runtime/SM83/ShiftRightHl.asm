cseg
cate.ShiftRightHl: public cate.ShiftRightHl
    push bc
        inc b
        dec b
        if nz
            do
                srl h
                rr l
                dec b
            while nz | wend
        endif
    pop bc
ret