/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/30/2021
 * Time: 3:56 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KeywordMgr {
		
		private List<Keyword> keywords;
		
		public KeywordMgr () {
			
			this.keywords=new List<Keyword>(new Keyword[] {
			                       	
			                       	new KWByte(),
			                       	new KWBecomes(),
			                       	new KWIncrease(),
			                       	new KWDecrease(),
			                       	new KWShort(),
			                       	new KWInteger(),
			                       	new KWCrash(),
			                       	new KWNofunc(),
			                       	new KWIf(),
			                       	new KWElse(),
			                       	new KWBoolean(),
			                       	new KWFunction(),
			                       	new KWReturn()
			                       	
			                       });
			
		}
		
		/// <returns>De-referenced keyword list</returns>
		public Keyword[] getKeywords () {
			
			return keywords.ToArray();
			
		}
		
		public UInt32 getVarTypeByteSize (String varType) {
			
			//HACK:: check variable type
			if (varType==KWByte.constName||varType==KWBoolean.constName)
				return 1;
			else if (varType==KWShort.constName)
				return 2;
			else if (varType==KWInteger.constName)
				return 4;
			else
				throw new Exception("(DEV) Invalid var type \""+varType+'"');
			
		}
		
	}
	
}
