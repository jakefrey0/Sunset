/*
 * Created by SharpDevelop.
 * User: GDSPIOSJDGOSDHGJSDhg
 * Date: 9/10/2022
 * Time: 8:56 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using Sunset.Stack;
using System.Collections.Generic;

namespace Sunset.Keywords {
	
	public class KWSetOrigin : Keyword {
		
		public const String constName="setOrigin";
		
		public KWSetOrigin () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute (Parser sender,String[]@params) {
			
			if (@params.Length!=1) throw new ParsingError("Expected 1 param for keyword \""+constName+"\" got "+@params.Length.ToString(),sender);
			
			UInt32 newAddr;
			if (!UInt32.TryParse(@params[0],out newAddr))
				throw new ParsingError("Invalid origin: \""+@params[0]+"\" (should be a memory address)",sender);
			
			sender.memAddress=newAddr;
			return base.execute(sender,@params);
			
		}
		
	}
}
