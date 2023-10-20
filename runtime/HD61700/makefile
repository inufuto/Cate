name=cate61h

all: $(name).lib

.asm.obj:
	asm61h.exe $*.asm

objects = \
	ShiftByteLeft.obj ShiftSignedByteRight.obj ShiftUnsignedByteRight.obj \
	ShiftWordLeft.obj ShiftSignedWordRight.obj ShiftUnsignedWordRight.obj \
	CompareSignedByte.obj CompareSignedWord.obj ExpandSigned.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
