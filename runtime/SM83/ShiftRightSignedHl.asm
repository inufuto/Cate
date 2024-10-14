cseg
cate.ShiftRightSignedHl: public cate.ShiftRightSignedHl
    push bc
        inc b
        dec b
        if nz
            do
                sra h
                rr l
                dec b
            while nz | wend
        endif
    pop bc
ret