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
		public List<String> classWords;
		
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
			                       	new KWReturn(),
			                       	new KWString(),
			                       	new KWDllReference(),
			                       	new KWWhile(),
			                       	new KWBreak(),
			                       	new KWContinue(),
			                       	new KWFinishCompiling(),
			                       	new KWToggleGui(),
			                       	new KWImport(),
			                       	new KWStruct(),
			                       	new KWNew(),
			                       	new KWVoid(),
			                       	new KWSetProcessHeapVar(),
			                       	new KWForeach(),
			                       	new KWCallptr()
			                       	
			                       });
			
			this.classWords=new List<String>();
			
		}
		
		/// <returns>De-referenced keyword list</returns>
		public Keyword[] getKeywords () {
			
			return keywords.ToArray();
			
		}
		
		public UInt32 getVarTypeByteSize (String varType) {
			
			//HACK:: check variable type
			if (varType==KWByte.constName||varType==KWBoolean.constName||varType==Parser.NULL_STR)
				return 1;
			else if (varType==KWShort.constName)
				return 2;
			else if (varType==KWInteger.constName||varType==KWString.constName||this.classWords.Contains(varType))
				return 4;
			else
				throw new Exception("(DEV) Invalid var type \""+varType+'"');
			
		}
		
	}
	
}
