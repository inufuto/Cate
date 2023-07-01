cseg
cate.CompareHlDe: public cate.CompareHlDe
    push bc
        ld c,a
            ld a,h
            cp d
            if nz
                ld a,l
                cp e
            endif
        ld a,c
    pop bc
ret
