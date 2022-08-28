/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/8/2021
 * Time: 2:22 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Sunset.Stack;

namespace Sunset.Keywords {
	
    // TODO:: look into Block->blockMemPositions, maybe add a function Block#addMemPosition that is static inclusive, same for blockRVAPositions and this.blockAddrBeforeAppendingReferences[block] and enterPositions etc.. (also every dictionary in Parser with a Block key)

	public class KWFunction : Keyword {
		
		public const String constName="func";
		
		public KWFunction () : base (constName,KeywordType.FUNCTION,true) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (sender.inFunction)
				throw new ParsingError("Tried to make a function inside of a function",sender);
			
            Modifier currentMods=sender.currentMods;
            sender.lastFuncOpcodeStartIndex=sender.getOpcodesCount();
            sender.lastFuncDataSectOpcodeStartIndex=(UInt32)Parser.dataSectBytes.Count();
            Boolean staticFunc=currentMods.HasFlag(Modifier.STATIC),shouldAddEsi=sender.addEsiToLocalAddresses&&!staticFunc;
            if (!staticFunc)
		        sender.tryCreateRestoreEsiFunc();
			
			OpcodeIndexReference pos=sender.GetStaticInclusiveOpcodesCount(1);
            if (!staticFunc)
			    sender.addBytes(new Byte[]{0xE9,0,0,0,0});
//			Byte[]newOpcodes=new Byte[]{0xE9,0,0,0,0}; //JMP TO MEM ADDR
			Byte[] newOpcodes=new Byte[0],endOpcodes=(@params.Length==0)?new Byte[]{0xC3}/*RET*/:new Byte[]{0xC2/*RET SHORT:(STACK RESTORATION AMOUNT)*/}.Concat(BitConverter.GetBytes((UInt16)(@params.Length*4))).ToArray();
			
			if (shouldAddEsi)
				endOpcodes=new Byte[]{0x5E/*POP ESI*/}.Concat(endOpcodes).ToArray();
			
			Block functionBlock=new Block(delegate{ 
                sender.inFunction=false;
                if(shouldAddEsi)
                    sender.pseudoStack.pop();
                sender.pseudoStack.pop();
             },sender.GetStaticInclusiveAddress(currentMods.HasFlag(Modifier.STATIC)),endOpcodes,true);
			
            if (staticFunc) {
                functionBlock.afterBlockClosedFunc=delegate {
                    sender.dwordsToIncByOpcodesUntilStaticFuncEnd.Clear();
                    Parser.dataSectBytes.AddRange(sender.appendAfterStaticFunc);
                    sender.appendAfterStaticFunc.Clear();
                };
            }

			//For information on this, see KWNew -> Extra esi dword var on classes information
			if (shouldAddEsi) {
				sender.addByte(0x56);//PUSH ESI
				sender.esiFuncReferences.Add(sender.getOpcodesCount()+1);
				sender.addBytes(new Byte[]{0xE8}.Concat(BitConverter.GetBytes(sender.memAddress))); //CALL DWORD RELATIVE ADDRESS
			}
			
			List<ValueTuple<String,VarType>>paramTypes=new List<ValueTuple<String,VarType>>();
			UInt16 paramIndex=0;
			foreach (String s in @params.Reverse()) {
                String[]pSP=s.Split(' ');
                if (pSP.Length!=2) throw new ParsingError("Expected function parameters declaration in (type, name) format",sender);
				sender.pseudoStack.push( new LocalVar(pSP[1]));
            }
			foreach (String s in @params) {
				
				String[]split=s.Split(' ');
				if (split.Length!=2)
					throw new ParsingError("Invalid function declaration parameter: \""+s+'"',sender);
				
				String unparsedType=split[0],varName=split[1];
				
				Tuple<String,VarType>varType=sender.getVarType(unparsedType);
				paramTypes.Add(new ValueTuple<String,VarType>(varType.Item1,varType.Item2));
				Console.WriteLine(varType.Item1+','+varType.Item2.ToString()+','+varName);
				functionBlock.localVariables.Add(varName,new Tuple<Tuple<String,VarType>>(varType));
				++paramIndex;
				
			}
			sender.pseudoStack.push(new ReturnPtr());
			if (shouldAddEsi)
				sender.pseudoStack.push(new EsiPtr());
			sender.addBlock(functionBlock,0,staticFunc);
            if (!staticFunc)
			    functionBlock.blockMemPositions.Add(pos);
			sender.inFunction=true;
			sender.nextFunctionParamsCount=(UInt16)@params.Length; 
			sender.lastFunctionBlock=functionBlock;
			
			sender.nextFunctionParamTypes=paramTypes.ToArray();
			
			sender.nextType=FunctionType.SUNSET;
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_FUNCTION_NAME,newOpcodes=newOpcodes};
		
		}
		
	}
	
}
