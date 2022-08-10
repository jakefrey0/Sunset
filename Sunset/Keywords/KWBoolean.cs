/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/8/2021
 * Time: 12:47 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Sunset.Keywords {
	
	public class KWBoolean : Keyword {
		
		public const String constName="bool",//maybe call this "twofold" cause it's funny
						    constTrue="true",
						  constFalse="false";
		
		public KWBoolean () : base (constName,KeywordType.TYPE) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			//booleans will be false by default since 0==false
			
			sender.varType=this.name;
			sender.lastReferencedVarType=VarType.NATIVE_VARIABLE;
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_VARIABLE_NAME,newOpcodes=new Byte[0]};
			
		}
		
	}
}
