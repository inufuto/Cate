cseg
cate.ShiftLeftA: public cate.ShiftLeftA
    inr b
    dcr b
    if nz
        do
            ora a | ral
            dcr b
        while nz | wend
    endif
ret