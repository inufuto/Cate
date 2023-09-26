cseg
; expand $0 to $10
cate.ExpandSigned: public cate.ExpandSigned
    ld $10,$0
    anc $10,&h80
    if z
        ld $11,$sx
    else
        ld $11,-1
    endif
rtn
