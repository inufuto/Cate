name=cate87

out= $(name).lib 

all: $(out)

.asm.obj:
	Asm87 $*.asm

objects = WorkingReg.obj \
	ShiftLeftA.obj ShiftRightA.obj ShiftRightSignedA.obj \
	ShiftLeftHl.obj ShiftRightHl.obj ShiftRightSignedHl.obj \
	LessThanByte.obj GreaterThanByte.obj \
	EqualWord.obj \
	LessThanWord.obj GreaterThanWord.obj \
	LessThanSignedWord.obj GreaterThanSignedWord.obj \
	MultiplyHlC.obj

$(out): $(objects)
	Lib8 $(name).lib $(objects)
	copy $(name).lib D:\8bit\cate\cate87.lib /y
