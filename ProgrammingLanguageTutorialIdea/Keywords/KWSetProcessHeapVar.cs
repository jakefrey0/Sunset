/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 7/15/2021
 * Time: 1:48 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWSetProcessHeapVar : Keyword {
		
		public const String constName="setpheapvar";
		
		public KWSetProcessHeapVar () : base (constName,KeywordType.NATIVE_CALL) { }
		
		public override KeywordResult execute (Parser sender,String[]@params) {
			
			sender.setProcessHeapVar();
			return base.execute(sender, @params);
			
		}
		
	}
	
}
