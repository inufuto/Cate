ext @WB0

cseg
cate.LessThanWord: public cate.LessThanWord
; if hl < bc then z := 1
    staw @WB0
    mov a,h
    sub a,b
    if sknz
        if sknc
            xra a,a
            inr a
        else
            xra a,a
        endif
    else
        mov a,l
        if lta a,c
            xra a,a
        else
            xra a,a
            inr a
        endif
    endif
    ldaw @WB0
ret
