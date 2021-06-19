
    format PE

start:
    
    ;== No params ==
    noParams:
        jmp     someFunctionBlockEnd ;Initially skip the function
        someFunction:
            
            ;...Function code here
            int3
            nop
            
            ;Returning value:
            mov     eax,123 
            ret ;Value in EAX is returned value
            
        someFunctionBlockEnd:
        
        call    someFunction ; .. calling function
        ;... EAX contains return value here
    
    ;== Params & Local Vars ==
    
    localVarsCount=2
    
    params:
        
        jmp     otherFunctionBlockEnd
        otherFunction: ; 2 Parameters
            
            enter   localVarsCount*4,0
            
            ;...Function code here

            ;Creating local var:
            mov     dword [ebp-8],1
            
            ;To access local vars:
            push    dword [ebp-4] ; EBP-((Local Var Index * 4)+4)
            
            ;To access parameters
            push    dword [ebp+8] ; EBP+((Param Index * 4)+8)
            
            ;Popping value into a local var
            pop     dword [ebp-8]
            
            leave
            ret
    
    otherFunctionBlockEnd:

    ;Calling function..
    push    2 ; Push parameter
    push    3 ; via pushValue
    call    otherFunction ; Call function
    
    xor     eax,eax ; exit code 0
    ret ; exit process