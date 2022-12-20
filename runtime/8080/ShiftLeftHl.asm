cseg
cate.ShiftLeftHl: public cate.ShiftLeftHl
    inr b
    dcr b
    if nz
        push psw
            do
                mov a,l
                    ora a | ral
                mov l,a
                mov a,h
                    ral
                mov h,a
                dcr b
            while nz | wend
        pop psw
    endif
ret