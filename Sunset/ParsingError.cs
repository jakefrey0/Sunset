/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/30/2021
 * Time: 4:06 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;

namespace Sunset {
	
	public class ParsingError : Exception {
		
		public ParsingError (String message,Parser sender) : base (
			'\n'+message+"\n\n"+
			((sender==null)?"":
				"Class: "+sender.className+'\n'+
				"Path: "+sender.fileName+'\n'+
				((sender.charCtr==-1)?"":
					"\nLine: "+(sender.lastDataToParse.Substring(0,sender.charCtr).Where(x=>x=='\n').Count()+1).ToString()+", Char: "+sender.charCtr.ToString()+'\n'+
					"At: "+String.Concat(sender.lastDataToParse.Substring(sender.lastDataToParse.Substring(0,sender.charCtr).LastIndexOf('\n')+1).TakeUntil(x=>x=='\n')).Replace("\r","").Replace("\t","")+'\n'+
					String.Concat(new Char[sender.charCtr-(sender.lastDataToParse.Substring(0,sender.charCtr).LastIndexOf('\n')+1)+2])+"^\n"+
					String.Concat(new Char[sender.charCtr-(sender.lastDataToParse.Substring(0,sender.charCtr).LastIndexOf('\n')+1)+2])+'|'))
		) { }
		
	}
	
}
