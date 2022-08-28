/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/4/2021
 * Time: 7:16 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Sunset.Keywords {
	
	public class KWNofunc : Keyword {
		
		public const String constName="nofunct";
		
		public KWNofunc () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (@params.Length==1) {
				
				String param=@params[0];
				UInt32 i;
				if (UInt32.TryParse(param,out i)) {
					Byte[]n=new Byte[i];
					UInt32 i0=0;
					while (i0!=i) {
						n[i0]=0x90; //NOP
						++i0;
					}
					return new KeywordResult(){newOpcodes=n,newStatus=ParsingStatus.SEARCHING_NAME};
				}
				
				switch (param) {
					case "DUMP_STACK_IMAGE":
						sender.pseudoStack.printStackDump();
						break;
					case "DUMP_STACK_IMAGE_READKEY":
						sender.pseudoStack.printStackDump(true);
						break;
					case "READKEY":
						Console.ReadKey(true);
						break;
					default:
						throw new ParsingError("Invalid compiler instruction for \""+constName+"\": \""+@params[0]+"\"",sender);
						
				}
				
			}
			else if (@params.Length!=0)
				throw new ParsingError("Expected 1 or 0 parameters for \""+constName+'"',sender);
			
			
//			Console.ReadKey();
			return new KeywordResult(){newOpcodes=new Byte[]{0x90},newStatus= ParsingStatus.SEARCHING_NAME};
			
		}
		
	}
	
}
