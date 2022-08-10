/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/8/2021
 * Time: 5:20 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq;
using System.Collections.Generic;
using Sunset.Stack;

namespace Sunset.Keywords {
	
	public class KWConstructor : Keyword {
		
		public const String constName="constructor";
		
		public KWConstructor () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute(Parser sender,String[] @params) {
			
			if (sender.inFunction)
				throw new ParsingError("Tried to make a constructor inside of a function");
			
			if (sender.constructor!=null)
				throw new ParsingError("Constructor already exists, and the \""+KWConstructor.constName+"\" keyword was called");
			
			OpcodeIndexReference pos=sender.GetStaticInclusiveOpcodesCount(1);
			sender.addBytes(new Byte[]{0xE9,0,0,0,0});
			Byte[] newOpcodes=new Byte[0],endOpcodes=(@params.Length==0)?new Byte[]{0xC3}/*RET*/:new Byte[]{0xC2/*RET SHORT:(STACK RESTORATION AMOUNT)*/}.Concat(BitConverter.GetBytes((UInt16)(@params.Length*4))).ToArray();
									
			if (sender.addEsiToLocalAddresses)
				endOpcodes=new Byte[]{0x5E/*POP ESI*/}.Concat(endOpcodes).ToArray();
			
			Block constructorBlock=new Block(delegate {
             	sender.inFunction=false;
             	sender.inConstructor=false;
             	if (sender.addEsiToLocalAddresses) sender.pseudoStack.pop();
             	sender.pseudoStack.pop();
				sender.tryCreateRestoreEsiFunc();
			},sender.memAddress,endOpcodes,true);
			
			//For information on this, see KWNew -> Extra esi dword var on classes information
			if (sender.addEsiToLocalAddresses) {
				sender.addByte(0x56);//PUSH ESI
				sender.esiFuncReferences.Add(sender.getOpcodesCount()+1);
				sender.addBytes(new Byte[]{0xE8}.Concat(BitConverter.GetBytes(sender.memAddress))); //CALL DWORD RELATIVE ADDRESS
			}
			
			List<Tuple<String,VarType>>paramTypes=new List<Tuple<String,VarType>>();
			UInt16 paramIndex=0;
			foreach (String s in @params.Reverse())
				sender.pseudoStack.push(new LocalVar(s.Split(' ')[1]));
			foreach (String s in @params) {
				
				String[]split=s.Split(' ');
				if (split.Length!=2)
					throw new ParsingError("Invalid function declaration parameter: \""+s+'"');
				
				String unparsedType=split[0],varName=split[1];
				
				Tuple<String,VarType>varType=sender.getVarType(unparsedType);
				paramTypes.Add(varType);
				Console.WriteLine(varType.Item1+','+varType.Item2.ToString()+','+varName);
				constructorBlock.localVariables.Add(varName,new Tuple<Tuple<String,VarType>>(varType));
				++paramIndex;
				
			}
			sender.pseudoStack.push(new ReturnPtr());
			if (sender.addEsiToLocalAddresses)
				sender.pseudoStack.push(new EsiPtr());
			sender.addBlock(constructorBlock,1);
			constructorBlock.blockMemPositions.Add(pos);
			sender.inFunction=true;
            sender.inConstructor=true;
			sender.constructor=new Tuple<UInt32,List<Tuple<String,VarType>>>(constructorBlock.startMemAddr,paramTypes);
			return base.execute(sender, @params);
			
		}
				
	}
	
}
