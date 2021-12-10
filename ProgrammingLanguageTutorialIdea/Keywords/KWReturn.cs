/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/8/2021
 * Time: 11:19 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;

namespace ProgrammingLanguageTutorialIdea.Keywords { 
	
	public class KWReturn : Keyword {
		
		public const String constName="retn";
		
		public KWReturn () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (@params.Length==0) {
				
				if (sender.inFunction&&sender.functions.Last().Value.Item2!=null)
					throw new ParsingError("Expected return value");
				
				if (sender.inFunction) {
					exitBlocks(sender);
					sender.blockAddrBeforeAppendingReferences[sender.lastFunctionBlock].Add(new Tuple<UInt32,Int16>(sender.GetStaticInclusiveOpcodesCount().index+1,0));
					return new KeywordResult{newOpcodes=new Byte[]{0xE9,}.Concat(BitConverter.GetBytes((UInt32)(sender.GetStaticInclusiveAddress()+1))).ToArray(),newStatus=ParsingStatus.SEARCHING_NAME};
				}
				else {
					this.exitBlocks(sender);
					sender.freeHeapsRefs.Add(sender.getOpcodesCount()+1);
					//                                                                                                 v--- this is +7 instead of +5 so that it will calculate to jump to not POP EAX and RETN but PUSH 0, POP EAX and RETN
					return new KeywordResult{newOpcodes=new Byte[]{0xE9}.Concat(BitConverter.GetBytes(sender.memAddress+7)).ToArray(), //JMP TO RELATIVE MEM ADDR
											newStatus=ParsingStatus.SEARCHING_NAME};												 
				}
				
			}
			else {
				
				if (@params.Length!=1)
					throw new ParsingError("Expected 1 or 0 parameters for \""+constName+'"');
				
				Tuple<String,VarType>retType=sender.pushValue(@params[0]);
				
				if (!sender.inFunction) {
//					Console.ReadKey();
					sender.addByte(0x58); //POP EAX
					this.exitBlocks(sender);
					sender.freeHeapsRefs.Add(sender.getOpcodesCount()+1);
					return new KeywordResult{newOpcodes=new Byte[]{0xE9}.Concat(BitConverter.GetBytes(sender.memAddress+4)).ToArray(), //JMP TO RELATIVE MEM ADDR
											newStatus=ParsingStatus.SEARCHING_NAME};
				}
				
				sender.addByte(0x58); //POP EAX
				
				this.exitBlocks(sender);
				
				if (sender.functions.Last().Value.Item2==null)
					throw new ParsingError("Did not expect return value");
				
				if ((sender.functions.Last().Value.Item2.Item2==VarType.NATIVE_VARIABLE&&retType.Item1!=KWString.constName)||sender.keywordMgr.getVarTypeByteSize(sender.functions.Last().Value.Item2.Item1)>=sender.keywordMgr.getVarTypeByteSize(retType.Item1))
					goto skipCheck;
				
				if (!sender.functions.Last().Value.Item2.Equals(retType))
					throw new ParsingError("Unexpected variable return type (Expected \""+sender.functions.Last().Value.Item2.Item1+"\" of \""+sender.functions.Last().Value.Item2.Item2+"\", yet the return value was \""+retType.Item1+"\" of \""+retType.Item2+"\")");
				
				skipCheck:
				
//				sender.pseudoStack.printStackDump(true);
				sender.blockAddrBeforeAppendingReferences[sender.lastFunctionBlock].Add(new Tuple<UInt32,Int16>(sender.GetStaticInclusiveOpcodesCount().index+1,0));
				return new KeywordResult{newOpcodes=new Byte[]{0xE9}.Concat(BitConverter.GetBytes((UInt32)(sender.GetStaticInclusiveAddress()+1))).ToArray(),newStatus=ParsingStatus.SEARCHING_NAME};
			
			}
				
		}
		
		private void exitBlocks (Parser sender) {
			
			UInt16 blocksToExit=(UInt16)((sender.blocks.Count-sender.blocks.Keys.ToList().IndexOf(sender.lastFunctionBlock))-1);
            sender.addBytes(new Byte[blocksToExit].Select(x=>x=0xC9)); // LEAVE (* blocksToExit)
			
		}
		
	}
	
}
