name=cate65

all: $(name).lib

.asm.obj:
	asm65.exe $*.asm

objects = ZeroPage.obj \
	ShiftLeftA.obj ShiftRightA.obj ShiftRightSignedA.obj \
	ShiftLeftWord.obj ShiftRightWord.obj ShiftRightSignedWord.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
