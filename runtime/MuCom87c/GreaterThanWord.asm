ext @WB0

cseg
cate.GreaterThanWord: public cate.GreaterThanWord
; if hl > bc then z := 1
    staw @WB0
    mov a,h
    sub a,b
    if sknz
        if sknc
            xra a,a
        else
            xra a,a
            inr a
        endif
    else
        mov a,l
        if gta a,c
            xra a,a
        else
            xra a,a
            inr a
        endif
    endif
    ldaw @WB0
ret
