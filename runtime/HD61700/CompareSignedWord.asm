cseg
; if $10 < $12 then c = 1
cate.CompareSignedWord: public cate.CompareSignedWord 
    phs $0
        xr $0,$13
        anc $0,&h80
    pps $0
    if z
        sbc $10,$12
    else
        sbc $12,$10
    endif
rtn
