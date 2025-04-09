name=cate900

all: $(name).lib

.asm.obj:
	asm900.exe $*.asm

objects = empty.obj

$(name).lib: $(objects)
	lib8.exe $(name).lib $(objects)
