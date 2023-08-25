cseg
cate.ShiftLeftA: public cate.ShiftLeftA
    push bc
        inc b
        dec b
        if nz
            do
                sla a
            dwnz
        endif
    pop bc
ret