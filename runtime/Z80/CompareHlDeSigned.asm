cseg
CompareHlDe:
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

cate.CompareHlDeSigned: public cate.CompareHlDeSigned
    push bc
        ld b,a
            ld a,h
            or a
            if p
                ld a,e
                or a
                if p
                    call CompareHlDe
                else
                    ld a,1
                    cp 0
                endif
            else
                ld a,d
                or a
                if m
                    call CompareHlDe
                else
                    xor a
                    cp 1
                endif
            endif
        ld a,b
    pop bc
ret
