cseg
cate.ShiftRightSignedHl: public cate.ShiftRightSignedHl
    inr b
    dcr b
    if nz
        push psw | push d
                mov a,h
                ani 80h
                mov e,a
            pop psw
            do
                mov a,h
                    ora a | rar
                mov h,a
                mov a,l
                    rar
                mov l,a
                dcr b
            while nz | wend
            mov a,h
                ora e
            mov h,a
        pop d | pop psw
    endif
ret