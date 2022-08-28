/*
 * Created by SharpDevelop.
 * User: usr
 * Date: 2022-08-18
 * Time: 7:11 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Sunset.VarTypes {
	
	public struct Function {
		
		public UInt32 memAddr,instanceID;
		public Tuple<String,VarType>returnType;
		public UInt16 expectedParameterCount;
		public FunctionType functionType;
		public CallingConvention callingConvention;
		public Modifier modifier;
		public List<ValueTuple<String,VarType>>parameterTypes;
		public Boolean isInherited;
		
		public Function (UInt32 memAddr,Tuple<String,VarType>returnType,UInt16 expectedParameterCount,FunctionType funcType,CallingConvention callingConvention,Modifier modifier,List<ValueTuple<String,VarType>>paramTypes,UInt32 instanceId,Boolean inherited) {
			this.memAddr=memAddr;
			this.returnType=returnType;
			this.expectedParameterCount=expectedParameterCount;
			this.functionType=funcType;
			this.callingConvention=callingConvention;
			this.modifier=modifier;
			this.parameterTypes=paramTypes;
			this.isInherited=inherited;
			this.instanceID=instanceId;
		}
		
	}
	
}
