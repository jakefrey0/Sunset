
    format PE

start:
    
                 ;Array format and consequently size is
                 ;Length of array, Size of array member,Array
    bytesSize=16 ;This should be:
                 ;dd, length of array
                 ;dd, size of each array member
                 ;... each array member 
    
    ;==== On Array Creation ====
        
    arrayCreation:
        ;Heap Allocation:
        push    eax ebx ecx
        ;Get bytesSize:
        mov     ebx,2 ;mov in the member size
        imul    ebx,4;multiply by length
        add     ebx,8;add in constant 8 bytes (2 uint32)
        ;If first heap allocation
        call    [GetProcessHeap] ;Make this so compiler only runs it once, by storing its value somewhere
        mov     [processHeapVar],eax
        ;Endif
        push    ebx
        mov     ebx,00000008h
        push    ebx
        push    dword [processHeapVar]
        call    [HeapAlloc]
        ;Storing allocated pointer
        mov     [heapHandle],eax
        pop     ecx ebx eax
        ;Set initial 8 bytes (UInt32 arrayLength,UInt32 arrayMemberByteSize)
        ;This example depicts an array of shorts, or other 2 byte structures, with a length of 4 members (8 bytes)
        int3
        push    0
        push    4
        call    setArrayValue
        int3
        push    4
        push    2
        call    setArrayValue
        
    ;====        End        ==== 

    int3

    ;=== On Array Indexing (SET) ===
    
    indexArray:
        push    1;Array Indexer (Zero-based)
        pop     eax
        imul    eax,2;By array member byte size (Shorts=Words=2)
        add     eax,8;Constant 8
        push    eax ;Index to set.. 8+(arrayIndexer*arrayMemberByteSize)
    
    push    255;Value to set
    call    setArrayValue
    jmp     setEnd
    
    ;Accessing the dynamic memory
    setArrayValue: ;First time called store this in compiler, after that only call this value
        pop     edx
        pop     ebx
        pop     eax
        push    edx
        add     eax,[heapHandle] ;if heapHandle==null in compiler, throw parsingerror
        mov     [eax],ebx
        ret
    
    ;===        End        ===
    
    setEnd:
    
    nop
    nop
    xor     ebx,ebx
    nop
    nop

    ;=== On Array Indexing (GET) ===
    
    getArrayValue:
        
        ;Accessing the dynamic memory
        push    eax ebx
        mov     eax,[heapHandle]
        add     eax,10 ;8+(arrayIndexer*arrayMemberByteSize)
        mov     ebx,[eax]
        pop     eax ebx
        ;Stored in ebx
    
    ;===          End          ===
    
    ;=== Free arr heap ===
    
    push    dword [heapHandle] ;this is the memory that will be freed
    push    0
    push    dword [processHeapVar] ;this will still be usable I believe (quite certain)
    call    [HeapFree]
    
    ;===      End      ===
    
    ret

heapHandle:
    dd 0

processHeapVar:
    dd 0

section '.idata' import data readable writeable

  dd 0,0,0,RVA kernel_name,RVA kernel_table
  dd 0,0,0,0,0

  kernel_table:
    HeapAlloc dd RVA _HeapAlloc
    GetProcessHeap dd RVA _GetProcessHeap
    HeapFree dd RVA _HeapFree
    dd 0

  kernel_name db 'kernel32.dll',0

  _HeapAlloc dw 0
    db 'HeapAlloc',0
  _GetProcessHeap dw 0
    db 'GetProcessHeap',0
  _HeapFree dw 0
    db 'HeapFree',0