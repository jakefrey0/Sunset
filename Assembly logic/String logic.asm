
    format PE

start:
    
    ;... Setting ptr
    push    myStr
    pop     dword [myStrPtr]
    
    ; Passing the string in as arguments for a dllreference
    ; (which will be implemented afterwards)
    push    0
    push    myStr
    push    myStr
    push    0
    call    [MessageBoxA]
    
    ; int3 ; pause
    xor     eax,eax ; exit code 0
    ret ; exit process
    

; When a String is declared, it will be stored as so:
; (Always static, no heap, at least I see that as the best way for now)
; When the variable is ever passed, 
; it will be passed as a pointer to this
myStr:
    db 'Hello world',0 ; < 0 is the end null byte, expected on each string, on any programming language, to my knowledge (perhaps there are exceptions)

myStrPtr:
    dd 0

; ------------------------------------------------
section '.idata' import data readable writeable

  dd 0,0,0,RVA user_name, RVA user_table
  dd 0,0,0,0,0
    
  user_table:
    MessageBoxA dd RVA _MessageBoxA
    dd 0

  user_name db 'USER32.DLL',0
 
  _MessageBoxA dw 0
    db 'MessageBoxA',0