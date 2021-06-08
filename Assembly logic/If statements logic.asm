
    format PE

start:

    ;== Booleans ==:
    ;If a boolean==0, it is false, else it is true
    boolean:
        
        ;pushValue(...)
        push    1 ;True (Zero Flag not set on test instruction)
        pop     eax

        test    al,al
        jz      ifBlockEnd
        ifBlockStart:
        ;If true run instructions here...
        jmp     elseBlockEnd ; This instruction is only written if there is an else block
        ifBlockEnd:
        elseBlockStart:
        ;If false run instructions here...
        elseBlockEnd:

    nop
    nop
    nop
    int3
    
    ;== Reverse Booleans (Is Not Equal To Boolean) ==:
    ;If a boolean==0, it is false, else it is true
    ;However, that logic is reversed here (Notice jz --> jnz)
    ;So effectively, boolean==0 is true, boolean!=0 is false
    booleanNot:
        
        ;pushValue(...)
        push    1 ;True (Zero Flag not set on test instruction)
        pop     eax

        test    al,al
        jnz     ifBlockEndNot
        ifBlockStartNot:
        ;If true run instructions here...
        jmp     elseBlockEndNot ; This instruction is only written if there is an else block
        ifBlockEndNot:
        elseBlockStartNot:
        ;If false run instructions here...
        elseBlockEndNot:

    nop
    nop
    nop
    int3
    
    ;== Greater than ==:
    greaterThan:
        
        ;pushValue(..)
        push    16
        pop     eax

        ;pushValue(..)
        push    16
        pop     edx

        cmp     eax,edx
        jng     elseBlockStart0
        ifBlockStart0:
        ;If true run instructions here...
        ifBlockEnd0:
        jmp     elseBlockEnd0 ; This instruction is only written if there is an else block
        elseBlockStart0:
        ;If false run instructions here...
        elseBlockEnd0:

    nop
    nop
    nop
    int3
      
    ;== Greater than || equal to ==:
    greaterThanOrEqualTo:
        
        ;pushValue(..)
        push    16
        pop     eax

        ;pushValue(..)
        push    16
        pop     edx

        cmp     eax,edx
        jnge    elseBlockStart1
        ifBlockStart1:
        ;If true run instructions here...
        ifBlockEnd1:
        jmp     elseBlockEnd1 ; This instruction is only written if there is an else block
        elseBlockStart1:
        ;If false run instructions here...
        elseBlockEnd1:

    nop
    nop
    nop
    int3
    
    ;== Less than ==:
    lessThan:
        
        ;pushValue(..)
        push    16
        pop     eax

        ;pushValue(..)
        push    16
        pop     edx

        cmp     eax,edx
        jnl     elseBlockStart2
        ifBlockStart2:
        ;If true run instructions here...
        ifBlockEnd2:
        jmp     elseBlockEnd2 ; This instruction is only written if there is an else block
        elseBlockStart2:
        ;If false run instructions here...
        elseBlockEnd2:

    nop
    nop
    nop
    int3

    ;== Less than || equal to ==:
    lessThanOrEqualTo:
        
        ;pushValue(..)
        push    16
        pop     eax

        ;pushValue(..)
        push    16
        pop     edx

        cmp     eax,edx
        jnle    elseBlockStart3
        ifBlockStart3:
        ;If true run instructions here...
        ifBlockEnd3:
        jmp     elseBlockEnd3 ; This instruction is only written if there is an else block
        elseBlockStart3:
        ;If false run instructions here...
        elseBlockEnd3:

    nop
    nop
    nop
    int3
        
    ;== Equal to ==:
    equalTo:
        
        ;pushValue(..)
        push    16
        pop     eax

        ;pushValue(..)
        push    16
        pop     edx

        cmp     eax,edx
        jnz     elseBlockStart4
        ifBlockStart4:
        ;If true run instructions here...
        ifBlockEnd4:
        jmp     elseBlockEnd4 ; This instruction is only written if there is an else block
        elseBlockStart4:
        ;If false run instructions here...
        elseBlockEnd4:

    nop
    nop
    nop
    int3
        
    ;== Not equal to ==:
    notEqualTo:
        
        ;pushValue(..)
        push    16
        pop     eax
        
        ;pushValue(..)
        push    17
        pop     edx
        
        cmp     eax,edx
        jz      elseBlockStart5
        ifBlockStart5:
        ;If true run instructions here...
        ifBlockEnd5:
        jmp     elseBlockEnd5 ; This instruction is only written if there is an else block
        elseBlockStart5:
        ;If false run instructions here...
        elseBlockEnd5:

    int3
    ret