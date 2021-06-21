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
			
			List<Byte>newOpcodes=new List<Byte>(new Byte[]{0xC9,0xE9});
			Block block;
			if (sender.blocks.Keys.Where(x=>x.isLoopBlock).Count()==0)
				block=sender.blocks.Keys.Last();
			else {
				
				block=sender.blocks.Keys.Where(x=>x.isLoopBlock).Last();
				UInt32 bonusLeaves=(UInt32)(sender.blocks.Count-sender.blocks.Keys.Cast<Block>().ToList().IndexOf(block))-1;
				Byte[]leaves=new Byte[bonusLeaves];
				UInt32 i=0;
				while (i!=leaves.Length) {
					
					leaves[i]=0xC9;
					++i;
					
				}
				newOpcodes.InsertRange(0,leaves);
				
			}
			
			if (block.continueAddress==0)
				throw new ParsingError("Unexpected continue (its block does not support continue)");
			
			return new KeywordResult(){newOpcodes=newOpcodes.Concat(BitConverter.GetBytes((Int32)block.continueAddress-(Int32)(sender.memAddress+newOpcodes.Count+4))).ToArray(),newStatus=ParsingStatus.SEARCHING_NAME};
			
		}
	
	}
}
