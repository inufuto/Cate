ext cate.Temp

cseg
cate.ExpandSigned: public cate.ExpandSigned
    php
        sep #$20 | a8
        sta <cate.Temp
        stz <cate.Temp+1
        bit #$80
        if ne
            dec <cate.Temp+1
        endif
        rep #$20 | a16
        lda <cate.Temp
    plp
rts
