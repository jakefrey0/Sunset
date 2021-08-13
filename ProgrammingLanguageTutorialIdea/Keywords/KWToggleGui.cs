/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 7/10/2021
 * Time: 9:32 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWToggleGui : Keyword {
		
		public const String constName="TOGGLE_GUI";
		
		public KWToggleGui () : base (constName,KeywordType.NATIVE_CALL) { }
		
		public override KeywordResult execute (Parser sender,String[]@params) {
			
			Keyword.throwIfShouldBeHeader(sender,constName);
			
			sender.gui=!sender.gui;
			sender.toggledGui=true;
			return base.execute(sender,@params);
			
		}
		
	}
	
}
