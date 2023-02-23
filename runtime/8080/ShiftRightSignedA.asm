cseg
cate.ShiftRightSignedA: public cate.ShiftRightSignedA
    inr b
    dcr b
    if nz
        push d
            push psw
                ani 80h
                mov e,a
            pop psw
            do
                ora a | rar
                dcr b
            while nz | wend
            ora e
        pop d
    endif
ret