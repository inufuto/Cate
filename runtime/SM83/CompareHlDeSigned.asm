cseg
CompareHlDe:
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

cate.CompareHlDeSigned: public cate.CompareHlDeSigned
    push bc
        ld b,a
            bit 7,h
            if nz
                bit 7,e
                if nz
                    call CompareHlDe
                else
                    ld a,1
                    cp a,0
                endif
            else
                bit 7,d
                if nz
                    call CompareHlDe
                else
                    xor a,a
                    cp a,1
                endif
            endif
        ld a,b
    pop bc
ret
