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
		
		public KWBecomes () : base (constName,KeywordType.ASSIGNMENT) { }
		
		override public KeywordResult execute (Parser sender) {
			
			if (String.IsNullOrEmpty(sender.lastReferencedVariable))
				throw new ParsingError("Invalid use of \""+constName+"\", no referenced variable found");
			
			sender.referencedVariable=sender.lastReferencedVariable;
			sender.referencedVarType=sender.lastReferencedVarType;
			
			Byte[] newOpcodes=new Byte[0];
			
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_VALUE,newOpcodes=newOpcodes};
			
		}
		
	}
	
}
