/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 7/5/2021
 * Time: 11:49 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWFinishCompiling : Keyword {
		
		public const String constName="finishcmp";
		
		public KWFinishCompiling () : base (constName,KeywordType.NATIVE_CALL) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			return new KeywordResult(){newOpcodes=new Byte[]{},newStatus= ParsingStatus.STOP_PARSING_IMMEDIATE};
			
		}
		
	}
	
}
