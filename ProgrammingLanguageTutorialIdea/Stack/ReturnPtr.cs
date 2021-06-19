/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/17/2021
 * Time: 12:38 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Stack {
	
	public class ReturnPtr : IPseudoStackItem {
		
		public ItemType type { set; get; }
		public ReturnPtr () { type=ItemType.RETURN_PTR; }
		
	}
	
}
