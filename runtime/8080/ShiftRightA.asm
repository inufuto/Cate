cseg
cate.ShiftRightA: public cate.ShiftRightA
    inr b
    dcr b
    if nz
        do
            ora a | rar
            dcr b
        while nz | wend
    endif
ret