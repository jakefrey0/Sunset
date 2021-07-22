
; Possible Issue:
; - If I programmed an opcode that directly jumps to an address etc..
;   it could potentailly be dangerous if it ends up in a class depending
;   on what it is exactly

    format PE

start:
    
    classSize=30 ; Class size in bytes, compiler can get this
    
    ; Mandatory GetProcessHeap - Functionality in Sunset already implemented
    call    [GetProcessHeap]
    ; - -
    ; Heap allocation
    push    classSize ; Size of class in bytes
    push    8 ; Initialize as all blank bytes
    push    eax ; Push process heap
    call    [HeapAlloc]
    mov     dword [classPtr],eax
    int3
    ; - -
    ; copy data at label myClass to heap
    mov     ecx,classSize ; Size of class in bytes
    mov     esi,myClass ; Pointer to default class stored in static memory
    mov     edi,eax ; Pointer to new heap
    rep     movsb ; Repeat copy the bytes in
    ; - -
    ; new(ClassName)
    call    eax ; Jump to start of heap to run through class, (when the "new" keyword is called, i.e new(MyClass))
    ; - -
    ; calling a function in a class
    ; classInstance.functionCall
    
    mov     eax,dword [classPtr]
    add     eax,10 ; offset to functionEx0
    call    eax ; Would call label functionEx0

    ; - -
    ; Accessing variable from class
    mov     eax,dword [classPtr]
    add     eax,classSize-8 ; offset to variableEx
    add     dword [eax],5 ; add 5 to the variable
    ; - -

    ; (Extra) See changes of class variable in OllyDBG
    mov     eax,dword [classPtr]
    jmp     eax

    retn ; Return with return exit code from label functionEx0

classPtr:
    dd 0

; Default class will be stored static in memory in appendAfter
; (Compiler can easily detect byte size of blocks parsed in)
myClass:
    
    jmp     functionExEnd ; 2 bytes
    functionEx:
        mov     eax,13 ;5  bytes
        retn ; 1 byte
    functionExEnd:
    
    jmp     functionEx0End ; 2 bytes
    functionEx0:
        mov     eax,145 ;5 bytes
        retn ; 1 byte
    functionEx0End:
    
    mov     eax,123 ;5 bytes
    retn ; 1 byte
    
    variableEx:
        dd 123 ;4 bytes
    
    variableEx0:
        dd 1234 ;4 bytes

;Total class block size: 26 bytes
    
section '.idata' import data readable writeable

  dd 0,0,0,RVA kernel_name,RVA kernel_table
  dd 0,0,0,0,0

  kernel_table:
    HeapAlloc dd RVA _HeapAlloc
    GetProcessHeap dd RVA _GetProcessHeap
    dd 0

  kernel_name db 'KERNEL32.DLL',0

  _HeapAlloc dw 0
    db 'HeapAlloc',0
  _GetProcessHeap dw 0
    db 'GetProcessHeap',0