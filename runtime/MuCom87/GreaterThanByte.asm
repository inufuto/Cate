ext @WB0

cseg
cate.GreaterThanByte: public cate.GreaterThanByte
; if a > c then z := 1
    staw @WB0
    xra a,c
    ani a,$80
    ldaw @WB0
    if skz
        if gta a,c
            xra a,a
        else
            xra a,a
            inr a
        endif
    else
        if gta a,c
            xra a,a
            inr a
        else
            xra a,a
        endif
    endif
    ldaw @WB0
ret
