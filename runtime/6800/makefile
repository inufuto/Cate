name=cate68

all: $(name).lib

.asm.obj:
	asm68.exe $*.asm

objects = Direct.obj \
	ShiftLeftA.obj ShiftRightA.obj ShiftRightSignedA.obj \
	ShiftLeftWord.obj ShiftRightWord.obj ShiftRightSignedWord.obj \
	AddXA.obj AddXB.obj AddXAB.obj \
	SubXA.obj SubXB.obj SubXAB.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
