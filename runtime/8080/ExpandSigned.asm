cseg
cate.ExpandSigned: public cate.ExpandSigned
    push psw
        mov l,a
        ani $80
        if nz
            mvi a,$ff
        endif
        mov h,a
    pop psw
ret