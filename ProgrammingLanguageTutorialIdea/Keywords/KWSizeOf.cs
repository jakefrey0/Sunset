/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/11/2021
 * Time: 2:05 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWSizeOf : Keyword {
		
		public const String constName="sizeof";
		
		public KWSizeOf () : base (constName,KeywordType.NATIVE_CALL_WITH_RETURN_VALUE) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (@params.Length!=1)
				throw new ParsingError("Expected 1 parameter for \""+constName+"\", got "+@params.Length.ToString()+" parameters");
			
			sender.addBytes(new Byte[]{0xB8}.Concat(BitConverter.GetBytes(sender.keywordMgr.getVarTypeByteSize(sender.pushValue(@params[0]).Item1)))); //MOV EAX,DWORD
			sender.addBytes(new Byte[]{0x83,0xC4,4}); //ADD ESP,4
			outputType=new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
			
			return base.execute(sender, @params);
			
		}
		
	}
	
}
