/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/30/2021
 * Time: 3:59 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWVoid : Keyword {
		
		public const String constName="void";
		
		public KWVoid () : base (constName,KeywordType.TYPE) { }
		
		override public KeywordResult execute (Parser sender,String[] @params) {
			
			sender.varType=this.name;
			sender.lastReferencedVarType=VarType.NONE;
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_VARIABLE_NAME,newOpcodes=new Byte[0]};
			
		}
		
	}
	
}
