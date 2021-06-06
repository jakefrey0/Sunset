/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/4/2021
 * Time: 7:16 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWNofunc : Keyword {
		
		public const String constName="nofunct";
		
		public KWNofunc () : base (constName,KeywordType.NATIVE_CALL) { }
		
		public override KeywordResult execute (Parser sender) {
			
			return new KeywordResult(){newOpcodes=new Byte[]{0x90},newStatus= ParsingStatus.SEARCHING_NAME};
			
		}
		
	}
	
}
