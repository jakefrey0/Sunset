/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/30/2021
 * Time: 3:56 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class Keyword {
		
		public readonly String name;
		public readonly Boolean hasParameters;
		public readonly KeywordType type;
		public Byte[] bytes;
		
		public Keyword (String name,KeywordType type,Boolean hasParameters=false) { 
			
			this.name=name; 
			this.hasParameters=hasParameters;
			this.type=type;
			Int32 i=1632;
			bytes= new Byte[i];
			
		}
		
		public virtual KeywordResult execute (Parser sender) {
			
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_NAME,newOpcodes=new Byte[0]};
			
		}
		
	}
	
}
