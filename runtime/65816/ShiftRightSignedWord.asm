ext cate.Temp, cate.Count
sign equ cate.Temp
count equ cate.Count

cseg
cate.ShiftRightSignedWord: public cate.ShiftRightSignedWord
    php
        rep #$10 | i16
        phx | phy
            sep #$10 | i8
            pha
                and #$8000
                sta <sign
            pla
            ldy <count
            if ne
                do
                    lsr a
                    ora <sign
                    dey
                while ne | wend
            endif
        rep #$10 | i16
        ply | plx
    plp
rts