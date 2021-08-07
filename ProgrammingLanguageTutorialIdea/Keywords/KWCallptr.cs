/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/5/2021
 * Time: 5:51 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	/// <summary>
	/// This is an unsafe keyword because it requires the parameters to be their expected values, though it places no restrictions (as the compiler doesn't know any)
	/// </summary>
	public class KWCallptr : Keyword {
		
		public const String constName="callptr";
		
		public KWCallptr () : base (constName,KeywordType.NATIVE_CALL_WITH_RETURN_VALUE,true) { }
		
		public override KeywordResult execute(Parser sender,String[] @params) {
			
			if (@params.Length==0)
				throw new ParsingError("Expected at least 1 parameter for \""+constName+"\" (func address as \""+KWInteger.constName+"\", + optional function parameters)");
			
			outputType=new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
			if (!sender.pushValue(@params[0]).Equals(outputType))
				throw new ParsingError("Expected an unsigned integer (\""+KWInteger.constName+"\") as the first parameter of \""+constName+"\", as the func address!");
			
			Byte i=0;
			
			if (@params.Length!=1) {
				foreach (String s in @params.Skip(1)) {
					sender.pushValue(s);
					i+=4;
				}
			}

			sender.addBytes(new Byte[]{0xFF,0x54,0x24,i}); //CALL [ESP+-OFFSET]
			sender.addBytes(new Byte[]{0x83,0xC4,4}); //ADD ESP,4
			
			return base.execute(sender, @params);
			
		}
		
	}
	
}
