cseg
cate.CompareAcSigned: public cate.CompareAcSigned
    push de
        ld d,a
            bit 7,a
            if nz
                bit 7,e
                if nz
                    cp a,e
                else
                    xor a,a
                    cp a,1
                endif
            else
                bit 7,e
                if z
                    cp a,e
                else
                    ld a,1
                    cp a,0
                endif
            endif
        ld a,d
    pop de
ret
