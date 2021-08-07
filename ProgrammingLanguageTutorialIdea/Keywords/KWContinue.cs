/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/21/2021
 * Time: 12:08 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWContinue : Keyword {
		
		public const String constName="continue";
		
		public KWContinue () : base (constName,KeywordType.NATIVE_CALL) { }
	
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (sender.blocks.Count==0)
				throw new ParsingError("Can't continue outside of a block");
			Block block=(sender.blocks.Keys.Where(x=>x.isLoopOrSwitchBlock).Count()==0)?sender.blocks.Keys.Last():sender.blocks.Keys.Where(x=>x.isLoopOrSwitchBlock).Last();
			
			sender.addByte(0xC9);
			List<Byte>newOpcodes=new List<Byte>(new Byte[]{0xE9});
			if (!(sender.blocks.Keys.Where(x=>x.isLoopOrSwitchBlock).Count()==0)) {
				
				block=sender.blocks.Keys.Where(x=>x.isLoopOrSwitchBlock).Last();
				UInt32 bonusLeaves=(UInt32)(sender.blocks.Count-sender.blocks.Keys.Cast<Block>().ToList().IndexOf(block))-1;
				Byte[]leaves=new Byte[bonusLeaves];
				UInt32 i=0;
				while (i!=leaves.Length) {
					
					leaves[i]=0xC9;
					++i;
					
				}
				sender.addBytes(leaves);
				
			}
			
			if (block.continueAddress==0)
				throw new ParsingError("Unexpected continue (its block does not support continue)");
			
			if (block.continueInstructions!=null)
				newOpcodes.InsertRange(0,block.continueInstructions);
			
			return new KeywordResult(){newOpcodes=newOpcodes.Concat(BitConverter.GetBytes((Int32)block.continueAddress-(Int32)(sender.memAddress+newOpcodes.Count+4))).ToArray(),newStatus=ParsingStatus.SEARCHING_NAME};
			
		}
	
	}
}
