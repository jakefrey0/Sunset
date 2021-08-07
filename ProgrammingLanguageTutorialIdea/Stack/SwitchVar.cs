/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/17/2021
 * Time: 12:31 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Stack {
	
	public class SwitchVar : IPseudoStackItem {
		
		public ItemType type { set; get; }
		public SwitchVar () { this.type=ItemType.SWITCH_VAR; }
		
	}
	
}
