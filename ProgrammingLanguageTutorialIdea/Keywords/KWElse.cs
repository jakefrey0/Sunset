/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/8/2021
 * Time: 9:31 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWElse : Keyword {
		
		public const String constName="else";
		
		public KWElse () : base (constName,KeywordType.NATIVE_CALL) { }
		
		public override KeywordResult execute(Parser sender,String[] @params) {
			
			//Sbyte Jump: 0xEB, (SByte)
			//Far Jump: 0xE9, (Signed Integer ,,,,)
			//DO NOTICE before changing the byte size of the jump opcodes take a look at Parser#elseBlockClosed
			sender.addBytes(new Byte[]{0xE9,0,0,0,0});//JMP TO MEM ADDR
			Block elseBlock=new Block(sender.elseBlockClosed,sender.memAddress,new Byte[0]);
			sender.addBlock(elseBlock);
			sender.blocksMemPositions[elseBlock].Add(sender.getOpcodesCount()-4);
			
			return base.execute(sender,@params);
			
		}
		
	}
	
}
