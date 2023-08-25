cseg
cate.ExpandSigned: public cate.ExpandSigned
    pushs a
        test a,80h
        if z
            mv a,0
        else
            mv a,0ffh
        endif
        ex a,b
    pops a
ret
