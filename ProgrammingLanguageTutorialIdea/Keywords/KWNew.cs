/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 7/11/2021
 * Time: 3:17 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using ProgrammingLanguageTutorialIdea.Stack;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWNew : Keyword {
		
		public const String constName="new";
		
		public KWNew () : base (constName,KeywordType.NATIVE_CALL_WITH_RETURN_VALUE,true) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (@params.Length!=1)
				throw new ParsingError("Expected 1 parameter for native call \""+constName+'"');
			
			Boolean setProcessHeapVar=false;
			if (sender.processHeapVar==null) {
				
				sender.setProcessHeapVar();
				setProcessHeapVar=true;
				
			}
			
			if (!(sender.containsImportedClass(@params[0])))
				throw new ParsingError("Class is not imported: \""+@params[0]+'"');
			
			Class cl=sender.importedClasses.Where(x=>x.className==@params[0]).First();
			
			sender.pushValue((cl.byteSize).ToString());
			sender.addBytes(new Byte[]{0x6A,8}); //PUSH 8
			if (setProcessHeapVar)
				sender.addByte(0x50); //PUSH EAX
			else
				sender.pushProcessHeapVar();
			const String HL="HeapAlloc",KERNEL32="KERNEL32.DLL";
			sender.referenceDll(KERNEL32,HL);
			sender.referencedFuncPositions[HL].Add((UInt32)(sender.getOpcodesCount()+2));
			sender.addBytes(new Byte[]{0xFF,0x15,0,0,0,0});//CALL FUNC HeapAlloc
			if (sender.referencedVariable!=null) {
				
				if (sender.referencedVariableIsFromClass) {
					
					if (!(sender.lastReferencedVariableIsLocal)) {
						
						sender.moveClassInstanceIntoEax(sender.lastReferencedClassInstance);
						//UNDONE:: idk what this is supposed to be
						
					}
					else {
						
						Block localVarHomeBlock=sender.getLocalVarHomeBlock(sender.lastReferencedClassInstance);
						if (localVarHomeBlock!=sender.getCurrentBlock())
							sender.localVarEBPPositionsToOffset[sender.getCurrentBlock()].Add((UInt32)(sender.getOpcodesCount()+2));
						sender.addBytes(new Byte[]{0x89,0x45,sender.pseudoStack.getVarEbpOffset(sender.lastReferencedClassInstance)}); //MOV [EBP+-OFFSET],EAX
						
					}
					
				}
				else {
				
					if (!(sender.referencedVariableIsLocal)) {
						
						Console.WriteLine(sender.referencedVariable);
						if (sender.addEsiToLocalAddresses) {
							
							sender.addBytes(new Byte[]{0x89,0x86,});
							sender.addBytes(BitConverter.GetBytes(sender.appendAfterIndex[sender.referencedVariable]));
							
						}
						else {
							
							sender.addByte(0xA3);//MOV DWORD[FOLLOWING PTR],EAX
							sender.classReferences[sender.referencedVariable].Add(sender.getOpcodesCount());
							sender.addBytes(new Byte[]{0,0,0,0});
						}
					}
					else {
						
						Block localVarHomeBlock=sender.getLocalVarHomeBlock(sender.referencedVariable);
						if (localVarHomeBlock!=sender.getCurrentBlock())
							sender.localVarEBPPositionsToOffset[sender.getCurrentBlock()].Add((UInt32)(sender.getOpcodesCount()+2));
						sender.addBytes(new Byte[]{0x89,0x45,sender.pseudoStack.getVarEbpOffset(sender.referencedVariable)}); //MOV [EBP+-OFFSET],EAX
					 	
					}
					
				}
				
			} 
			
			sender.addByte(0x56);//PUSH ESI
			sender.addByte(0xB9);//MOV FOLLOWING DWORD INTO ECX
			sender.addBytes(BitConverter.GetBytes(cl.byteSize));
			
			if (sender.addEsiToLocalAddresses) {
				sender.addBytes(new Byte[]{0x8B,0x34,0x24,//MOV ESI,[ESP]
				                	0x81,0xC6}.Concat(BitConverter.GetBytes(cl.initialAppendAfterCount)));//ADD ESI,DWORD
				
			}
			else {
				sender.addByte(0xBE);//MOV FOLLOWING DWORD INTO ESI
				sender.staticClassReferences[cl].Add(sender.getOpcodesCount());
				sender.addBytes(new Byte[]{0,0,0,0}); //DWORD
			}
			sender.addBytes(new Byte[]{0x89,0xC7}); //MOV EDI,EAX
			sender.addBytes(new Byte[]{0xF3,0xA4}); //REP MOVS BYTE PTR ES:[EDI],BYTE PTR DS:[ESI]
			if (cl.classType==ClassType.NORMAL) {
				
				sender.addBytes(new Byte[]{0x8B,0xF0}); //MOV ESI,EAX
				sender.addBytes(new Byte[]{0x81,0xC6}.Concat(BitConverter.GetBytes(cl.opcodePortionByteSize))); //ADD ESI,DWORD
				sender.addByte(0x50); //PUSH EAX
				// The ESI push is only necessary because the start of functions need to calibrate
				// the ESI register to the value it is set to here. The reason an ESI register might
				// be invalid, is because if a 3rd party function calls a Sunset function, the ESI
				// register could be invalid during the sunset function (i.e CreateWindowExA).
				// Accomodations were made:
				//  - Parser#restoreEsiFuncAddr
				//  - Parser#tryCreateRestoreEsiFunc (This can later be adjusted to only being created if any function's memory address was ever accessed via $funcName)
				//  - KWNew -> Following PUSH ESI instruction, and its corresponding ADD ESP,4 instruction
				//  - Parser#setEsiFuncVar
				//  - Parser#fillEsiFuncReferences
				//  - KWFunc -> Preservation of ESI (This can later be adjusted to only being created if the functions memory address was ever accessed via $funcName)
				//  - KWFunc -> Calling the ESI restoration func (This can later be adjusted to only being created if the functions memory address was ever accessed via $funcName)
				//  - Parser#callFunc -> restoreEsiCondition
				sender.addByte(0x56);//PUSH ESI
				sender.pseudoStack.push(new EsiPtr());
				sender.addBytes(new Byte[]{0xFF,0xD0}); //CALL EAX
				sender.addBytes(new Byte[]{0x83,0xC4,4});//ADD ESP,4
				sender.pseudoStack.pop();
				sender.addByte(0x58); //POP EAX
				
			}
			sender.addByte(0x5E);//POP ESI
			outputType=new Tuple<String,VarType>(cl.className,VarType.CLASS);
			
			return base.execute(sender,@params);
			
		}
		
	}
	
}