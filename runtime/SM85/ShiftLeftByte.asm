cseg
cate.ShiftLeftByte: public cate.ShiftLeftByte
; r0,r1
    push r1
        or r1,r1
        do | while nz
            sll r0
            dec r1
        wend
    pop r1
ret
