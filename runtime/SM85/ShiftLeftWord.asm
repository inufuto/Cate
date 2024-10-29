cseg
cate.ShiftLeftWord: public cate.ShiftLeftWord
; rr0,r3
    push r3
        or r3,r3
        do | while nz
            sll r1 | rlc r0
            dec r3
        wend
    pop r3
ret
