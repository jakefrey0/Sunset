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

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWBreak : Keyword {
		
		public const String constName="break";
		
		public KWBreak () : base (constName,KeywordType.NATIVE_CALL) { }
		
		public override KeywordResult execute (Parser sender,String[]@params) {
			
			if (sender.blocks.Count==0)
				throw new ParsingError("Can't break outside of a block");
			
			List<Byte>newOpcodes=new List<Byte>(new Byte[]{0xC9,0xE9,0,0,0,0});
			if (sender.blocks.Keys.Where(x=>x.isLoopBlock).Count()==0) {
				sender.blocks.Keys.Last().blockRVAPositions.Add(new Tuple<UInt32,UInt32>(sender.getOpcodesCount()+2,sender.memAddress+6));
			}
			else {
				
				UInt32 bonusLeaves=(UInt32)(sender.blocks.Count-sender.blocks.Keys.Cast<Block>().ToList().IndexOf(sender.blocks.Keys.Where(x=>x.isLoopBlock).Last()))-1;
				Byte[]leaves=new Byte[bonusLeaves];
				UInt32 i=0;
				while (i!=leaves.Length) {
					
					leaves[i]=0xC9;
					++i;
					
				}
				newOpcodes.InsertRange(0,leaves);
				sender.blocks.Keys.Where(x=>x.isLoopBlock).Last().blockRVAPositions.Add(new Tuple<UInt32,UInt32>(sender.getOpcodesCount()+2+bonusLeaves,sender.memAddress+6+bonusLeaves));
				
			}
			return new KeywordResult(){newOpcodes=newOpcodes.ToArray(),newStatus=ParsingStatus.SEARCHING_NAME};
		}
		
	}
	
}
