/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/6/2021
 * Time: 6:49 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sunset.VarTypes;

namespace Sunset.Keywords {
	
	public class KWInherit : Keyword {
		
		public const String constName="INHERIT";
		
		public KWInherit () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute(Parser sender,String[]@params) {
			
			Keyword.throwIfShouldBeHeader(sender,constName);
			if (@params.Length==0) throw new ParsingError("Expected params for \""+constName+"\", but got none",sender);
			if (sender.@struct) throw new ParsingError("Can't inherit classes as a struct",sender);
			foreach (String str in @params) {
				//.... get class that is referenced (throw parsingerror if non existant/non imported)		
				if (sender.containsImportedClass(str)) {
					//.... copy static funcs
					//.... require non-private variables/arrays of parent to be defined
					//.... require non-private funcs of parent to be defined
					Class cl=sender.importedClasses.Where(x=>x.className==str).First();
					foreach(KeyValuePair<String,Function>kp in cl.functions.Where(x=>!x.Value.modifier.HasFlag(Modifier.PRIVATE)&&!x.Value.modifier.HasFlag(Modifier.STATIC))) {
						if (sender.inhFuncsToDefine.ContainsKey(kp.Key)) throw new ParsingError("Can't inherit \""+str+"\" because a function with name \""+kp.Key+"\" is already waiting to be inherited! (An inherited class is incompatible with another one because of duplicate/shared instance names)",sender);
						sender.inhFuncsToDefine.Add(kp.Key,kp.Value);
					}
					foreach(KeyValuePair<String,Tuple<UInt32,String,Modifier,UInt32>>kp in cl.variables.Where(x=>!x.Value.Item3.HasFlag(Modifier.PRIVATE)&&!x.Value.Item3.HasFlag(Modifier.STATIC))) {
						if (sender.inhVarsToDefine.ContainsKey(kp.Key)) throw new ParsingError("Can't inherit \""+str+"\" because a variable with name \""+kp.Key+"\" is already waiting to be inherited! (An inherited class is incompatible with another one because of duplicate/shared instance names)",sender);
						sender.inhVarsToDefine.Add(kp.Key,kp.Value);
					}
					foreach(KeyValuePair<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>>kp in cl.arrays.Where(x=>!x.Value.Item4.HasFlag(Modifier.PRIVATE)&&!x.Value.Item4.HasFlag(Modifier.STATIC))) {
						if (sender.inhArrsToDefine.ContainsKey(kp.Key)) throw new ParsingError("Can't inherit \""+str+"\" because an array with name \""+kp.Key+"\" is already waiting to be inherited! (An inherited class is incompatible with another one because of duplicate/shared instance names)",sender);
						sender.inhArrsToDefine.Add(kp.Key,kp.Value);
					}
					foreach(KeyValuePair<String,Tuple<UInt32,String,Class,Modifier,UInt32>> kp in cl.classes.Where(x=>!x.Value.Item4.HasFlag(Modifier.PRIVATE)&&!x.Value.Item4.HasFlag(Modifier.STATIC))) {
						if (sender.inhClassesToDefine.ContainsKey(kp.Key)) throw new ParsingError("Can't inherit \""+str+"\" because a class instance with name \""+kp.Key+"\" is already waiting to be inherited! (An inherited class is incompatible with another one because of duplicate/shared instance names)",sender);
						sender.inhClassesToDefine.Add(kp.Key,kp.Value);
					}
					sender.inheritedClasses.Add(cl);
					
					// Solution Idea:
					// at start of all classes, if (esi,=,0), set opcodeportionbytesize + inherittaaindex to ESI, then RETN
					// back at movClassItemAddrIntoEax, in func case, push esi,xor esi esi,call EAX,add EAX [EAX+ESI+fn.instanceId*4],pop esi
					
				} else throw new ParsingError("Class \""+str+"\" does not exist. Did you forget to import?",sender);
			}
	
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_NAME,newOpcodes=new Byte[0]};
			
			
		}
		
	}
	
}
