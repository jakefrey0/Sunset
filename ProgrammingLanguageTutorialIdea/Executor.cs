/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/10/2021
 * Time: 12:50 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ProgrammingLanguageTutorialIdea.Keywords;
using System.Collections.Generic;

namespace ProgrammingLanguageTutorialIdea {
	
	public struct Executor {
		
		public Keyword kw;
		public String func;
		/// <summary>
		/// Class Origin,Func,Is origin local
		/// </summary>
		public Tuple<IEnumerable<String>,String,Boolean>classFunc;
        public Tuple<Class,String>externalStaticFunc;
        public Tuple<String,String,String>internalStaticFunc;
		
	}
	
}
