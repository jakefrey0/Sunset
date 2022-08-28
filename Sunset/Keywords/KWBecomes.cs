/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/30/2021
 * Time: 11:32 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Sunset.Keywords {
	
	public class KWBecomes : Keyword {
		
		public const String constName="becomes";
		
		public KWBecomes () : base (constName,KeywordType.ASSIGNMENT) { }
		
		override public KeywordResult execute (Parser sender,String[] @params) {
			
			if (String.IsNullOrEmpty(sender.lastReferencedVariable))
				throw new ParsingError("Invalid use of \""+constName+"\", no referenced variable found",sender);
			
			sender.referencedVariable=sender.lastReferencedVariable;
			sender.referencedVarType=sender.lastReferencedVarType;
			sender.referencedVariableIsLocal=sender.lastReferencedVariableIsLocal;
			sender.referencedVariableIsFromClass=sender.lastReferencedVariableIsFromClass;
            sender.referencedVariableIsStatic=sender.lastReferencedVariableIsStatic;
            sender.lastReferencedVariableIsStatic=false;
			Console.WriteLine("Becomes: referencedVariableIsLocal: "+sender.referencedVariableIsLocal.ToString());
			
			Byte[] newOpcodes=new Byte[0];
			
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_VALUE,newOpcodes=newOpcodes};
			
		}
		
	}
	
}
