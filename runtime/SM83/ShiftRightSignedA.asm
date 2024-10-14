cseg
cate.ShiftRightSignedA: public cate.ShiftRightSignedA
    push bc
        inc b
        dec b
        if nz
            do
                sra a
                dec b
            while nz | wend
        endif
    pop bc
ret