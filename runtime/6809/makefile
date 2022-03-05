name=cate09

all: $(name).lib

.asm.obj:
	asm09.exe $*.asm

objects = Direct.obj \
	ShiftLeftA.obj ShiftRightA.obj ShiftRightSignedA.obj \
	ShiftLeftWord.obj ShiftRightWord.obj ShiftRightSignedWord.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
