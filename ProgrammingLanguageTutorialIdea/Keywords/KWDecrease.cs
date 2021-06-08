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
	
	public class KWDecrease : Keyword {
		
		public const String constName="--";
		
		public KWDecrease () : base (constName,KeywordType.DECREMENT) { }
		
		override public KeywordResult execute (Parser sender,String[] @params) {
			
			if (String.IsNullOrEmpty(sender.lastReferencedVariable))
				throw new ParsingError("Invalid use of \""+constName+"\", no referenced variable found");
			
			
			Byte[] newOpcodes=new Byte[0];
			
			String varType=sender.getVariablesType(sender.lastReferencedVariable);
			
			//HACK:: check variable type
			if (varType==KWByte.constName) {
				
				sender.variableReferences[sender.lastReferencedVariable].Add(sender.getOpcodesCount()+2);
				newOpcodes=new Byte[]{0xFE,0x0D,0,0,0,0};
				
			}
			else if (varType==KWShort.constName) {
				
				sender.variableReferences[sender.lastReferencedVariable].Add(sender.getOpcodesCount()+3);
				newOpcodes=new Byte[]{0x66,0xFF,0x0D,0,0,0,0};
				
			}
			else if (varType==KWInteger.constName) {
				
				sender.variableReferences[sender.lastReferencedVariable].Add(sender.getOpcodesCount()+2);
				newOpcodes=new Byte[]{0xFF,0x0D,0,0,0,0};
				
				
			}
			else throw new ParsingError("Can't decrease variable type \""+varType+'"');
			
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_NAME,newOpcodes=newOpcodes};
			
		}
		
	}
	
}
