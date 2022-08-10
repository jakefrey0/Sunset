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

namespace Sunset.Keywords {
	
	public class KWTypeSizeOf : Keyword {
		
		public const String constName="Tsizeof";
		
		public KWTypeSizeOf () : base (constName,KeywordType.NATIVE_CALL_WITH_RETURN_VALUE) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (@params.Length!=1)
				throw new ParsingError("Expected 1 parameter for \""+constName+"\", got "+@params.Length.ToString()+" parameters");
			
			sender.addBytes(new Byte[]{0xB8}.Concat(BitConverter.GetBytes(sender.keywordMgr.getVarTypeByteSize(@params[0])))); //MOV EAX,DWORD
			outputType=new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
			
			return base.execute(sender, @params);
			
		}
		
	}
	
}
