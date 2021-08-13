/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/11/2021
 * Time: 1:44 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWLengthOf : Keyword {
		
		public const String constName="lengthof";
		
		public KWLengthOf () : base (constName,KeywordType.NATIVE_CALL_WITH_RETURN_VALUE) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (@params.Length!=1)
				throw new ParsingError("Expected 1 array parameter for \""+constName+"\", got "+@params.Length.ToString()+" parameters");
			
			VarType type=sender.pushValue(@params[0]).Item2;
			if (type!=VarType.NATIVE_ARRAY)
				throw new ParsingError("Expected an array as a parameter of \""+constName+"\", but got \""+type.ToString()+'"');
			
			sender.addBytes(new Byte[]{0x8B,4,0x24}); //MOV EAX,[ESP]
			sender.addBytes(new Byte[]{0x8B,0}); //MOV EAX,[EAX]
			sender.addBytes(new Byte[]{0x83,0xC4,4}); //ADD ESP,4
			outputType=new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
			
			return base.execute(sender, @params);
			
		}
		
	}
	
}
