ext @WB0

cseg
cate.EqualWord: public cate.EqualWord
; if hl == bc then z := 1
    staw @WB0
    mov a,h
    sub a,b
    if skz
        mov a,l
        sub a,c
    endif
    ldaw @WB0
ret
