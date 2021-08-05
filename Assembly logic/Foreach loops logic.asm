
    format PE

start:
    
foreachBlockPrecursor:
    xor     ecx,ecx
foreachBlockContinueAddr:
    mov     ebx,[dwordArray]
    cmp     ecx,[ebx]
    jz      foreachBlockEnd
    mov     eax,[ebx+4]
    mul     ecx
    add     eax,ebx
    push    dword [eax+8] ; Compiler stores local var on pseudoStack
                          ; ^ Should check for var type byte size first
    inc     ecx
foreachBlockStart:
    push    ecx ; Compiler stores preserved ECX on pseudoStack
    enter   0,0
    
    ; .... Block instructions
    ; Acessing local var normally:
    mov     eax,[ebp+8]
    int3
    
    leave
    pop     ecx
    add     esp,4 ; Compiler removes local var from pseudoStack
    jmp     foreachBlockContinueAddr
foreachBlockEnd:
    
    ; ---------------
    xor     eax,eax
    retn
    ; ---------------


dwordArray:
    dd dwordArrayMem
dwordArrayMem:
    
    ; ----------------
    ; Array length
    dd 7
    ; Array member byte size
    dd 4
    ; ----------------
    ; Array members
    ; ----------------
    dd 3
    rd 2
    dd 6
    dd 4
    rd 2