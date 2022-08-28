/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/20/2021
 * Time: 5:21 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;

namespace Sunset.Keywords {
	
	public class KWBreak : Keyword {
		
		public const String constName="break";
		
		public KWBreak () : base (constName,KeywordType.NATIVE_CALL) { }
		
		public override KeywordResult execute (Parser sender,String[]@params) {
			
			if (sender.blocks.Count==0)
				throw new ParsingError("Can't break outside of a block",sender);
			
			Action ac=null;
			Block block=(sender.blocks.Keys.Where(x=>x.isLoopOrSwitchBlock).Count()==0)?sender.blocks.Keys.Last():sender.blocks.Keys.Where(x=>x.isLoopOrSwitchBlock).Last();
			
			List<Byte>newOpcodes=new List<Byte>(new Byte[]{0xC9,0xE9,0,0,0,0});
			if (sender.blocks.Keys.Where(x=>x.isLoopOrSwitchBlock).Count()==0) {
				block.blockRVAPositions.Add(new Tuple<UInt32,UInt32>((UInt32)(sender.GetStaticInclusiveOpcodesCount().index+2+(block.breakInstructions==null?0:block.breakInstructions.Length)),(UInt32)(sender.GetStaticInclusiveAddress()+6+(block.breakInstructions==null?0:block.breakInstructions.Length))));
				if (block.breakInstructions!=null)
					newOpcodes.InsertRange(1,block.breakInstructions);
			}
			else {
				
				UInt32 bonusLeaves=(UInt32)(sender.blocks.Count-sender.blocks.Keys.Cast<Block>().ToList().IndexOf(sender.blocks.Keys.Where(x=>x.isLoopOrSwitchBlock).Last()))-1;
				ac=delegate {
					Block block0=sender.blocks.Keys.Last();
					if (block0.caseOrDefaultBlock) {
						
						--bonusLeaves;
						sender.closeBlock(block0);
						
					}
				};
				Byte[]leaves=new Byte[bonusLeaves];
				UInt32 i=0;
				while (i!=leaves.Length) {
					
					leaves[i]=0xC9;
					++i;
					
				}
				newOpcodes.InsertRange(0,leaves);
				sender.blocks.Keys.Where(x=>x.isLoopOrSwitchBlock).Last().blockRVAPositions.Add(new Tuple<UInt32,UInt32>((UInt32)(sender.GetStaticInclusiveOpcodesCount().index+2+bonusLeaves+(block.breakInstructions==null?0:block.breakInstructions.Length)),(UInt32)(sender.GetStaticInclusiveAddress()+6+bonusLeaves+(block.breakInstructions==null?0:block.breakInstructions.Length))));
				
				if (block.breakInstructions!=null)
					newOpcodes.InsertRange((Int32)bonusLeaves+1,block.breakInstructions);
				
			}
			
			return new KeywordResult(){newOpcodes=newOpcodes.ToArray(),newStatus=ParsingStatus.SEARCHING_NAME,action=ac};
		}
		
	}
	
}
