cseg
cate.ShiftLeftA: public cate.ShiftLeftA
    push bc
        inc b
        dec b
        if nz
            do
                sla a
                dec b
            while nz | wend
        endif
    pop bc
ret