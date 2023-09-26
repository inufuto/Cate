cseg
; if $0 < $1 then c = 1
cate.CompareSignedByte: public cate.CompareSignedByte 
    phs $0
        xr $0,$1
        anc $0,&h80
    pps $0
    if z
        sbc $0,$1
    else
        sbc $1,$0
    endif
rtn