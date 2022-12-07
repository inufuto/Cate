cseg
cate.ExpandSigned: public cate.ExpandSigned
    ; dect r10 | mov r11,*r10
    dect r10 | mov r1,*r10
        mov r0,r1
        andi r1,>80
        if ne
            li r1,>ff00
        endif
        or r1,r0
    mov *r10+,r1
    ; mov *r10+,r11
rt
