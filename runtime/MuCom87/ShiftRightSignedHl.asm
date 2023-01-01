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
            mov a,l | mov c,a
            mov a,h
            push v
                ani a,$80
                staw sign
            pop v
            do
                shar | rcr
                dcr b
                skz
            repeat
            oraw sign
            mov h,a
            mov a,c | mov l,a
        endif
    pop b | pop v
ret