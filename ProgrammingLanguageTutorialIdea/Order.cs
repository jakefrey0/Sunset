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
		
		private List<OrderItem>pushedValues;
		private List<Tuple<OrderItem,OrderItem,OrderMathType>>interactions;
		
		public Order () {
			
			this.pushedValues=new List<OrderItem>();
			this.interactions=new List<Tuple<OrderItem,OrderItem,OrderMathType>>();
				
		}
		
		public void addOperation (OrderItem i,OrderItem i0,OrderMathType m) {
			
			foreach (OrderItem i1 in new []{i,i0}) {
				
				if (!this.pushedValues.Contains(i1))
					this.pushedValues.Add(i1);
				
			}
			
			interactions.Add(new Tuple<OrderItem,OrderItem,OrderMathType>(i,i0,m));
			
			Console.WriteLine("Operation added - "+i.unparsedValue+','+i0.unparsedValue+','+m.ToString());
//			Console.ReadKey();
			
		}
		
		public void addOperation (List<OrderItem>items,OrderMathType m) {
			
			if (items.Count!=2) throw new Exception("Expected 2 OrderItems");
			
			this.addOperation(items[0],items[1],m);
			items[0]=items[1];
			items.RemoveAt(1);
			
		}
		
		public void writeBytes (Parser sender) {
			
			if (interactions.Count==0) return;
			
			//0x8B,0x44,0x24,SByte Offset - MOV EAX,[ESP+-OFFSET]
			//1,0x44,0x24,SByte Offset - ADD [ESP+-OFFSET],EAX
			//0x29,0x44,0x24,SByte Offset - SUB [ESP+-OFFSET],EAX
			
			//if there is an issue with pushValues.Count being 0, Push the first value and keep on stack
			
			this.pushedValues.Reverse();
			sender.writeStrOpcodes("Start of order bytes");
			foreach (Tuple<OrderItem,OrderItem,OrderMathType>interaction in this.interactions) {
				
				if (interaction.Item3==OrderMathType.ADDITION) {
					
					UInt16 offset;
					if (this.tryCheckOffsetFitsInByte(interaction.Item1,out offset))
						sender.addBytes(new Byte[]{0x8B,0x44,0x24,(Byte)offset}); //MOV EAX,[ESP+OFFSET]
					else {
						sender.addBytes(new Byte[]{0x8B,0x84,0x24}); //MOV EAX,[ESP+OFFSET]
						sender.addBytes(BitConverter.GetBytes((UInt32)offset)); //DWORD
					}
					
					if (this.tryCheckOffsetFitsInByte(interaction.Item2,out offset))
						sender.addBytes(new Byte[]{1,0x44,0x24,(Byte)(offset)}); //ADD [ESP+-OFFSET],EAX
					else {
						sender.addBytes(new Byte[]{1,0x84,0x24,(Byte)(offset)}); //ADD [ESP+-OFFSET],EAX
						sender.addBytes(BitConverter.GetBytes((UInt32)offset)); //DWORD
					}
						
					
				}
				else /*interaction.Item3==OrderMathType.SUBTRACTION*/ {
					
					UInt16 offset;
					if (this.tryCheckOffsetFitsInByte(interaction.Item1,out offset))
						sender.addBytes(new Byte[]{0x8B,0x44,0x24,(Byte)offset}); //MOV EAX,[ESP+OFFSET]
					else {
						
						sender.addBytes(new Byte[]{0x8B,0x84,0x24}); //MOV EAX,[ESP+OFFSET]
						sender.addBytes(BitConverter.GetBytes((UInt32)offset)); //DWORD
						
					}
					if (this.tryCheckOffsetFitsInByte(interaction.Item2,out offset)) 
						sender.addBytes(new Byte[]{0x2B,0x44,0x24,(Byte)offset, //SUB EAX,[ESP+OFFSET]
						                           0x89,0x44,0x24,(Byte)offset}); //MOV [ESP+OFFSET],EAX
					else
						sender.addBytes(new Byte[]{0x2B,0x84,0x24,(Byte)offset, //SUB EAX,[ESP+OFFSET]
						                           0x89,0x44,0x24,(Byte)offset}); //MOV [ESP+OFFSET],EAX
					
					//Item1: 13, Item2: 6
					//13-6=7
					//EAX:13
					//[ESP+4]:6
					//SUB EAX,[ESP+4]
					//MOV [ESP+4],EAX
					
				}
				
			}
			this.pushedValues.Reverse();
			sender.writeStrOpcodes("Esp restoration");
			
			if (this.pushedValues.Count>0) {
			
				sender.addByte(0x58);//POP EAX
				
				UInt32 espRestoration=(UInt32)((this.pushedValues.Count*4)-4);
				if (espRestoration<=SByte.MaxValue)
					sender.addBytes(new Byte[]{0x83,0xC4,(Byte)(espRestoration)});//ADD ESP,BYTE
				else {
					
					sender.addBytes(new Byte[]{0x81,0xC4}); //ADD ESP,DWWORD
					sender.addBytes(BitConverter.GetBytes(espRestoration));
					
				}
				
				sender.addByte(0x50); //PUSH EAX
			
			}
			sender.writeStrOpcodes("End of order bytes");
			
		}
		
		public void dumpData (Boolean readKey=false) {
			
			Console.WriteLine(@"
Note: interactions might be misleading because values might change over time,
these should only give a rough idea of what you are working with.

Specifically, they change if any DIVISION / MULTIPLICATION / MODULUS
is performed on a given unparsed number value (the numbers on the left and
right hand sides of the mathematical operators).
");
			
			Console.WriteLine("Total interactions: "+this.interactions.Count.ToString());
			
			foreach (Tuple<OrderItem,OrderItem,OrderMathType>tpl in this.interactions)
				Console.WriteLine("Interaction: "+tpl.Item1.unparsedValue+((tpl.Item3== OrderMathType.ADDITION)?'+':'-')+tpl.Item2.unparsedValue);
			
			Console.WriteLine("Total pushed values: "+this.pushedValues.Count.ToString());
			
			foreach (OrderItem i in this.pushedValues)
				Console.WriteLine("Pushed value: \""+i.unparsedValue+'"');
			
			if (readKey)
				Console.ReadKey();
			
		}
		
		private Boolean tryCheckOffsetFitsInByte (OrderItem pushedValue,out UInt16 offset) {
			
			offset=(UInt16)(pushedValues.IndexOf(pushedValue)*4);
			return offset<=SByte.MaxValue;
			
		}
		
	}
		
	public enum OrderMathType {
		
		ADDITION,SUBTRACTION
		
	}
	
	public class OrderItem { public String unparsedValue; }
	
}
