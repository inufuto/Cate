ext cate.Temp, cate.Count
sign equ cate.Temp
count equ cate.Count

cseg
cate.ShiftRightSignedByte: public cate.ShiftRightSignedByte
    php
        rep #$10 | i16
        phx | phy
            pha
                and #$80
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