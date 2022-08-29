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
using Sunset.Stack;
using System.Collections.Generic;

namespace Sunset.Keywords {
	
	public class KWNew : Keyword {
		
		public const String constName="new";
		
		public KWNew () : base (constName,KeywordType.NATIVE_CALL_WITH_RETURN_VALUE,true) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (@params.Length==0)
				throw new ParsingError("Expected at least 1 parameter for native call \""+constName+'"',sender);
			
			Boolean setProcessHeapVar=false;
			if (Parser.processHeapVar==UInt32.MaxValue) {
				
				sender.setProcessHeapVar();
				setProcessHeapVar=true;
				
			}
			
			if (!(sender.containsImportedClass(@params[0])))
				throw new ParsingError("Class is not imported: \""+@params[0]+'"',sender);
			
			Class cl=sender.importedClasses.Where(x=>x.className==@params[0]).First();

			sender.pushValue((cl.bytesToReserve+cl.classAppendAfterCount).ToString());
			sender.addBytes(new Byte[]{0x6A,8}); //PUSH 8
			if (setProcessHeapVar)
				sender.addByte(0x50); //PUSH EAX
			else
				sender.pushProcessHeapVar();
			const String HL="HeapAlloc",KERNEL32="KERNEL32.DLL";
			sender.referenceDll(KERNEL32,HL);
            sender.ReferenceRefdFunc(HL,2);
            
			sender.addBytes(new Byte[]{0xFF,0x15,0,0,0,0});//CALL FUNC HeapAlloc

			sender.addByte(0x56);//PUSH ESI
			sender.addByte(0xB9);//MOV FOLLOWING DWORD INTO ECX
			sender.addBytes(BitConverter.GetBytes(cl.byteSize));
			
			sender.addByte(0xBE);//MOV FOLLOWING DWORD INTO ESI
			sender.addBytes(BitConverter.GetBytes(Parser.GetSkeletonAddress(cl))); //DWORD
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
				if (cl.constructor!=null) {
					
					sender.addBytes(new Byte[]{0x8B,0x74,0x24,4});//MOV ESI,[ESP+4] (restore ESI to original class value)
					
					if (@params.Length-1!=cl.constructor.Item2.Count)
						throw new ParsingError("Expected "+cl.constructor.Item2.Count.ToString()+" parameters for the constructor of \""+cl.className+"\", but got "+(@params.Length-1).ToString(),sender);
					Byte i=0;
					Tuple<String,VarType>[]types= cl.constructor.Item2.ToArray().Reverse().ToArray();
					if (@params.Length!=1) {
						foreach (String s in @params.Skip(1).Reverse()) {
							sender.tryConvertVars(types[i/4],sender.pushValue(s),s);
							i+=4;
						}
					}
					sender.addBytes(new Byte[]{0x8B,0x44,0x24,i});//MOV EAX,[ESP+-OFFSET]
					sender.addBytes(new Byte[]{5}.Concat(BitConverter.GetBytes(cl.constructor.Item1))); //ADD EAX,DWORD
					sender.addBytes(new Byte[]{0xFF,0xD0}); //CALL EAX
					
				}
				else if (@params.Length!=1) throw new ParsingError("\""+cl.className+"\" has no constructors parameters, but got parameters in \""+KWNew.constName+'"',sender);
				sender.addByte(0x58); //POP EAX
				
			}
			else if (cl.classType==ClassType.ENUM)
				throw new ParsingError("Can't initialize the class \""+cl.className+"\" using the keyword \""+constName+"\" because \""+cl.className+"\" is an enum.",sender);
			sender.addByte(0x5E);//POP ESI
			outputType=new Tuple<String,VarType>(cl.className,VarType.CLASS);
			
			return base.execute(sender,@params);
			
		}
		
	}
	
}