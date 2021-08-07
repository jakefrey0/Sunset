/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/31/2021
 * Time: 12:42 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWIncrease : Keyword {
		
		public const String constName="++";
		
		public KWIncrease () : base (constName,KeywordType.INCREMENT) { }
		
		override public KeywordResult execute (Parser sender,String[] @params) {
			
			if (String.IsNullOrEmpty(sender.lastReferencedVariable))
				throw new ParsingError("Invalid use of \""+constName+"\", no referenced variable found");
			
			Byte[] newOpcodes=new Byte[0];
			if (sender.lastReferencedVariableIsLocal) {
				
				String varType=sender.getLocalVarHomeBlock(sender.lastReferencedVariable).localVariables[sender.lastReferencedVariable].Item1.Item1;
				
				if (varType==KWByte.constName) {
					
					if (sender.getLocalVarHomeBlock(sender.lastReferencedVariable)!=sender.getCurrentBlock())
						sender.localVarEBPPositionsToOffset[sender.getCurrentBlock()].Add(sender.getOpcodesCount()+2);
					sender.addBytes(new Byte[]{0xFE,0x45,sender.pseudoStack.getVarEbpOffset(sender.lastReferencedVariable)}); //INC BYTE [EBP+-OFFSET]
					
				}
				else if (varType==KWShort.constName) {
					
					if (sender.getLocalVarHomeBlock(sender.lastReferencedVariable)!=sender.getCurrentBlock())
						sender.localVarEBPPositionsToOffset[sender.getCurrentBlock()].Add(sender.getOpcodesCount()+3);
					sender.addBytes(new Byte[]{0x66,0xFE,0x45,sender.pseudoStack.getVarEbpOffset(sender.lastReferencedVariable)}); //INC WORD [EBP+-OFFSET]
					
					
				}
				else if (varType==KWInteger.constName) {
					
					if (sender.getLocalVarHomeBlock(sender.lastReferencedVariable)!=sender.getCurrentBlock())
						sender.localVarEBPPositionsToOffset[sender.getCurrentBlock()].Add(sender.getOpcodesCount()+2);
					sender.addBytes(new Byte[]{0xFF,0x45,sender.pseudoStack.getVarEbpOffset(sender.lastReferencedVariable)}); //INC DWORD [EBP+-OFFSET]
					
				}
				
			}
			else {
				
				String varType=sender.getVariablesType(sender.lastReferencedVariable);
			
				//TODO:: make this work for more than native variables & local variables (in whole file, but even here it can be for more than these, it can work for classes etc.)
				//HACK:: check variable type
				if (varType==KWByte.constName) {
					
					sender.variableReferences[sender.lastReferencedVariable].Add(sender.getOpcodesCount()+2);
					newOpcodes=new Byte[]{0xFE,5,0,0,0,0};
					
				}
				else if (varType==KWShort.constName) {
					
					sender.variableReferences[sender.lastReferencedVariable].Add(sender.getOpcodesCount()+3);
					newOpcodes=new Byte[]{0x66,0xFF,5,0,0,0,0};
					
					
				}
				else if (varType==KWInteger.constName) {
					
					sender.variableReferences[sender.lastReferencedVariable].Add(sender.getOpcodesCount()+2);
					newOpcodes=new Byte[]{0xFF,5,0,0,0,0};
					
					
				}
				else throw new ParsingError("Can't increase variable type \""+varType+'"');
				
			}
			
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_NAME,newOpcodes=newOpcodes};
			
		}
		
	}
	
}
