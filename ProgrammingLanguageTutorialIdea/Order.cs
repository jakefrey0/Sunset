/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/28/2021
 * Time: 2:31 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace ProgrammingLanguageTutorialIdea {
	
	public class Order {
		
		private List<OrderItem>pushedValues,unparsedValues;
		private List<Tuple<OrderItem,OrderItem,OrderMathType>>interactions;
		
		public Order () {
			
			this.pushedValues=new List<OrderItem>();
			this.unparsedValues=new List<OrderItem>();
			this.interactions=new List<Tuple<OrderItem,OrderItem,OrderMathType>>();
				
		}
		
		public void addOperation (OrderItem i,OrderItem i0,OrderMathType m) {
			
			foreach (OrderItem i1 in new []{i,i0}) {
				
				if (i1.type==OrderItemType.PARSED)
					this.pushedValues.Add(i1);
				else
					this.unparsedValues.Add(i1);
				
			}
			
			interactions.Add(new Tuple<OrderItem,OrderItem,OrderMathType>(i,i0,m));
			
		}
		
		public void addOperation (List<OrderItem>items,OrderMathType m) {
			
			if (items.Count!=2) throw new Exception("Expected 2 OrderItems");
			
			this.addOperation(items[0],items[1],m);
			items.Clear();
			
		}
		
		public void writeBytes (Parser sender) {
			
			if (interactions.Count==0) return;
			
			//0x8B,0x44,0x24,SByte Offset - MOV EAX,[ESP+-OFFSET]
			//1,0x44,0x24,SByte Offset - ADD [ESP+-OFFSET],EAX
			//0x29,0x44,0x24,SByte Offset - SUB [ESP+-OFFSET],EAX
			
			this.interactions.Reverse();
			this.pushedValues.Reverse();
			foreach (Tuple<OrderItem,OrderItem,OrderMathType>tpl in this.interactions) {
				if (tpl.Item1.type==OrderItemType.PARSED) {
					
					// mov to eax
					UInt16 espOffset=(UInt16)(pushedValues.IndexOf(tpl.Item1)*4);
					if (espOffset<=SByte.MaxValue)
						sender.addBytes(new Byte[]{0x8B,0x44,0x24,(Byte)(espOffset)});//MOV EAX,[ESP+-OFFSET]
					else
						;//UNDONE::
					
				}
				else {
					
					sender.pushValue(tpl.Item1.unparsedValue);
					sender.addByte(0x58); //POP EAX
					
				}
				
				if (tpl.Item3== OrderMathType.ADDITION) {
					
					if (tpl.Item2.type==OrderItemType.PARSED) {
						
						UInt16 espOffset=(UInt16)(pushedValues.IndexOf(tpl.Item2)*4);
						if (espOffset<=SByte.MaxValue)
							sender.addBytes(new Byte[]{1,0x44,0x24,(Byte)(espOffset)}); //ADD [ESP+-OFFSET],EAX
						else
							;//UNDONE::
							
					}
					else {
						
						sender.pushValue(tpl.Item2.unparsedValue);
						sender.addBytes(new Byte[]{1,4,0x24}); //ADD [ESP],EAX
						sender.addBytes(new Byte[]{1,0x44,0x24,4}); //ADD [ESP+4],EAX
						
					}
					
				}
				else /*==OrderMathType.SUBTRACTION */ {
					
					if (tpl.Item2.type==OrderItemType.PARSED) {
						
						UInt16 espOffset=(UInt16)(pushedValues.IndexOf(tpl.Item2)*4);
						if (espOffset<=SByte.MaxValue)
							sender.addBytes(new Byte[]{0x29,0x44,0x24,(Byte)(espOffset)}); //SUB [ESP+-OFFSET],EAX
						else
							;//UNDONE::
							
					}
					else {
						
						sender.pushValue(tpl.Item2.unparsedValue);
						sender.addBytes(new Byte[]{0x29,4,0x24}); //SUB [ESP],EAX
						sender.addBytes(new Byte[]{0x29,0x44,0x24,4}); //SUB [ESP+4],EAX
					
					}
					
				}
				
			}
			
			sender.addByte(0x58);//POP EAX
			
			UInt16 res=(UInt16)(pushedValues.Count*4);
			
			if (res<=SByte.MaxValue)
				sender.addBytes(new Byte[]{0x83,0xC4,(Byte)res}); //SUB ESP,SBYTE
			else {
				
				sender.addBytes(new Byte[]{0x81,0xC4}); //SUB ESP,DWORD
				sender.addBytes(BitConverter.GetBytes((UInt32)res));
				
			}
			
			sender.addByte(0x50);//PUSH EAX
			
		}
		
		public void dumpData (Boolean readKey=false) {
			
			Console.WriteLine(@"
Note: interactions might be misleading because values might change over time,
these should only give a rough idea of what you are working with.

Specifically, they change if any DIVISION / MULTIPLICATION / MODULUS
is performed on a given unparsed number value (the numbers on the left and
right hand sides of the mathematical operators).
");
			
			foreach (Tuple<OrderItem,OrderItem,OrderMathType>tpl in this.interactions)
				Console.WriteLine("Interaction: "+tpl.Item1.unparsedValue+((tpl.Item3== OrderMathType.ADDITION)?'+':'-')+tpl.Item2.unparsedValue);
			
			foreach (OrderItem i in this.pushedValues)
				Console.WriteLine("Pushed value: \""+i.unparsedValue+'"');
			
			foreach (OrderItem i in this.unparsedValues)
				Console.WriteLine("Unparsed value: \""+i.unparsedValue+'"');
			
			if (readKey)
				Console.ReadKey();
			
		}
		
	}
	
	public enum OrderItemType {
		
		PARSED,UNPARSED
		
	}
	
	public enum OrderMathType {
		
		ADDITION,SUBTRACTION
		
	}
	
	public struct OrderItem {
		
		public OrderItemType type;
		public String unparsedValue;
		
	}
	
}
