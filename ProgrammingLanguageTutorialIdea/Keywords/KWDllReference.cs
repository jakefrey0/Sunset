/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/8/2021
 * Time: 9:31 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWDllReference : Keyword {
		
		public const String constName="dllref";
		
		public KWDllReference () : base (constName,KeywordType.FUNCTION,true) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (@params.Length==0)
				throw new ParsingError("Expected 1 or more parameters for \""+constName+'"');
			
			List<Tuple<String,VarType>>varTypes=new List<Tuple<String,VarType>>(@params.Length-1);
			foreach (String s in @params.Skip(1))
				varTypes.Add(sender.getVarType(s));
			
			sender.nextFunctionParamTypes=varTypes.ToArray();
			sender.nextType=FunctionType.DLL_REFERENCED;
			sender.nextReferencedDLL=@params[0];
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_FUNCTION_NAME,newOpcodes=new Byte[0]};
			
		}
		
	}
	
}