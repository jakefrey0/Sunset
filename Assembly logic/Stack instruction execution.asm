
    format PE

start:
    
    ; This isn't important but I found it
    ; while debugging, and messed around
    ; with it. It's pretty cool
    ; (This will return exit code 16, or any 0-255 if you set it to do so)
    
    mov     ebp,esp
    push    9090C3ECh
    push    08958166Ah
    jmp     esp