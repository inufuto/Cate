ext @WB0

cseg
cate.LessThanWord: public cate.LessThanWord
; if hl < bc then z := 1
    staw @WB0
    mov a,h
    sub a,b
    if skz
        mov a,l
        if lta a,c
            xra a,a
        else
            xra a,a
            inr a
        endif
    else
        if skc
            xra a,a
        else
            xra a,a
            inr a
        endif
    endif
    ldaw @WB0
ret
