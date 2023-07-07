name=cate62

all: $(name).lib

.asm.obj:
	asm62.exe $*.asm

objects = InternalRam.obj \
	ShiftLeftA.obj ShiftRightA.obj ShiftRightSignedA.obj \
	ShiftLeftWord.obj ShiftRightWord.obj ShiftRightSignedWord.obj \
	CompareSignedByte.obj CompareSignedWord.obj \
	ExpandSigned.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
