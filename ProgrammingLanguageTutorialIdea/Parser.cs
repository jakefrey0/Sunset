/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/29/2021
 * Time: 9:09 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using ProgrammingLanguageTutorialIdea.Keywords;
using System.Linq;
using ProgrammingLanguageTutorialIdea.Stack;

namespace ProgrammingLanguageTutorialIdea {
	
	public class Parser {
		
		public String lastReferencedVariable {
			
			private set;
			get;
			
		}
		public String referencedVariable;
		public Boolean referencedVariableIsLocal;
		
		internal Boolean lastReferencedVariableIsLocal;
		
		internal VarType lastReferencedVarType=VarType.NONE,referencedVarType=VarType.NONE;
		internal String varType; //to fix for classes later, maybe set to a Tuple<String,String>//Name,Origin or something? where Origin is something like the filename, or something to get the exact class that is being referred to
		internal Dictionary<String,List<UInt32>> variableReferences=new Dictionary<String,List<UInt32>>(),//Name,(Index in the Opcodes List)
												 arrayReferences=new Dictionary<String,List<UInt32>>();//Name,(Index in the Opcodes List)
		
		internal Dictionary<String,List<String>> toImport;//DllName,Functions
		internal Dictionary<String,List<UInt32>> referencedFuncPositions;//FuncName,Opcode pos
		
		internal Dictionary<Block,UInt32> blocks;//block,end of block mem address
		internal Dictionary<Block,UInt16> blockBracketBalances;//block,bracket balance
		internal Dictionary<Block,UInt16> blockVariablesCount;//Block,# Of Variables Defined
		internal Dictionary<Block,List<Tuple<UInt32,Int16>>> blockAddrBeforeAppendingReferences;//Block,(Opcode index to place the mem address of end of block before extra opcodes are appended,Mem Addr Offset)
		internal Dictionary<Block,UInt32> enterPositions; //Block,Position in opcodes of the parameters (total 3 bytes) for ENTER 0,0
		//Any references of a local var outside of its home block should have its EBP increased by +4 per new local variable created afterwards in a lower nested block
		internal Dictionary<Block,List<UInt32>> localVarEBPPositionsToOffset; //Referencing Block,Indexes in the opcodes list - (Block where variable was referenced, List of all references to variables from a lower nested level block)
		internal UInt32 memAddress {
			
			private set;
			get;
			
		}
		
		internal Boolean expectsBlock=false,expectsElse=false,searchingFunctionReturnType=false,inFunction=false;
		internal Byte setExpectsElse=0,setExpectsBlock=0;
		
		internal Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16>> functions;//Function Name,(Memory Address,(Return Type, Return Var Type),No. of expected parameters)
		internal Dictionary<String,List<Tuple<UInt32,UInt32>>> functionReferences;//Function Name,(Indexes in Opcodes of the reference,Memory Address at time of Reference)
		internal UInt16 nextFunctionParamsCount;
		internal Dictionary<String,List<Tuple<String,VarType>>> functionParamTypes;//Function Name,Var Type
		internal Tuple<String,VarType>[] nextFunctionParamTypes;
		
		internal readonly Action elseBlockClosed;
		internal KeywordType[] nextExpectedKeywordTypes=new []{KeywordType.NONE};
		
		internal List<UInt32>freeHeapsRefs;
		
		internal Block lastFunctionBlock;
		
		internal readonly PseudoStack pseudoStack;
		internal readonly KeywordMgr keywordMgr;
		internal Block lastBlockClosed {
			
			get;
			private set;
							
		}
			
		private List<Byte> opcodes=new List<Byte>(),importOpcodes=null,finalBytes=new List<Byte>(),appendAfter=new List<Byte>();
		private ParsingStatus status;
		private Dictionary<String,Tuple<UInt32,String>> variables=new Dictionary<String,Tuple<UInt32,String>>();//Name,(Mem Address,Var Type)
		private Dictionary<String,Tuple<UInt32,String,ArrayStyle>> arrays=new Dictionary<String,Tuple<UInt32,String,ArrayStyle>>();//Name,(Ptr To Mem Address of Heap Handle(Dynamic) or Mem Block(Static),Array Var Type,ArrayStyle(Dynamic or Static))
		
		private List<Tuple<UInt32,List<UInt32>>>stringsAndRefs; //(Mem Addr,List of References by Opcode Index),Note: Currently the Inner list of Opcode Indexes will only have a length of 1 (6/19/2021 5:19PM)
		
		private Tuple<UInt32,List<UInt32>> processHeapVar;//Mem Addr, References
		
		private ArrayStyle style;//TODO:: do static memory block, later do stack allocation, and also allow this to be changed outside of compiler in the code with keyword SetArrayStyle (constName: SetArrStyle, expectsParams=true)
		
		private Boolean winApp;
		private Dictionary<String,UInt32> setArrayValueFuncPtrs;
		
		private Executor waitingToExecute;
		
		private Char nextChar;
		
		private UInt16 squareBracketBalance=0,roundBracketBalance=0,nestedLevel=0;
		
		
		private UInt32 freeHeapsMemAddr;
		
		private const String KERNEL32="KERNEL32.DLL";
		
		public Parser (Boolean winApp=true) {
			
			memAddress=winApp?0x00401000:(UInt32)0;
			keywordMgr=new KeywordMgr();
			style=winApp?ArrayStyle.DYNAMIC_MEMORY_HEAP:ArrayStyle.STATIC_MEMORY_BLOCK;
			this.winApp=winApp;
			toImport=new Dictionary<String,List<String>>();
			this.referencedFuncPositions=new Dictionary<String,List<UInt32>>();
			this.setArrayValueFuncPtrs=new Dictionary<String,UInt32>();
			this.blocks=new Dictionary<Block,UInt32>();
			this.blockBracketBalances=new Dictionary<Block,UInt16>();
			elseBlockClosed=delegate {
				
				Byte i;
				
				foreach (UInt32 index in lastBlockClosed.pairedBlock.blockMemPositions) {
					
					Int32 prevNum=BitConverter.ToInt32(new []{opcodes[(Int32)index],opcodes[(Int32)index+1],opcodes[(Int32)index+2],opcodes[(Int32)index+3]},0);
					Byte[]newNum=BitConverter.GetBytes(prevNum+5);//5 because the opcodes are 0xE9,Integer.... (See KWElse -> JMP TO MEM ADDR)
					
					Console.WriteLine("Updating index: "+index+" (Calculated Mem Addr: "+(0x00401000+index).ToString("X")+") to "+(prevNum+5));
//					Console.ReadKey();
					
					i=0;
					while (i!=4) {
						
						opcodes[(Int32)index+i]=newNum[i];
						++i;
						
					}
					
				}
				
			};
			functions=new Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16>>();
			functionReferences=new Dictionary<String,List<Tuple<UInt32,UInt32>>>();
			freeHeapsRefs=new List<UInt32>();
			functionParamTypes=new Dictionary<String,List<Tuple<String,VarType>>>();
			enterPositions=new Dictionary<Block,UInt32>();
			blockVariablesCount=new Dictionary<Block,UInt16>();
			blockAddrBeforeAppendingReferences=new Dictionary<Block,List<Tuple<UInt32,Int16>>>();
			pseudoStack=new PseudoStack();
			localVarEBPPositionsToOffset=new Dictionary<Block,List<UInt32>>();
			stringsAndRefs=new List<Tuple<UInt32,List<UInt32>>>();
			
		}
		
		public Byte[] parse (String data) {
			
			status=ParsingStatus.SEARCHING_NAME;
			StringBuilder nameReader=new StringBuilder();
			squareBracketBalance=0;
			roundBracketBalance=0;
			String arrName=null;
			List<String> paramsList=new List<String>();
			Int32 currentChar=0;
			
			data+=' ';
			
			foreach (Char c in data) {
				
				Console.WriteLine(" - Checking: \""+c+"\",ParsingStatus: "+status.ToString()+", blockBracketBalance #no: "+blockBracketBalances.Count.ToString());
				
				if (setExpectsElse>0) {
					
					--setExpectsElse;
					expectsElse=true;
					
				}
				
				if (setExpectsBlock>0) {
					
					--setExpectsBlock;
					expectsBlock=true;
					
				}
				
				switchStart:
				switch (status) {
					
					case ParsingStatus.SEARCHING_FUNCTION_NAME:
					case ParsingStatus.SEARCHING_ARRAY_NAME:
					case ParsingStatus.SEARCHING_VALUE:
					case ParsingStatus.SEARCHING_VARIABLE_NAME:
					case ParsingStatus.SEARCHING_NAME:
						if (!this.isFormOfBlankspace(c)) {
							
							nameReader.Append(c);
							++status;
							
						}
						break;
					
					case ParsingStatus.READING_NAME:
						
						if (this.isArrayDeclarationChar(c)) {
							
							if (searchingFunctionReturnType) {
								
								nameReader.Append(c);
								this.functions[this.functions.Last().Key]=new Tuple<UInt32,Tuple<String,VarType>,UInt16>(this.functions.Last().Value.Item1,this.getVarType(nameReader.ToString()),functions.Last().Value.Item3);
								status=ParsingStatus.SEARCHING_NAME;
								this.setExpectsBlock=1;
								
							}
							
							else {
							
								this.declareArray(nameReader.ToString());
								
								this.resetLastReferencedVar();
							
							}
								
							nameReader.Clear();
							
						}
						else if (this.beginsArrayIndexer(c)) {
							
							arrName=nameReader.ToString();
							nameReader.Clear();
							++squareBracketBalance;
							status=ParsingStatus.READING_ARRAY_INDEXER;
							
						}
						else if (this.opensBlock(c)) {
							
							if (!(this.expectsBlock))
								throw new ParsingError("Got \""+c+"\" but did not expect block");
							
							this.updateBlockBalances(c);
							
							this.expectsBlock=false;
							
						}
						else if (this.closesBlock(c)) {
							
							if (this.blockBracketBalances.Count==0)
								throw new ParsingError("Tried to close block, when no block detected");
							
							this.updateBlockBalances(c);
							
						}
						else if (Char.IsLetterOrDigit(c)||this.refersToIncrementOrDecrement(c)) nameReader.Append(c);
						else {
							
							String prevLastReferencedVariable=lastReferencedVariable;
							
							String name=nameReader.ToString();
//							Console.WriteLine("Name: "+name);
							
							if (name.Length>2&&(this.refersToIncrementOrDecrement(name[0])||this.refersToIncrementOrDecrement(name[name.Length-1]))) {
								
								if (name.StartsWith(KWIncrease.constName)) {
									
									name=name.Substring(2,name.Length-2);
									this.chkName(name);
									this.chkName(KWIncrease.constName);
									
								}
								else if (name.StartsWith(KWDecrease.constName)) {
									
									name=name.Substring(2,name.Length-2);
									this.chkName(name);
									this.chkName(KWDecrease.constName);
									
								}
								
								else if (name.EndsWith(KWIncrease.constName)) {
									
									name=name.Substring(0,name.Length-2);
									this.chkName(name);
									this.chkName(KWIncrease.constName);
									
								}
								else if (name.EndsWith(KWDecrease.constName)) {
									
									name=name.Substring(0,name.Length-2);
									this.chkName(name);
									this.chkName(KWDecrease.constName);
									
								}
								else this.chkName(name);
								
							}
							else this.chkName(name);
							
							expectsElse=expectsBlock=false;
							nameReader.Clear();
							
						}
						
						break;
						
					case ParsingStatus.READING_VARIABLE_NAME:
						
						if (Char.IsLetterOrDigit(c)) nameReader.Append(c);
						else {
							
							this.registerVariable(nameReader.ToString());
							nameReader.Clear();
							
						}
						
						break;
						
					case ParsingStatus.READING_VALUE:
						
						if (!(this.isFormOfBlankspace(c))) nameReader.Append(c);
						else {
							
							this.processValue(nameReader.ToString());
							nameReader.Clear();
							referencedVariable=null;
							referencedVarType=VarType.NONE;
							
						}
						break;
						
					case ParsingStatus.READING_ARRAY_NAME:
						if (Char.IsLetterOrDigit(c)) nameReader.Append(c);
						else {
							
							this.registerArray(nameReader.ToString());
							nameReader.Clear();
							
						}
						break;
						
					case ParsingStatus.READING_ARRAY_INDEXER:
						
						if (this.beginsArrayIndexer(c)) ++squareBracketBalance;
						else if (this.endsArrayIndexer(c)) --squareBracketBalance;
						
						if (squareBracketBalance==0) {
							
							status=this.indexArray(arrName,nameReader.ToString());
							nameReader.Clear();
							this.lastReferencedVariable=arrName;
							this.lastReferencedVariableIsLocal=this.isALocalVar(arrName);
							arrName=null;
							this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.DECREMENT,KeywordType.INCREMENT};
							this.lastReferencedVarType=VarType.NATIVE_ARRAY_INDEXER;
							
						}
						else nameReader.Append(c);
						
						break;
						
					case ParsingStatus.SEARCHING_PARAMETERS:
						
						if (this.beginsParameters(c)) {
							
							roundBracketBalance=1;
							status=ParsingStatus.READING_PARAMETERS;
							
						}
						else if (!this.isFormOfBlankspace(c)) {
							this.exec(this.waitingToExecute,new String[0]);
							goto switchStart;
						}
						
						break;
					case ParsingStatus.READING_PARAMETERS:
						
						if (this.beginsParameters(c)) ++roundBracketBalance;
						
						else if (this.endsParameters(c)) --roundBracketBalance;
						
						else if (this.splitsParameters(c)) { 
							
							paramsList.Add(nameReader.ToString());
							nameReader.Clear();
						
						}
						
						if (roundBracketBalance==0) {
							
							paramsList.Add(nameReader.ToString());
							nameReader.Clear();
							this.exec(this.waitingToExecute,paramsList.ToArray());
							paramsList.Clear();
							
						}
						
						else if (!(this.splitsParameters(c))) nameReader.Append(c);
						
						break;
						
					case ParsingStatus.READING_FUNCTION_NAME:
						
						if (Char.IsLetterOrDigit(c)) nameReader.Append(c);
						else {
							
							this.setExpectsBlock=1;
							String funcName=nameReader.ToString();
							nameReader.Clear();
							this.functionParamTypes.Add(funcName,new List<Tuple<String,VarType>>());
							this.functionParamTypes[funcName]=this.nextFunctionParamTypes.ToList();
							this.functions.Add(funcName,new Tuple<UInt32,Tuple<String,VarType>,UInt16>(blocks.Keys.Last().startMemAddr,null,(UInt16)this.nextFunctionParamTypes.Length));
							this.functionReferences.Add(funcName,new List<Tuple<UInt32,UInt32>>());
							status=ParsingStatus.SEARCHING_NAME;
							this.nextExpectedKeywordTypes=new []{KeywordType.TYPE};
							this.searchingFunctionReturnType=true;
							
						}
						
						break;
						
				}
				
				++currentChar;
				if (data.Length>currentChar)
					nextChar=data[currentChar];
				
			}
			
			return compile();
			
		}
		
		private Byte[] compile () {
			
			this.addBytes(new Byte[]{0x6A,0}); //PUSH 0
			freeHeapsMemAddr=memAddress;
			this.freeHeaps();
			this.addByte(0x58); //POP EAX to set the exit code (return value) of process (HACK:: NOTICE::return value is an UNSIGNED value)
			this.addByte(0xC3); //Add RETN call to end of our exe, so no matter what happens in terms of the source, it should not be a blank application & will exit
			
			opcodes.AddRange(appendAfter);
			this.updateVariableReferences();
			this.fillFunctionReferences();  
			this.fillHeapFreeReferences();
			this.fillConstantStringReferences();
			
			if ((!(winApp))&&(this.toImport.Count!=0))
				throw new ParsingError("Can not reference DLL's on non-PE app");
			
			if (winApp) {
				
				List<Tuple<String,UInt32>>funcMemAddrs=null;
				
				if (this.toImport.Count>0)
					importOpcodes=this.getImportSection(out funcMemAddrs);
				
				if (funcMemAddrs!=null)
					this.fillFuncMemAddrs(funcMemAddrs);
				
				PEHeader hdr=PEHeaderFactory.newHdr(opcodes,importOpcodes,memAddress,-this.appendAfter.Count);
				
				finalBytes.AddRange(hdr.toBytes());
				
				while (opcodes.Count%512!=0)
					opcodes.Add(0x00);
				
			}
			
			while(opcodes.Count%512!=0)
				opcodes.Add(0);
			
			finalBytes.AddRange(opcodes);
			if (importOpcodes!=null)
				finalBytes.AddRange(importOpcodes);
			
			return finalBytes.ToArray();
			
		}
		
		internal void addByte (Byte b) {
			
			Dictionary<String,Tuple<UInt32,String>> newDict=new Dictionary<String,Tuple<UInt32,String>>(this.variables.Count);
			foreach (KeyValuePair<String,Tuple<UInt32,String>> kvp in this.variables) {
//				Console.WriteLine("For variable: "+kvp.Key+", updating mem address to: "+(kvp.Value.Item1+1).ToString("X"));
				newDict.Add(kvp.Key,new Tuple<UInt32,String>(kvp.Value.Item1+1,kvp.Value.Item2));
			}
			
			this.variables=new Dictionary<String,Tuple<UInt32,String>>(newDict);
			
			Dictionary<String,Tuple<UInt32,String,ArrayStyle>> newDict0=new Dictionary<String,Tuple<UInt32,String,ArrayStyle>>(this.arrays.Count);
			foreach (KeyValuePair<String,Tuple<UInt32,String,ArrayStyle>> kvp in this.arrays) {
				
				if (kvp.Value.Item1==0) newDict0.Add(kvp.Key,kvp.Value);
				
				else {
				
//					Console.WriteLine("For array: "+kvp.Key+", updating mem address to: "+(kvp.Value.Item1+1).ToString("X"));
					
					newDict0.Add(kvp.Key,new Tuple<UInt32,String,ArrayStyle>(kvp.Value.Item1+1,kvp.Value.Item2,kvp.Value.Item3));
				
				}
			}
			
			this.arrays=new Dictionary<String,Tuple<UInt32,String,ArrayStyle>>(newDict0);
			
			List<Tuple<UInt32,List<UInt32>>>newList=new List<Tuple<UInt32,List<UInt32>>>(this.stringsAndRefs.Count);
			foreach (Tuple<UInt32,List<UInt32>>str in this.stringsAndRefs) {
				
				newList.Add(new Tuple<UInt32,List<UInt32>>(str.Item1+1,str.Item2));
				
			}
			this.stringsAndRefs=new List<Tuple<UInt32,List<UInt32>>>(newList);
			
			if (processHeapVar!=null){
				
				this.processHeapVar=new Tuple<UInt32,List<UInt32>>(this.processHeapVar.Item1+1,this.processHeapVar.Item2);
				
			}
			
			opcodes.Add(b);
			++memAddress;
			
		}
		
		internal void addBytes (IEnumerable<Byte> bytes) {
			
			foreach (Byte b in bytes)
				this.addByte(b);
			
		}
		
		private void chkName (String name) {
			
			//HACK:: check var type
			
			KeywordType[] pKTs=this.nextExpectedKeywordTypes;
			Boolean wasSearchingFuncReturnType=searchingFunctionReturnType;
			this.nextExpectedKeywordTypes=new []{KeywordType.NONE};
			searchingFunctionReturnType=false;
			
			Console.WriteLine("Got name: \""+name+'"');
			
			if (!pKTs.Contains(KeywordType.NONE))
				goto checkKeywords;
			
			if (this.variables.ContainsKey(name)) {
				
				this.lastReferencedVariable=name;
				Console.WriteLine("LRV:"+this.lastReferencedVariable);
				this.status=ParsingStatus.SEARCHING_NAME;
				this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
				this.lastReferencedVarType=VarType.NATIVE_VARIABLE;
				lastReferencedVariableIsLocal=false;
				return;
				
			}
			
			if (this.arrays.ContainsKey(name)) {
				
				this.lastReferencedVariable=name;
				this.lastReferencedVarType=VarType.NATIVE_ARRAY;
				this.status=ParsingStatus.SEARCHING_NAME;
				this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
				lastReferencedVariableIsLocal=false;
				return;
				
			}
			
			if (this.functions.ContainsKey(name)) {
				
				Console.WriteLine("PARAM CT: "+this.functions[name].Item3.ToString());
				
				lastReferencedVariableIsLocal=false;
				
				if (this.functions[name].Item3!=0) {
					
					status=(this.beginsParameters(this.nextChar))?ParsingStatus.READING_PARAMETERS:ParsingStatus.SEARCHING_PARAMETERS;
					roundBracketBalance=1;
					this.waitingToExecute=new Executor(){func=name};
					
				}
				else this.callFunction(name,new String[0]);
				return;
				
			}
			
			if (this.isALocalVar(name)) {
				
				Console.WriteLine("Local Var Detected");
				this.lastReferencedVariable=name;
				this.lastReferencedVariableIsLocal=true;
				this.lastReferencedVarType=this.getLocalVarHomeBlock(name).localVariables[name].Item1.Item2;
				this.status=ParsingStatus.SEARCHING_NAME;
				this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
				return;
				
			}
			
			checkKeywords:
			foreach (Keyword kw in this.keywordMgr.getKeywords()) {
				
				if (kw.name==name) {
					
					Console.WriteLine("Exec: "+kw.name);
					
					if (!pKTs.Contains(KeywordType.NONE)&&!pKTs.Contains(kw.type)) {
						if (pKTs.Length>1) {
							StringBuilder sb=new StringBuilder();
							foreach (KeywordType kt in pKTs)
								sb.Append('"'+kt.ToString()+"\", ");
							throw new ParsingError("Expected a keyword of any of the following types: "+String.Concat(sb.ToString().Take(sb.Length-2)));
						}
						else throw new ParsingError("Expected a keyword of type \""+pKTs[0].ToString()+'"');
					}
					
					if (kw.name==KWElse.constName&&!expectsElse)
						throw new ParsingError("Got \""+KWElse.constName+"\", but was not expecting an else reference");
					else if (kw.hasParameters) {
						
						status=(this.beginsParameters(this.nextChar))?ParsingStatus.READING_PARAMETERS:ParsingStatus.SEARCHING_PARAMETERS;
						waitingToExecute=new Executor(){kw=kw};
						roundBracketBalance=1;
						
					}
					else
						this.execKeyword(kw,new String[0]);
					
					if (wasSearchingFuncReturnType) {
						this.functions[this.functions.Last().Key]=new Tuple<UInt32,Tuple<String,VarType>,UInt16>(this.functions.Last().Value.Item1,new Tuple<String,VarType>(this.varType,VarType.NATIVE_VARIABLE),functions.Last().Value.Item3);
						status=ParsingStatus.SEARCHING_NAME;
						this.setExpectsBlock=1;
					}
					lastReferencedVariableIsLocal=false;
					return;
					
				}
				
			}
			
					
			if (name.Length==1) {
				
				Char c=name[0];
				
				if (this.opensBlock(c)) {
					
					if (!(this.expectsBlock))
						throw new ParsingError("Got \""+c+"\" but did not expect block");
					
					this.updateBlockBalances(c);
					
					this.expectsBlock=false;
					status=ParsingStatus.SEARCHING_NAME;
					lastReferencedVariableIsLocal=false;
					return;
					
				}
				else if (this.closesBlock(c)) {
					
					if (this.blockBracketBalances.Count==0)
						throw new ParsingError("Tried to close block, when no block detected");
					
					this.updateBlockBalances(c);
					
					status=ParsingStatus.SEARCHING_NAME; 
					lastReferencedVariableIsLocal=false;
					return;
					
				}
					
			}
			
			throw new ParsingError("Unexpected name: \""+name+'"');
			
		}
		
		private void registerVariable (String varName) {
			
			Console.WriteLine("Registering variable "+varName+" (a type of \""+this.varType+"\"), memAddress: "+memAddress.ToString("X"));
			
			if (this.nameExists(varName))
				throw new ParsingError("The name \""+varName+"\" is already in use");
		
			this.tryIncreaseBlockVarCount();
			
			//when classes are a thing, make sure they are accounted for here
			//if (class) -> appendAfter.addRange ... class or struct size.. because, the pointers are 4 bytes, but the actual struct could and probably is greater or different than 4bytes
			if (this.blocks.Count==0) {//not local var
				this.variables.Add(varName,new Tuple<UInt32,String>(memAddress+(UInt32)appendAfter.Count,this.varType));
				this.appendAfter.AddRange(new Byte[keywordMgr.getVarTypeByteSize(this.varType)]);
				this.variableReferences.Add(varName,new List<UInt32>());
			}
			else {//should be local var
				this.pseudoStack.push(new LocalVar(varName));
				this.getCurrentBlock().localVariables.Add(varName,new Tuple<Tuple<String,VarType>>(new Tuple<String,VarType>(this.varType,VarType.NATIVE_VARIABLE)));
				this.lastReferencedVariableIsLocal=true;
				foreach (UInt32 index in this.localVarEBPPositionsToOffset.Where(x=>x.Key.nestedLevel>this.getCurrentBlock().nestedLevel).SelectMany(x=>x.Value))
					this.offsetEBP(4,index);
			}
			this.lastReferencedVariable=varName;
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void processValue (String value) {
			
			Console.WriteLine("is local: "+this.referencedVariableIsLocal.ToString()+", var name: "+this.referencedVariable);
			String type;
			if (!(this.referencedVariableIsLocal))
				type=(this.referencedVarType==VarType.NATIVE_ARRAY||this.referencedVarType==VarType.NATIVE_ARRAY_INDEXER)?this.arrays[this.referencedVariable].Item2:this.variables[this.referencedVariable].Item2;
			else
				type=this.getLocalVarHomeBlock(this.referencedVariable).localVariables[this.referencedVariable].Item1.Item1;
				
//			Console.WriteLine("referencingArray: "+referencingArray.ToString());
//			Console.WriteLine("this.arrays[referencedVariable]: "+this.arrays[referencedVariable].Item2);
			
			//HACK:: check variable type
			if (this.referencedVarType==VarType.NATIVE_ARRAY) {
				
				if (this.isNativeArrayCreationIndicator(value[0])) {
				
					Console.WriteLine("Making array named \""+this.referencedVariable+"\" of array type \""+type+"\" with value \""+value+"\".");
					
					this.addByte(0x51); //PUSH ECX
					
					if (this.processHeapVar==null)
						this.setProcessHeapVar();
					
					String adjustedValue=value.Substring(1);
					this.pushValue(adjustedValue);
					this.addByte(0xBB);//MOV FOLLOWING UINT32 TO EBX
					this.addBytes(BitConverter.GetBytes(keywordMgr.getVarTypeByteSize(type)));//UINT32 HERE
					this.addBytes(new Byte[]{0x0F,0xAF,0x1C,0x24});//IMUL EAX BY [ESP]
					this.addBytes(new Byte[]{0x83,0xC4,4});//ADD 4 TO ESP
					this.addBytes(new Byte[]{0x83,0xC3,8});//ADD 8 TO EBX
					this.addByte(0x53);//PUSH EBX
					this.addBytes(new Byte[]{0xBB}.Concat(BitConverter.GetBytes((UInt32)8))); //MOV EBX,08000000 (HEAP_ZERO_MEMORY)
					this.addByte(0x53);//PUSH EBX
					this.pushProcessHeapVar();
					const String HL="HeapAlloc";
					this.referenceDll(Parser.KERNEL32,HL);
					this.referencedFuncPositions[HL].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{0xFF,0x15,0,0,0,0});//CALL FUNC HeapAlloc
	//				Console.WriteLine(this.referencedVariable);
	//				Console.WriteLine("------------");
	//				foreach (String s in this.arrays.Select(x=>x.Key))
	//					Console.WriteLine(" - "+s);
	//				Console.WriteLine("------------");
					if (this.referencedVariableIsLocal) {
						
						if (this.getLocalVarHomeBlock(this.referencedVariable)!=this.getCurrentBlock())
							this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
						this.addBytes(new Byte[]{0x89,0x45,this.pseudoStack.getVarEbpOffset(this.referencedVariable)}); //MOV [EBP+-OFFSET],EAX
						
					}
					else {
						
						Tuple<UInt32,String,ArrayStyle>_refdVar=this.arrays[this.referencedVariable];
						this.arrays[this.referencedVariable]=new Tuple<UInt32,String,ArrayStyle>(this.memAddress+(UInt32)(appendAfter.Count),_refdVar.Item2,_refdVar.Item3);
						this.appendAfter.AddRange(new Byte[4]);
						this.arrayReferences[this.referencedVariable].Add((UInt32)(opcodes.Count+1));
						this.addBytes(new Byte[]{0xA3,0,0,0,0});//STORE ALLOCATED PTR TO VARIABLE
					
					}
					this.addBytes(new Byte[]	{
					             				0x59,//POP ECX
					              				0x6A,0,//PUSH 0
					              				});
					
					this.pushValue(adjustedValue); // PUSH ARRAY LENGTH
					this.callSetArrayValue(this.referencedVariable);
					this.addBytes(new Byte[]{
					              				0x6A,4,//PUSH 4
					              				0x6A//PUSH FOLLOWING BYTE
					              });
					this.addByte((Byte)(keywordMgr.getVarTypeByteSize(type)));//THIS BYTE (array member byte size)
					this.callSetArrayValue(this.referencedVariable);
					status=ParsingStatus.SEARCHING_NAME;
					return;
				
				}
				else if (this.arrays.ContainsKey(value)) {
					
					this.arrays[this.referencedVariable]=new Tuple<UInt32,String,ArrayStyle>(this.arrays[value].Item1,this.arrays[value].Item2,this.arrays[value].Item3);
					return;
					
				}
				else if (this.isValidFunction(value)) {
					
					if (this.arrays[this.referencedVariable].Item1!=0) {
						
						const String HF="HeapFree";
						this.referenceDll(Parser.KERNEL32,HF);
						this.arrayReferences[this.referencedVariable].Add((UInt32)(opcodes.Count+2));
						this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0});//push pMemory
						this.addBytes(new Byte[]{0x6A,0});//push Flags
						this.pushProcessHeapVar();//push hHeap
						this.referencedFuncPositions[HF].Add((UInt32)this.opcodes.Count+2);
						this.addBytes(new Byte[]{0xFF,0x15,0,0,0,0});//call HeapFree
						
					}
					else {
						
						Tuple<UInt32,String,ArrayStyle>_refdVar=this.arrays[this.referencedVariable];
						this.arrays[this.referencedVariable]=new Tuple<UInt32,String,ArrayStyle>(this.memAddress+(UInt32)(appendAfter.Count),_refdVar.Item2,_refdVar.Item3);
						this.appendAfter.AddRange(new Byte[4]);
						
					}
					
					this.pushValue(value);
					this.arrayReferences[this.referencedVariable].Add((UInt32)(opcodes.Count+2));
					this.addBytes(new Byte[]{0x8F,5,0,0,0,0}); // POP [PTR]
					
				}
				else throw new ParsingError("Invalid array assignment value: \""+value+'"');
				
			}
			
			else if (this.referencedVarType==VarType.NATIVE_ARRAY_INDEXER) {
				
				this.addByte(0x58);//POP EAX
				this.pushValue(value);
				this.addByte(0x5A);//POP EDX
				this.addBytes(new Byte[]{0x89,0x10});//MOV [EAX],EDX
				
			}
			
			else {
				
//				Console.WriteLine("Did not make an array");
				Tuple<String,VarType> tpl=this.pushValue(value);
				this.tryConvertVars(new Tuple<String,VarType>(type,this.referencedVarType),tpl);
				
				if (tpl.Item1==KWBoolean.constName&&type!=KWBoolean.constName)
					throw new ParsingError("You can only apply \""+KWBoolean.constTrue+"\" and +\""+KWBoolean.constFalse+"\" to boolean variables");
				
				if (type!=KWString.constName&&tpl.Item1==KWString.constName)
					throw new Exception("Can't convert \""+tpl.Item1+"\" to a string (\""+KWString.constName+"\").");
				
				if (this.referencedVariableIsLocal) {
					if (this.getLocalVarHomeBlock(this.referencedVariable)!=this.getCurrentBlock())
						this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{0x8F,0x45,this.pseudoStack.getVarEbpOffset(this.referencedVariable)}); //POP [EBP+-OFFSET]
				}
				else {
					
					if (type==KWByte.constName||type==KWBoolean.constName) {
					
						this.variableReferences[this.referencedVariable].Add((UInt32)this.opcodes.Count+7);
						this.addBytes(new Byte[]{
						              	
						              	0x31,0xDB, //XOR EBX,EBX
						              	0x58, //POP EAX
						              	0x88,0xC3, //MOV BL,AL
						              	0x88,0x1D,0,0,0,0 //MOV BYTE[PTR],BL
						              	
						              });
							
					}
						
					
					else if (type==KWShort.constName) {
					
						this.variableReferences[this.referencedVariable].Add((UInt32)this.opcodes.Count+3);
						this.addBytes(new Byte[]{
						              	
						              	0x66,0x8F,5,0,0,0,0 //POP WORD [PTR]
															//NOTICE:: this fucks up the stack and should be changed
						              	
						              });
						
					}
					
					else if (type==KWInteger.constName||type==KWString.constName) {
					
						this.variableReferences[this.referencedVariable].Add((UInt32)this.opcodes.Count+2);
						this.addBytes(new Byte[]{
						              	
						              	0x8F,5,0,0,0,0 //POP DWORD [PTR]
						              	
						              });
					
					}
				
				}
				
			}
			
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void updateVariableReferences () {
			
			foreach (KeyValuePair<String,List<UInt32>> references in this.variableReferences) {
				
				foreach (UInt32 index in references.Value) {
					
					Byte[]memAddrBytes=BitConverter.GetBytes(this.variables[references.Key].Item1);
					
					Byte i=0;
					while (i!=4) {
						this.opcodes[(Int32)index+i]=memAddrBytes[i];
						++i;
					}
					
				}
				
			}
			
			foreach (KeyValuePair<String,List<UInt32>> references in this.arrayReferences) {
				
				foreach (UInt32 index in references.Value) {
					
					Byte[]memAddrBytes=BitConverter.GetBytes(this.arrays[references.Key].Item1);
					
					Byte i=0;
					while (i!=4) {
						this.opcodes[(Int32)index+i]=memAddrBytes[i];
						++i;
					}
					
				}
				
			}
			
			if (processHeapVar!=null) {
				foreach (UInt32 index in this.processHeapVar.Item2) {
					
					Byte[]memAddrBytes=BitConverter.GetBytes(this.processHeapVar.Item1);
					
					Byte i=0;
					while (i!=4) {
						this.opcodes[(Int32)index+i]=memAddrBytes[i];
						++i;
					}
					
				}
			}
			
		}
		
		private void declareArray (String arrayVarType) {
			
			//the way arrays are stored in memory are:
			//dd Array Length
			//dd Array Member size in Bytes
			//... array members
			this.varType=arrayVarType;
			this.status=ParsingStatus.SEARCHING_ARRAY_NAME;
			
		}
		
		private void registerArray (String arrayName) {
			
			Console.WriteLine("Registering array "+arrayName+" (an array type of \""+this.varType+"\"), memAddress: "+memAddress.ToString("X"));
		
			if (this.nameExists(arrayName))
				throw new ParsingError("The name \""+arrayName+"\" is already in use");
			
			this.tryIncreaseBlockVarCount();
			
			if (this.blocks.Count==0) {
				this.arrays.Add(arrayName,new Tuple<UInt32,String,ArrayStyle>(0,this.varType,this.style));
				this.arrayReferences.Add(arrayName,new List<UInt32>());
	//			this.appendAfter.AddRange(new Byte[keywordMgr.getVarTypeByteSize(this.varType)]);
			}
			else {//should be local var
				this.pseudoStack.push(new LocalVar(arrayName));
				this.getCurrentBlock().localVariables.Add(arrayName,new Tuple<Tuple<String,VarType>>(new Tuple<String,VarType>(this.varType,VarType.NATIVE_ARRAY)));
				this.lastReferencedVariableIsLocal=true;
				foreach (UInt32 index in this.localVarEBPPositionsToOffset.Where(x=>x.Key.nestedLevel>this.getCurrentBlock().nestedLevel).SelectMany(x=>x.Value))
					this.offsetEBP(4,index);
			}
			
			this.lastReferencedVariable=arrayName;
			this.lastReferencedVarType=VarType.NATIVE_ARRAY;
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private List<Byte> getImportSection (out List<Tuple<String,UInt32>>funcMemAddrs) {
			
			List<Tuple<String,UInt32>> RVAnames    =new List<Tuple<String,UInt32>>(),//dll,pos
									   RVAtables   =new List<Tuple<String,UInt32>>(),//dll,pos
									   RVAfunctions=new List<Tuple<String,UInt32>>();//func,pos
			
			funcMemAddrs=new List<Tuple<String,UInt32>>();
									   
			List<Byte> opcodes=new List<Byte>();
			UInt32 sectMem=memAddress+(UInt32)appendAfter.Count;
			Console.WriteLine("0x"+sectMem.ToString("X"));
			while (sectMem%0x1000!=0)
				++sectMem;
			Console.WriteLine("0x"+sectMem.ToString("X"));
									   
			//Initial RVA's
			foreach (String s in this.toImport.Keys) {
				
				opcodes.AddRange(new Byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00});
				RVAnames.Add(new Tuple<String,UInt32>(s,(UInt32)opcodes.Count));
				opcodes.AddRange(new Byte[]{0x00,0x00,0x00,0x00});
				RVAtables.Add(new Tuple<String,UInt32>(s,(UInt32)opcodes.Count));
				opcodes.AddRange(new Byte[]{0x00,0x00,0x00,0x00});
				
			}
			
			opcodes.AddRange(new Byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00});
			
			//Add tables
			foreach (KeyValuePair<String,List<String>> kvp in this.toImport) {
				
				Console.WriteLine("Opcodes: "+opcodes.Count.ToString()+", Pos: "+(sectMem+opcodes.Count).ToString("X"));
				Byte[]bytes=BitConverter.GetBytes((UInt32)(sectMem+opcodes.Count));
				UInt32 pos=RVAtables.Where(x=>x.Item1==kvp.Key).First().Item2;
				opcodes[(Int32)pos]=bytes[0];
				opcodes[(Int32)pos+1]=bytes[1];
				
				foreach (String s in kvp.Value) {
					
					RVAfunctions.Add(new Tuple<String,UInt32>(s,(UInt32)opcodes.Count));
					funcMemAddrs.Add(new Tuple<String,UInt32>(s,sectMem+(UInt32)opcodes.Count));
					opcodes.AddRange(new Byte[]{0x00,0x00,0x00,0x00});
					
				}
				
				opcodes.AddRange(new Byte[]{0x00,0x00,0x00,0x00});
				
			}
			
			//dll names
			foreach (String s in this.toImport.Keys) {
				
				Console.WriteLine("Opcodes: "+opcodes.Count.ToString()+", Pos: "+(sectMem+opcodes.Count).ToString("X"));
				Byte[]bytes=BitConverter.GetBytes((UInt32)(sectMem+opcodes.Count));
				UInt32 pos=RVAnames.Where(x=>x.Item1==s).First().Item2;
				opcodes[(Int32)pos]=bytes[0];
				opcodes[(Int32)pos+1]=bytes[1];
				
				opcodes.AddRange(Encoding.ASCII.GetBytes(s));
				opcodes.Add(0x00);
				
			}
			
			//function names
			foreach (String key in this.toImport.Keys)
			foreach (String s in this.toImport[key]) {
				
				Console.WriteLine("Opcodes: "+opcodes.Count.ToString()+", Pos: "+(sectMem+opcodes.Count).ToString("X"));
				Byte[]bytes=BitConverter.GetBytes((UInt32)(sectMem+opcodes.Count));
				UInt32 pos=RVAfunctions.Where(x=>x.Item1==s).First().Item2;
				opcodes[(Int32)pos]=bytes[0];
				opcodes[(Int32)pos+1]=bytes[1];
				
				opcodes.AddRange(new Byte[]{0x00,0x00});
				opcodes.AddRange(Encoding.ASCII.GetBytes(s));
				opcodes.Add(0x00);
				
			}
			
			while (opcodes.Count%512!=0)
				opcodes.Add(0x00);
			
			return opcodes;
			
		}
		
		private void fillFuncMemAddrs (List<Tuple<String,UInt32>>funcMemAddrs) {
			
			Byte i;
			Byte[]memAdd;
			
			foreach (Tuple<String,UInt32>funcMemAddr in funcMemAddrs) {
				
				foreach (UInt32 pos in this.referencedFuncPositions[funcMemAddr.Item1]) {
					
					i=0;
					memAdd=BitConverter.GetBytes(funcMemAddr.Item2);
					while (i!=4) {
						
						Console.WriteLine("Total opcodes: "+this.opcodes.Count.ToString()+", Writing at: "+(pos+i).ToString());
						this.opcodes[(Int32)(pos+i)]=memAdd[i];
						
						++i;
						
					}
					
				}
				
			}
			
		}
		
		internal void referenceDll (String dllName,String funcName) {
			
			dllName=dllName.ToUpper();
			
			if (!(dllName.EndsWith(".DLL")))
				dllName+=".DLL";
			
			if (!(this.toImport.ContainsKey(dllName))) {
				
				this.toImport.Add(dllName,new List<String>());
				this.toImport[dllName].Add(funcName);
				
			}
			else
				if (!(this.toImport[dllName].Contains(funcName)))
					this.toImport[dllName].Add(funcName);
			
			if (!(this.referencedFuncPositions.ContainsKey(funcName)))
				this.referencedFuncPositions.Add(funcName,new List<UInt32>());
			
		}
		
		private void setProcessHeapVar () {
			
			const String GPH="GetProcessHeap";
			
			this.referenceDll(Parser.KERNEL32,GPH);
			this.referencedFuncPositions[GPH].Add((UInt32)(this.opcodes.Count+2));
			this.processHeapVar=new Tuple<UInt32,List<UInt32>>(this.memAddress+(UInt32)this.appendAfter.Count,new List<UInt32>(new UInt32[]{(UInt32)(opcodes.Count+7)}));
			this.appendAfter.AddRange(new Byte[4]);
			                  
			this.addBytes(new Byte[]{
			              	
			              	0xFF,0x15, //CALL FUNC
			              	0,0,0,0, //MEM ADDR TO GetProcessHeap
			              	
			              	0xA3, //MOV EAX TO
			              	0,0,0,0 //MEM ADDR TO processHeapVar
			              	
			              });
			
		}
		
		private void pushProcessHeapVar () {
			
			if (processHeapVar==null)
				setProcessHeapVar();
			
			this.processHeapVar.Item2.Add((UInt32)(this.opcodes.Count+2));
			this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0});
			
		}
		
		/// <summary>
		/// First push byte index and value 
		/// </summary>
		private void callSetArrayValue (String arrayName) {
			
			if (!(this.setArrayValueFuncPtrs.ContainsKey(arrayName))) {
				
				this.setArrayValueFuncPtrs.Add(arrayName,memAddress+2);
				
				if (this.isALocalVar(arrayName)) {
					
//					this.localVarEBPPositions[this.getLocalVarHomeBlock(arrayName)][arrayName].Add((UInt32)(this.opcodes.Count+8));
					
					this.addBytes(new Byte[] {
					              	
						            0xEB,10, // JMP 10 BYTES
					              	0x5A, //POP EDX
					              	0x5B, //POP EBX
					              	0x58, //POP EAX
					              	0x52, //PUSH EDX
					              	0x8B,0x45,this.pseudoStack.getVarEbpOffset(arrayName), //MOV EAX,[EBP+-OFFSET]
					              	0x89,0x18, //MOV EBX TO [EAX]
					              	0xC3 //RETN
					              	
					              });
					
				}
				
				else {
				
					this.arrayReferences[arrayName].Add((UInt32)(this.opcodes.Count+8));
					
					this.addBytes(new Byte[] {
					              	
						            0xEB,0x0D, // JMP 14 BYTES
					              	0x5A, //POP EDX
					              	0x5B, //POP EBX
					              	0x58, //POP EAX
					              	0x52, //PUSH EDX
					              	3,5,0,0,0,0, //ADD VALUE AT PTR TO EAX
					              	0x89,0x18, //MOV EBX TO [EAX]
					              	0xC3 //RETN
					              	
					              });
					
				}
				
			}
			
			Console.WriteLine("Calling setArrayValue: memAddress: "+memAddress.ToString("X")+", setArrayValueFuncPtrs[arrayName]: "+setArrayValueFuncPtrs[arrayName].ToString("X"));
			this.addBytes(new Byte[]{0xE8}.Concat(BitConverter.GetBytes((Int32)setArrayValueFuncPtrs[arrayName]-(Int32)memAddress-5)));
			
		}
		
		private void resetLastReferencedVar () {
			
			Console.WriteLine("Resetting last referenced variable");
			this.lastReferencedVariable=null;
			this.lastReferencedVarType=VarType.NONE;
			
		}
		
		
		internal Boolean isArrayIndexer (String str) { return str.EndsWith("]")&&str.Contains("["); }
		internal Boolean isValidArrayIndexer (String str) { return isArrayIndexer(str)&&this.arrays.ContainsKey(str.Split('[')[0]); }
		internal Boolean tryCheckIfValidArrayIndexer (String str) {
			
			if (this.isArrayIndexer(str)) {
				
				String referencedArray=str.Split('[')[0];
				
				if (this.arrays.ContainsKey(referencedArray))
					return true;
				
				foreach (Block blc in this.blocks.Keys)
					foreach (String str0 in blc.localVariables.Keys)
						if (blc.localVariables[str0].Item1.Item2==VarType.NATIVE_ARRAY)
							return true;
				
				throw new ParsingError("Invalid array indexer: Array \""+referencedArray+"\" does not exist");
				
			}
			else return false;
			
		}
		
		
		internal Boolean nameExists (String name) {
			
			//HACK:: check var type
			
			foreach (String str in keywordMgr.getKeywords().Select(x=>x.name))
				if (str==name) return true;
			
			return this.arrays.ContainsKey(name) ||
				this.variables.ContainsKey(name) ||
				name==KWBoolean.constFalse       ||
				name==KWBoolean.constTrue        ||
				this.functions.ContainsKey(name) ||
				this.isALocalVar(name);
			
		}
		
		/// <summary>
		/// Pushes the memory address address to an index in an array
		/// </summary>
		/// <param name="arrName">name of array</param>
		/// <param name="indexer">value inside first set of square brackets (not including first and last square brackets)</param>
		/// <returns>new status to update</returns>
		private ParsingStatus indexArray (String arrName,String indexer,Boolean recursing=false) {
			
			Console.WriteLine("Indexing array: \""+arrName+"\", indexer: "+indexer);
			
			//push the mem addr..
			if (this.tryCheckIfValidArrayIndexer(indexer)) {
				
				String sub=indexer.Substring(indexer.IndexOf('[')+1),
				  arrName0=indexer.Split('[')[0].Replace(" ",""),
				  indexer0=(sub.EndsWith("]"))?String.Concat(sub.Take(sub.Length-1)):sub;
					   
				this.indexArray(arrName0,indexer0,true);
				this.addBytes(
					(keywordMgr.getVarTypeByteSize(this.arrays[arrName0].Item2)==1) ?
						 new Byte[]{
						 	0x31,0xD2,    // XOR EDX,EDX
						 	0x8A,0x10     // MOV DL,[EAX]
						 }
					: (keywordMgr.getVarTypeByteSize(this.arrays[arrName0].Item2)==2) ?
					   new Byte[] {
					   		0x31,0xD2,     // XOR EDX,EDX
					   		0x66,0x8B,0x10 // MOV DX,[EAX]
					   }
					:
					new Byte[] { 0x8B,0x10 } // MOV EDX,[EAX]
				);
				if (this.isALocalVar(arrName)) {
					if (this.getCurrentBlock()!=this.getLocalVarHomeBlock(arrName))
						this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{3,0x55,this.pseudoStack.getVarEbpOffset(arrName)});
				}
				else {
					this.arrayReferences[arrName].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{3,0x15,0,0,0,0});//ADD EDX,VALUE @ PTR
				}
				this.addBytes(new Byte[]{0x83,0xC2,8});//ADD EDX,8
				if (recursing)
					this.addByte(0x92);//XCHG EAX,EDX
				else
					this.addByte(0x52);//PUSH EDX
					
				
			}
					
			else {
				
				Console.WriteLine("Trying to pushValue: \""+indexer+'"');
				this.pushValue(indexer);//PUSH ......
				this.addByte(0x58);//POP EAX
				Byte arrMemByteSize=(Byte)keywordMgr.getVarTypeByteSize((this.isALocalVar(arrName)?this.getLocalVarHomeBlock(arrName).localVariables[arrName].Item1.Item1:this.arrays[arrName].Item2));
				if (arrMemByteSize!=1)
					this.addBytes(new Byte[]{0x6B,0xC0,arrMemByteSize});//IMUL EAX BY ARRAY MEMBER BYTE SIZE
				this.addBytes(new Byte[]{0x83,0xC0,8}); //ADD 8 TO EAX
				if (this.isALocalVar(arrName)) { 
					
					if (this.getLocalVarHomeBlock(arrName)!=this.getCurrentBlock())
						this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{3,0x45,this.pseudoStack.getVarEbpOffset(arrName)});
				}
				else {
					this.arrayReferences[arrName].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{3,5,0,0,0,0});//ADD EAX,VALUE @ PTR
				}
				if (!recursing)
					this.addByte(0x50);//PUSH EAX
				
			}
			
			return ParsingStatus.SEARCHING_NAME;
			
		}
		
		/// <summary>
		/// Push a value (constant number,var,array,array indexer) onto the stack
		/// </summary>
		internal Tuple<String,VarType> pushValue (String value) {
			
			//HACK:: check var type here
			
			//constants:
			UInt32 _value;
			if (UInt32.TryParse(value,out _value)) {
				
				String rv;
				
				if (_value<=Byte.MaxValue) {
					
					this.addBytes(new Byte[]{0x6A,(Byte)(_value)});//PUSH BYTE _value
					return new Tuple<String,VarType>(KWByte.constName,VarType.NATIVE_VARIABLE);
					
				}
				else if (_value<=UInt16.MaxValue)
					rv=KWShort.constName;
				else rv=KWInteger.constName;
				
				//Words & Dwords use the same push opcode
				
				this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes(_value)));
				
				return new Tuple<String,VarType>(rv,VarType.NATIVE_VARIABLE);
				
			}
			else if (this.variables.ContainsKey(value)) {
				
				UInt32 byteSize=keywordMgr.getVarTypeByteSize(this.variables[value].Item2);
				if (byteSize==1) {
					
					this.addBytes(new Byte[]{0x31,0xDB}); //XOR EBX,EBX
					this.variableReferences[value].Add((UInt32)this.opcodes.Count+2);
					this.addBytes(new Byte[]{0x8A,0x1D,0,0,0,0}); //MOV BL,[PTR]
					this.addByte(0x53); //PUSH EBX
					return new Tuple<String,VarType>(this.variables[value].Item2,VarType.NATIVE_VARIABLE);
					
				}
				else if (byteSize==2) {
					
					this.variableReferences[value].Add((UInt32)this.opcodes.Count+3);
					this.addBytes(new Byte[]{0x66,0xFF,0x35,0,0,0,0}); //PUSH WORD [PTR]
					return new Tuple<String,VarType>(KWShort.constName,VarType.NATIVE_VARIABLE);
					
				}
				else /*byteSize==4*/ {
					
					this.variableReferences[value].Add((UInt32)this.opcodes.Count+2);
					this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0}); //PUSH DWORD [PTR]
					return new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
					
				}
				
			}
			else if (this.arrays.ContainsKey(value)) {
				
				this.arrayReferences[value].Add((UInt32)this.opcodes.Count+2);
				this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0}); //PUSH DWORD [PTR]
				return new Tuple<String,VarType>(this.arrays[value].Item2,VarType.NATIVE_ARRAY);
				
			}
			else if (this.tryCheckIfValidArrayIndexer(value)) {
				
				String sub=value.Substring(value.IndexOf('[')+1),
				   arrName=value.Split('[')[0];
				this.indexArray(arrName,(sub.EndsWith("]"))?String.Concat(sub.Take(sub.Length-1)):sub);
				
				UInt32 varTypeByteSize=(this.isALocalVar(arrName))?this.keywordMgr.getVarTypeByteSize(this.getLocalVarHomeBlock(arrName).localVariables[arrName].Item1.Item1):this.keywordMgr.getVarTypeByteSize(this.arrays[arrName].Item2);
				this.addBytes((varTypeByteSize==4)?
				              new Byte[]{0x5B,           //POP EBX
				              			 0xFF,0x33}      //PUSH DWORD [EBX]
				             :(varTypeByteSize==2)?
				              new Byte[]{0x5B,           //POP EBX
				              			 0x66,0xFF,0x33} //PUSH WORD [EBX]
				             :/*varTypeByteSize==1*/
				              new Byte[]{0x5B,           //POP EBX
				              			 0x31,0xD2,      //XOR EDX,EDX
				              			 0x8A,0x13,      //MOV DL,[EBX]
				              			 0x52}           //PUSH EDX
				             );
				return new Tuple<String,VarType>((varTypeByteSize==4)?KWInteger.constName:(varTypeByteSize==2)?KWShort.constName:KWByte.constName,VarType.NATIVE_ARRAY_INDEXER);
				
			}
			else if (value==KWBoolean.constFalse) {
				
				this.addBytes(new Byte[]{0x6A,0}); //PUSH 0
				return new Tuple<String,VarType>(KWBoolean.constName,VarType.NATIVE_VARIABLE);
				
			}
			else if (value==KWBoolean.constTrue) {
				
				this.addBytes(new Byte[]{0x6A,1}); //PUSH 1
				return new Tuple<String,VarType>(KWBoolean.constName,VarType.NATIVE_VARIABLE);
				
			}
			else if (value.Contains('(')&&functions.ContainsKey(value.Split('(')[0])) {
				
				String funcName=value.Split('(')[0];
				
				if (functions[funcName].Item2==null)
					throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained");
				
				String unparsedParams=value.Substring(value.IndexOf('(')+1);
				Byte roundBracketBalance=1;
				List<String>@params=new List<String>();
				StringBuilder paramBuilder=new StringBuilder();
				//HACK:: sub parsing
				foreach (Char c in unparsedParams) {
					
					if (c=='(') ++roundBracketBalance;
					else if (c==')') --roundBracketBalance;
					else if (c==',') {
						
						@params.Add(paramBuilder.ToString());
						paramBuilder.Clear();
						
					}
					else paramBuilder.Append(c);
					
					if (roundBracketBalance==0) {
						@params.Add(paramBuilder.ToString());
						break;
					}
					
				}
				this.callFunction(funcName,@params.ToArray());
				this.addByte(0x50); //PUSH EAX
				return this.functions[funcName].Item2;
				
			}
			else if (functions.ContainsKey(value)) {
				
				String funcName=value.Split('(')[0];
				
				if (functions[funcName].Item2==null)
					throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained");
				
				this.callFunction(funcName,new String[0]);
				this.addByte(0x50); //PUSH EAX
				return this.functions[funcName].Item2;
				
			}
			else if (this.isALocalVar(value)) {
				
				Block localVarHomeBlock=this.getLocalVarHomeBlock(value);
				if (localVarHomeBlock!=this.getCurrentBlock())
					this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
				this.addBytes(new Byte[]{0xFF,0x75,pseudoStack.getVarEbpOffset(value)});
				return localVarHomeBlock.localVariables[value].Item1;
				
			}
			else if (value.StartsWith("\"")&&value.EndsWith("\"")) {
				
				String innerText=value.Substring(1,value.Length-2);
				Byte[]chars=new Byte[innerText.Length+1];//+1 = Null Byte
				UInt16 i=0;
				foreach (Byte ch in innerText.Select(x=>(Byte)x)) {
					
					chars[i]=ch;
					
					++i;
					
				}
				
				this.stringsAndRefs.Add(new Tuple<UInt32,List<UInt32>>((UInt32)(this.memAddress+this.appendAfter.Count),new List<UInt32>(new UInt32[]{(UInt32)(this.opcodes.Count+1)})));
				this.appendAfter.AddRange(chars);
				this.addBytes(new Byte[]{0x68,0,0,0,0});
				
				return new Tuple<String,VarType>(KWString.constName,VarType.NATIVE_VARIABLE);
				
			}
			else throw new ParsingError("Invalid value: \""+value+'"');
			
		}
		
		private void freeHeaps () {
			
			//If no arrays (or anything that allocates memory), no point referencing HeapFree
			if (this.arrays.Count==0)
				return;
			
			if (this.arrays.Where(x=>x.Value.Item1==0).Count()==this.arrays.Count)
				return;
			
			const String HF="HeapFree";
			this.referenceDll(Parser.KERNEL32,HF);
			
			List<UInt32> doneMemAddrs=new List<UInt32>();
			foreach (KeyValuePair<String,Tuple<UInt32,String,ArrayStyle>> array in this.arrays) {
				
				if (doneMemAddrs.Contains(array.Value.Item1))
					continue;
				
				this.arrayReferences[array.Key].Add((UInt32)this.opcodes.Count+2);
				this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0});//push pMemory
				this.addBytes(new Byte[]{0x6A,0});//push Flags
				this.pushProcessHeapVar();//push hHeap
				this.referencedFuncPositions[HF].Add((UInt32)this.opcodes.Count+2);
				this.addBytes(new Byte[]{0xFF,0x15,0,0,0,0});
				doneMemAddrs.Add(array.Value.Item1);
				
			}
			
		}
		
		private void execKeyword (Keyword kw,String[] @params) {
			
			KeywordResult res=kw.execute(this,@params);
			this.status=res.newStatus;
			this.addBytes(res.newOpcodes);
			
			if (kw.type==KeywordType.ASSIGNMENT||kw.type==KeywordType.DECREMENT||kw.type==KeywordType.INCREMENT)
				this.resetLastReferencedVar();
			
		}
		
		internal void addBlock (Block block,Byte setExpectsBlock=1) {
			
			block.nestedLevel=this.nestedLevel;
			this.blocks.Add(block,0);
			this.blockBracketBalances.Add(block,0);
			this.blockVariablesCount.Add(block,0);
			this.blockAddrBeforeAppendingReferences.Add(block,new List<Tuple<UInt32,Int16>>());
			this.localVarEBPPositionsToOffset.Add(block,new List<UInt32>());
			this.setExpectsBlock=setExpectsBlock;
			//NOTE:: first param to ENTER is a word(short), second one is a byte
			if (block.addEnterAutomatically)
				this.enterBlock(block);
			
			Console.WriteLine("Blocks count: "+this.blocks.Count.ToString());
			++this.nestedLevel;
			
		}
		
		private void onBlockClosed (Block block) {
			
			Console.WriteLine("onBlockClosed called (Hash code: "+block.GetHashCode().ToString()+')');
			
			this.lastBlockClosed=block;
			
			if (block.shouldXOREAX) this.addBytes(new Byte[]{0x31,0xC0}); //XOR EAX,EAX
			UInt32 beforeAppendingMemAddr=this.memAddress;
			
			this.addByte(0xC9); // LEAVE
			this.pseudoStack.pop((UInt16)(1+block.localVariables.Count));//pseudo pop ebp and local vars
			this.addBytes(block.opcodesToAddOnBlockEnd);
			
			if (block.onBlockEnd!=null)
				block.onBlockEnd.Invoke();
			
			this.blocks[block]=memAddress;
			Console.WriteLine("onBlockClosed: "+memAddress.ToString("X")+','+block.startMemAddr.ToString("X"));
			Console.WriteLine(memAddress.ToString("X")+'-'+block.startMemAddr.ToString("X")+'='+(memAddress-block.startMemAddr).ToString());
			Byte[] memAddr=BitConverter.GetBytes((Int32)memAddress-(Int32)block.startMemAddr);
			Byte i;
			foreach (Block b in this.blocks.Select(x=>x.Key)) {
				
				Console.WriteLine("Found block: "+b.startMemAddr.ToString("X")+" (This block: "+block.startMemAddr.ToString("X")+')');
				
			}
			foreach (UInt32 index in block.blockMemPositions) {
				
				Console.WriteLine("Block Mem Pos @ "+index.ToString());
				
				i=0;
				
				while (i!=4) {
					
					opcodes[(Int32)index+i]=memAddr[i];
					++i;
					
				}
				
				
			}
			foreach (Tuple<UInt32,Int16>indexAndOffset in this.blockAddrBeforeAppendingReferences[block]) {
				
				i=0;
				Int32 addrOfJump=BitConverter.ToInt32(new Byte[]{this.opcodes[(Int32)indexAndOffset.Item1],this.opcodes[(Int32)indexAndOffset.Item1+1],this.opcodes[(Int32)indexAndOffset.Item1+2],this.opcodes[(Int32)indexAndOffset.Item1+3]},0);
				Byte[]memAddr0=BitConverter.GetBytes((Int32)beforeAppendingMemAddr-(addrOfJump+indexAndOffset.Item2)-4);//minus constant 4 at end to make up for jump opcode length
				
				while (i!=4) {
					
					opcodes[(Int32)indexAndOffset.Item1+i]=memAddr0[i];
					++i;
					
				}
				
			}
			
			Console.WriteLine("blockBracketBalances ct: "+blockBracketBalances.Count.ToString());
			this.blocks.Remove(block);
			this.blockBracketBalances.Remove(block);
			Byte[]varCt=BitConverter.GetBytes((UInt16)(this.blockVariablesCount[block]*4));
			this.opcodes[(Int32)this.enterPositions[block]]=varCt[0];
			this.opcodes[(Int32)this.enterPositions[block]+1]=varCt[1];
			this.blockVariablesCount.Remove(block);
			this.enterPositions.Remove(block);
			if (this.blocks.Count==0)
				this.localVarEBPPositionsToOffset.Clear();
			Console.WriteLine("blockBracketBalances post ct: "+blockBracketBalances.Count.ToString());
			Console.WriteLine("Searching again: ");
			foreach (Block b in this.blocks.Select(x=>x.Key)) {
				
				Console.WriteLine("Found block: "+b.startMemAddr.ToString("X")+" (This block: "+block.startMemAddr.ToString("X")+", Same block: "+(b==block).ToString()+')');
				
			}
			Console.WriteLine("New Block No.: "+blockBracketBalances.Count.ToString());
			--this.nestedLevel;
			
		}
		
		private void updateBlockBalances (Char c) {
			
			Console.WriteLine(this.blockBracketBalances.Count.ToString());
			Dictionary<Block,UInt16>newDict=new Dictionary<Block,UInt16>(this.blockBracketBalances.Count);
			Block toClose=null;
			foreach (KeyValuePair<Block,UInt16> kvp in this.blockBracketBalances) {
				
				Console.WriteLine("Block Mem Addr: "+kvp.Key.startMemAddr.ToString("X"));
				Console.WriteLine("Old: "+kvp.Value.ToString());
				
				newDict.Add(kvp.Key,(UInt16)((this.closesBlock(c))?kvp.Value-1:kvp.Value+1));
				if (newDict[kvp.Key]==0) {
					toClose=kvp.Key;
				}
				
				Console.WriteLine("New: "+newDict[kvp.Key].ToString());
				
			}
			this.blockBracketBalances=new Dictionary<Block,UInt16>(newDict);
			if (toClose!=null)
				this.onBlockClosed(toClose);
			
		}
		
		private void callFunction (String functionName,String[]@params) {
			
			if (!(this.functions.ContainsKey(functionName)))
				throw new ParsingError("Function does not exist: \""+functionName+'"');
			
			if (this.functions[functionName].Item3!=@params.Length)
				throw new ParsingError("Expected \""+this.functions[functionName].Item3.ToString()+"\" parameters for \""+functionName+'"');
			
			if (this.functionParamTypes.Count!=0) {
				UInt16 i=(UInt16)(this.functionParamTypes.Count-1);
				foreach (String str in @params.Reverse()) {
					
					this.tryConvertVars(this.functionParamTypes[functionName][i],this.pushValue(str));
					if (i==0) break;
					--i;
					
				}
			}
			this.addBytes(new Byte[]{0xE8,0,0,0,0}); //CALL Mem Addr
			this.functionReferences[functionName].Add(new Tuple<UInt32,UInt32>((UInt32)opcodes.Count-4,this.memAddress));
			
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void fillFunctionReferences () {
			
			Byte[]memAddr;
			Byte i;
			
			foreach (String funcName in this.functions.Keys) {
				
				foreach (Tuple<UInt32,UInt32>tpl in this.functionReferences[funcName]) {
					
					i=0;
					Console.WriteLine("Writing opcodes @ "+tpl.Item1.ToString());
					Console.WriteLine("Filling function ref @ "+tpl.Item1.ToString()+": "+this.functions[funcName].Item1.ToString("X")+'-'+tpl.Item2.ToString("X")+'='+((Int32)this.functions[funcName].Item1-(Int32)tpl.Item2).ToString());
					memAddr=BitConverter.GetBytes(((Int32)this.functions[funcName].Item1-(Int32)tpl.Item2));
					
					while (i!=4) {
						
						this.opcodes[(Int32)tpl.Item1+i]=memAddr[i];
						++i;
						
					}
					
				}
				
			}
			
		}
		
		private void fillHeapFreeReferences () {
			
			Byte[]memAddr;
			Byte i;
			
			foreach (UInt32 pos in this.freeHeapsRefs) {
				
				i=0;
				
				memAddr=BitConverter.GetBytes(((Int32)freeHeapsMemAddr)-BitConverter.ToInt32(new Byte[]{opcodes[(Int32)pos],opcodes[(Int32)pos+1],opcodes[(Int32)pos+2],opcodes[(Int32)pos+3]},0));
				
				while (i!=4) {
					
					this.opcodes[(Int32)pos+i]=memAddr[i];
					++i;
					
				}
				
			}
			
		}
		
		internal UInt32 getOpcodesCount () { return (UInt32)this.opcodes.Count; }
		
		internal Tuple<String,VarType> getVarType (String value) {
			
			//HACK:: check var type here
			
			
			//NATIVE_VARIABLE
			foreach (Keyword kw in keywordMgr.getKeywords().Where(x=>x.type==KeywordType.TYPE)) {
				
				if (value==kw.name)
					return new Tuple<String,VarType>(kw.name,VarType.NATIVE_VARIABLE);
				
			}
			
			//NATIVE_ARRAY
			if (value.EndsWith("#")) {
				
				String newValue=String.Concat(value.Take(value.Length-1));
				
				foreach (Keyword kw in keywordMgr.getKeywords().Where(x=>x.type==KeywordType.TYPE)) {
				
					if (newValue==kw.name)
						return new Tuple<String,VarType>(kw.name,VarType.NATIVE_ARRAY);
					
				}
				
			}
			
			//CLASS
			
			throw new ParsingError("Not a var type: \""+value+'"');
				
			
		}
		
		private void exec (Executor executor,String[]@params) {
			
			if (!(String.IsNullOrEmpty(executor.func)))
				this.callFunction(executor.func,@params);
			else if (executor.kw!=null)
				this.execKeyword(executor.kw,@params);
			
		}
		
		private void tryConvertVars (Tuple<String,VarType>to,Tuple<String,VarType>from) {
			
			//HACK:: check var type here
			if (from.Item2==VarType.NATIVE_ARRAY_INDEXER||from.Item2==VarType.NATIVE_VARIABLE) {
				if (keywordMgr.getVarTypeByteSize(to.Item1)<keywordMgr.getVarTypeByteSize(from.Item1))
					throw new ParsingError("Can't convert \""+to.Item1+"\" to \""+from.Item1+'"');
			}
			else if (from.Item2!=to.Item2&&keywordMgr.getVarTypeByteSize(to.Item1)!=4) // TODO:: if there are ever any NON 4 byte variable types (ptrs), fix this, what this does is allow pointers of native arrays etc to be moved into native integers!
				throw new ParsingError("Can't convert \""+to.Item2.ToString()+"\" of \""+to.Item1.ToString()+"\" to \""+from.Item2.ToString()+'"');
				
			
		}
		
		internal String getVariablesType (String varName) {
			
			return this.variables[varName].Item2;
			
		}
		
		private Boolean isValidFunction (String value) {
			
			return (value.Contains('(')&&functions.ContainsKey(value.Split('(')[0])) || (functions.ContainsKey(value));
			
		}
		
		private Boolean isALocalVar (String value) {
			
			foreach (IEnumerable<String> strings in this.blocks.Select(x=>x.Key.localVariables.Keys.Cast<String>())) {
				
				if (strings.Contains(value))
					return true;
				
			}
			
			return false;
			
		}
		
		private Block getLocalVarHomeBlock (String value) {
			
			return this.blocks.Where(x=>x.Key.localVariables.ContainsKey(value)).First().Key;
			
		}
		
		private Boolean tryGetCurrentBlock (out Block block) {
			
			block=null;
			if (this.blocks.Count==0)
				return false;
			else {
				
				block=this.blocks.Last().Key;
				return true;
				
			}
			
		}
		
		private void registerClassInstance () {
			
			//TODO:: Parser#registerClassInstance
			this.tryIncreaseBlockVarCount();
			
		}
		
		private void tryIncreaseBlockVarCount () {
			
			Block b;
			if (this.tryGetCurrentBlock(out b))
				++this.blockVariablesCount[b];
			
		}
		
		private Block getCurrentBlock () { return this.blocks.Last().Key; }
		
		private void enterBlock (Block block,Int32 offset=0) {
			
			this.enterPositions.Add(block,(UInt32)(this.opcodes.Count+1+offset));
			this.addBytes(new Byte[]{0xC8,0,0,0}); //ENTER 0,0 (first parameter 0 should be overwritten later if local variables are introduced)
			this.pseudoStack.push(new PreservedEBP());
			
		}
		
		internal Byte[] getEnterBlockOpcodes (Block block,Int32 offset=0) {
			
			this.enterPositions.Add(block,(UInt32)(this.opcodes.Count+1+offset));
			this.pseudoStack.push(new PreservedEBP());
			return new Byte[]{0xC8,0,0,0}; //ENTER 0,0 (first parameter 0 should be overwritten later if local variables are introduced)
			
		}
		
		private void offsetEBP(SByte offset,UInt32 index) {
		
			this.opcodes[(Int32)index]=unchecked((Byte)((SByte)((this.opcodes[(Int32)index])+offset)));
			
		}
		
		private void fillConstantStringReferences () {
			
			Byte i0;
			Byte[]bytes;
			foreach (Tuple<UInt32,List<UInt32>>tpl in this.stringsAndRefs) {
				
				foreach (UInt32 i in tpl.Item2) {
					
					i0=0;
					bytes=BitConverter.GetBytes(tpl.Item1);
					while (i0!=4) {
						
						this.opcodes[(Int32)(i+i0)]=bytes[i0];
						++i0;
						
					}
					
				}
				
			}
			
		}
		
		private Boolean isFormOfBlankspace (Char c) {
			
			return c==' '||c=='\n'||c=='\r'||c=='\t';
			
		}
		
		private Boolean refersToIncrementOrDecrement (Char c) {
			
			return c=='+'||c=='-';
			
		}
		
		private Boolean isArrayDeclarationChar (Char c) {
			
			return c=='#';
			
		}
		
		private Boolean beginsArrayIndexer (Char c) {
			
			return c=='[';
			
		}
		
		private Boolean endsArrayIndexer (Char c) {
			
			return c==']';
			
		}
		
		private Boolean isNativeArrayCreationIndicator (Char c) {
			
			return c=='#';
			
		}
		
		private Boolean beginsParameters (Char c) {
			
			return c=='(';
			
		}
		
		private Boolean endsParameters (Char c) {
			
			return c==')';
			
		}
		
		private Boolean splitsParameters (Char c) {
			
			return c==',';
			
		}
		
		private Boolean opensBlock (Char c) {
			
			return c=='{';
			
		}
		
		private Boolean closesBlock (Char c) {
			
			return c=='}';
			
		}
		
	}
	
}
