/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 7/11/2021
 * Time: 2:34 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWStruct : Keyword {
		
		public const String constName="STRUCT";
		
		public KWStruct () : base (constName,KeywordType.CLASS_TYPE_SETTER) { }
		
		public override KeywordResult execute(Parser sender,String[]@params) {
			
			if (sender.getOpcodesCount()!=0||sender.getAppendAfterCount()!=0)
				throw new ParsingError("The \""+constName+"\" keyword was expected at the header (top, beginning) of your source file");
			
			sender.@struct=true;
			sender.style=ArrayStyle.STATIC_MEMORY_BLOCK;
			
			return base.execute(sender,@params);
			
		}
		
	}
	
}

