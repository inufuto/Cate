cseg
cate.ShiftLeftByte: public cate.ShiftLeftByte
; r1,r3
    push r3
        or r3,r3
        do | while nz
            sll r1
            dec r3
        wend
    pop r3
ret
