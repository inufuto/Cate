name=cate99

out= $(name).lib

all: $(out)

.asm.obj:
	Asm99.exe $*.asm

objects = \
	ShiftLeft.obj ShiftRight.obj ShiftRightSigned.obj

$(out): $(objects)
	Lib8.exe $(name).lib $(objects)
