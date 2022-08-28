/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/30/2021
 * Time: 3:56 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Sunset.Keywords {
	
	public class Keyword {
		
		public readonly String name;
		public readonly Boolean hasParameters;
		public readonly KeywordType type;
		internal Tuple<String,VarType>outputType;
		
		public Keyword (String name,KeywordType type,Boolean hasParameters=false) { 
			
			this.name=name; 
			this.hasParameters=hasParameters;
			this.type=type;
			
		}
		
		public virtual KeywordResult execute (Parser sender,String[] @params) {
			
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_NAME,newOpcodes=new Byte[0]};
			
		}
		
		public static void throwIfShouldBeHeader (Parser sender,String kwName) {
			
			if (sender.getOpcodesCount()==sender.tableFuncBytes.Length&&sender.tableAddrIndex!=0) return;
			if (sender.getOpcodesCount()!=0||sender.getAppendAfterCount()!=0)
				throw new ParsingError("The \""+kwName+"\" keyword was expected at the header (top, beginning) of your source file",sender);
			
		}
		
	}
	
}
