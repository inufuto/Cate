cseg
cate.ShiftRightHl: public cate.ShiftRightHl
    inr b
    dcr b
    if nz
        push psw
            do
                mov a,h
                    ora a | rar
                mov h,a
                mov a,l
                    rar
                mov l,a
                dcr b
            while nz | wend
        pop psw
    endif
ret