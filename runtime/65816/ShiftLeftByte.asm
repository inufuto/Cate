ext cate.Count
count equ cate.Count

cseg
cate.ShiftLeftByte: public cate.ShiftLeftByte
    php
        rep #$10 | i16
        phx | phy
            sep #$30 | a8 | i8
            ldy <count
            if ne
                do
                    asl a
                    dey
                while ne | wend
            endif
        rep #$10 | i16
        ply | plx
    plp
rts