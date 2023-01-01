cseg
cate.ShiftLeftA: public cate.ShiftLeftA
; a: value
; b: count
    push b
        push v
            mov a,b
            ani a,7
        pop v
        if sknz
            do
                clc|ral
                dcr b
                sknz | eqa a,a
            repeat
        endif
    pop b
ret