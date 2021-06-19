/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/7/2021
 * Time: 4:13 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace ProgrammingLanguageTutorialIdea {
	
	public class Block {
		
		public readonly Action onBlockEnd;
		public UInt32 startMemAddr;
		public UInt32 endMemAddr {
			
			private set;
			get;
			
		}
		public readonly Byte[] opcodesToAddOnBlockEnd;
		
		public Dictionary<String,Tuple<Tuple<String,VarType>>> localVariables;
		
		public readonly Boolean shouldXOREAX,addEnterAutomatically;
		
		public UInt16 nestedLevel=0;
		
		public List<UInt32>blockMemPositions;
		
		/// <summary>
		/// i.e If & Else
		/// </summary>
		public Block pairedBlock {
			
			get;
			protected set;
						
		}
		
		public Block (Action onBlockEnd,UInt32 startMemAddr,Byte[] opcodesToAddOnBlockEnd,Boolean xorEax=false,Boolean addEnterAutomatically=true) {
			
			this.startMemAddr=startMemAddr;
			this.onBlockEnd=onBlockEnd;
			this.opcodesToAddOnBlockEnd=opcodesToAddOnBlockEnd;
			this.localVariables=new Dictionary<String,Tuple<Tuple<String,VarType>>>();
			this.shouldXOREAX=xorEax;
			this.addEnterAutomatically=addEnterAutomatically;
			this.blockMemPositions=new List<UInt32>();
			
		}
		
		public static void pairBlocks (Block b,Block b0) {
			
			b.pairedBlock=b0;
			b0.pairedBlock=b;
			
		}
		
	}
	
}
