name=cate816

all: $(name).lib

.asm.obj:
	Asm65.exe -65816 $*.asm

objects = ZeroPage.obj \
	ExpandSigned.obj \
	ShiftLeftByte.obj ShiftRightByte.obj ShiftRightSignedByte.obj \
	ShiftLeftWord.obj ShiftRightWord.obj ShiftRightSignedWord.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
