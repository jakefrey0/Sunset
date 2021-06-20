
    format PE

start:
    
    whileBlockStart:
        
        ; -- If check --
        
        ; this varies based on parameters
        ; as well as the boolean operator
        ; if 3 parameters are used. The
        ; logic is the same as KWIf.cs
        
        push    dword [someInt] ; pushValue
        pop     eax
        push    dword [otherInt] ; pushValue
        pop     edx
        cmp     eax,edx
        
        ; -------------------------------------------------------
        ; - Boolean condition (if greater than in this example) -
        jng     whileBlockEnd
        ; -------------------------------------------------------
        
        ; -- -- -- -- --
        
        ; -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
        ;   Code inside while block goes in this section
        ; -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
        
        inc     dword [otherInt] ;++otherInt 
        
        ; -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
        
        jmp     whileBlockStart
    
    whileBlockEnd:
    
    xor     eax,eax ; exit code 0
    ret ; exit process

someInt:
    dd 6

otherInt:
    dd 3
    