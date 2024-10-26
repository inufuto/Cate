cseg
cate.ShiftLeftHl: public cate.ShiftLeftHl
    push bc
        inc b
        dec b
        if nz
            do
                sla l
                rl h
                dec b
            while nz | wend
        endif
    pop bc
ret