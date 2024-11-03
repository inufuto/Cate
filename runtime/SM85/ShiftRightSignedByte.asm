cseg
cate.ShiftRightSignedByte: public cate.ShiftRightSignedByte
; r1,r3
    push r3
        or r3,r3
        do | while nz
            sra r1
            dec r3
        wend
    pop r3
ret
