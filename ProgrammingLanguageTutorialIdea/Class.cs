/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 7/11/2021
 * Time: 2:50 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ProgrammingLanguageTutorialIdea.Keywords;

namespace ProgrammingLanguageTutorialIdea {
	
	public class Class {
		
		public readonly String className;
		public readonly UInt32 byteSize,opcodePortionByteSize,initialAppendAfterCount,classAppendAfterCount,bytesToReserve;
		public readonly ClassType classType;
		public UInt32 memAddr;
		public Dictionary<String,Tuple<UInt32,String>>variables;
		public Dictionary<String,Tuple<UInt32,String,Class>>classes;//Name,(Offset To Mem Address of Heap Handle,Class type name,Class type)
		public Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention>>functions;
		public Dictionary<String,Tuple<UInt32,String,ArrayStyle>> arrays=new Dictionary<String,Tuple<UInt32,String,ArrayStyle>>();//Name,(Ptr To Mem Address of Heap Handle(Dynamic) or Mem Block(Static),Array Var Type,ArrayStyle(Dynamic or Static))
		public Tuple<UInt32,List<Tuple<String,VarType>>> constructor;//Memory Address,Func Param Types
		private List<String>defineTimeOrder;
		
		public Class (String className,UInt32 byteSize,ClassType classType,UInt32 memAddr,Parser parserUsed,UInt32 opcodePortionByteSize,UInt32 initialAppendAfterCount,UInt32 classAppendAfterCount) {
			
			this.bytesToReserve=parserUsed.compiledBytesFinalNo;
			this.className=className;
			this.byteSize=byteSize;
			this.classType=classType;
			this.memAddr=memAddr;
			this.variables=new Dictionary<String,Tuple<UInt32,String>>();
			this.classes=new Dictionary<String,Tuple<UInt32,String,Class>>();
			this.functions=new Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention>>();
			this.defineTimeOrder=new List<String>(parserUsed.defineTimeOrder);
			this.opcodePortionByteSize=opcodePortionByteSize;
			this.initialAppendAfterCount=initialAppendAfterCount;
			this.classAppendAfterCount=classAppendAfterCount;
			foreach (KeyValuePair<String,Tuple<UInt32,String>>kvp in parserUsed.getVariables())
				this.variables.Add(kvp.Key,new Tuple<UInt32,String>(kvp.Value.Item1-parserUsed.memAddress,kvp.Value.Item2));
			foreach (KeyValuePair<String,Tuple<UInt32,String,Class>>kvp in parserUsed.getClasses())
				this.classes.Add(kvp.Key,new Tuple<UInt32,String,Class>(kvp.Value.Item1-parserUsed.memAddress,kvp.Value.Item2,kvp.Value.Item3));
			this.functions=parserUsed.getFunctions();
			foreach (KeyValuePair<String,Tuple<UInt32,String,ArrayStyle>>kvp in parserUsed.getArrays())
				this.arrays.Add(kvp.Key,new Tuple<UInt32,String,ArrayStyle>(kvp.Value.Item1-parserUsed.memAddress,kvp.Value.Item2,kvp.Value.Item3));
			this.constructor=parserUsed.constructor;
					
		}
		
		public Tuple<String,VarType> getVarType (String name) {
			
			if (variables.ContainsKey(name))
				return new Tuple<String,VarType>(this.variables[name].Item2,VarType.NATIVE_VARIABLE);
			else if (classes.ContainsKey(name))
				return new Tuple<String,VarType>(this.classes[name].Item2,VarType.CLASS);
			else if (functions.ContainsKey(name))
				return this.functions[name].Item2;
			else if (arrays.ContainsKey(name))
				return new Tuple<String,VarType>(this.arrays[name].Item2,VarType.NATIVE_ARRAY_INDEXER);
			else throw new ParsingError("Variable \""+name+"\" does not exist in \""+className+"\" (?!)");
			
		}
		
	}
	
}
