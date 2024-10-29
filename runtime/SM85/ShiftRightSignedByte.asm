cseg
cate.ShiftRightSignedByte: public cate.ShiftRightSignedByte
; r0,r1
    push r1
        or r1,r1
        do | while nz
            sra r0
            dec r1
        wend
    pop r1
ret
