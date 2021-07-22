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
using System.Runtime.InteropServices;

namespace ProgrammingLanguageTutorialIdea {
	
	public class Parser {
		
		public const String NULL_STR="null";
		
		public readonly String parserName;
		
		public String lastReferencedVariable {
			
			private set;
			get;
			
		}
		public Boolean winApp {
			
			get;
			internal set;
			
		}
		public String referencedVariable;
		public Boolean referencedVariableIsLocal,referencedVariableIsFromClass;
		
		internal Boolean lastReferencedVariableIsLocal,lastReferencedVariableIsFromClass;
		
		internal VarType lastReferencedVarType=VarType.NONE,referencedVarType=VarType.NONE;
		internal String varType; //to fix for classes later, maybe set to a Tuple<String,String>//Name,Origin or something? where Origin is something like the filename, or something to get the exact class that is being referred to
		internal Dictionary<String,List<UInt32>> variableReferences=new Dictionary<String,List<UInt32>>(),//Name,(Index in the Opcodes List)
												 arrayReferences=new Dictionary<String,List<UInt32>>(),//Name,(Index in the Opcodes List)
												 classReferences=new Dictionary<String,List<UInt32>>();//Name,(Index in the Opcodes List)
		internal Dictionary<Class,List<UInt32>> staticClassReferences=new Dictionary<Class,List<UInt32>>();//Name,(Index in the Opcodes List)
		
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
		internal UInt32 restoreEsiFuncAddr {
			
			private set;
			get;
			
		}
		
		internal Boolean expectsBlock=false,expectsElse=false,searchingFunctionReturnType=false,inFunction=false,@struct=false;
		internal Byte setExpectsElse=0,setExpectsBlock=0;
		
		internal Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention>> functions;//Function Name,(Memory Address,(Return Type, Return Var Type),No. of expected parameters,Function Type,Calling Convention)
		internal Dictionary<String,List<Tuple<UInt32,UInt32>>> functionReferences;//Function Name,(Indexes in Opcodes of the reference,Memory Address at time of Reference)
		internal UInt16 nextFunctionParamsCount;
		internal Dictionary<String,List<Tuple<String,VarType>>> functionParamTypes;//Function Name,Var Type
		internal Tuple<String,VarType>[] nextFunctionParamTypes;
		
		internal readonly Action elseBlockClosed;
		internal KeywordType[] nextExpectedKeywordTypes=new []{KeywordType.NONE};
		
		internal List<UInt32>freeHeapsRefs;
		internal Dictionary<String,List<Int32>>refdFuncsToIncreaseWithOpcodes;
		
		internal Block lastFunctionBlock;
		
		internal readonly PseudoStack pseudoStack;
		internal readonly KeywordMgr keywordMgr;
		internal Block lastBlockClosed {
			
			get;
			private set;
							
		}
		
		internal FunctionType nextType;
		internal String nextReferencedDLL;
		internal Boolean gui=false,toggledGui=false;
		
		internal Boolean setToWinAppIfDllReference {
			
			get;
			private set;
							
		}
		
		internal ArrayStyle style;//TODO:: do static memory block, later do stack allocation, and also allow this to be changed outside of compiler in the code with keyword SetArrayStyle (constName: SetArrStyle, expectsParams=true)
		
		internal List<Class>importedClasses;
		internal List<String> defineTimeOrder;
		internal String lastReferencedClassInstance;
		internal List<UInt32>esiFuncReferences=new List<UInt32>();
		
		internal Tuple<UInt32,List<UInt32>> processHeapVar;//Mem Addr, References
		internal Boolean addEsiToLocalAddresses=false;
		/// <summary>
		/// Only set if addEsiToLocalAddresses is true
		/// LEA [ESI-esiOffsetFromStart] = Start of Opcodes Mem Address
		/// </summary>
		internal UInt32 esiOffsetFromStart=0;
		internal Dictionary<String,UInt32> appendAfterIndex=new Dictionary<String,UInt32>();
		
		private List<Byte> opcodes=new List<Byte>(),importOpcodes=null,finalBytes=new List<Byte>(),appendAfter=new List<Byte>();
		private ParsingStatus status;
		private Dictionary<String,Tuple<UInt32,String>> variables=new Dictionary<String,Tuple<UInt32,String>>();//Name,(Mem Address,Var Type)
		private Dictionary<String,Tuple<UInt32,String,ArrayStyle>> arrays=new Dictionary<String,Tuple<UInt32,String,ArrayStyle>>();//Name,(Ptr To Mem Address of Heap Handle(Dynamic) or Mem Block(Static),Array Var Type,ArrayStyle(Dynamic or Static))
		private Dictionary<String,Tuple<UInt32,String,Class>> classes=new Dictionary<String,Tuple<UInt32,String,Class>>();//Name,(Ptr To Mem Address of Heap Handle,Class type name,Class type)
		private List<UInt32>int32sToSubtractByFinalOpcodesCount=new List<UInt32>();
		
		private List<Tuple<UInt32,List<UInt32>>>stringsAndRefs; //(Mem Addr,List of References by Opcode Index),Note: Currently the Inner list of Opcode Indexes will only have a length of 1 (6/19/2021 5:19PM)
		private Dictionary<String,UInt32> setArrayValueFuncPtrs;
		private Executor waitingToExecute;
		private Char nextChar;
		private UInt16 squareBracketBalance=0,roundBracketBalance=0,nestedLevel=0;
		private UInt32 freeHeapsMemAddr,esiFuncVarIndex=0;
		private Boolean attemptingClassAccess=false,gettingClassItem=false;
		
		private const String KERNEL32="KERNEL32.DLL";
		private readonly Char[] mathOperators=new []{'+','-','*','/','%'};
		private readonly Boolean skipHdr,fillToNextPage,writeImportSection;
		private const Char accessorChar='.';
		
		public Parser (String name,Boolean winApp=true,Boolean setToWinAppIfDllReference=false,Boolean skipHdr=false,Boolean fillToNextPage=true,Boolean writeImportSection=true) {
			
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
					
					i=0;
					while (i!=4) {
						
						opcodes[(Int32)index+i]=newNum[i];
						++i;
						
					}
					
				}
				
			};
			functions=new Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention>>();
			functionReferences=new Dictionary<String,List<Tuple<UInt32,UInt32>>>();
			freeHeapsRefs=new List<UInt32>();
			functionParamTypes=new Dictionary<String,List<Tuple<String,VarType>>>();
			enterPositions=new Dictionary<Block,UInt32>();
			blockVariablesCount=new Dictionary<Block,UInt16>();
			blockAddrBeforeAppendingReferences=new Dictionary<Block,List<Tuple<UInt32,Int16>>>();
			pseudoStack=new PseudoStack();
			localVarEBPPositionsToOffset=new Dictionary<Block,List<UInt32>>();
			stringsAndRefs=new List<Tuple<UInt32,List<UInt32>>>();
			this.setToWinAppIfDllReference=setToWinAppIfDllReference;
			this.skipHdr=skipHdr;
			this.parserName=name;
			refdFuncsToIncreaseWithOpcodes=new Dictionary<String,List<Int32>>();
			this.fillToNextPage=fillToNextPage;
			this.writeImportSection=writeImportSection;
			importedClasses=new List<Class>();
			classes=new Dictionary<String,Tuple<UInt32,String,Class>>();
			staticClassReferences=new Dictionary<Class,List<UInt32>>();
			defineTimeOrder=new List<String>();
			
			Console.WriteLine("Parser \""+parserName+"\" skipHdr:"+skipHdr.ToString());
			
		}
		
		public Byte[] parse (String data) {
			
			status=ParsingStatus.SEARCHING_NAME;
			StringBuilder nameReader=new StringBuilder();
			squareBracketBalance=0;
			roundBracketBalance=0;
			String arrName=null;
			List<String> paramsList=new List<String>();
			Int32 currentChar=0;
			Boolean inDoubleQuotes=false;
			UInt16 rbbrv=0;
			
			data+=' ';
			
			foreach (Char c in data) {
				
				Console.WriteLine(" - Checking:\""+c+"\",ParsingStatus:"+status.ToString()+",blockBracketBalance #no:"+blockBracketBalances.Count.ToString()+",rbbrv:"+rbbrv.ToString());
				
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
							
							if (this.indicatesComment(c))
								status=ParsingStatus.IN_COMMENT;
							else {
							
								inDoubleQuotes=c=='"';
								nameReader.Append(c);
								++status;
								if (status==ParsingStatus.READING_VALUE) 
									rbbrv=this.modRbbrv(c,inDoubleQuotes,rbbrv);
								
							}
							
						}
						break;
					
					case ParsingStatus.READING_NAME:
						
						if (this.isArrayDeclarationChar(c)) {
							
							if (searchingFunctionReturnType) {
								
								nameReader.Append(c);
								this.functions[this.functions.Last().Key]=new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention>(this.functions.Last().Value.Item1,this.getVarType(nameReader.ToString()),functions.Last().Value.Item3,functions.Last().Value.Item4,functions.Last().Value.Item5);
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
						else if (this.accessingClass(c)) {
							
							this.tryCreateRestoreEsiFunc();
							this.attemptingClassAccess=true;
							this.chkName(nameReader.ToString());
							nameReader.Clear();
							
							
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
						
						if (Char.IsLetterOrDigit(c)||this.isUnderscore(c)) nameReader.Append(c);
						else {
							
							this.registerVariable(nameReader.ToString());
							nameReader.Clear();
							
						}
						
						break;
						
					case ParsingStatus.READING_VALUE:
						
						rbbrv=this.modRbbrv(c,inDoubleQuotes,rbbrv);
						
						if ((inDoubleQuotes&&c=='"')||(!inDoubleQuotes&&this.isFormOfBlankspace(c)&&rbbrv==0)) {
							
							if (inDoubleQuotes) nameReader.Append(c);
							this.processValue(nameReader.ToString());
							nameReader.Clear();
							referencedVariable=null;
							referencedVarType=VarType.NONE;
							inDoubleQuotes=false;
							
						}
						else nameReader.Append(c);
						
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
						
						else if (this.splitsParameters(c)&&roundBracketBalance==1) { 
							
							paramsList.Add(nameReader.ToString());
							nameReader.Clear();
						
						}
						
						if (roundBracketBalance==0) {
							
							paramsList.Add(nameReader.ToString());
							nameReader.Clear();
							this.exec(this.waitingToExecute,paramsList.ToArray());
							paramsList.Clear();
							
						}
						
						else if (!(this.splitsParameters(c)&&roundBracketBalance==1)) nameReader.Append(c);
						
						break;
						
					case ParsingStatus.READING_FUNCTION_NAME:
						
						if (Char.IsLetterOrDigit(c)||this.isCallingConventionIdentifier(c)) nameReader.Append(c);
						else {
							
							this.setExpectsBlock=1;
							String funcName=nameReader.ToString();
							CallingConvention cl=CallingConvention.StdCall;
							if (funcName.Any(x=>this.isCallingConventionIdentifier(x))) {
								
								if (this.nextType==FunctionType.SUNSET)
									throw new ParsingError("Did not expect calling convention for Sunset defined function (calling convention is stdcall)");
								else if (this.nextType==FunctionType.DLL_REFERENCED) {
									
									String[]sp=funcName.Split(':');
									if (sp.Length!=2)
										throw new ParsingError("Invalid syntax for calling convention identifier");
									
									funcName=sp[1];
									switch (sp[0].ToLower()) {
											
										case "stdcall":
											break;
										case "cdecl":
											cl=CallingConvention.Cdecl;
											break;
										case "fastcall":
										case "thiscall":
											throw new ParsingError("Calling convention not supported: \""+sp[0]+'"');
										default:
											throw new ParsingError("Invalid calling convention: \""+sp[0]+"\", did you mean \"stdcall\" or \"cdecl\"?");
											
									}
									
								}
								
							}
							if (this.nextType==FunctionType.DLL_REFERENCED)
								this.referenceDll(this.nextReferencedDLL,funcName);
							nameReader.Clear();
							if (functionParamTypes.ContainsKey(funcName))
								throw new ParsingError("A function is already declared with the name \""+funcName+'"');
							this.functionParamTypes.Add(funcName,new List<Tuple<String,VarType>>(this.nextFunctionParamTypes));
							this.functions.Add(funcName,new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention>((this.nextType== FunctionType.SUNSET)?blocks.Keys.Last().startMemAddr:0,null,(UInt16)this.nextFunctionParamTypes.Length,this.nextType,cl));
							this.functionReferences.Add(funcName,new List<Tuple<UInt32,UInt32>>());
							status=ParsingStatus.SEARCHING_NAME;
							if (this.nextType==FunctionType.SUNSET)
								this.nextExpectedKeywordTypes=new []{KeywordType.TYPE};
							this.searchingFunctionReturnType=true;
							
						}
						
						break;
						
					case ParsingStatus.IN_COMMENT:
						
						if (this.isNewlineOrReturn(c))
							status=ParsingStatus.SEARCHING_NAME;
						
						break;
						
					case ParsingStatus.STOP_PARSING_IMMEDIATE:
						return this.compile();
						
				}
				
				++currentChar;
				if (data.Length>currentChar)
					nextChar=data[currentChar];
				
			}
			
			return compile();
			
		}
		
		private Byte[] compile () {
			
			Console.WriteLine("Compiling: "+this.parserName);
			
			if (!@struct) {
				
				this.addBytes(new Byte[]{0x6A,0}); //PUSH 0
				freeHeapsMemAddr=memAddress;
				this.freeHeaps();
				this.addByte(0x58); //POP EAX to set the exit code (return value) of process (HACK:: NOTICE::return value is an UNSIGNED value)
				this.addByte(0xC3); //Add RETN call to end of our exe, so no matter what happens in terms of the source, it should not be a blank application & will exit
			}
			
			this.setEsiFuncVar();
			this.fillEsiFuncReferences();
			opcodes.AddRange(appendAfter);
			this.updateVariableReferences();
			this.fillFunctionReferences();  
			this.fillHeapFreeReferences();
			this.fillConstantStringReferences();
			this.subtractInt32s();
			
			if ((!(winApp))&&(this.toImport.Count!=0)) {
				
				if (this.setToWinAppIfDllReference)
					this.winApp=true;
				else
					throw new ParsingError("Can not reference DLL's on non-PE app ("+parserName+')');
				
			}
			
			if (winApp) {
				
				List<Tuple<String,UInt32>>funcMemAddrs=null;
				
				if (!(skipHdr)) {
					
					if (writeImportSection) {
					
						if (this.toImport.Count>0)
							importOpcodes=this.getImportSection(out funcMemAddrs);
						
						if (funcMemAddrs!=null)
							this.fillFuncMemAddrs(funcMemAddrs);
					
					}
					
					PEHeader hdr=PEHeaderFactory.newHdr(opcodes,importOpcodes,memAddress,-this.appendAfter.Count,this.gui);
					
					finalBytes.AddRange(hdr.toBytes());
					
				}
				
				if (this.fillToNextPage) {
					
					while (opcodes.Count%512!=0)
						opcodes.Add(0x00);
					
				}
				
			}
			
			finalBytes.AddRange(opcodes);
			
			if (writeImportSection&&importOpcodes!=null) {
				finalBytes.AddRange(importOpcodes);
			}
			
			return finalBytes.ToArray();
			
		}
		
		internal void addByte (Byte b) {
			
			if (@struct) {
				if (referencedVariable==null)
					throw new ParsingError("A struct is limited to only variable declarations");
				else return;
			}
			
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
			
			Dictionary<String,Tuple<UInt32,String,Class>> newDict1=new Dictionary<String,Tuple<UInt32,String,Class>>(this.classes.Count);
			foreach (KeyValuePair<String,Tuple<UInt32,String,Class>> kvp in this.classes) {
				
				if (kvp.Value.Item1==0) newDict1.Add(kvp.Key,kvp.Value);
				
				else {
				
					Console.WriteLine("For class: "+kvp.Key+", updating mem address to: "+(kvp.Value.Item1+1).ToString("X"));
					
					newDict1.Add(kvp.Key,new Tuple<UInt32,String,Class>(kvp.Value.Item1+1,kvp.Value.Item2,kvp.Value.Item3));
				
				}
			}
			
			foreach (Class cl in this.importedClasses)
				++cl.memAddr;
			
			this.classes=new Dictionary<String,Tuple<UInt32,String,Class>>(newDict1);
			
			List<Tuple<UInt32,List<UInt32>>>newList=new List<Tuple<UInt32,List<UInt32>>>(this.stringsAndRefs.Count);
			foreach (Tuple<UInt32,List<UInt32>>str in this.stringsAndRefs) {
				
				newList.Add(new Tuple<UInt32,List<UInt32>>(str.Item1+1,str.Item2));
				
			}
			this.stringsAndRefs=new List<Tuple<UInt32,List<UInt32>>>(newList);
			
			if (processHeapVar!=null&&!addEsiToLocalAddresses){
				
				this.processHeapVar=new Tuple<UInt32,List<UInt32>>(this.processHeapVar.Item1+1,this.processHeapVar.Item2);
				
			}
			
			opcodes.Add(b);
			++memAddress;
			this.increaseRefdFuncsToIncreaseWithOpcodes();
			
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
			
			if (!pKTs.Contains(KeywordType.NONE)||attemptingClassAccess)
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
			
			if (this.classes.ContainsKey(name)) {
				
				lastReferencedVariableIsLocal=false;
				this.lastReferencedVarType=VarType.CLASS;
				this.status=ParsingStatus.SEARCHING_NAME;
				this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
				this.lastReferencedVariable=name;
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
			
			if (this.containsImportedClass(name)) {
					
					if (!pKTs.Contains(KeywordType.NONE)&&!pKTs.Contains(KeywordType.TYPE)) {
						if (pKTs.Length>1) {
							StringBuilder sb=new StringBuilder();
							foreach (KeywordType kt in pKTs)
								sb.Append('"'+kt.ToString()+"\", ");
							throw new ParsingError("Expected a keyword of any of the following types: "+String.Concat(sb.ToString().Take(sb.Length-2)));
						}
						else throw new ParsingError("Expected a keyword of type \""+pKTs[0].ToString()+'"');
					}
				
					this.varType=name;
					this.lastReferencedVarType=VarType.CLASS;
					status=ParsingStatus.SEARCHING_VARIABLE_NAME;
					
					if (wasSearchingFuncReturnType) {
						this.functions[this.functions.Last().Key]=new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention>(this.functions.Last().Value.Item1,new Tuple<String,VarType>(this.varType,VarType.CLASS),functions.Last().Value.Item3,functions.Last().Value.Item4,functions.Last().Value.Item5);
						status=ParsingStatus.SEARCHING_NAME;
						this.setExpectsBlock=1;
					}
					lastReferencedVariableIsLocal=false;
					return;
				
			}
			else if (attemptingClassAccess) {
				
				if (this.classes.ContainsKey(name)) {
					
					this.lastReferencedVariable=name;
					this.status=ParsingStatus.SEARCHING_NAME;
					this.gettingClassItem=true;
					this.attemptingClassAccess=false;
					lastReferencedVariableIsLocal=false;
					this.lastReferencedClassInstance=name;
					return;
					
				}
				else if (this.isALocalVar(name)&&this.getLocalVarHomeBlock(name).localVariables[name].Item1.Item2==VarType.CLASS) {
					
					this.lastReferencedVariable=name;
					this.lastReferencedVariableIsLocal=true;
					this.gettingClassItem=true;
					this.status=ParsingStatus.SEARCHING_NAME;
					this.attemptingClassAccess=false;
					this.lastReferencedClassInstance=name;
					return;
					
				}
				else throw new ParsingError("Expected class instance, got: \""+name+'"');
				
			}
			else if (gettingClassItem) {
				
				gettingClassItem=false;
				lastReferencedVariableIsFromClass=true;
				Class cl;
				if (lastReferencedVariableIsLocal) cl=this.importedClasses.Where(x=>x.className==this.getLocalVarHomeBlock(this.lastReferencedClassInstance).localVariables[this.lastReferencedClassInstance].Item1.Item1).First();
				else cl=this.classes[this.lastReferencedClassInstance].Item3;
				if (cl.variables.ContainsKey(name)) {
					
					this.lastReferencedVariable=name;
					Console.WriteLine("LRV:"+this.lastReferencedVariable);
					this.status=ParsingStatus.SEARCHING_NAME;
					this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
					this.lastReferencedVarType=VarType.NATIVE_VARIABLE;
					return;
					
				}
				else if (cl.classes.ContainsKey(name)) {
					
					this.lastReferencedVariable=name;
					Console.WriteLine("LRV:"+this.lastReferencedVariable);
					this.status=ParsingStatus.SEARCHING_NAME;
					this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
					this.lastReferencedVarType=VarType.CLASS;
					return;
					
				}
				else if (cl.functions.ContainsKey(name)) {
					
					lastReferencedVariableIsLocal=false;
					this.status=ParsingStatus.SEARCHING_NAME;
					lastReferencedVariableIsFromClass=false;
					
					if (cl.functions[name].Item3!=0) {
						
						status=(this.beginsParameters(this.nextChar))?ParsingStatus.READING_PARAMETERS:ParsingStatus.SEARCHING_PARAMETERS;
						roundBracketBalance=1;
						this.waitingToExecute=new Executor(){classFunc=new Tuple<String,String>(this.lastReferencedVariable,name)};
						
					}
					else {
						
						this.callClassFunc(this.lastReferencedVariable,name,new String[0]);
						
					}
					return;
					
				}
				else throw new ParsingError("Invalid class item: \""+name+'"');
				
			}
			
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
						this.functions[this.functions.Last().Key]=new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention>(this.functions.Last().Value.Item1,new Tuple<String,VarType>(this.varType,this.lastReferencedVarType),functions.Last().Value.Item3,functions.Last().Value.Item4,functions.Last().Value.Item5);
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
			
			if (this.containsImportedClass(this.varType)) {
				
				this.registerClassInstance(varName);
				return;
				
			}
			
			Console.WriteLine("Registering variable "+varName+" (a type of \""+this.varType+"\"), memAddress: "+memAddress.ToString("X"));
			
			if (this.nameExists(varName))
				throw new ParsingError("The name \""+varName+"\" is already in use");
		
			this.tryIncreaseBlockVarCount();
			
			//when classes are a thing, make sure they are accounted for here
			//if (class) -> appendAfter.addRange ... class or struct size.. because, the pointers are 4 bytes, but the actual struct could and probably is greater or different than 4bytes
			if (this.blocks.Count==0) {//not local var
				this.defineTimeOrder.Add(varName);
				this.variables.Add(varName,new Tuple<UInt32,String>(memAddress+(UInt32)appendAfter.Count,this.varType));
				this.appendAfterIndex.Add(varName,(UInt32)appendAfter.Count);
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
			
			Console.WriteLine("is local: "+this.referencedVariableIsLocal.ToString()+", var name: "+this.referencedVariable+", referenced var type: "+this.referencedVarType.ToString());
			String type;
			if (this.referencedVariableIsFromClass)
				type=getClassFromInstanceName(this.lastReferencedClassInstance).getVarType(this.referencedVariable).Item1;//Create function getClassFromInstanceName that works with local vars and non local vars
			else if (!(this.referencedVariableIsLocal))
				type=(this.referencedVarType==VarType.NATIVE_ARRAY||this.referencedVarType==VarType.NATIVE_ARRAY_INDEXER)?this.arrays[this.referencedVariable].Item2:(this.referencedVarType==VarType.CLASS?this.classes[this.referencedVariable].Item2:this.variables[this.referencedVariable].Item2);
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
						this.appendAfterIndex.Add(referencedVariable,(UInt32)appendAfter.Count);
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
				UInt32 size=this.keywordMgr.getVarTypeByteSize(varType);
				this.addBytes(size==1?new Byte[]{0x88,0x10}://MOV DWORD [EAX],EDX
				              size==2?new Byte[]{0x66,0x89,0x10}:
				              /*size==4*/new Byte[]{0x89,0x10}
				             );
				
			}
			else if (this.referencedVarType==VarType.NATIVE_VARIABLE&&referencedVariableIsFromClass) {
				
				this.moveClassItemAddrIntoEax(this.lastReferencedClassInstance,this.referencedVariable,this.referencedVarType);
				
				UInt32 size=keywordMgr.getVarTypeByteSize(varType);
				
				this.addByte(0x50);//PUSH EAX
				Tuple<String,VarType> tpl=this.pushValue(value);
				this.tryConvertVars(new Tuple<String,VarType>(type,this.referencedVarType),tpl);
				this.addByte(0x5A);//POP EDX
				this.addByte(0x58);//POP EAX
				//EDX has VALUE, EAX has PTR
				
				if (tpl.Item1==KWBoolean.constName&&type!=KWBoolean.constName)
					throw new ParsingError("You can only apply \""+KWBoolean.constTrue+"\" and +\""+KWBoolean.constFalse+"\" to boolean variables");
				
				if (type!=KWString.constName&&tpl.Item1==KWString.constName)
					throw new ParsingError("Can't convert \""+tpl.Item1+"\" to a string (\""+KWString.constName+"\").");
				
				if (type==KWString.constName&&tpl.Item1!=KWString.constName)
					throw new ParsingError("Can't convert a string (\""+KWString.constName+"\") to \""+tpl.Item1+"\".");
					
				if (type==KWByte.constName||type==KWBoolean.constName) {
				
					this.addBytes(new Byte[]{
					              	
					              	0x88,0x10 //MOV BYTE[EAX],DL
					              	
					              });
						
				}
					
				
				else if (type==KWShort.constName) {
				
					this.addBytes(new Byte[]{
					              	
					              	0x66,0x89,0x10 //MOV [EAX],EDX
												   //NOTICE:: this fucks up the stack and should be changed
					              	
					              });
					
				}
				
				else if (type==KWInteger.constName||type==KWString.constName) {
				
					this.addBytes(new Byte[]{
					              	
					              	0x89,0x10 //MOV DWORD [EAX],EDX
					              	
					              });
				
				}
				
			}
			else if (this.referencedVarType==VarType.CLASS&&this.referencedVariableIsFromClass) {
				
				this.moveClassItemAddrIntoEax(this.lastReferencedClassInstance,this.referencedVariable,this.referencedVarType);
				
				this.addByte(0x50);//PUSH EAX
				Tuple<String,VarType> tpl=this.pushValue(value);
				this.tryConvertVars(new Tuple<String,VarType>(type,this.referencedVarType),tpl);
				this.addByte(0x5A);//POP EDX
				this.addByte(0x58);//POP EAX
				
				this.addBytes(new Byte[]{
					              	0x89,0x10 //MOV DWORD [EAX],EDX
					              });
				
			}
			
			else if (this.referencedVarType==VarType.CLASS) {
				
				Tuple<String,VarType> tpl=this.pushValue(value);
				if (this.referencedVariableIsLocal) {
					if (this.getLocalVarHomeBlock(this.referencedVariable)!=this.getCurrentBlock())
						this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{0x8F,0x45,this.pseudoStack.getVarEbpOffset(this.referencedVariable)}); //POP [EBP+-OFFSET]
				}
				else {
					
					if (addEsiToLocalAddresses)
						this.addBytes(new Byte[]{0x8F,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[this.referencedVariable]))); //POP DWORD [MEM ADDR+ESI]
					else {
						this.classReferences[this.referencedVariable].Add((UInt32)(this.opcodes.Count+2));
						this.addBytes(new Byte[]{0x8F,5,0,0,0,0}); //POP DWORD [MEM ADDR]
					}
					
				}
				
			}
			
			
			else {
				
//				Console.WriteLine("Did not make an array");
				Tuple<String,VarType> tpl=this.pushValue(value);
				this.tryConvertVars(new Tuple<String,VarType>(type,this.referencedVarType),tpl);
				
				if (tpl.Item1==KWBoolean.constName&&type!=KWBoolean.constName)
					throw new ParsingError("You can only apply \""+KWBoolean.constTrue+"\" and \""+KWBoolean.constFalse+"\" to boolean variables");
				
				if (type!=KWString.constName&&tpl.Item1==KWString.constName)
					throw new ParsingError("Can't convert \""+tpl.Item1+"\" to a string (\""+KWString.constName+"\").");
				
				if (type==KWString.constName&&tpl.Item1!=KWString.constName)
					throw new ParsingError("Can't convert a string (\""+KWString.constName+"\") to \""+tpl.Item1+"\".");
				
				if (this.referencedVariableIsLocal) {
					if (this.getLocalVarHomeBlock(this.referencedVariable)!=this.getCurrentBlock())
						this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{0x8F,0x45,this.pseudoStack.getVarEbpOffset(this.referencedVariable)}); //POP [EBP+-OFFSET]
				}
				else {
					
					if (type==KWByte.constName||type==KWBoolean.constName) {
					
						if (addEsiToLocalAddresses) {
							
							this.addBytes(new Byte[]{
							              	
							              	0x31,0xDB, //XOR EBX,EBX
							              	0x58, //POP EAX
							              	0x88,0xC3, //MOV BL,AL
							              	0x88,0x9E}.Concat(BitConverter.GetBytes(this.appendAfterIndex[this.referencedVariable]))); //MOV BYTE[PTR+ESI],BL
							
						}
						else {
							this.variableReferences[this.referencedVariable].Add((UInt32)this.opcodes.Count+7);
							this.addBytes(new Byte[]{
							              	
							              	0x31,0xDB, //XOR EBX,EBX
							              	0x58, //POP EAX
							              	0x88,0xC3, //MOV BL,AL
							              	0x88,0x1D,0,0,0,0 //MOV BYTE[PTR],BL
							              	
							              });
						}
							
					}
						
					
					else if (type==KWShort.constName) {
					
						if (this.addEsiToLocalAddresses) {
							
							this.addBytes(new Byte[]{
						              	
						              	0x66,0x8F,0x86 }.Concat(BitConverter.GetBytes((UInt32)(appendAfter.Count-2)))); //POP WORD [PTR+ESI]
															   //NOTICE:: this fucks up the stack and should be changed
						              	
						             
							
						}
						else {
							this.variableReferences[this.referencedVariable].Add((UInt32)this.opcodes.Count+3);
							this.addBytes(new Byte[]{
							              	
							              	0x66,0x8F,5,0,0,0,0 //POP WORD [PTR]
																//NOTICE:: this fucks up the stack and should be changed
							              	
							              });
						}
						
					}
					
					else if (type==KWInteger.constName||type==KWString.constName) {
						
						if (this.addEsiToLocalAddresses)
							this.addBytes(new Byte[]{0x8F,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[this.referencedVariable]))); //POP DWORD [PTR+ESI]																								   //SUBTRACT 4 FROM APPENDAFTER COUNT BECAUSE THAT'S BYTESIZE OF VAR
						else {
							this.variableReferences[this.referencedVariable].Add((UInt32)this.opcodes.Count+2);
							this.addBytes(new Byte[]{
							              	
							              	0x8F,5,0,0,0,0 //POP DWORD [PTR]
							              	
							              });
						}
					
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
			
			foreach (KeyValuePair<String,List<UInt32>>references in this.classReferences) {
				
				foreach (UInt32 index in references.Value) {
				
					
					Byte[]memAddrBytes=BitConverter.GetBytes(this.classes[references.Key].Item1);
					
					Byte i=0;
					while (i!=4) {
						this.opcodes[(Int32)index+i]=memAddrBytes[i];
						++i;
					}
				}
				
			}
			
			foreach (KeyValuePair<Class,List<UInt32>>references in this.staticClassReferences) {
				
				foreach (UInt32 index in references.Value) {
				
					Byte[]memAddrBytes=BitConverter.GetBytes(references.Key.memAddr);
					
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
				this.defineTimeOrder.Add(arrayName);
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
				
				Console.WriteLine(funcMemAddr.Item1);
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
		
		internal void setProcessHeapVar () {
			
			const String GPH="GetProcessHeap";
			
			if (blocks.Count!=0) {
				
				//TODO:: insert set process heap var at start of application
				
				return;
				
			}
			
			this.referenceDll(Parser.KERNEL32,GPH);
			this.referencedFuncPositions[GPH].Add((UInt32)(this.opcodes.Count+2));
			this.processHeapVar=new Tuple<UInt32,List<UInt32>>((addEsiToLocalAddresses?0:this.memAddress)+(UInt32)this.appendAfter.Count,new List<UInt32>((addEsiToLocalAddresses?new UInt32[0]:new UInt32[]{(UInt32)(opcodes.Count+7)})));
			this.appendAfter.AddRange(new Byte[4]);
			
			if (addEsiToLocalAddresses) {
				
				this.addBytes(new Byte[]{
				              	
				              	0xFF,0x15, //CALL FUNC
				              	0,0,0,0, //MEM ADDR TO GetProcessHeap
				              	
				              	0x89,0x86 //MOV EAX TO [PTR + ESI]
				              }.Concat(BitConverter.GetBytes((UInt32)(appendAfter.Count-4))));
				
			}
			
			else {
				
				this.addBytes(new Byte[]{
				              	
				              	0xFF,0x15, //CALL FUNC
				              	0,0,0,0, //MEM ADDR TO GetProcessHeap
				              	
				              	0xA3, //MOV EAX TO
				              	0,0,0,0 //MEM ADDR TO processHeapVar
				              	
				              });
				
			}
			
		}
		
		internal void pushProcessHeapVar () {
			
			if (processHeapVar==null)
				setProcessHeapVar();
			
			if (addEsiToLocalAddresses)
				this.addBytes(new Byte[]{0xFF,0xB6}.Concat(BitConverter.GetBytes((UInt32)this.processHeapVar.Item1)));
			else {
				this.processHeapVar.Item2.Add((UInt32)(this.opcodes.Count+2));
				this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0});
			}
			
		}
		
		/// <summary>
		/// First push byte index and value
		/// this works for 4 byte member arrays
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
			this.lastReferencedVariableIsFromClass=false;
			
		}
		
		
		internal Boolean isArrayIndexer (String str) { return str.Contains('[')&&str.Where(x=>this.beginsArrayIndexer(x)).Count()==str.Where(x=>this.endsArrayIndexer(x)).Count()&&!(str[0]=='(')&&!str.Substring(str.LastIndexOf(']')+1).Any(x=>this.isMathOperator(x));; }
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
				this.isALocalVar(name)           ||
				name==Parser.NULL_STR            ||
				this.containsImportedClass(name);
			
		}
		
		/// <summary>
		/// Pushes the memory address address to an index in an array
		/// </summary>
		/// <param name="arrName">name of array</param>
		/// <param name="indexer">value inside first set of square brackets (not including first and last square brackets)</param>
		/// <returns>new status to update</returns>
		private ParsingStatus indexArray (String arrName,String indexer,Boolean recursing=false) {
			
			Console.WriteLine("Indexing array: \""+arrName+"\", indexer: \""+indexer+'"');
			
			Boolean containsMathOps=indexer.Any(x=>this.isMathOperator(x));
			//push the mem addr..
			if ((containsMathOps&&this.isArrayIndexer(indexer))||this.tryCheckIfValidArrayIndexer(indexer)) {
					
				String sub=null,arrName0=null,indexer0=null,slack=null;
				
				if (containsMathOps) {
					
					Console.WriteLine(" ====== containsMathOps =======");
					
					
					this.parseMath(indexer,delegate(String str) {
					               	
					               	if (this.isArrayIndexer(str)) { 
										Int32 idx=str.IndexOf('[')+1;
										sub=str.Substring(idx,(str.LastIndexOf(']')+1)-idx);
										arrName0=str.Split('[')[0];
										indexer0=(sub.EndsWith("]"))?String.Concat(sub.Take(sub.Length-1)):sub;
										slack=str.Substring(str.LastIndexOf(']')+1);
										Console.WriteLine("pushValue: \""+str+'"');
										Console.WriteLine("arrName0: \""+arrName0+'"');
										Console.WriteLine("indexer0: \""+indexer0+'"');
										Console.WriteLine("sub: \""+sub+'"');
										Console.WriteLine("slack: \""+slack+'"');
										this.indexArray(arrName0,indexer0);
										
										this.addBytes(
											(keywordMgr.getVarTypeByteSize(this.arrays[arrName0].Item2)==1) ?
												 new Byte[]{
												 	0x31,0xDB, //XOR EBX,EBX   
												 	0x88,0x1F, //MOV BYTE [EDI],BL
													0x53 //PUSH EBX
												 }
											: (keywordMgr.getVarTypeByteSize(this.arrays[arrName0].Item2)==2) ?
											   new Byte[] {
											   		0x5F,          //POP EDI
													0x66,0xFF,0x37 //PUSH WORD [EDI]
											   }
											:
											new Byte[] { 
												0x5F,     //POP EDI
												0xFF,0x37 //PUSH DWORD [EDI]
											}
										);
									}
									else this.pushValue(str);
					               	
					               },null);
					
					this.addBytes(new Byte[]{
							              	
					              			0x58,//POP EAX
							              	0x51,//PUSH ECX
							                0xB9}.Concat(BitConverter.GetBytes(keywordMgr.getVarTypeByteSize(this.arrays[arrName].Item2))).Concat(new Byte[]{//MOV ECX,DWORD...
							                                                                                                                 	0xF7,0xE1,//MUL ECX
							                                                                                                                 	0x59,//POP ECX
							                                                                                                                     }));
					this.addBytes(new Byte[]{0x83,0xC0,8});//ADD EAX,8
					this.arrayReferences[arrName].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{3,5,0,0,0,0});//ADD EAX,VALUE @ PTR
					
					this.addByte(0x50);//PUSH EAX
					
					Console.WriteLine(" ====== =============== =======");
					
				}
				else {
					
					sub=indexer.Substring(indexer.IndexOf('[')+1);
					arrName0=indexer.Split('[')[0].Replace(" ","");
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
					
					this.addByte(0x92);//XCHG EAX,EDX
					this.addBytes(new Byte[]{0xB9}.Concat(BitConverter.GetBytes(keywordMgr.getVarTypeByteSize((this.isALocalVar(arrName)?this.getLocalVarHomeBlock(arrName).localVariables[arrName].Item1.Item1:this.arrays[arrName].Item2))))); // MOV ECX,BYTE SIZE OF ARR MEMBER
					this.addByte(0x52);//PUSH EDX
					this.addBytes(new Byte[]{0xF7,0xE1});//MUL ECX
					this.addByte(0x5A);//POP EDX
					this.addByte(0x92);//XCHG EAX,EDX
					
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
			
			Boolean gettingAddr=value.StartsWith("$");
			if (gettingAddr)
				value=value.Substring(1);
			
			//constants:
			UInt32 _value;
			Int32 _value0;
			if (UInt32.TryParse(value,out _value)) {
				
				this.throwIfAddr(gettingAddr,value);
				
				String rv;
				
				if (_value<=SByte.MaxValue) {
					
					if (@struct)
						this.appendAfter[(Int32)(this.variables[this.referencedVariable].Item1)]=(Byte)(_value);
					else
						this.addBytes(new Byte[]{0x6A,(Byte)(_value)});//PUSH BYTE _value
					return new Tuple<String,VarType>(KWByte.constName,VarType.NATIVE_VARIABLE);
					
				}
				else if (_value<=UInt16.MaxValue)
					rv=KWShort.constName;
				else rv=KWInteger.constName;
				
				//Words & Dwords use the same push opcode
				
				if (@struct) {
					
					UInt32 sz=keywordMgr.getVarTypeByteSize(rv),i=0;
					Byte[]bytes=BitConverter.GetBytes(_value);
					while (i!=sz) {
						
						this.appendAfter[(Int32)(i+this.variables[this.referencedVariable].Item1)]=bytes[i];
						++i;
						
					}
					
				}
				else
					this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes(_value)));
				
				return new Tuple<String,VarType>(rv,VarType.NATIVE_VARIABLE);
				
			}
			else if (Int32.TryParse(value,out _value0)) {
				
				//TODO:: SIGNED VARIABLES
				this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes(_value0)));
				return new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
				
			}
			else if (value==KWBoolean.constFalse) {
				
				this.throwIfAddr(gettingAddr,value);
				
				if (!@struct)
					this.addBytes(new Byte[]{0x6A,0}); //PUSH 0
				return new Tuple<String,VarType>(KWBoolean.constName,VarType.NATIVE_VARIABLE);
				
			}
			else if (value==KWBoolean.constTrue) {
				
				this.throwIfAddr(gettingAddr,value);
				
				const Byte constTrueValue=1;
				if (@struct)
					this.appendAfter[(Int32)(this.variables[this.referencedVariable].Item1)]=constTrueValue;
				else this.addBytes(new Byte[]{0x6A,constTrueValue}); //PUSH 1
				return new Tuple<String,VarType>(KWBoolean.constName,VarType.NATIVE_VARIABLE);
				
			}
			else if (value==Parser.NULL_STR) {
				
				this.throwIfAddr(gettingAddr,value);
				
				if (!@struct)
					this.addBytes(new Byte[]{0x6A,0}); // PUSH 0
				return new Tuple<String,VarType>(Parser.NULL_STR,VarType.NONE);
				
			}
			else if (@struct)
				throw new ParsingError("Can only apply constant values to a struct");
			else if (this.variables.ContainsKey(value)) {
				
				if (gettingAddr) {
					
					if (addEsiToLocalAddresses) {
						
						this.addBytes(new Byte[]{0x8D,0x86,}.Concat(BitConverter.GetBytes(this.appendAfterIndex[value]))); //LEA EAX,DWORD [ESI+OFFSET]
						this.addByte(0x50);//PUSH EAX
						
					}
					else {
						this.variableReferences[value].Add((UInt32)this.opcodes.Count+1);
						this.addBytes(new Byte[]{0x68,0,0,0,0});
					}
					return new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
					
				}
				
				UInt32 byteSize=keywordMgr.getVarTypeByteSize(this.variables[value].Item2);
				if (byteSize==1) {
					
					this.addBytes(new Byte[]{0x31,0xDB}); //XOR EBX,EBX
					if (addEsiToLocalAddresses) {
						this.addBytes(new Byte[]{0x8A,0x9E}.Concat(BitConverter.GetBytes(this.appendAfterIndex[value]))); //MOV BL,[PTR+ESI]
					}
					else {
						this.variableReferences[value].Add((UInt32)this.opcodes.Count+2);
						this.addBytes(new Byte[]{0x8A,0x1D,0,0,0,0}); //MOV BL,[PTR]
					}
					this.addByte(0x53); //PUSH EBX
					return new Tuple<String,VarType>(this.variables[value].Item2,VarType.NATIVE_VARIABLE);
					
				}
				else if (byteSize==2) {
					
					this.variableReferences[value].Add((UInt32)this.opcodes.Count+3);
					this.addBytes(new Byte[]{0x66,0xFF,0x35,0,0,0,0}); //PUSH WORD [PTR]
					return new Tuple<String,VarType>(KWShort.constName,VarType.NATIVE_VARIABLE);
					
				}
				else /*byteSize==4*/ {
					
					if (addEsiToLocalAddresses)
						this.addBytes(new Byte[]{0xFF,0xB6}.Concat(BitConverter.GetBytes(appendAfterIndex[value]))); //PUSH DWORD [PTR+ESI]
					else {
						
						this.variableReferences[value].Add((UInt32)this.opcodes.Count+2);
						this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0}); //PUSH DWORD [PTR]
					
					}
					return new Tuple<String,VarType>(this.variables[value].Item2,VarType.NATIVE_VARIABLE);
					
				}
				
			}
			else if (this.arrays.ContainsKey(value)) {
				
				if (gettingAddr) {
					
					this.arrayReferences[value].Add((UInt32)this.opcodes.Count+1);
					this.addBytes(new Byte[]{0x68,0,0,0,0}); //PUSH DWORD
					return new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
					
				}
				this.arrayReferences[value].Add((UInt32)this.opcodes.Count+2);
				this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0}); //PUSH DWORD [PTR]
				return new Tuple<String,VarType>(this.arrays[value].Item2,VarType.NATIVE_ARRAY);
				
			}
			else if (this.isFuncWithParams(value)) {
				
				this.throwIfAddr(gettingAddr,value);
				
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
					else if (c==','&&roundBracketBalance==1) {
						
						@params.Add(paramBuilder.ToString());
						paramBuilder.Clear();
						
					}
					
					if (roundBracketBalance==0) {
						@params.Add(paramBuilder.ToString());
						break;
					}
					else if (!(c==','&&roundBracketBalance==1)) paramBuilder.Append(c);
					
				}
				Console.WriteLine("unparsedParams: \""+unparsedParams+'"');
				this.callFunction(funcName,@params.ToArray());
				this.addByte(0x50); //PUSH EAX
				return this.functions[funcName].Item2;
				
			}
			else if (functions.ContainsKey(value)) {
				
				if (gettingAddr) {
					
					if (addEsiToLocalAddresses) {
						this.int32sToSubtractByFinalOpcodesCount.Add((UInt32)this.opcodes.Count+2);
						this.addBytes(new Byte[]{0x8D,0x86}.Concat(BitConverter.GetBytes(this.functions[value].Item1)));
						this.addByte(0x50);//PUSH EAX
					}
					else
						this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes((UInt32)(this.functions[value].Item1))));
					return new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
					
				}
				
				String funcName=value.Split('(')[0];
				
				if (functions[funcName].Item2==null)
					throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained");
				
				this.callFunction(funcName,new String[0]);
				this.addByte(0x50); //PUSH EAX
				return this.functions[funcName].Item2;
				
			}
			else if (this.keywordMgr.getKeywords().Where(x=>x.type==KeywordType.NATIVE_CALL_WITH_RETURN_VALUE).Select(x=>x.name).Contains(value)) {
				
				this.throwIfAddr(gettingAddr,value);
				
				Keyword[] query=this.keywordMgr.getKeywords().Where(x=>x.type==KeywordType.NATIVE_CALL_WITH_RETURN_VALUE&&x.name==value).ToArray();
				if (query.Length==0)
					throw new ParsingError("Function \""+value+"\" has no return value, therefore its return value can't be obtained");
				Keyword kw=query.First();
				this.execKeyword(kw,new String[0]);
				return kw.outputType;
				
			}
			else if (value.Contains('(')&&this.keywordMgr.getKeywords().Where(x=>x.type==KeywordType.NATIVE_CALL_WITH_RETURN_VALUE).Select(x=>x.name).Contains(value.Split('(')[0])&&value.Contains(')')&&!value.Substring(value.LastIndexOf(')')+1).Any(x=>this.isMathOperator(x))) {
				
				this.throwIfAddr(gettingAddr,value);
				
				String funcName=value.Split('(')[0];
				
				Keyword[] query=this.keywordMgr.getKeywords().Where(x=>x.type==KeywordType.NATIVE_CALL_WITH_RETURN_VALUE&&x.name==funcName).ToArray();
				if (query.Length==0)
					throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained");
				Keyword kw=query.First();
				
				String unparsedParams=value.Substring(value.IndexOf('(')+1);
				Byte roundBracketBalance=1;
				List<String>@params=new List<String>();
				StringBuilder paramBuilder=new StringBuilder();
				//HACK:: sub parsing
				foreach (Char c in unparsedParams) {
					
					if (c=='(') ++roundBracketBalance;
					else if (c==')') --roundBracketBalance;
					else if (c==','&&roundBracketBalance==1) {
						
						@params.Add(paramBuilder.ToString());
						paramBuilder.Clear();
						
					}
					
					if (roundBracketBalance==0) {
						@params.Add(paramBuilder.ToString());
						break;
					}
					else if (!(c==','&&roundBracketBalance==1)) paramBuilder.Append(c);
					
				}
				Console.WriteLine("unparsedParams: \""+unparsedParams+'"');
				this.execKeyword(kw,@params.ToArray());
				this.addByte(0x50); //PUSH EAX
				return kw.outputType;
				
			}
			else if (this.isALocalVar(value)) {
				
				Block localVarHomeBlock=this.getLocalVarHomeBlock(value);
				
				if (localVarHomeBlock!=this.getCurrentBlock())
					this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
				if (gettingAddr) {
					this.addBytes(new Byte[]{0x8D,0x45,pseudoStack.getVarEbpOffset(value)}); //LEA EAX,[EBP+-OFFSET]
					this.addByte(0x50); //PUSH EAX
				}
				else
					this.addBytes(new Byte[]{0xFF,0x75,pseudoStack.getVarEbpOffset(value)});
				return localVarHomeBlock.localVariables[value].Item1;
				
			}
			else if (this.classes.ContainsKey(value)) {
				
				if (gettingAddr) {
						
					if (addEsiToLocalAddresses) {
						
						this.addBytes(new Byte[]{0x8D,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[value]))); //LEA EAX,[PTR+ESI]
						this.addByte(0x50); //PUSH EAX
						
					}
					else {
						this.classReferences[value].Add((UInt32)this.opcodes.Count+1);
						this.addBytes(new Byte[]{0x68,0,0,0,0}); //PUSH DWORD
					}
					return new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
					
				}
				if (addEsiToLocalAddresses)
					this.addBytes(new Byte[]{0xFF,0xB6}.Concat(BitConverter.GetBytes(this.appendAfterIndex[value]))); //PUSH DWORD [PTR+ESI]
				else {
					this.classReferences[value].Add((UInt32)this.opcodes.Count+2);
					this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0}); //PUSH DWORD [PTR]
				}
				return new Tuple<String,VarType>(this.classes[value].Item2,VarType.CLASS);
				
			}
			else if (value.StartsWith("\"")&&value.EndsWith("\"")) {
				
				this.throwIfAddr(gettingAddr,value);
				
				String innerText=value.Substring(1,value.Length-2);
				Byte[]chars=new Byte[innerText.Length+1];//+1 = Null Byte
				UInt16 i=0;
				foreach (Byte ch in innerText.Select(x=>(Byte)x)) {
					
					chars[i]=ch;
					
					++i;
					
				}
				
				if (addEsiToLocalAddresses) {
					
					this.addBytes(new Byte[]{0x8D,0x86,}.Concat(BitConverter.GetBytes((UInt32)(this.appendAfter.Count)))); //LEA EAX,DWORD [ESI+OFFSET]
					this.addByte(0x50);//PUSH EAX
					this.appendAfter.AddRange(chars);
					
				}
				else {
				
					this.stringsAndRefs.Add(new Tuple<UInt32,List<UInt32>>((UInt32)(this.memAddress+this.appendAfter.Count),new List<UInt32>(new UInt32[]{(UInt32)(this.opcodes.Count+1)})));
					this.appendAfter.AddRange(chars);
					this.addBytes(new Byte[]{0x68,0,0,0,0}); //PUSH DWORD
				
				}
				
				return new Tuple<String,VarType>(KWString.constName,VarType.NATIVE_VARIABLE);
				
			}
			else if (value.Any(x=>this.isMathOperator(x))) {
				
				Console.WriteLine(" ! value: "+value);
				
				//NOTE:: math is also parsed & calculated at Parser#indexArray and Parser#pushArrValue
				
				if (gettingAddr)
					value='$'+value;
				
				return this.parseMath(value,delegate(String str){ if (this.isArrayIndexer(str))this.pushArrValue(str); else this.pushValue(str); },pushValue=>(this.isArrayIndexer(pushValue))?this.pushArrValue(pushValue):this.pushValue(pushValue));
				
			}
			
			else if (this.tryCheckIfValidArrayIndexer(value)) { 
				
				return this.pushArrValue(value,gettingAddr);
				
			}
			
			else if (value.Any(x=>this.accessingClass(x))&&(this.classes.ContainsKey(value.Split(Parser.accessorChar).First())||(this.isALocalVar(value.Split(Parser.accessorChar).First())))) {
				
				String[]accessors=value.Split(Parser.accessorChar);
				String first=accessors.First();
				Class initialClass;
				this.writeStrOpcodes("Test");
					
				if (this.isALocalVar(first)) {
					
					if (this.getLocalVarHomeBlock(first).localVariables[first].Item1.Item2!=VarType.CLASS)
						throw new ParsingError("Not a class, can't read member of: \""+first+'"');
					
					initialClass=this.importedClasses.Where(x=>x.className==this.getLocalVarHomeBlock(first).localVariables[first].Item1.Item1).First();
					
				}
				else initialClass=this.classes[first].Item3;
				
				String pValue=value.Substring(first.Length+1);
				UInt32 endChar=0;
				Boolean inQuotes=false;
				foreach (Char c in pValue) {
					
					if (c=='"')inQuotes=!inQuotes;
					
					if (!inQuotes&&this.accessingClass(c)) break;
					
					++endChar;
					
				}
				pValue=pValue.Substring(0,(Int32)endChar);
				
				if (this.isALocalVar(first)) {
				
					if (this.getLocalVarHomeBlock(first)!=this.getCurrentBlock())
						this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{0x8B,0x45,this.pseudoStack.getVarEbpOffset(first)}); //MOV [EBP+-OFFSET],EAX
					
				}
				else {
					
					if (addEsiToLocalAddresses)
						this.addBytes(new Byte[]{0x8B,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[first])));//MOV EAX,DWORD[PTR+ESI]
					else  {
						this.addByte(0xA1);//MOV DWORD[FOLLOWING PTR],EAX
						this.classReferences[first].Add(this.getOpcodesCount());
						this.addBytes(new Byte[]{0,0,0,0});
					}
					
				}
				
				if (/* TODO:: Accessing a class that is further accessing another class item, i.e myClass.otherClass.otherClassItem */false) {
					
					//Can't use accessors because strings may contain '.', maybe substring from start index endChar to end of string and parse the result
					throw new Exception("Unimplemented");
					
				}
				else if (initialClass.variables.ContainsKey(pValue)) {
					
					this.addByte(5);//ADD EAX,FOLLOWING DWORD
					this.addBytes(BitConverter.GetBytes(initialClass.variables[pValue].Item1+initialClass.opcodePortionByteSize)); //DWORD HERE
					
					this.addBytes(new Byte[]{0x8B,0xF8}); //MOV EDI,EAX
					UInt32 sz=this.keywordMgr.getVarTypeByteSize(initialClass.variables[pValue].Item2);
					if (gettingAddr)
						this.addByte(0x57);//PUSH EDI
					else
						this.addBytes(sz==1?
						              new Byte[]{0x31,0xC0, //XOR EAX,EAX
						              	0x8A,7, //MOV AL,[EDI]
						              	0x50}: //PUSH EAX
						              sz==2?
						              new Byte[]{0x31,0xC0, //XOR EAX,EAX
						              	0x66,0x8B,7, //MOV AX,[EDI]
						              	0x50}: //PUSH EAX
						              new Byte[]{0xFF,0x37}/*PUSH DWORD[EDI]*/);
					
					UInt32 size=keywordMgr.getVarTypeByteSize(varType);
					return new Tuple<String,VarType>(initialClass.variables[pValue].Item2,VarType.NATIVE_VARIABLE);
					
				}
				else if (initialClass.classes.ContainsKey(pValue)) {
					
					this.addByte(5);//ADD EAX,FOLLOWING DWORD
					this.addBytes(BitConverter.GetBytes(initialClass.classes[pValue].Item1+initialClass.opcodePortionByteSize)); //DWORD HERE
				
					this.addBytes(new Byte[]{0x8B,0xF8}); //MOV EDI,EAX
					UInt32 sz=this.keywordMgr.getVarTypeByteSize(initialClass.classes[pValue].Item2);
					if (gettingAddr) {
						this.addByte(0x57);//PUSH EDI
						return new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
					}
					this.addBytes(new Byte[]{0xFF,0x37}/*PUSH DWORD[EDI]*/);
					
					UInt32 size=keywordMgr.getVarTypeByteSize(varType);
					return new Tuple<String,VarType>(initialClass.classes[pValue].Item2,VarType.CLASS);
					
				}
				else if (initialClass.functions.ContainsKey(pValue)) {
					
					if (gettingAddr) {
					
						this.addByte(5);//ADD EAX,FOLLOWING DWORD
						this.addBytes(BitConverter.GetBytes(initialClass.functions[pValue].Item1)); //DWORD HERE
						return new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
						
					}
					Tuple<String,VarType>retType=initialClass.functions[pValue].Item2;
					if (retType==null)
						throw new ParsingError("Function \""+pValue+"\" has no return value, therefore its return value can't be obtained");
					
					this.callClassFunc(first,pValue,new String[0],true);
					this.addByte(0x50);//PUSH EAX
					
					return retType;
					
				}
				else if (this.isFuncWithParams(pValue)) {
				
					this.throwIfAddr(gettingAddr,value);
					
					//UNDONE:: this whole block is undoned
					
					String funcName=pValue.Split('(')[0];
					
					if (functions[funcName].Item2==null)
						throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained");
					
					String unparsedParams=pValue.Substring(pValue.IndexOf('(')+1);
					Byte roundBracketBalance=1;
					List<String>@params=new List<String>();
					StringBuilder paramBuilder=new StringBuilder();
					//HACK:: sub parsing
					foreach (Char c in unparsedParams) {
						
						if (c=='(') ++roundBracketBalance;
						else if (c==')') --roundBracketBalance;
						else if (c==','&&roundBracketBalance==1) {
							
							@params.Add(paramBuilder.ToString());
							paramBuilder.Clear();
							
						}
						
						if (roundBracketBalance==0) {
							@params.Add(paramBuilder.ToString());
							break;
						}
						else if (!(c==','&&roundBracketBalance==1)) paramBuilder.Append(c);
						
					}
					Console.WriteLine("unparsedParams: \""+unparsedParams+'"');
					this.callFunction(funcName,@params.ToArray());
					this.addByte(0x50); //PUSH EAX
					return this.functions[funcName].Item2;
					
				}
				else throw new ParsingError("Item \""+value+"\" is not accessible and may not exist from \""+initialClass.className+'"');
				
			}
			
			else throw new ParsingError("Invalid value: \""+value+'"');
			
		}
		
		private void freeHeaps () {
			
			return;
						
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
			Console.WriteLine("Mem addr @ start of onBlockClosed: "+this.memAddress.ToString());
			
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
			foreach (Tuple<UInt32,UInt32>RVAindex in block.blockRVAPositions) {
				
				memAddr=BitConverter.GetBytes((Int32)memAddress-(Int32)RVAindex.Item2);
				
				i=0;
				
				while (i!=4) {
					
					opcodes[(Int32)RVAindex.Item1+i]=memAddr[i];
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
			
			Console.WriteLine("Mem addr @ end of onBlockClosed: "+this.memAddress.ToString());
			
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
		
		internal void callFunction (String functionName,String[]@params) {
				
			if (!(this.functions.ContainsKey(functionName)))
				throw new ParsingError("Function does not exist: \""+functionName+'"');
			
			Boolean restoreEsiCondition=functions[functionName].Item4==FunctionType.DLL_REFERENCED&&addEsiToLocalAddresses&&inFunction;
			
			Console.WriteLine(" == callFunction: "+functionName+" == ");
			foreach (String str in @params)
				Console.WriteLine(str);
			Console.WriteLine(" == total params: "+@params.Length.ToString()+ " == ");
			
			
			foreach (String s in @params) {
				
				Console.WriteLine(" P- "+s);
				
			}
			
			if (this.functions[functionName].Item3!=@params.Length)
				throw new ParsingError("Expected \""+this.functions[functionName].Item3.ToString()+"\" parameters for \""+functionName+"\", got \""+@params.Length+'"');
			
			if (restoreEsiCondition) {
				pseudoStack.push(new EsiPtr());
				this.addByte(0x56);//PUSH ESI
			}
			
			if (this.functionParamTypes[functionName].Count!=0) {
				UInt16 i=(UInt16)(this.functionParamTypes[functionName].Count-1);
				foreach (String str in @params.Reverse()) {
					
					this.tryConvertVars(this.functionParamTypes[functionName][i],this.pushValue(str));
					if (i==0) break;
					--i;
					
				}
			}
			
			if (restoreEsiCondition) {
				this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
				this.addBytes(new Byte[]{0x8B,0x75,this.pseudoStack.getLatestEsiOffset()}); //MOV ESI,[EBP+-OFFSET]
			}
			
			this.addBytes((this.functions[functionName].Item4==FunctionType.SUNSET)?new Byte[]{0xE8,0,0,0,0}:new Byte[]{0xFF,0x15,0,0,0,0}); //CALL Mem Addr
			if (this.functions[functionName].Item4==FunctionType.SUNSET)
				this.functionReferences[functionName].Add(new Tuple<UInt32,UInt32>((UInt32)opcodes.Count-4,this.memAddress));
			else
				this.referencedFuncPositions[functionName].Add((UInt32)opcodes.Count-4);
			if (this.functions[functionName].Item5==CallingConvention.Cdecl)
				this.addBytes(new Byte[]{0x81,0xC4}.Concat(BitConverter.GetBytes((UInt32)this.functions[functionName].Item3*4)));
			if (restoreEsiCondition) {
				this.addByte(0x5E);//POP ESI
				pseudoStack.pop();
			}
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
					
					if (this.functions[funcName].Item4==FunctionType.SUNSET)
						memAddr=BitConverter.GetBytes(((Int32)this.functions[funcName].Item1-(Int32)tpl.Item2));
					else 
						memAddr=BitConverter.GetBytes(this.functions[funcName].Item1);
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
			if (this.containsImportedClass(value)) {
				
				return new Tuple<String,VarType>(value,VarType.CLASS);
				
			}
			
			throw new ParsingError("Not a var type: \""+value+'"');
				
			
		}
		
		/// <summary>
		/// Sets new parsing status
		/// </summary>
		private void exec (Executor executor,String[]@params) {
			
			if (!(String.IsNullOrEmpty(executor.func)))
				this.callFunction(executor.func,@params);
			else if (executor.kw!=null)
				this.execKeyword(executor.kw,@params);
			else if (executor.classFunc!=null)
				this.callClassFunc(executor.classFunc,@params);
			
		}
		
		private void tryConvertVars (Tuple<String,VarType>to,Tuple<String,VarType>from) {
			
			//HACK:: check var type here
			if (from.Item2==VarType.NATIVE_ARRAY_INDEXER||from.Item2==VarType.NATIVE_VARIABLE) {
				if (keywordMgr.getVarTypeByteSize(to.Item1)<keywordMgr.getVarTypeByteSize(from.Item1))
					throw new ParsingError("Can't convert \""+to.Item1+"\" to \""+from.Item1+'"');
			}
			else if (from.Item2==VarType.NATIVE_VARIABLE&&to.Item2==VarType.NATIVE_VARIABLE&&(from.Item1==KWString.constName||to.Item1==KWString.constName)&&from.Item1!=to.Item1)
				throw new ParsingError("Can't convert \""+from.Item1+"\" to \""+to.Item1+'"');
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
		
		internal Block getLocalVarHomeBlock (String value) {
			
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
		
		private void registerClassInstance (String varName) {
			
			Console.WriteLine("Registering class "+varName+" (a type of \""+this.varType+"\"), memAddress: "+memAddress.ToString("X"));
			
			if (this.nameExists(varName))
				throw new ParsingError("The name \""+varName+"\" is already in use");
		
			this.tryIncreaseBlockVarCount();
			
			if (this.blocks.Count==0) {//not local var
				this.defineTimeOrder.Add(varName);
				this.classes.Add(varName,new Tuple<UInt32,String,Class>(memAddress+(UInt32)appendAfter.Count,this.varType,this.importedClasses.Where(x=>x.className==this.varType).First()));
				this.appendAfterIndex.Add(varName,(UInt32)appendAfter.Count);
				this.appendAfter.AddRange(new Byte[4]);
				this.classReferences.Add(varName,new List<UInt32>());
			}
			else {//should be local var
				this.pseudoStack.push(new LocalVar(varName));
				this.getCurrentBlock().localVariables.Add(varName,new Tuple<Tuple<String,VarType>>(new Tuple<String,VarType>(this.varType,VarType.CLASS)));
				this.lastReferencedVariableIsLocal=true;
				foreach (UInt32 index in this.localVarEBPPositionsToOffset.Where(x=>x.Key.nestedLevel>this.getCurrentBlock().nestedLevel).SelectMany(x=>x.Value))
					this.offsetEBP(4,index);
			}
			this.lastReferencedVariable=varName;
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void tryIncreaseBlockVarCount () {
			
			Block b;
			if (this.tryGetCurrentBlock(out b))
				++this.blockVariablesCount[b];
			
		}
		
		internal Block getCurrentBlock () { return this.blocks.Last().Key; }
		
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
		
		internal void writeJump (UInt32 gotoMemAddr) {
			
			this.addBytes(new Byte[]{0xE9}.Concat(BitConverter.GetBytes((Int32)gotoMemAddr-(Int32)(this.memAddress+5))));
			
		}
		
		internal void writeStrOpcodes (String str) {
			
			return;
			
			#if DEBUG
			
			if (str.Length>=SByte.MaxValue)
				str=str.Substring(0,SByte.MaxValue);
			
			UInt32 pos=(UInt32)this.opcodes.Count+1;
			this.addBytes(new Byte[]{0xEB,0});//JMP SHORT (SByte)
			this.addByte(0x90); //NOP
			this.addBytes(Encoding.ASCII.GetBytes(str));
			this.addByte(0); // STRING NULL TERMINATOR
			this.addByte(0x90); //NOP
			this.opcodes[(Int32)pos]=(Byte)(3+str.Length);
			
			#endif
			
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
		
		private Boolean indicatesComment (Char c) {
			
			return c==';';
			
		}
		
		private Boolean isNewlineOrReturn (Char c) { 
			
			return c=='\n'||c=='\r'; 
			
		}
		
		private Boolean isMathOperator (Char c) {
			
			foreach (Char c0 in this.mathOperators)
				if (c0==c)
					return true;
			
			return false;
			
		}
		
		private Boolean isAddition (Char c) {
			
			return c=='+';
			
		}
		
		private Boolean isSubtraction (Char c) {
			
			return c=='-';
			
		}
		
		/// <summary>
		/// probably shouldn't be used regularly, it was a broad-use function named 'split' at first but eventually had to become a specific parsing function when order of operations was introduced
		/// </summary>
		/// <param name="shouldAppendFunc">can be null</param>
		private String[]parseSplit(String str,Char[]splitChars,Char[]incBalanceChars,Char[]decBalanceChars,out Char[]splitChars0) {
			
			StringBuilder sb=new StringBuilder();
			List<String>splitStrings=new List<String>();
			List<Char>splitChars1=new List<Char>();
			UInt16 balance=0;
			Char previousCharacter=(Char)(0);
			List<UInt16>doAppendBalances=new List<UInt16>();
			
			//HACK:: sub parsing
			foreach (Char c in str) {
				
				if (balance==0) {
					foreach (Char c0 in splitChars) {
						if (c==c0) {
							
							Console.WriteLine("In splitchars");
							splitStrings.Add(sb.ToString());
							sb.Clear();
							splitChars1.Add(c);
							goto skip;
									
						}
					}
				}
				
				if (balance>1||(balance>0&&incBalanceChars.Contains(c))) {
					
					sb.Append(c);
					if (incBalanceChars.Contains(c))
						++balance;
					else if (decBalanceChars.Contains(c))
						--balance;
					previousCharacter=c;
					continue;
					
				}
				
				if (c!='('&&c!=')')
					sb.Append(c);
				else if (c=='(') {
					if (!incBalanceChars.Contains(previousCharacter)&&!this.isMathOperator(previousCharacter)&&(Char.IsLetter(previousCharacter)||Char.IsDigit(previousCharacter))) {
						
						sb.Append(c);
						doAppendBalances.Add(balance);
					}
					
				}
				
				skip:
				if (incBalanceChars.Contains(c))
					++balance;
				else if (decBalanceChars.Contains(c))
					--balance;
				
				if (c==')') {
					
					if (doAppendBalances.Contains(balance)) {
						
						doAppendBalances.Remove(balance);
						sb.Append(c);
						
					}
					
				}
				
				previousCharacter=c;
				Console.WriteLine("Looping char \""+c+"\", balance: "+balance.ToString()+", sb: "+sb.ToString());
				
			}
			
			splitStrings.Add(sb.ToString());
			
			splitChars0=splitChars1.ToArray();
			return splitStrings.ToArray();
			
		}
		
		private Tuple<String,VarType> pushArrValue (String value,Boolean gettingAddr=false) {
			
			this.writeStrOpcodes("PAV S");
			
			Console.WriteLine("------------------------------------------------------------");
			Console.WriteLine("Value: "+value.ToString());
				
			Console.WriteLine(value);
			
			Int32 idx=value.IndexOf('[')+1;
			String sub=value.Substring(idx,(value.LastIndexOf(']')+1)-idx),
			   arrName=value.Split('[')[0],
			   slack=value.Substring(value.LastIndexOf(']')+1);
			   
			Console.WriteLine("sub: "+sub+", arrName: "+arrName+", slack: "+slack);
			Console.WriteLine("------------------------------------------------------------");
			this.indexArray(arrName,(sub.EndsWith("]"))?String.Concat(sub.Take(sub.Length-1)):sub);
			
			UInt32 varTypeByteSize=(this.isALocalVar(arrName))?this.keywordMgr.getVarTypeByteSize(this.getLocalVarHomeBlock(arrName).localVariables[arrName].Item1.Item1):this.keywordMgr.getVarTypeByteSize(this.arrays[arrName].Item2);
			if (!gettingAddr)
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
			
			
			if (!(String.IsNullOrEmpty(slack))) {
				
				if (slack.Any(x=>this.isMathOperator(x))) {
					
					this.parseMath(value,delegate(String str){ if (this.isArrayIndexer(str))this.pushArrValue(str); else this.pushValue(str); },pushValue=>(this.isArrayIndexer(pushValue))?this.pushArrValue(pushValue):this.pushValue(pushValue),true);
				
					/*
					Char[]splitChars;
					String[]innerValues=this.parseSplit(slack,this.mathOperators,new Char[]{'[','('},new Char[]{']',')'},out splitChars);
					UInt16 ctr=0;
					
					String fi=innerValues.First();
					if (!(String.IsNullOrEmpty(fi)))
						throw new ParsingError("Unexpected: "+fi);
					
					//First value already pushed
					
					foreach (String s in innerValues.Skip(1)) {
						
						Console.WriteLine(s);
					
						Char op=splitChars[ctr];
						++ctr;
						String pushValue=s.Replace(" ","");
						if (this.isArrayIndexer(pushValue))  this.pushArrValue(pushValue);
						else this.pushValue(pushValue);
						this.addByte(0x58); //POP EAX
						if (this.isAddition(op))
							this.addBytes(new Byte[]{1,4,0x24}); //ADD [ESP],EAX
						else if (this.isSubtraction(op))
							this.addBytes(new Byte[]{0x29,4,0x24}); //SUB [ESP],EAX
						else if (this.isMultiplication(op))
							this.addBytes(new Byte[]{
							              	0xF7,0x24,0x24, //MUL [ESP]
							              	0x89,4,0x24     //MOV [ESP],EAX
							              });
						else if (this.isDivision(op)||this.isModulus(op))
							this.addBytes(new Byte[]{
							              	0x31,0xD2, // XOR EDX,EDX
							              	0x87,4,0x24, //XCHG [ESP],EAX
							              	0xF7,0x34,0x24, //DIV [ESP+4]
							              	0x89,(Byte)((this.isDivision(op))?4:0x14),0x24 // MOV [ESP],EAX || MOV [ESP],EDX
							              });
						else
							throw new ParsingError("Unexpected math operator \""+op+"\" (?!)");
						
					}
					*/
					
				}
				else
					throw new ParsingError("Unexpected: \""+slack+'"');
				
			}
			
			this.writeStrOpcodes("PAV E");
			
			return new Tuple<String,VarType>((varTypeByteSize==4)?KWInteger.constName:(varTypeByteSize==2)?KWShort.constName:KWByte.constName,VarType.NATIVE_ARRAY_INDEXER);
			
		}
		
		private Tuple<String,VarType> parseMath (String value,Action<String>processPushValue,Func<String,Tuple<String,VarType>>getRetTypeFromPushValue,Boolean skipFirstValue=false) {
			
			Char[]splitChars;
			String[]innerValues=this.parseSplit(value,this.mathOperators,new Char[]{'[','('},new Char[]{']',')'},out splitChars);
			UInt16 ctr=0;//splitChars[ctr] should point to the math operator after the current iteration innerValue in the foreach loops
			List<OrderItem>orderItems=new List<OrderItem>();
			Order order=new Order();
			String pushValue;
			
			foreach (String s in innerValues) {
				
				pushValue=s.Replace(" ","");
				
				Char op;
				if (splitChars.Length>ctr)
					op=splitChars[ctr];
				else {
					try { op=splitChars.Last(); }
					catch { op=(Char)0; }
				}
				
				if (this.isAddition(op)||this.isSubtraction(op)) {
					
					processPushValue(s);//this.pushValue(s);
					orderItems.Add(new OrderItem(){unparsedValue=pushValue});
					if (orderItems.Count==2) order.addOperation(orderItems,(this.isAddition(splitChars[ctr-1]))?OrderMathType.ADDITION:OrderMathType.SUBTRACTION);
					
				}
				else break;
				
				++ctr;
				
			}
			
			innerValues=innerValues.Skip(ctr).ToArray();
			Tuple<String,VarType>retType=null;
			
			if (skipFirstValue)
				goto afterFirstValue;
			
			#region .
			#if DEBUG
			UInt16 dbg_storedCtr=ctr;
			foreach (String s in innerValues) {
				
				//"6*4/2+3*6-4"
				
				if (ctr>=splitChars.Length) Console.WriteLine(s);
				else {
					Console.Write(s+splitChars[ctr]);
					++ctr;
				}
				
			}
			Console.WriteLine("Inner values:");
			foreach (String s in innerValues)
				Console.WriteLine(" - "+s);
//				order.dumpData(true);
			ctr=dbg_storedCtr;
			#endif
			#endregion
			if (innerValues.Length==0)
				goto writeOrder;
			pushValue=innerValues.First();
			if (getRetTypeFromPushValue==null) {
				retType=null;
				processPushValue(pushValue);
			}
			else retType=getRetTypeFromPushValue(pushValue);//(this.isArrayIndexer(pushValue))?this.pushArrValue(pushValue):this.pushValue(pushValue);
			orderItems.Add(new OrderItem(){unparsedValue=pushValue});
			if (orderItems.Count==2)
				order.addOperation(orderItems,(this.isAddition(splitChars[ctr-1]))?OrderMathType.ADDITION:OrderMathType.SUBTRACTION);
			
			afterFirstValue:
			
			foreach (String s in innerValues.Skip(1)) {
				
//					Console.WriteLine("Looping innervalue: \""+s+'"');
				
				pushValue=s.Replace(" ","");
				Char op=(Char)0;
				if (splitChars.Length>ctr)
					op=splitChars[ctr];
				else
					op=splitChars.Last();
				++ctr;
				
				Console.WriteLine("Looping innervalue: \""+s+"\", op: '"+op+'\'');
				
				if (this.isSubtraction(op)||this.isAddition(op)) {
					
					Console.WriteLine("SUB/AD: \""+s+"\",orderItems: "+orderItems.Count.ToString()+", OrderMathType: "+((this.isSubtraction(op))?OrderMathType.SUBTRACTION:OrderMathType.ADDITION).ToString());
					if (orderItems.Count==0) {
						
						orderItems.Add(new OrderItem(){unparsedValue=pushValue});
						processPushValue(pushValue);//this.pushValue(pushValue);
						
					}
					else /*orderItems.Count==1*/ {
						
						orderItems.Add(new OrderItem(){unparsedValue=pushValue});
						order.addOperation(orderItems,(this.isSubtraction(op))?OrderMathType.SUBTRACTION:OrderMathType.ADDITION);
						processPushValue(pushValue);//this.pushValue(pushValue);
						
					}
					continue;
					
				}
				
				processPushValue(pushValue);
				/*
				Console.WriteLine("Pushing: "+pushValue);
				if (this.isArrayIndexer(pushValue)) this.pushArrValue(pushValue);
				else this.pushValue(pushValue);
				*/
				
				this.addByte(0x58); //POP EAX
				if (this.isMultiplication(op))
					this.addBytes(new Byte[]{
					              	0xF7,0x24,0x24, //MUL [ESP]
					              	0x89,4,0x24     //MOV [ESP],EAX
					              });
				else if (this.isDivision(op)||this.isModulus(op))
					this.addBytes(new Byte[]{
					              	0x31,0xD2, // XOR EDX,EDX
					              	0x87,4,0x24, //XCHG [ESP],EAX
					              	0xF7,0x34,0x24, //DIV [ESP+4]
					              	0x89,(Byte)((this.isDivision(op))?4:0x14),0x24 // MOV [ESP],EAX || MOV [ESP],EDX
					              });
				else
					throw new ParsingError("Unexpected math operator \""+op+"\" (?!)");
				
			}
//				order.dumpData(true);
			writeOrder:
			order.writeBytes(this);
			
			if (retType==null)
				retType=new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
			
			return retType;
			
		}
		
		private UInt16 modRbbrv (Char c,Boolean inDoubleQuotes,UInt16 rbbrv) {
			
			if (!inDoubleQuotes&&this.beginsParameters(c))
				return ++rbbrv;
			else if (!inDoubleQuotes&&this.endsParameters(c))
				return --rbbrv;
			else
				return rbbrv;
			
		}
		
		internal void addBlockToAppendAfter (IEnumerable<Byte> block) {
			
			this.appendAfter.AddRange(block);
			
		}
		
		private void increaseRefdFuncsToIncreaseWithOpcodes () {
			
			foreach (KeyValuePair<String,List<Int32>>kvp in this.refdFuncsToIncreaseWithOpcodes)
				foreach (Int32 i in kvp.Value)
					++this.referencedFuncPositions[kvp.Key][i];
			
		}
		
		internal Int32 getAppendAfterCount () {
			
			return this.appendAfter.Count; 
			
		}
		
		internal Boolean containsImportedClass (String name) {
			
			return this.importedClasses.Select(x=>x.className).Contains(name);
			
		}
		
		internal Dictionary<String,Tuple<UInt32,String>>getVariables () {
			
			return this.variables;
			
		}
		
		internal Dictionary<String,Tuple<UInt32,String,Class>>getClasses () {
			
			return this.classes;
			
		}
		
		#if DEBUG
		
		internal void debugLine (String str) {
			
			Console.WriteLine(str);
			Console.ReadKey(true);
			
		}
		
		#endif
		
		internal Boolean isFuncWithParams (String value) {
			
			return value.Contains('(')&&functions.ContainsKey(value.Split('(')[0])&&value.Contains(')')&&!value.Substring(value.LastIndexOf(')')+1).Any(x=>this.isMathOperator(x))&&!this.hasClassAccessorOutsideParentheses(value);
			
		}
		
		internal Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention>>getFunctions () {
			
			return this.functions;
			
		}
		
		private Boolean hasClassAccessorOutsideParentheses (String str) {
			
			Boolean inQ=false;
			UInt16 parenthesesBalance=0;
			foreach (Char c in str) {
				
				if (c=='"') inQ=!inQ;
				else {
					
					if (!inQ&&c==')') {
						
						if (parenthesesBalance==0)
							throw new ParsingError("Unbalanced parentheses in \""+str+'"');
						
						--parenthesesBalance;
						
					}
					else if (c=='(')
						++parenthesesBalance;
					else if (c=='.'&&parenthesesBalance==0&&!inQ)
						return true;
					
				}
				
			}
			return false;
			
		}
		
		/// <summary>
		/// Does not throw exceptions
		/// </summary>
		private Class getClassFromInstanceName (String name) {
			
			if (this.isALocalVar(name))
				return this.importedClasses.Where(x=>x.className==this.getLocalVarHomeBlock(name).localVariables[name].Item1.Item1).First();
			else
				return this.classes[name].Item3;
			
		}
		
		private void moveClassItemAddrIntoEax (String classInstance,String item,VarType vt,Boolean classInstanceAlreadyInEax=false) {
			
			if (!classInstanceAlreadyInEax)
				this.moveClassInstanceIntoEax(classInstance);
			
			Class cl=this.getClassFromInstanceName(classInstance);
				
			this.addByte(5);//ADD EAX,FOLLOWING DWORD
			
			switch (vt) {
					
				case VarType.NATIVE_VARIABLE:
					this.addBytes(BitConverter.GetBytes(cl.variables[item].Item1+cl.opcodePortionByteSize)); //DWORD HERE
					break;
					
				case VarType.CLASS:
					this.addBytes(BitConverter.GetBytes(cl.classes[item].Item1+cl.opcodePortionByteSize)); //DWORD HERE
					break;
					
				case VarType.FUNCTION:
					this.addBytes(BitConverter.GetBytes(cl.functions[item].Item1)); //DWORD HERE
					break;
				
				default:
					throw new ParsingError("Invalid VarType (?!) ("+vt.ToString()+')');
					
			}
			
		}
		
		private void throwIfAddr (Boolean gettingAddr,String value) {
			
			if (gettingAddr)
				throw new ParsingError("Can't get address of: \""+value+'"');
			
		}
		
		internal void moveClassInstanceIntoEax (String classInstance) {
			
			if (!(this.isALocalVar(classInstance))) {
				
				if (addEsiToLocalAddresses) {
					
					this.addBytes(new Byte[]{0x8B,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[classInstance])));//MOV EAX,DWORD[PTR+ESI]
					
				}
				else {
					this.addByte(0xA1);//MOV EAX,DWORD[FOLLOWING PTR]
					this.classReferences[classInstance].Add(this.getOpcodesCount());
					this.addBytes(new Byte[]{0,0,0,0});
				}
				
			}
			else {
				
				Block localVarHomeBlock=this.getLocalVarHomeBlock(classInstance);
				if (localVarHomeBlock!=this.getCurrentBlock())
					this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
				this.addBytes(new Byte[]{0x8B,0x45,this.pseudoStack.getVarEbpOffset(this.lastReferencedClassInstance)}); //MOV [EBP+-OFFSET],EAX
				
			}
			
		}
		
		private void callClassFunc (String classInstance,String func,String[]parameters,Boolean classInstanceAlreadyInEax=false) {
			
			foreach (String s in parameters)
				Console.WriteLine("Param - \""+s+'"');
			
			Class cl=this.getClassFromInstanceName(classInstance);
			
			if (cl.functions[func].Item3!=parameters.Length)
				throw new ParsingError("Expected \""+cl.functions[func].Item3+"\" parameters for \""+func+"\", got \""+parameters.Length+'"');
			
			this.addByte(0x56);//PUSH ESI
			
			foreach (String s in parameters)
				this.pushValue(s);
			
			if (!classInstanceAlreadyInEax)
				this.moveClassInstanceIntoEax(classInstance);
			this.addBytes(new Byte[]{0x8B,0xF0}); //MOV ESI,EAX
			this.moveClassItemAddrIntoEax(classInstance,func,VarType.FUNCTION,true);
			this.addBytes(new Byte[]{0x81,0xC6}.Concat(BitConverter.GetBytes(cl.opcodePortionByteSize)));
			this.addBytes(new Byte[]{0xFF,0xD0}); //CALL EAX
			this.addByte(0x5E);//POP ESI
			
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void callClassFunc (Tuple<String,String>tpl,String[]parameters) {
			
			this.callClassFunc(tpl.Item1,tpl.Item2,parameters);
			
		}
		
		private void subtractInt32s () {
			
			foreach (UInt32 i in this.int32sToSubtractByFinalOpcodesCount) {
				
				Byte[] newNum=BitConverter.GetBytes(BitConverter.ToInt32(new Byte[]{opcodes[(Int32)i],opcodes[(Int32)i+1],opcodes[(Int32)i+2],opcodes[(Int32)i+3]},0)-memAddress);
				Byte i0=0;
				while (i0!=4) {
					this.opcodes[(Int32)(i+i0)]=newNum[i0];
					++i0;
				}
				
			}
			
		}
		
		internal void tryCreateRestoreEsiFunc () {
		
			if (addEsiToLocalAddresses&&restoreEsiFuncAddr==0&&opcodes.Count!=0&&!@struct) {
				
				// Create restore esi function
				this.addBytes(new Byte[]{0x8D,0x44,0x24,4});//LEA EAX,[ESP+4]
				esiFuncVarIndex=(UInt32)(this.opcodes.Count+2);
				this.addBytes(new Byte[]{0x89,0x86,0,0,0,0});//MOV [ESI+-DWORD],EAX
				this.addBytes(new Byte[]{0xEB,7});//JMP 7 BYTES
				restoreEsiFuncAddr=memAddress;
				this.addBytes(new Byte[]{0x8B,0x35,0,0,0,0});//MOV ESI,[PTR]
				this.addByte(0xC3);//RETN
				
			}
			
		}
		
		private void setEsiFuncVar () {
			
			if (!addEsiToLocalAddresses||@struct||restoreEsiFuncAddr==0)
				return;
			
			Byte[] bytes=BitConverter.GetBytes(-(opcodes.Count-restoreEsiFuncAddr)+2);
			Byte i=0;
			while (i!=4) {
				
				this.opcodes[(Int32)(i+esiFuncVarIndex)]=bytes[(Int32)i];
				++i;
				
			}
			
		}
		
		private void fillEsiFuncReferences () {
			
			foreach (UInt32 index in this.esiFuncReferences) {
				
				Byte[] bytes=BitConverter.GetBytes(((Int32)restoreEsiFuncAddr)-BitConverter.ToInt32(new Byte[]{opcodes[(Int32)index],opcodes[(Int32)index+1],opcodes[(Int32)index+2],opcodes[(Int32)index+3]},0)-5);
				Byte i=0;
				while (i!=4) {
					
					this.opcodes[(Int32)(i+index)]=bytes[(Int32)i];
					++i;
					
				}
				
			}
			
		}
		
		#region Character parsing helpers
		
		private Boolean isMultiplication (Char c) {
			
			return c=='*';
			
		}
		
		private Boolean isCallingConventionIdentifier (Char c) {
			
			return c==':';
			
		}
		
		private Boolean isDivision (Char c) {
			
			return c=='/';
			
		}
		
		private Boolean isModulus (Char c) {
			
			return c=='%';
			
		}
		
		private Boolean isUnderscore (Char c) {
			
			return c=='_';
			
		}
		
		private Boolean accessingClass (Char c) {
			
			return c=='.';
			
		}
		
		#endregion
		
	}
	
}
