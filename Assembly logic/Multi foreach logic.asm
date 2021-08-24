
	format PE

start:
setup:

; Getting the array with the lowest count (length),
; result is placed in ECX then moved to the stack:

	jmp 	donePutLocalVar
putLocalVar: ; Make a local var from array index
	     ; (For later)
	mov     eax,[ebx+4] ; Get array member byte size
	mul     ecx
	add     eax,ebx
	mov	eax,[eax+8]
	retn	; Return doesn't clear parameters because they are stored in registers 
	        ; (EBX=array addr, ECX=array index)

donePutLocalVar:

; First array
	mov	ecx,[arrayOne]
; -----------

; Every following array
	cmp	[arrayTwo],ecx
	jge	nextArray
	mov	ecx,[arrayTwo]
nextArray:
; -----------------

doneQuery:
	push	ecx ; Store ecx on stack
		    ; Compiler: store on pseudoStack
	xor	ecx,ecx ; Zero ecx
	
continueAddr: ; 'continue' keyword will be directed here
	cmp	ecx,[esp] ; Compare ECX to loop count stored on stack
	jz	b_end ; Jump to block end if done looping
	
arrayOne_putLocalVar: ; Repeat these instructions per array
	mov	ebx,arrayOne
	call	putLocalVar
	push	eax ; Store local var on pseudoStack
; ---------------------------------------------

arrayTwo_putLocalVar: ; ...
	mov	ebx,arrayTwo
	call	putLocalVar
	push	eax ; Store local var on pseudoStack
; ---------------------------------------------
	
	inc 	ecx ; Increase iteration #
	
blockStart:
	push	ecx ; Store ecx (iteration #)
	enter	0,0 ; Enter block...
	
	; ... Block instructions
	
	leave ; Leave block...
	pop	ecx ; Restore ecx (iteration #)
	add	esp,8 ; Restore stack from all local variables
		      ; Compiler removes all local variables from pseudoStack
		      ; Number to add to ESP is # of arrays * 4 (2 * 4 = 8)
	jmp	continueAddr ; Loop
	
b_end:	add	esp,4 ; Clear preserved ecx	
p_end:	xor	eax,eax ; Exit code 0
	retn ; Exit process
	
	

arrayOne:

	; Length
	dd 6
	; Mem size
	dd 4
	
	dd 3
	rd 4
	dd 2
	
arrayTwo:
	
	; Length
	dd 7
	; Mem size
	dd 4
	
	dd 2
	rd 6
