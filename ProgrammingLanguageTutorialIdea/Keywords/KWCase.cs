/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/6/2021
 * Time: 6:18 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWCase : Keyword {
		
		public const String constName="case";
		
		public KWCase () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute(Parser sender,String[] @params) {
			if (@params.Length==0) throw new ParsingError("Expected at least 1 parameter for \""+constName+'"');
			if (sender.blocks.Keys.Where(x=>x.switchBlock).Count()==0) throw new ParsingError("Can't \""+constName+"\" outside of a \""+KWSwitch.constName+"\" block.");
			KWCase.checkForCaseFallThrough(sender);
			Block caseBlock=null;
			caseBlock=new Block(null,sender.memAddress,new Byte[0],false){caseOrDefaultBlock=true,hasParentheses=false};
			if (@params.Length==1) {
				sender.pushValue(@params[0]);
				sender.addByte(0x58); //POP EAX
				sender.addBytes(new Byte[]{0x3B,0x45,sender.pseudoStack.getLatestSwitchVarOffset()}); //CMP EAX,[EBP+-OFFSET]
				caseBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount(2));
				sender.addBytes(new Byte[]{0x0F,0x85,0,0,0,0});//JNZ
				caseBlock.startMemAddr=sender.memAddress;
				sender.addBlock(caseBlock,0);
			}
			else {
				
				Int32 pl=@params.Length;
				Tuple<UInt32,UInt32>[]toBlockStart=new Tuple<UInt32,UInt32>[pl];
				UInt16 i=0;
				while (i!=pl) {
					
					sender.pushValue(@params[i]);
					sender.addByte(0x58); //POP EAX
					sender.addBytes(new Byte[]{0x3B,0x45,sender.pseudoStack.getLatestSwitchVarOffset()}); //CMP EAX,[EBP+-OFFSET]
					toBlockStart[i]=new Tuple<UInt32,UInt32>(sender.getOpcodesCount()+2,sender.memAddress);
					sender.addBytes(new Byte[]{0x0F,0x84,0,0,0,0});//JZ
					++i;
					
				}
				caseBlock.blockMemPositions.Add(sender.GetStaticInclusiveOpcodesCount(1));
				sender.addBytes(new Byte[]{0xE9,0,0,0,0});//JMP
				Byte[]memAddr;
				
				foreach (Tuple<UInt32,UInt32>tpl in toBlockStart) {
					
					memAddr=BitConverter.GetBytes((Int32)sender.memAddress-(Int32)tpl.Item2-6);
					i=0;
					
					while (i!=4) {
						
						sender.setByte(tpl.Item1+i,memAddr[i]);
						++i;
						
					}
					
					
				}
				caseBlock.startMemAddr=sender.memAddress;
				sender.addBlock(caseBlock,0);
				
				
			}
			
			return new KeywordResult(){newOpcodes=new Byte[0],newStatus=ParsingStatus.SEARCHING_COLON};
			
			
		}
		
		public static void checkForCaseFallThrough (Parser sender) {
			
			Block lastBlock=sender.blocks.Keys.Last();
			if (lastBlock.caseOrDefaultBlock) {
				lastBlock.afterBlockClosedOpcodes=null;
				sender.closeBlock(lastBlock);
			}
			
		}
		
	}
	
}
