ext ZB0

cseg
cate.CompareSignedWord: public cate.CompareSignedWord 
; if dx < cx then cf = 1
    pushs a
        mv a,dh
        xor dh,ch
        test dh,80h
        mv dh,a
    pops a
    if z
        cmpw dx,cx
    else
        cmpw cx,dx
    endif
ret