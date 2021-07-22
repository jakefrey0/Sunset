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
	
	public class EsiPtr : IPseudoStackItem {
		
		public ItemType type { set; get; }
		public EsiPtr () { type=ItemType.ESI_PTR; }
		
	}
	
}
