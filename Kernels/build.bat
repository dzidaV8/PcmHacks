@echo off
: -fomit-frame-pointer = remove the boilerplate linkw/unlk instructions
: -c    = compile but do not link
: -O1   = optimization level
: -g    = include debug information - not using this because the
:         disassembly is either corrupt or just incomprehensible

if -%KernelBuild%- == -- call fixpath.bat

call clean.bat

REM * See: Build.cmd -h
call Build.cmd -c -d -aFF8000 -gc:\SysGCC\m68k-elf\bin\ -p..\Apps\PcmHammer\bin\debug\kernel.bin

c:\mingw\bin\g++ -o test.exe test.cpp crc.c

copy kernel.bin ..\Apps\PcmHammer\bin\debug\kernel.bin
dir *.bin

