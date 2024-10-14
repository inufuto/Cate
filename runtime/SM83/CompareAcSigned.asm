cseg
cate.CompareAcSigned: public cate.CompareAcSigned
    push bc
        ld b,a
            bit 7,a
            if nz
                bit 7,c
                if nz
                    cp a,c
                else
                    xor a,a
                    cp a,1
                endif
            else
                bit 7,d
                if z
                    cp a,c
                else
                    ld a,1
                    cp a,0
                endif
            endif
        ld a,b
    pop bc
ret
