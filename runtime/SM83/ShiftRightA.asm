cseg
cate.ShiftRightA: public cate.ShiftRightA
    push bc
        inc b
        dec b
        if nz
            do
                srl a
                dec b
            while nz | wend
        endif
    pop bc
ret