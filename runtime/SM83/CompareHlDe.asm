cseg
cate.CompareHlDe: public cate.CompareHlDe
    push bc
        ld b,a
            ld a,h
            cp a,d
            if z
                ld a,l
                cp a,e
            endif
        ld a,b
    pop bc
ret
