ext ZB0

cseg
cate.CompareSignedByte: public cate.CompareSignedByte 
; if dl < dh then cf = 1
    pushs a
        mv a,dl
        xor dl,dh
        test dl,80h
        mv dl,a
    pops a
    if z
        cmp dl,dh
    else
        cmp dh,dl
    endif
ret