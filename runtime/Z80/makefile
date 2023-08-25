name=cate80

all: $(name).lib

.asm.obj:
	asm80.exe $*.asm

objects = Temp.obj \
	ShiftLeftA.obj ShiftRightA.obj ShiftRightSignedA.obj \
	ShiftLeftHl.obj ShiftRightHl.obj ShiftRightSignedHl.obj \
	CompareHlDe.obj CompareHlDeSigned.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
