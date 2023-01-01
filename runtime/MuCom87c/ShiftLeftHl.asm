cseg
cate.ShiftLeftHl: public cate.ShiftLeftHl
; hl: value
; b: count
    push v | push b
        mov a,b
        ani a,15
        if sknz
            mov a,l | mov c,a
            mov a,h
            do
                push v
                    mov a,c | clc|ral | mov c,a
                pop v
                ral
                dcr b
                sknz | eqa a,a
            repeat
            mov h,a
            mov a,c | mov l,a
        endif
    pop b | pop v
ret