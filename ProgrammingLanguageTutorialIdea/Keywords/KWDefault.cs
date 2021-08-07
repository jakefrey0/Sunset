/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/6/2021
 * Time: 6:49 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWDefault : Keyword {
		
		public const String constName="default";
		
		public KWDefault () : base (constName,KeywordType.NATIVE_CALL) { }
		
		public override KeywordResult execute(Parser sender,String[]@params) {
			
			KWCase.checkForCaseFallThrough(sender);
			Block defaultBlock=new Block(null,sender.memAddress,new Byte[0]){caseOrDefaultBlock=true,hasParentheses=false};
			sender.addBlock(defaultBlock,0);
			return new KeywordResult(){newOpcodes=new Byte[0],newStatus=(sender.isColon(sender.nextChar)?ParsingStatus.SEARCHING_NAME:ParsingStatus.SEARCHING_COLON)};
			
		}
		
	}
	
}
