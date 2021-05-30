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
			                       	
			                       	new KWByte()
			                       	
			                       });
			
		}
		
		/// <returns>De-referenced keyword list</returns>
		public Keyword[] getKeywords () {
			
			return keywords.ToArray();
			
		}
		
	}
	
}
