name=cate80i

out= $(name).lib

all: $(out)

.asm.obj:
	asm80i.exe $*.asm

objects = Temp.obj \
	ShiftLeftA.obj ShiftRightA.obj ShiftRightSignedA.obj \
	ShiftLeftHl.obj ShiftRightHl.obj ShiftRightSignedHl.obj \
	ExpandSigned.obj CompareAESigned.obj \
	CompareHlDe.obj CompareHlDeSigned.obj

$(out): $(objects)
	lib8 $(name).lib $(objects)
