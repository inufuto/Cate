cseg
cate.ShiftRightByte: public cate.ShiftRightByte
; r0,r1
    push r1
        or r1,r1
        do | while nz
            srl r0
            dec r1
        wend
    pop r1
ret
