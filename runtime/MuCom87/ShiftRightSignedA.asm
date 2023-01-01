ext @WB0
sign equ @WB0

cseg
cate.ShiftRightSignedA: public cate.ShiftRightSignedA
; a: value
; b: count
    push b
        push v
            ani a,$80
            staw sign
            mov a,b
            ani a,7
        pop v
        if sknz
            do
                shar
                dcr b
                skz
            repeat
        endif
        oraw sign
    pop b
ret