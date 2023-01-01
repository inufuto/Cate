ext @WB0
ext cate.LessThanWord

cseg
cate.LessThanSignedWord: public cate.LessThanSignedWord
; if hl < bc then z := 1
    staw @WB0
    mov a,h
    xra a,b
    ani a,$80
    ldaw @WB0
    sknz | jmp cate.LessThanWord
    call cate.LessThanWord
    if sknz
        xra a,a
    else
        xra a,a
        inr a
    endif
    ldaw @WB0
ret
