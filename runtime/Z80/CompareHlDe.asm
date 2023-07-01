cseg
cate.CompareHlDe: public cate.CompareHlDe
    push bc
        ld b,a
            ld a,h
            cp d
            if nz
                ld a,l
                cp e
            endif
        ld b,c
    pop bc
ret
