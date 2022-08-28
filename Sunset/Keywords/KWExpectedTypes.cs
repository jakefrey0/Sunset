/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/8/2021
 * Time: 9:49 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunset.Keywords {
	
	public class KWExpectedTypes : Keyword {
		
		public const String constName="EXPECTED_TYPES";
		
		public KWExpectedTypes () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute(Parser sender,String[] @params) {
			
			Keyword.throwIfShouldBeHeader(sender,constName);
			if (@params.Length==0)
				throw new ParsingError("Expected at least 1 parameter for \""+constName+'"',sender);
			
			if (sender.passedVarTypes==null)
				throw new ParsingError("Expected "+@params.Length+" passed types on class import, got none",sender);
			
			if (@params.Length!=sender.passedVarTypes.Count)
				throw new ParsingError("Expected "+@params.Length+" passed types on class import, got "+sender.passedVarTypes.Count+" ("+Parser.merge(sender.passedVarTypes.Select(x=>x.Item2.Item1),", ")+')',sender);
			
			UInt32 i=0;
			List<Tuple<String,Tuple<String,VarType>>>list=new List<Tuple<String,Tuple<String,VarType>>>();
			foreach (String s in @params) {
				
				if (sender.pvtContainsKey(s)) throw new ParsingError("In \""+constName+"\", got duplicate name \""+s+'"',sender);
				else if (sender.nameExists(s)) throw new ParsingError("Name already exists: \""+s+'"',sender);
				Tuple<String,VarType> tpl=sender.passedVarTypes[(Int32)i].Item2;
				list.Add(new Tuple<String,Tuple<String,VarType>>(s,tpl));
				sender.keywordMgr.synonyms.Add(s,tpl.Item1);
				++i;
				
			}
			sender.passedVarTypes=list;
			
			return base.execute(sender, @params);
			
		}
		
	}
}
