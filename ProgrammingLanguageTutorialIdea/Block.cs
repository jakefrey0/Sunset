/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/7/2021
 * Time: 4:13 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea {
	
	public class Block {
		
		public readonly Action onBlockEnd;
		public UInt32 startMemAddr;
		public UInt32 endMemAddr {
			
			private set;
			get;
			
		}
		public readonly Byte[] opcodesToAddOnBlockEnd;
		
		public Block (Action onBlockEnd,UInt32 startMemAddr,Byte[] opcodesToAddOnBlockEnd) {
			
			this.startMemAddr=startMemAddr;
			this.onBlockEnd=onBlockEnd;
			this.opcodesToAddOnBlockEnd=opcodesToAddOnBlockEnd;
			
		}
		
	}
	
}
