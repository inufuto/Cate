cseg
cate.ShiftRightSignedA: public cate.ShiftRightSignedA
    push bc
        inc b
        dec b
        if nz
            do
                sra a
            dwnz
        endif
    pop bc
ret