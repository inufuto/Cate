cseg
cate.ShiftRightWord: public cate.ShiftRightWord
; rr0,r3
    push r3
        or r3,r3
        do | while nz
            srl r0 | rrc r1
            dec r3
        wend
    pop r3
ret
