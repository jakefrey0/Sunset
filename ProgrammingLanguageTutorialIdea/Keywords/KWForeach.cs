/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/5/2021
 * Time: 1:23 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ProgrammingLanguageTutorialIdea.Stack;
using System.Linq;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWForeach : Keyword {
		
		public const String constName="foreach";
		
		public KWForeach () : base(constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			if (@params.Length!=2)
				throw new ParsingError("Expected 2 parameters for \""+constName+"\" (NAME OF ITEM,ARRAY NAME)");
			sender.addBytes(new Byte[]{0x31,0xC9}); //XOR ECX,ECX
			UInt32 cMemAddr=sender.GetStaticInclusiveAddress();
			Block foreachBlock=new Block(delegate {sender.writeJump(cMemAddr);sender.pseudoStack.pop();},0,new Byte[]{0x59,0x83,0xC4,4}/*POP ECX,ADD ESP,4*/,false,false){isLoopOrSwitchBlock=true,continueAddress=cMemAddr};
			foreachBlock.continueInstructions=foreachBlock.breakInstructions=new Byte[]{0x59,0x83,0xC4,4};//POP ECX && ADD ESP,4
			String arrName=@params[1],varName=@params[0];
			if (sender.nameExists(varName))
				throw new ParsingError("Foreach iteration variable name \""+varName+"\" is already in use");
			if (varName.Any(x=>!Char.IsLetterOrDigit(x)))
				throw new ParsingError("Invalid foreach iteration variable name \""+varName+"\" (should be alphanumeric)");
			// EBX can be preserved throughout the whole loop so it doesn't have to constantly run pushValue instructions
			// (but it doesn't happen here, it is an optimization idea)
			sender.addByte(0x51);//PUSH ECX
			Tuple<String,VarType>vt=sender.pushValue(arrName);
			sender.addByte(0x5B);//POP EBX
			sender.addByte(0x59);//POP ECX
			if (vt.Item2==VarType.NATIVE_ARRAY) {
				sender.addBytes(new Byte[]{0x3B,0x0B}); //CMP ECX,[EBX]
				foreachBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount(2));
				sender.addBytes(new Byte[]{0x0F,0x84,0,0,0,0});//JZ
				foreachBlock.startMemAddr=sender.GetStaticInclusiveAddress();
				sender.addBytes(new Byte[]{0x8B,0x43,4}); //MOV EAX,[EBX+4]
				sender.addBytes(new Byte[]{0xF7,0xE1}); //MUL ECX
				sender.addBytes(new Byte[]{1,0xD8}); //ADD EAX,EBX
				switch (sender.keywordMgr.getVarTypeByteSize(vt.Item1)) {
					
					// case 1 & 2 use a bit of a "trick" here because on the MUL ECX instruction
					// EDX will in most cases be set to zero (though not always) so it should be
					// a temporary "trick" and probably changed later (though I haven't looked into it)
					
					case 1:
						sender.addBytes(new Byte[]{0x8A,0x50,8});//MOV DL,[EAX+8]
						sender.addByte(0x52);//PUSH EDX
						break;
					case 2:
						sender.addBytes(new Byte[]{0x66,0x8B,0x50,8});//MOV DX,[EAX+8]
						sender.addByte(0x52);//PUSH EDX
						break;
					default:
					case 4:
						sender.addBytes(new Byte[]{0xFF,0x70,8}); //PUSH DWORD [EAX+8]
						break;
						
				}
				sender.pseudoStack.push(new LocalVar(varName));
				sender.addBlock(foreachBlock);
				sender.getCurrentBlock().localVariables.Add(varName,new Tuple<Tuple<String,VarType>>(sender.getVarType(vt.Item1)));
				sender.addByte(0x41);//INC ECX
				sender.addByte(0x51);//PUSH ECX
				sender.pseudoStack.push(new PreservedECX());
				sender.addBytes(sender.getEnterBlockOpcodes(foreachBlock));
				return base.execute(sender,@params);
			}
			if (vt.Item2==VarType.NATIVE_VARIABLE&&vt.Item1==KWString.constName) {
				sender.addBytes(new Byte[]{0x80,0x3C,0x19,0}); //CMP BYTE [ECX+EBX],0
				foreachBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount(2));
				sender.addBytes(new Byte[]{0x0F,0x84,0,0,0,0});//JZ
				foreachBlock.startMemAddr=sender.memAddress;
				sender.addBytes(new Byte[]{0x33,0xD2}); //XOR EDX,EDX
				sender.addBytes(new Byte[]{0x8A,0x14,0x19}); //MOV DL,[ECX+EBX]
				sender.addByte(0x52);//PUSH EDX
				sender.addBlock(foreachBlock);
				foreachBlock.localVariables.Add(varName,new Tuple<Tuple<String,VarType>>(new Tuple<String,VarType>(KWByte.constName,VarType.NATIVE_VARIABLE)));
				sender.pseudoStack.push(new LocalVar(varName));
				sender.addByte(0x41);//INC ECX
				sender.addByte(0x51);//PUSH ECX
				sender.pseudoStack.push(new PreservedECX());
				sender.addBytes(sender.getEnterBlockOpcodes(foreachBlock));
				
				return base.execute(sender,@params);
			}
			else
				throw new ParsingError("Expected var of type array or string, got \""+arrName+"\" of type \""+vt.Item1+"\" (\""+vt.Item2.ToString()+"\")");
			
		}		
				
	}
	
}
