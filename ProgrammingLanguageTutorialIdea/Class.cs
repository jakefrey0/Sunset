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

namespace ProgrammingLanguageTutorialIdea {
	
	public class Class {
		
		public readonly String className,path,classID;
		public readonly UInt32 byteSize,opcodePortionByteSize,skeletonIndex,classAppendAfterCount,bytesToReserve;
		public readonly ClassType classType;
		public Dictionary<String,Tuple<UInt32,String,Modifier>>variables;
		public Dictionary<String,Tuple<UInt32,String,Class,Modifier>>classes;//Name,(Offset To Mem Address of Heap Handle,Class type name,Class type)
		public Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>>functions;
		public Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier>> arrays=new Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier>>();//Name,(Ptr To Mem Address of Heap Handle(Dynamic) or Mem Block(Static),Array Var Type,ArrayStyle(Dynamic or Static))
		public Tuple<UInt32,List<Tuple<String,VarType>>> constructor;//Memory Address,Func Param Types
        public Dictionary<String,Tuple<UInt32,Tuple<String,VarType>>>constants=new Dictionary<String,Tuple<UInt32,Tuple<String,VarType>>>();//var name,(constant value,(Generic Var Type Tuple))		
        private List<String>defineTimeOrder;
		
		public Class (String className,String path,UInt32 byteSize,ClassType classType,Parser parserUsed,UInt32 opcodePortionByteSize,UInt32 skeletonIndex,UInt32 classAppendAfterCount,String classID) {
			
			this.bytesToReserve=parserUsed.byteCountBeforeDataSect;
			this.className=className;
			this.byteSize=byteSize;
			this.classType=classType;
			this.variables=new Dictionary<String,Tuple<UInt32,String,Modifier>>();
			this.classes=new Dictionary<String,Tuple<UInt32,String,Class,Modifier>>();
			this.functions=new Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>>();
			this.defineTimeOrder=new List<String>(parserUsed.defineTimeOrder);
			this.opcodePortionByteSize=opcodePortionByteSize;
			this.skeletonIndex=skeletonIndex;
			this.classAppendAfterCount=classAppendAfterCount;
			foreach (KeyValuePair<String,Tuple<UInt32,String,Modifier>>kvp in parserUsed.getVariables())
				this.variables.Add(kvp.Key,new Tuple<UInt32,String,Modifier>(kvp.Value.Item1-parserUsed.memAddress,kvp.Value.Item2,kvp.Value.Item3));
			foreach (KeyValuePair<String,Tuple<UInt32,String,Class,Modifier>>kvp in parserUsed.getClasses())
				this.classes.Add(kvp.Key,new Tuple<UInt32,String,Class,Modifier>(kvp.Value.Item1-parserUsed.memAddress,kvp.Value.Item2,kvp.Value.Item3,kvp.Value.Item4));
			this.functions=parserUsed.getFunctions();
			foreach (KeyValuePair<String,Tuple<UInt32,String,ArrayStyle,Modifier>>kvp in parserUsed.getArrays())
				this.arrays.Add(kvp.Key,new Tuple<UInt32,String,ArrayStyle,Modifier>(kvp.Value.Item1-parserUsed.memAddress,kvp.Value.Item2,kvp.Value.Item3,kvp.Value.Item4));
			this.constructor=parserUsed.constructor;
            this.constants=parserUsed.getConstants();
			this.path=path;
            this.classID=classID;

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
