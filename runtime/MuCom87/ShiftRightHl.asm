cseg
cate.ShiftRightHl: public cate.ShiftRightHl
; hl: value
; b: count
    push v | push b
        mov a,b
        ani a,15
        if sknz
            mov a,l | mov c,a
            mov a,h
            do
                shar | rcr
                dcr b
                skz
            repeat
            mov h,a
            mov a,c | mov l,a
        endif
    pop b | pop v
ret