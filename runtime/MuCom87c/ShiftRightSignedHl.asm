ext @WB0
sign equ @WB0

cseg
cate.ShiftRightSignedHl: public cate.ShiftRightSignedHl
; hl: value
; b: count
    push v | push b
        mov a,b
        ani a,15
        if sknz
            push d
                mov a,l | mov c,a
                mov a,h
                push v
                    ani a,$80
                    mov d,a
                pop v
                do
                    clc|rar
                    push v
                        mov a,c
                        rar
                        mov c,a
                    pop v
                    dcr b
                    sknz | eqa a,a
                repeat
                ora a,d
                mov h,a
                mov a,c | mov l,a
            pop d
        endif
    pop b | pop v
ret