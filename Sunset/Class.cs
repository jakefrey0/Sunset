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
using Sunset.VarTypes;

namespace Sunset {
	
	public class Class {
		
		public readonly String className,path,classID;
		public readonly UInt32 byteSize,opcodePortionByteSize,skeletonIndex,classAppendAfterCount,bytesToReserve,inhTblAppendAfterIndex;
		public readonly ClassType classType;
		public readonly Parser parserUsed;
		public Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>>variables;
		public Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>>classes;//Name,(Offset To Mem Address of Heap Handle,Class type name,Class type),Instance ID
		public Dictionary<String,Function>functions;//Function Name,(Memory Address,(Return Type, Return Var Type),No. of expected parameters,Function Type,Calling Convention,Modifiers)
		public Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>> arrays=new Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>>();//Name,(Ptr To Mem Address of Heap Handle(Dynamic) or Mem Block(Static),Array Var Type,ArrayStyle(Dynamic or Static)),Instance ID
		public Tuple<UInt32,List<Tuple<String,VarType>>> constructor;//Memory Address,Func Param Types
        public Dictionary<String,Tuple<UInt32,Tuple<String,VarType>>>constants=new Dictionary<String,Tuple<UInt32,Tuple<String,VarType>>>();//var name,(constant value,(Generic Var Type Tuple))		
        private List<String>defineTimeOrder;
        public List<Class>inheritedClasses;
		
		public Class (String className,String path,UInt32 byteSize,ClassType classType,Parser parserUsed,UInt32 opcodePortionByteSize,UInt32 skeletonIndex,UInt32 classAppendAfterCount,String classID) {

			this.bytesToReserve=parserUsed.byteCountBeforeDataSect;
			this.className=className;
			this.byteSize=byteSize;
			this.classType=classType;
			this.variables=new Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>>();
			this.classes=new Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>>();
			this.functions=new Dictionary<String,Function>();
			this.defineTimeOrder=new List<String>(parserUsed.defineTimeOrder);
			this.opcodePortionByteSize=opcodePortionByteSize;
			this.skeletonIndex=skeletonIndex;
			this.classAppendAfterCount=classAppendAfterCount;
			this.parserUsed=parserUsed;
			foreach (KeyValuePair<String,Tuple<UInt32,String,Modifier,UInt32>>kvp in parserUsed.getVariables())
				this.variables.Add(kvp.Key,new Tuple<UInt32,String,Modifier,UInt32>(kvp.Value.Item1-parserUsed.memAddress,kvp.Value.Item2,kvp.Value.Item3,kvp.Value.Item4));
			foreach (KeyValuePair<String,Tuple<UInt32,String,Class,Modifier,UInt32>>kvp in parserUsed.getClasses()) {
                if (kvp.Value.Item1==0) this.classes.Add(kvp.Key,new Tuple<UInt32,String,Class,Modifier,UInt32>(0,kvp.Value.Item2,kvp.Value.Item3,kvp.Value.Item4,kvp.Value.Item5));
                else this.classes.Add(kvp.Key,new Tuple<UInt32,String,Class,Modifier,UInt32>(kvp.Value.Item1-parserUsed.memAddress,kvp.Value.Item2,kvp.Value.Item3,kvp.Value.Item4,kvp.Value.Item5));
            }
			foreach (KeyValuePair<String,Function> kp in parserUsed.getFunctions()) {
				this.functions.Add(kp.Key,kp.Value);
			}
			foreach (KeyValuePair<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>>kvp in parserUsed.getArrays())
				this.arrays.Add(kvp.Key,new Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>(kvp.Value.Item1-parserUsed.memAddress,kvp.Value.Item2,kvp.Value.Item3,kvp.Value.Item4,kvp.Value.Item5));
			this.constructor=parserUsed.constructor;
            this.constants=parserUsed.getConstants();
			this.path=path;
            this.classID=classID;
            this.inheritedClasses=new List<Class>(parserUsed.inheritedClasses);
            this.inhTblAppendAfterIndex=parserUsed.inhTblAppendAfterIndex;
		}
		
		public Tuple<String,VarType> getVarType (String name) {
			
			if (variables.ContainsKey(name))
				return new Tuple<String,VarType>(this.variables[name].Item2,VarType.NATIVE_VARIABLE);
			else if (classes.ContainsKey(name))
				return new Tuple<String,VarType>(this.classes[name].Item2,VarType.CLASS);
			else if (functions.ContainsKey(name))
				return this.functions[name].returnType;
			else if (arrays.ContainsKey(name))
				return new Tuple<String,VarType>(this.arrays[name].Item2,VarType.NATIVE_ARRAY_INDEXER);
			else throw new ParsingError("Variable \""+name+"\" does not exist in \""+className+"\" (?!)",null);
			
		}
		
	}
	
}
