ext @WB0
sign equ @WB0

cseg
cate.ShiftRightSignedA: public cate.ShiftRightSignedA
; a: value
; b: count
    push b
        push v
            ani a,$80
            mov c,a
            mov a,b
            ani a,7
        pop v
        if sknz
            do
                clc|rar
                dcr b
                sknz | eqa a,a
            repeat
        endif
        ora a,c
    pop b
ret