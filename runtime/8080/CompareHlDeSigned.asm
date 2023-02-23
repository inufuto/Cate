ext cate.CompareHlDe

cseg

CompareHlDe:
    push b
        mov b,a
            mov a,h
            cmp d
            if z
                mov a,l
                cmp e                
            endif
        mov a,b
    pop b
ret

cate.CompareHlDeSigned: public cate.CompareHlDeSigned
    push b
        mov b,a
            mov a,h
            ora a
            if p
                mov a,e
                ora a
                if p
                    call CompareHlDe
                else
                    mvi a,1
                    cpi 0
                endif
            else
                mov a,d
                ora a
                if m
                    call CompareHlDe
                else
                    xra a
                    cpi 1
                endif
            endif
        mov a,b
    pop b
ret
