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
		
		/// <summary>
		/// (Key) Var Name, (Value) (Var Type,EBP offset)
		/// </summary>
		public Dictionary<String,Tuple<Tuple<String,VarType>,SByte>> localVariables;
		
		public readonly Boolean shouldXOREAX,addEnterAutomatically;
		
		public Block (Action onBlockEnd,UInt32 startMemAddr,Byte[] opcodesToAddOnBlockEnd,Boolean xorEax=false,Boolean addEnterAutomatically=true) {
			
			this.startMemAddr=startMemAddr;
			this.onBlockEnd=onBlockEnd;
			this.opcodesToAddOnBlockEnd=opcodesToAddOnBlockEnd;
			this.localVariables=new Dictionary<String,Tuple<Tuple<String,VarType>,SByte>>();
			this.shouldXOREAX=xorEax;
			this.addEnterAutomatically=addEnterAutomatically;
			
		}
		
	}
	
}
