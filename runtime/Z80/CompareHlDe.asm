cseg
cate.CompareHlDe: public cate.CompareHlDe
    push bc
        ld b,a
            ld a,h
            cp d
            if z
                ld a,l
                cp e
            endif
        ld a,b
    pop bc
ret
