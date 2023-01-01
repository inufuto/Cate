ext @WB0
ext cate.GreaterThanWord

cseg
cate.GreaterThanSignedWord: public cate.GreaterThanSignedWord
; if hl > bc then z := 1
    staw @WB0
    mov a,h
    xra a,b
    ani a,$80
    ldaw @WB0
    sknz | jmp cate.GreaterThanWord
    call cate.GreaterThanWord
    if sknz
        xra a,a
    else
        xra a,a
        inr a
    endif
    ldaw @WB0
ret
