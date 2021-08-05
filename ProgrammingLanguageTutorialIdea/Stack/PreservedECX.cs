/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/17/2021
 * Time: 12:35 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Stack {
	
	public class PreservedECX : IPseudoStackItem {
		
		public ItemType type { set; get; } 
		public PreservedECX () { this.type=ItemType.PRESERVED_ECX; }
		
	}
	
}
