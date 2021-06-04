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
	
	public class KWCrash : Keyword {
		
		public const String constName="crash";
		
		public KWCrash () : base (constName) { }
		
		public override KeywordResult execute (Parser sender) {
			
			return new KeywordResult(){newOpcodes=new Byte[]{0xCC},newStatus= ParsingStatus.SEARCHING_NAME};
			
		}
		
	}
	
}
