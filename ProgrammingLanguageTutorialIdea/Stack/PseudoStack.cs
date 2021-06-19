/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/17/2021
 * Time: 12:21 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgrammingLanguageTutorialIdea.Stack {
	
	public class PseudoStack  {
		
		private List<IPseudoStackItem> items;
		
		public PseudoStack () { items=new List<IPseudoStackItem>(); }
		
		public void push (IPseudoStackItem item) {
			
			this.items.Add(item);
			
		}
		
		public void pop (UInt16 pops=1) {
			
			while (pops!=0) {
				
				this.items.RemoveAt(this.items.Count-1);
				--pops;
				
			}
			
		}
		
		public Byte getVarEbpOffset (String varName) {
			
			Int32 topEbpIndex=items.FindLastIndex(x=>x.type==ItemType.PRESERVED_EBP)
				 ,varIndex=items.IndexOf(items.Where(x=>x.type==ItemType.LOCAL_VAR&&((LocalVar)x).varName==varName).First());
			
			return unchecked((Byte)((SByte)((topEbpIndex-varIndex)*4)));
			
		}
		
		public void printStackDump (Boolean readKey=false) {
			
			Console.Write("\n\n== Stack ==\nTotal # of items: "+this.items.Count.ToString()+"\n\n");
			
			UInt16 i=0;
			foreach (IPseudoStackItem stackItem in this.items.ToArray().Reverse()) {
				
				switch (stackItem.type) {
						
					case ItemType.LOCAL_VAR:
						String varName=((LocalVar)stackItem).varName;
						Byte offset=this.getVarEbpOffset(varName);
						Console.WriteLine("Item #"+i.ToString()+": Local var \""+varName+"\", list index: "+this.items.IndexOf(stackItem).ToString()+", current EBP offset: "+offset.ToString("X").PadLeft(2,'0')+"h ("+unchecked((SByte)offset).ToString()+')');
						break;
					case ItemType.PRESERVED_EBP:
						Console.WriteLine("Item #"+i.ToString()+": Preserved EBP ptr, list index: "+this.items.IndexOf(stackItem).ToString());
						break;
					case ItemType.RETURN_PTR:
						Console.WriteLine("Item #"+i.ToString()+": Call return ptr, list index: "+this.items.IndexOf(stackItem).ToString());
						break;
				}
				++i;
				
			}
			
			Console.Write("\n\n===========\n\n");
			
			if (readKey)
				Console.ReadKey();
			
		}
		
	}
	
}
