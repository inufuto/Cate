ext cate.CompareHlDe

cseg
cate.CompareHlDeSigned: public cate.CompareHlDeSigned
    push b
        mov b,a
            mov a,h
            ora a
            if p
                mov a,e
                ora a
                if p
                    call cate.CompareHlDe
                else
                    mvi a,1
                    cpi 0
                endif
            else
                mov a,d
                ora a
                if m
                    call cate.CompareHlDe
                else
                    xra a
                    cpi 1
                endif
            endif
        mov a,b
    pop b
ret
