ext cate.Count
count equ cate.Count

cseg
cate.ShiftRightWord: public cate.ShiftRightWord
    php
        rep #$10 | i16
        phx | phy
            rep #$20 | a16
            sep #$10 | i8
            ldy <count
            if ne
                do
                    lsr a
                    dey
                while ne | wend
            endif
        rep #$10 | i16
        ply | plx
    plp
rts