/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/7/2021
 * Time: 11:41 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunset.Keywords {
	
	public class KWIf : Keyword {
		
		public const String constName="if",
							BOOL_OP_GREATER_THAN=">",
							BOOL_OP_GREATER_THAN_OR_EQUAL_TO=">=",
							BOOL_OP_LESS_THAN="<",
							BOOL_OP_LESS_THAN_OR_EQUAL_TO="<=",
							BOOL_OP_EQUAL_TO="=",
							BOOL_OP_NOT_EQUAL_TO="=/=";
							
		public static String lastBoolOp {
			
			get;
			private set;
								
		}
		
		public KWIf () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (@params.Length!=3&&@params.Length!=1)
				throw new ParsingError("Expected 3 or 1 necessary parameters for \""+constName+'"',sender);
			
			List<Byte>newOpcodes=new List<Byte>();
			Block ifBlock=new Block(delegate{sender.setExpectsElse=1;},sender.GetStaticInclusiveAddress(),new Byte[0],false,false);
			UInt32 opcodesCountAtStart=sender.GetStaticInclusiveOpcodesCount().index;
			
			if (@params.Length==3) {
				
				//maybe I should make a getPushValueBytes method, instead of doing this, but w.e
				sender.pushValue(@params[0]);
				sender.pushValue(@params[2]);
                newOpcodes.AddRange(new Byte[]{0x5A,0x58 });//POP EDX,POP EAX

				newOpcodes.AddRange(new Byte[]{0x39,0xD0}); // CMP EAX,EDX
				
				String boolOp=@params[1];
				KWIf.lastBoolOp=boolOp;
				sender.addBlock(ifBlock);
				
				//DO NOTICE before changing the byte size of the jump opcodes take a look at Parser#elseBlockClosed
				if (boolOp==KWIf.BOOL_OP_GREATER_THAN) {
					
					/*
					  JNG (JLE)
   				      ifBlockEnd:
					  Signed Byte Jump: (<SByte.MaxValue)
					  0x7E (Byte Distance)
					  2 Bytes
					  Signed Far Jump: (>SByte.MaxValue)
					  0x0F,0x8E,(Integer Distance)
					  6 Bytes
					*/
					
					ifBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount((UInt32)newOpcodes.Count+2));
					newOpcodes.AddRange(new Byte[]{0x0F,0x8E,0,0,0,0});//JNG
					
				}
				else if (boolOp==KWIf.BOOL_OP_GREATER_THAN_OR_EQUAL_TO||boolOp==String.Concat(KWIf.BOOL_OP_GREATER_THAN_OR_EQUAL_TO.Reverse())) {
					
					ifBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount((UInt32)newOpcodes.Count+2));
					newOpcodes.AddRange(new Byte[]{0x0F,0x8C,0,0,0,0});//JNGE
					
				}
				else if (boolOp==KWIf.BOOL_OP_LESS_THAN) {
					
					ifBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount((UInt32)newOpcodes.Count+2));
					newOpcodes.AddRange(new Byte[]{0x0F,0x8D,0,0,0,0});//JNL
					
				}
				else if (boolOp==KWIf.BOOL_OP_LESS_THAN_OR_EQUAL_TO||boolOp==String.Concat(KWIf.BOOL_OP_LESS_THAN_OR_EQUAL_TO.Reverse())) {
					
					ifBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount((UInt32)newOpcodes.Count+2));
					newOpcodes.AddRange(new Byte[]{0x0F,0x8F,0,0,0,0});//JNLE
					
				}
				else if (boolOp==KWIf.BOOL_OP_EQUAL_TO) {
					
					ifBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount((UInt32)newOpcodes.Count+2));
					newOpcodes.AddRange(new Byte[]{0x0F,0x85,0,0,0,0});//JNZ
					
				}
				else if (boolOp==KWIf.BOOL_OP_NOT_EQUAL_TO) {
					
					ifBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount((UInt32)newOpcodes.Count+2));
					newOpcodes.AddRange(new Byte[]{0x0F,0x84,0,0,0,0});//JZ
					
				}
				else throw new ParsingError("Invalid boolean operator \""+boolOp+'"',sender);
				
				ifBlock.startMemAddr+=(UInt32)(newOpcodes.Count+(sender.GetStaticInclusiveOpcodesCount().GetIndexAsInt()-opcodesCountAtStart));
				newOpcodes.AddRange(sender.getEnterBlockOpcodes(ifBlock,newOpcodes.Count));
				return new KeywordResult(){newOpcodes=newOpcodes.ToArray(),newStatus=ParsingStatus.SEARCHING_NAME};
				
			}
			else /*@params.Length==1*/ {
				
				sender.addBlock(ifBlock);
				
				Console.WriteLine("Param: \""+@params[0]+'"');
				Boolean meansNot=false;
				if (KWIf.meansNot(@params[0][0])) {
					@params[0]=@params[0].Substring(1);
					meansNot=true;
				}
				Tuple<String,VarType>result=sender.pushValue(@params[0]);
				if (result.Item2!=VarType.NATIVE_VARIABLE)
					throw new ParsingError("Expected a native variable as if statement parameters (did you mean to use the 3 parameters syntax?)",sender);
				else if (result.Item1!=KWBoolean.constName)
					throw new ParsingError("Expected a boolean variable as if statement parameters (did you mean to use the 3 parameters syntax?)",sender);
				
				newOpcodes.Add(0x58);//POP EAX
				newOpcodes.AddRange(new Byte[]{0x84,0xC0});//TEST AL,AL
				ifBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount((UInt32)newOpcodes.Count+2));
				newOpcodes.AddRange((meansNot)
				    ?new Byte[]{0x0F,0x85,0,0,0,0}  //JNE (DISTANCE AS SIGNED INTEGER)          
				    :new Byte[]{0x0F,0x84,0,0,0,0});//JE  (DISTANCE AS SIGNED INTEGER)
				
				ifBlock.startMemAddr+=(UInt32)(newOpcodes.Count+(sender.GetStaticInclusiveOpcodesCount().GetIndexAsInt()-opcodesCountAtStart));
				newOpcodes.AddRange(sender.getEnterBlockOpcodes(ifBlock,newOpcodes.Count));
				return new KeywordResult(){newOpcodes=newOpcodes.ToArray(),newStatus=ParsingStatus.SEARCHING_NAME};
				
			}
			
			
		}
		
		public static Boolean meansNot (Char c) {
			
			return c=='!';
			
		}
		
	}
	
}
