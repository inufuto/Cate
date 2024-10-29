cseg
cate.ShiftRightSignedWord: public cate.ShiftRightSignedWord
; rr0,r3
    push r3
        or r3,r3
        do | while nz
            sra r0 | rrc r1
            dec r3
        wend
    pop r3
ret
