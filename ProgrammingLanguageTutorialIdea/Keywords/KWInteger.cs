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
	
	public class KWInteger : Keyword {
		
		public const String constName="int";
		
		public KWInteger () : base (constName) { }
		
		override public KeywordResult execute (Parser sender) {
			
			sender.varType=this.name;
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_VARIABLE_NAME,newOpcodes=new Byte[0]};
			
		}
		
	}
	
}
