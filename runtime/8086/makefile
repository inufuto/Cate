name=cate86

all: $(name).lib

.asm.obj:
	asm86.exe $*.asm

objects = Temp.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
