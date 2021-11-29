format PE

mov     eax,[someStaticVar]
mov     edx,eax
call    staticFunc
add     eax,edx
retn


section '.data' data readable writeable

someStaticVar: dd 1234

otherStaticVar: dd 1234

staticFunc:
        mov     eax,123
        retn


; Test 512 + bytes  in data func
; Also test without .idata section

section '.idata' import data readable writeable

  dd 0,0,0,RVA user_name, RVA user_table
  dd 0,0,0,RVA kernel_name, RVA kernel_table
  dd 0,0,0,0,0
    
  user_table:

    MessageBoxA dd RVA _MessageBoxA
    dd 0
    
  kernel_table:

    HeapAlloc dd RVA _HeapAlloc
    GetProcessHeap dd RVA _GetProcessHeap
    dd 0

  user_name db 'USER32.DLL',0
  kernel_name db 'KERNEL32.DLL',0
 
  _MessageBoxA dw 0
    db 'MessageBoxA',0
  _HeapAlloc dw 0
    db 'HeapAlloc',0
  _GetProcessHeap dw 0
    db 'GetProcessHeap',0