ext @WB0

cseg
cate.LessThanByte: public cate.LessThanByte
; if a < c then z := 1
    staw @WB0
    xra a,c
    ani a,$80
    ldaw @WB0
    if sknz
        if lta a,c
            xra a,a
            inr a
        else
            xra a,a
        endif
    else
        if lta a,c
            xra a,a
        else
            xra a,a
            inr a
        endif
    endif
    ldaw @WB0
ret
