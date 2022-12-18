cseg
cate.CompareAESigned: public cate.CompareAESigned
    push d
        ora a
        if p
            mov d,a
            mov a,e
            ora a
            mov a,d
            if p
                cmp e
            else
                mvi a,1
                cpi 0
                mov a,d
            endif
        else
            mov d,a
            mov a,e
            ora a
            mov a,d
            if m
                cmp e
            else
                xra a
                cpi 1
                mov a,d
            endif
        endif
    pop d
ret
