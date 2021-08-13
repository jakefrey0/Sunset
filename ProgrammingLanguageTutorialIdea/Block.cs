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
		public List<Tuple<UInt32,UInt32>>blockRVAPositions;
		
		public Boolean isLoopOrSwitchBlock=false,switchBlock=false,caseOrDefaultBlock=false,hasParentheses=true;
		public UInt32 continueAddress=0;
		
		/// <summary>
		/// i.e If & Else
		/// </summary>
		public Block pairedBlock {
			
			get;
			protected set;
						
		}
		
		public Byte[] breakInstructions,continueInstructions;
		/// <summary>
		/// For after the block is fully closed (i.e all leave statements)
		/// These will be appended last on the block closing process
		/// (Optional as are all non-constructor items)
		/// </summary>
		public Byte[] afterBlockClosedOpcodes;
		
		public List<String>restoreArraySetValueFuncs;
		
		public Block (Action onBlockEnd,UInt32 startMemAddr,Byte[] opcodesToAddOnBlockEnd,Boolean xorEax=false,Boolean addEnterAutomatically=true) {
			
			this.startMemAddr=startMemAddr;
			this.onBlockEnd=onBlockEnd;
			this.opcodesToAddOnBlockEnd=opcodesToAddOnBlockEnd;
			this.localVariables=new Dictionary<String,Tuple<Tuple<String,VarType>>>();
			this.shouldXOREAX=xorEax;
			this.addEnterAutomatically=addEnterAutomatically;
			this.blockMemPositions=new List<UInt32>();
			this.blockRVAPositions=new List<Tuple<UInt32,UInt32>>();
			this.restoreArraySetValueFuncs=new List<String>();
			
		}
		
		public static void pairBlocks (Block b,Block b0) {
			
			b.pairedBlock=b0;
			b0.pairedBlock=b;
			
		}
		
	}
	
}
