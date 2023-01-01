cseg
cate.ShiftLeftDe1: public cate.ShiftLeftDe1
    push v | push b
        mov a,e | mov c,a
        mov a,d
        shcl | ral
        mov d,a
        mov a,c | mov e,a
    pop b | pop v
ret