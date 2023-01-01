cseg
cate.ShiftRightA: public cate.ShiftRightA
; a: value
; b: count
    push b
        push v
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
    pop b
ret