cseg
cate.ShiftRightA: public cate.ShiftRightA
    push bc
        inc b
        dec b
        if nz
            do
                srl a
            dwnz
        endif
    pop bc
ret