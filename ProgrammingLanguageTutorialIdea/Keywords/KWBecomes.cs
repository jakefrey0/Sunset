/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/30/2021
 * Time: 11:32 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWBecomes : Keyword {
		
		public const String constName="becomes";
		
		public KWBecomes () : base (constName) { }
		
		override public KeywordResult execute (Parser sender) {
			
			if (String.IsNullOrEmpty(sender.lastReferencedVariable))
				throw new ParsingError("Invalid use of \""+constName+"\", no referenced variable found");
			
			sender.referencedVariable=sender.lastReferencedVariable;
			
			Byte[] newOpcodes=new Byte[0];
			
			//HACK:: check variable type
			if (sender.varType==KWByte.constName) {
				sender.variableReferences[sender.referencedVariable].Add(sender.getOpcodesCount()+2);
				newOpcodes=new Byte[]{0xC6,5,0,0,0,0};
			}
			else if (sender.varType==KWShort.constName) {
				sender.variableReferences[sender.referencedVariable].Add(sender.getOpcodesCount()+3);
				newOpcodes=new Byte[]{0x66,0xC7,5,0,0,0,0};
			}
			else if (sender.varType==KWInteger.constName) {
				
				sender.variableReferences[sender.referencedVariable].Add(sender.getOpcodesCount()+2);
				newOpcodes=new Byte[]{0xC7,5,0,0,0,0};
				
			}
			
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_VALUE,newOpcodes=newOpcodes};
			
		}
		
	}
	
}
