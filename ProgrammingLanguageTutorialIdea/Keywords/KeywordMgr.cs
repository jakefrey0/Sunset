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
		public Dictionary<String,String> synonyms;
        public Dictionary<String,String> acknowledgements;
		
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
			                       	new KWCallptr(),
			                       	new KWSwitch(),
			                       	new KWCase(),
			                       	new KWDefault(),
			                       	new KWConstructor(),
			                       	new KWExpectedTypes(),
			                       	new KWGetProcessHeap(),
			                       	new KWLengthOf(),
			                       	new KWSizeOf(),
			                       	new KWTypeSizeOf(),
                                    new KWAcknowledge(),
                                    new KWAs(),
                                    new KWCast(),
                                    new KWMultiForeach(),
                                    new KWGoto(),
                                    new KWPublic(),
                                    new KWLocal(),
                                    new KWPrivate(),
                                    new KWPullable(),
                                    new KWStatic(),
                                    new KWConstant(),
                                    new KWExit(),
                                    new KWByteSizeOf()
			                       	
			                       });
			
			this.classWords=new List<String>();
			this.synonyms=new Dictionary<String,String>();
            this.acknowledgements=new Dictionary<String,String>();
			
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
			else if (synonyms.ContainsKey(varType))
				return getVarTypeByteSize(synonyms[varType]);
            else if (acknowledgements.ContainsKey(varType))
                return getVarTypeByteSize(acknowledgements[varType]);
			else
				throw new Exception("(DEV) Invalid var type \""+varType+'"');
			
		}
		
	}
	
}
