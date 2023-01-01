cseg
cate.MultiplyHlC: public cate.MultiplyHlC
    push v | push d | push b
        mov a,l | mov e,a
        mov a,h | mov d,a
        lxi h,0
        do
            mov a,c
        while nei a,0
            push v
                mov a,c
                clc|rar
                mov c,a
            pop v
            if sknc
            else
                mov a,l | add a,e | mov l,a
                mov a,h | adc a,d | mov h,a
            endif
            mov a,e | clc|ral | mov e,a
            mov a,d | ral | mov d,a
        wend
    pop b | pop d | pop v
ret
