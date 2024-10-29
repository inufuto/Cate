name=cate85

all: $(name).lib

.asm.obj:
	asm85.exe $*.asm

objects = RegisterFile.obj \
	ShiftLeftByte.obj ShiftRightByte.obj ShiftRightSignedByte.obj \
	ShiftLeftWord.obj ShiftRightWord.obj ShiftRightSignedWord.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
