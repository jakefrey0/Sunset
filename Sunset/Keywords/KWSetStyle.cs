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
	/// <summary>
	/// Note, in Sunset, setStyle shouldn't ever get indented because it doesn't care about blocks, i.e:
	/// if (true) {
	/// 	do (this)
	/// 	...
	/// setStyle(Array,Static)
	/// 	continueDoingThings(foo)
	/// 	yeee
	/// }
	/// </summary>
	public class KWSetStyle : Keyword {
		
		public const String constName="setStyle";
		
		public KWSetStyle () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute (Parser sender,String[]@params) {
			
			if (@params.Length!=2) throw new ParsingError("Expected 2 params for keyword \""+constName+"\" got "+@params.Length.ToString(),sender);
			
			if (@params[0].ToLower().Equals("array")) {
				switch (@params[1].ToLower()) {
					case "dynamic":
					case "heap":
						sender.style=ArrayStyle.DYNAMIC_MEMORY_HEAP;
						break;
					case "static":
						sender.style=ArrayStyle.STATIC_MEMORY_BLOCK;
						break;
					case "stackalloc":
					case "stack":
						sender.style=ArrayStyle.STACK_ALLOCATION;
						throw new ParsingError("Unimplemented",sender);//UNDONE:: do stack allocation array style. it will probably have to only exist within blocks
						//break;
					default:
						throw new ParsingError("Unexpected array style type in \""+constName+"\", expected \"dynamic\"/,\"heap\", \"static\", or \"stack\"/\"stackalloc\" but got \""+@params[1]+"\".",sender);
				}
				return base.execute(sender,@params);
			}
			
			throw new ParsingError("Unexpected style type in \""+constName+"\", expected \"array\", but got \""+@params[0]+"\".",sender);
			
		}
		
	}
}
