/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/29/2021
 * Time: 9:09 PM
 * 
 */
using System;
using System.Collections.Generic;
using System.Text;
using Sunset.Keywords;
using System.Linq;
using Sunset.Stack;
using System.Runtime.InteropServices;
using Sunset.VarTypes;

namespace Sunset {
	
	public class Parser {

		public const String NULL_STR="null",THIS_STR="this",PTR_STR="PTR",FUNC_PTR_STR="FUNCPTR";
         public readonly static Tuple<String,VarType>PTR=new Tuple<String,VarType>(PTR_STR,VarType.NATIVE_VARIABLE),FUNC_PTR=new Tuple<String,VarType>(FUNC_PTR_STR,VarType.NATIVE_VARIABLE);

	     public static Dictionary<String,Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,Modifier,Class>>>staticInstances=new Dictionary<String,Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,Modifier,Class>>>();
         public static Dictionary<String,Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>>> staticFunctions=new Dictionary<String,Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>>>();//Class ID, (Function Name,(Memory Address,(Return Type, Return Var Type),No. of expected parameters,Function Type,Calling Convention,Modifiers))
         public static Dictionary<String,UInt32>classSkeletons=new Dictionary<String,UInt32>(); // The default copy of a class with nothing modified. (Class ID, Index in dataSectBytes)
         public static Dictionary<String,Class>classByIDs=new Dictionary<String,Class>(); // ID,Class

         public static List<Byte>dataSectBytes=new List<Byte>();

         internal static UInt32 instanceID=0;
         public static UInt32 processHeapVar=UInt32.MaxValue;//Mem Addr, References

		public readonly String parserName;
		
		public String lastReferencedVariable {
			
			private set;
			get;
			
		} 
         public readonly String fileName;
		public Boolean winApp {
			
			get;
			internal set;
			
		}
		public String referencedVariable,rTypeDefinition;
		public Boolean referencedVariableIsLocal,referencedVariableIsFromClass,referencedVariableIsStatic;

         public UInt32 byteCountBeforeDataSect;
         public SunsetProject attatchedProject;
         public Boolean hasAttatchedProject;
		
		internal Boolean lastReferencedVariableIsLocal,lastReferencedVariableIsFromClass,lastReferencedVariableIsStatic;
		
		internal VarType lastReferencedVarType=VarType.NONE,referencedVarType=VarType.NONE;
		internal String varType,lastDataToParse;
		internal Dictionary<String,List<UInt32>> variableReferences=new Dictionary<String,List<UInt32>>(),//Name,(Index in the Opcodes List)
												 arrayReferences=new Dictionary<String,List<UInt32>>(),//Name,(Index in the Opcodes List)
												 classReferences=new Dictionary<String,List<UInt32>>();//Name,(Index in the Opcodes List)
		internal Dictionary<Class,List<OpcodeIndexReference>> staticClassReferences=new Dictionary<Class,List<OpcodeIndexReference>>();//Name,(Index in the Opcodes List)
		
		internal Dictionary<String,List<String>> toImport;//DllName,Functions
         internal List<OpcodeIndexReference>procHeapVarRefs=new List<OpcodeIndexReference>();
		
		internal Dictionary<Block,UInt32> blocks;//block,end of block mem address
		internal Dictionary<Block,UInt16> blockBracketBalances;//block,bracket balance
		internal Dictionary<Block,UInt16> blockVariablesCount;//Block,# Of Variables Defined
		internal Dictionary<Block,List<Block>> blocksClosed;//Block,Blocks that were closed inside that block
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
		
		internal Boolean expectsBlock=false,expectsElse=false,searchingFunctionReturnType=false,inFunction=false,@struct=false,inConstructor=false;
		internal Byte setExpectsElse=0,setExpectsBlock=0;
		
		internal Dictionary<String,Function> functions;//Function Name,(Memory Address,(Return Type, Return Var Type),No. of expected parameters,Function Type,Calling Convention,Modifiers,Instance ID)
		internal Tuple<UInt32,List<Tuple<String,VarType>>> constructor;//Memory Address,Func Param Types
		internal Dictionary<String,List<Tuple<UInt32,UInt32>>> functionReferences;//Function Name,(Indexes in Opcodes of the reference,Memory Address at time of Reference)
		internal UInt16 nextFunctionParamsCount;
		internal ValueTuple<String,VarType>[] nextFunctionParamTypes;
         internal Dictionary<String,List<OpcodeIndexReference>> referencedFuncPositions;//FuncName,Opcode pos
		
		internal readonly Action elseBlockClosed;
		internal KeywordType[] nextExpectedKeywordTypes=new []{KeywordType.NONE};
		
		internal List<UInt32>freeHeapsRefs,dwordsToIncByOpcodesUntilStaticFuncEnd=new List<UInt32>();
		
		internal Block lastFunctionBlock;
		
		internal readonly PseudoStack pseudoStack;
		internal readonly KeywordMgr keywordMgr;
		internal Block lastBlockClosed {
			
			get;
			private set;
							
		}
		
		internal FunctionType nextType;
		internal String nextReferencedDLL,className;
		internal Boolean gui=false,toggledGui=false;
		
		internal Boolean setToWinAppIfDllReference {
			
			get;
			private set;
							
		}
		
		internal ArrayStyle style;//TODO:: do static memory block, later do stack allocation, and also allow this to be changed outside of compiler in the code with keyword SetArrayStyle (constName: SetArrStyle, expectsParams=true)
		
		internal List<Class>importedClasses;
		internal List<String> defineTimeOrder;
		internal List<UInt32>esiFuncReferences=new List<UInt32>();
         internal List<Byte>appendAfterStaticFunc=new List<Byte>();
         internal List<Class>inheritedClasses=new List<Class>();
         
		internal Boolean addEsiToLocalAddresses=false;
		/// <summary>
		/// (esiOffsetFromStart) Only set if addEsiToLocalAddresses is true
		/// LEA [ESI-esiOffsetFromStart] = Start of Opcodes Mem Address
		/// </summary>
		internal UInt32 esiOffsetFromStart=0,compiledBytesFinalNo,lastFuncOpcodeStartIndex,lastFuncDataSectOpcodeStartIndex;
		internal Dictionary<String,UInt32> appendAfterIndex=new Dictionary<String,UInt32>();
		internal List<String>lastReferencedClassInstance=new List<String>();
		internal Char nextChar {
			get;
			private set;
		}
		internal List<Tuple<String,Tuple<String,VarType>>>passedVarTypes;
		internal Dictionary<String,Tuple<String,VarType>>acknowledgements=new Dictionary<String,Tuple<String, VarType>>();
         internal Dictionary<String,List<Tuple<UInt32,UInt32>>>labelReferences=new Dictionary<String,List<Tuple<UInt32,UInt32>>>(); // Name of label,(Opcode Index, Mem Address at exact point of Reference before the long jump opcode)
         internal Modifier currentMods=Modifier.NONE;
         internal Dictionary<String,UInt32>importedClassAppendAfterIndex=new Dictionary<String,UInt32>();//Imported Class (not class instance), start index in appendAfter
		internal Dictionary<String,Function> inhFuncsToDefine;//Function Name,(Memory Address,(Return Type, Return Var Type),No. of expected parameters,Function Type,Calling Convention,Modifiers)
		internal Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>> inhVarsToDefine=new Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>>();//Name,(Mem Address,Var Type,Modifiers,)
		internal Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>> inhArrsToDefine=new Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>>();//Name,(Mem Address,Var Type,Modifiers,Instance ID)
		internal Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>> inhClassesToDefine=new Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>>();//Name,(Ptr To Mem Address of Heap Handle,Class type name,Class type),Modifier,Instance ID
       	internal UInt32 inhTblAppendAfterIndex,tableAddrIndex;
       	internal Int32 charCtr=-1;
         internal List<UInt32>instanceTable=new List<UInt32>((Int32)instanceID);
         //test esi,esi
         //jnz 11
         // mov esi, dword
         // mov edx, dword
         // retn
         internal Byte[]tableFuncBytes=new Byte[]{0x85,0xF6,0x75,11,0xBE,0,0,0,0,0xBA,0,0,0,0,0xC3};
         
		private List<Byte> opcodes=new List<Byte>(),importOpcodes=null,finalBytes=new List<Byte>(),appendAfter=new List<Byte>();
		private ParsingStatus status;
		private Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>> variables=new Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>>();//Name,(Mem Address,Var Type,Modifiers,Instance ID)
		private Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>> arrays=new Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>>();//Name,(Ptr To Mem Address of Heap Handle(Dynamic) or Mem Block(Static),Array Var Type,ArrayStyle(Dynamic or Static),Modifiers),Instance ID
		private Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>> classes=new Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>>();//Name,(Ptr To Mem Address of Heap Handle,Class type name,Class type),Modifier,Instance ID
		private List<UInt32>int32sToSubtractByFinalOpcodesCount=new List<UInt32>();
		private List<String>pvClassInstanceOrigin;
         private Dictionary<String,UInt32>labels=new Dictionary<String,UInt32>();//Name, Mem Address
         private Dictionary<String,Tuple<UInt32,Tuple<String,VarType>>>constants=new Dictionary<String,Tuple<UInt32,Tuple<String,VarType>>>();//var name,(constant value,(Generic Var Type Tuple))
         private List<UInt32>dwordsToIncByOpcodes=new List<UInt32>();

		private List<Tuple<UInt32,List<UInt32>>>stringsAndRefs; //(Mem Addr,List of References by Opcode Index),Note: Currently the Inner list of Opcode Indexes will only have a length of 1 (6/19/2021 5:19PM)
		private Dictionary<String,UInt32> setArrayValueFuncPtrs;
		private Executor waitingToExecute;
		private UInt16 squareBracketBalance=0,roundBracketBalance=0,nestedLevel=0;
		private Int16 sharpbb=0;
		private UInt32 freeHeapsMemAddr,esiFuncVarIndex=0;
		private const UInt32 tableAddrIndexConst=5,appendAfterIndexConst=10;
		private Boolean attemptingClassAccess=false,gettingClassItem=false,clearNextPvOrigin=false;
         private String constantBeingSet=null,ID;
		
		private const String KERNEL32="KERNEL32.DLL";
		private readonly Char[] mathOperators=new []{'+','-','*','/','%'};
		private readonly Boolean skipHdr,fillToNextPage,writeImportSection;
		private const Char accessorChar='.';
		private readonly UInt32 startingMemAddr;
		
		public Parser (String name,String fileName,Boolean winApp=true,Boolean setToWinAppIfDllReference=false,Boolean skipHdr=false,Boolean fillToNextPage=true,Boolean writeImportSection=true) {
			
			memAddress=winApp?0x00401000:(UInt32)0;
			startingMemAddr=memAddress;
			keywordMgr=new KeywordMgr(this);
			style=winApp?ArrayStyle.DYNAMIC_MEMORY_HEAP:ArrayStyle.STATIC_MEMORY_BLOCK;
			this.winApp=winApp;
			toImport=new Dictionary<String,List<String>>();
			this.referencedFuncPositions=new Dictionary<String,List<OpcodeIndexReference>>();
			this.setArrayValueFuncPtrs=new Dictionary<String,UInt32>();
			this.blocks=new Dictionary<Block,UInt32>();
			this.blockBracketBalances=new Dictionary<Block,UInt16>();
			elseBlockClosed=delegate {
				
				Byte i;
				
				foreach (OpcodeIndexReference @ref in lastBlockClosed.pairedBlock.blockMemPositions) {
					
                    Int32 index=@ref.GetIndexAsInt();
					Int32 prevNum=(@ref.type==OpcodeIndexType.CODE_SECT_REFERENCE?BitConverter.ToInt32(new []{opcodes[index],opcodes[index+1],opcodes[index+2],opcodes[index+3]},0):BitConverter.ToInt32(new []{dataSectBytes[index],dataSectBytes[index+1],dataSectBytes[index+2],dataSectBytes[index+3]},0));
					Byte[]newNum=BitConverter.GetBytes(prevNum+5);//5 because the opcodes are 0xE9,Dword.... (See KWElse -> JMP TO MEM ADDR)
					
					Console.WriteLine("Updating index: "+index+" (Calculated Mem Addr: "+(0x00401000+index).ToString("X")+") to "+(prevNum+5));
					
					i=0;
					while (i!=4) {
						
                        SetStaticInclusiveByte(@ref,newNum[i],i);
						++i;
						
					}
					
				}
				
			};
			functions=new Dictionary<String,Function>();
			inhFuncsToDefine=new Dictionary<String,Function>();
			functionReferences=new Dictionary<String,List<Tuple<UInt32,UInt32>>>();
			freeHeapsRefs=new List<UInt32>();
			enterPositions=new Dictionary<Block,UInt32>();
			blockVariablesCount=new Dictionary<Block,UInt16>();
			blockAddrBeforeAppendingReferences=new Dictionary<Block,List<Tuple<UInt32,Int16>>>();
			pseudoStack=new PseudoStack();
			localVarEBPPositionsToOffset=new Dictionary<Block,List<UInt32>>();
			stringsAndRefs=new List<Tuple<UInt32,List<UInt32>>>();
			this.setToWinAppIfDllReference=setToWinAppIfDllReference;
			this.skipHdr=skipHdr;
			this.parserName=name;
			this.fillToNextPage=fillToNextPage;
			this.writeImportSection=writeImportSection;
			importedClasses=new List<Class>();
			staticClassReferences=new Dictionary<Class,List<OpcodeIndexReference>>();
			defineTimeOrder=new List<String>();
			pvClassInstanceOrigin=new List<String>();
			blocksClosed=new Dictionary<Block,List<Block>>();
            acknowledgements.Add(PTR_STR,new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE));
            acknowledgements.Add(FUNC_PTR_STR,new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE));
            keywordMgr.acknowledgements.Add(PTR_STR,KWInteger.constName);
            keywordMgr.acknowledgements.Add(FUNC_PTR_STR,KWInteger.constName);
            this.fileName=fileName;
            instanceTable.AddRange(new UInt32[instanceTable.Capacity]);
            if (className==null) className=KWImport.GetClassName(fileName);
			ID=KWImport.CreateClassID(fileName,className);
            if (!staticInstances.ContainsKey(ID))
                staticInstances.Add(ID,new Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,Modifier,Class>>());
            if (!staticFunctions.ContainsKey(ID))
                staticFunctions.Add(ID,new Dictionary<String,Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>>());
            
			Console.WriteLine("Parser \""+parserName+"\" skipHdr:"+skipHdr.ToString());
           
		}
		
		public Byte[] parse (String data) {
			
			if (addEsiToLocalAddresses) {
				this.pseudoStack.push(new EsiPtr());
            	//TEST ESI,ESI
            	//JNZ 11
            	//MOV ESI,0
            	//MOV EDX,0
            	//RETN
            	tableAddrIndex=tableAddrIndexConst;//should be opcodes.Count + 5 if opcodes are added before this
            	this.addBytes(tableFuncBytes);
            	Parser.dataSectBytes.AddRange(new Byte[32]);
            }
			
			status=ParsingStatus.SEARCHING_NAME;
			StringBuilder nameReader=new StringBuilder();
			squareBracketBalance=0;
			roundBracketBalance=0;
			String arrName=null;
			List<String> paramsList=new List<String>();
			Int32 currentChar=0;
			Boolean inDoubleQuotes=false,readAlphaNumeric=false;
			UInt16 rbbrv=0,sharprv=0;
			
			lastDataToParse=data;
			data+=' ';
			charCtr=0;
			
			foreach (Char c in data) {
				
				Console.WriteLine(" - Checking:\""+c+"\",ParsingStatus:"+status.ToString()+",blockBracketBalance #no:"+blockBracketBalances.Count.ToString()+",rbbrv:"+rbbrv.ToString()+",attemptingClassAccess:"+attemptingClassAccess.ToString()+",gettingClassItem:"+gettingClassItem.ToString()+",lastReferencedClassInstance:"+merge(lastReferencedClassInstance,"#")+",LRV:"+lastReferencedVariable+",RV:"+referencedVariable+",File Path:"+fileName);
				
				if (setExpectsElse>0) {
					
					--setExpectsElse;
					expectsElse=true;
					
				}
				
				if (setExpectsBlock>0) {
					
					--setExpectsBlock;
					expectsBlock=true;
					
				}
				
				switchStart:
				++charCtr;
				switch (status) {

                    case ParsingStatus.SEARCHING_TYPE_DEFINITION_NAME:
					case ParsingStatus.SEARCHING_FUNCTION_NAME:
					case ParsingStatus.SEARCHING_ARRAY_NAME:
					case ParsingStatus.SEARCHING_VALUE:
					case ParsingStatus.SEARCHING_VARIABLE_NAME:
					case ParsingStatus.SEARCHING_NAME:
						if (!this.isFormOfBlankspace(c)) {
							
							if (this.indicatesComment(c))
								status=ParsingStatus.IN_COMMENT;
                            else if (this.indicatesLabel(c))
                                status=ParsingStatus.READING_LABEL;
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
							
                            if (sharprv!=0) nameReader.Append(c);
                            else {

    							String n=nameReader.ToString();
    							if (!this.pvtNull()&&this.pvtContainsKey(n))
    								n=this.pvtGet(n).Item1;
    							
    							if (searchingFunctionReturnType) {
    								
    								n+=c;
                                    String fn=this.functions.Last().Key;
                                    Function f=functions[fn];
                                    this.functions[fn]=new Function(f.memAddr,getVarType(n),f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier,f.parameterTypes,f.instanceID,f.isInherited);
    								if (this.inhFuncsToDefine.ContainsKey(fn)) {
										if ((this.inhFuncsToDefine[fn].returnType==null&&functions[fn].returnType!=null)||!this.inhFuncsToDefine[fn].returnType.Equals(this.functions[fn].returnType))
											throw new ParsingError("Expected "+(inhFuncsToDefine[fn].returnType==null?"no return type":"return type \""+inhFuncsToDefine[fn].returnType.Item1+"\" of \""+inhFuncsToDefine[fn].returnType.Item2+"\"")+ " for func \""+fn+"\".",this);
										this.inhFuncsToDefine.Remove(fn);
									}
    								if (staticFunctions[ID].ContainsKey(fn))
    									staticFunctions[ID][fn]=new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>(f.memAddr,this.getVarType(n),f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier);
                                    status=ParsingStatus.SEARCHING_NAME;
    								this.setExpectsBlock=1;
    								
    							}
    							
    							else {
    							
    								this.declareArray(n);
    								
    								this.resetLastReferencedVar();
    							
    							}
    								
    							nameReader.Clear();

                            }
							
						}
						else if (this.beginsArrayIndexer(c)) {
							
							arrName=nameReader.ToString();
							nameReader.Clear();
							++squareBracketBalance;
							status=ParsingStatus.READING_ARRAY_INDEXER;
							
						}
						else if (this.opensBlock(c)) {
							
							if (!(this.expectsBlock))
								throw new ParsingError("Got \""+c+"\" but did not expect block",this);
							
							this.updateBlockBalances(c);
							
							this.expectsBlock=false;
							
						}
						else if (this.closesBlock(c)) {
							
							if (this.blockBracketBalances.Count==0)
								throw new ParsingError("Tried to close block, when no block detected",this);
							
							this.updateBlockBalances(c);
							
						}
						else if (this.accessingClass(c)) {
							
							this.attemptingClassAccess=true;
							this.chkName(nameReader.ToString());
							nameReader.Clear();
							
							
						}
						else if (this.startsPassingTypes(c)) {
							++sharprv;
							nameReader.Append(c);
						}
						else if (this.endsPassingTypes(c)) {
							
							if (sharprv==0) throw new ParsingError("Unbalanced sharp ('<', '>') brackets",this);
							--sharprv;
							nameReader.Append(c);
							
						}
						else if (this.splitsParameters(c)&&sharprv!=0) nameReader.Append(c);
						else if (this.isValidNameChar(c)||this.refersToIncrementOrDecrement(c)) nameReader.Append(c);
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
						
						if (this.isValidNameChar(c)) nameReader.Append(c);
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
						if (this.isValidNameChar(c)) nameReader.Append(c);
						else {
							
							this.registerArray(nameReader.ToString());
							nameReader.Clear();
							
						}
						break;
						
					case ParsingStatus.READING_ARRAY_INDEXER:
						
						if (this.beginsArrayIndexer(c)) ++squareBracketBalance;
						else if (this.endsArrayIndexer(c)) --squareBracketBalance;
						
						if (squareBracketBalance==0) {
							
							if (gettingClassItem) {
								
								this.lastReferencedVariableIsLocal=this.isALocalVar(this.lastReferencedClassInstance.First());
								gettingClassItem=false;
								lastReferencedVariableIsFromClass=true;
								this.moveClassOriginItemAddrIntoEax(this.lastReferencedClassInstance,arrName,VarType.NATIVE_ARRAY,this.lastReferencedVariableIsLocal);
								this.addBytes(new Byte[]{0x8B,0});//MOV EAX,[EAX]
								status=this.indexArray(null,nameReader.ToString(),0,true,this.keywordMgr.getVarTypeByteSize(this.getOriginFinalClass(this.lastReferencedClassInstance,this.lastReferencedVariableIsLocal).arrays[arrName].Item2));
								
							}
							else {
								this.lastReferencedVariableIsLocal=this.isALocalVar(arrName);
								status=this.indexArray(arrName,nameReader.ToString());
							}
							nameReader.Clear();
							this.lastReferencedVariable=arrName;
							arrName=null;
							this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.DECREMENT,KeywordType.INCREMENT};
							this.lastReferencedVarType=VarType.NATIVE_ARRAY_INDEXER;
							
							
						}
						else nameReader.Append(c);
						
						break;
						
					case ParsingStatus.SEARCHING_PARAMETERS:
						
						if (this.beginsParameters(c)) {
							
							roundBracketBalance=1;
							sharpbb=0;
							status=ParsingStatus.READING_PARAMETERS;
							
						}
						else if (!this.isFormOfBlankspace(c)) {
							this.exec(this.waitingToExecute,new String[0]);
							goto switchStart;
						}
						
						break;
					case ParsingStatus.READING_PARAMETERS:
						
						//FIXME:: check for inQuotes maybe
						
						if (this.isValidNameChar(c)) readAlphaNumeric=true;
						
						if (this.beginsParameters(c)) ++roundBracketBalance;
						
						else if (this.endsParameters(c)) --roundBracketBalance;
						
						else if (this.startsPassingTypes(c)&&readAlphaNumeric) ++sharpbb;
						
						else if (this.endsPassingTypes(c)&&readAlphaNumeric) --sharpbb;
						
						else if (this.splitsParameters(c)&&roundBracketBalance==1&&sharpbb==0) {
							
							paramsList.Add(nameReader.ToString());
							Console.WriteLine("Parameter: "+nameReader.ToString());
							nameReader.Clear();
							readAlphaNumeric=false;
						
						}
						
						if (roundBracketBalance==0) {
							
							paramsList.Add(nameReader.ToString());
							Console.WriteLine("Parameter: "+nameReader.ToString());
							nameReader.Clear();
							this.exec(this.waitingToExecute,paramsList.ToArray());
							paramsList.Clear();
							readAlphaNumeric=false;
							
						}
						
						else if (!(this.splitsParameters(c)&&roundBracketBalance==1&&sharpbb==0)) nameReader.Append(c);
						
						break;
						
					case ParsingStatus.READING_FUNCTION_NAME:
						
						if (this.isValidNameChar(c)||this.isCallingConventionIdentifier(c)) nameReader.Append(c);
						else {
							
							this.setExpectsBlock=1;
							String funcName=nameReader.ToString();
							CallingConvention cl=CallingConvention.StdCall;
							if (funcName.Any(x=>this.isCallingConventionIdentifier(x))) {
								
								if (this.nextType==FunctionType.SUNSET)
									throw new ParsingError("Did not expect calling convention for Sunset defined function (calling convention is stdcall)",this);
								else if (this.nextType==FunctionType.DLL_REFERENCED) {
									
									String[]sp=funcName.Split(':');
									if (sp.Length!=2)
										throw new ParsingError("Invalid syntax for calling convention identifier",this);
									
									funcName=sp[1];
									switch (sp[0].ToLower()) {
											
										case "stdcall":
											break;
										case "cdecl":
											cl=CallingConvention.Cdecl;
											break;
										case "fastcall":
										case "thiscall":
											throw new ParsingError("Calling convention not supported: \""+sp[0]+'"',this);
										default:
											throw new ParsingError("Invalid calling convention: \""+sp[0]+"\", did you mean \"stdcall\" or \"cdecl\"?",this);
											
									}
									
								}
								
							}
							if (this.nextType==FunctionType.DLL_REFERENCED)
								this.referenceDll(this.nextReferencedDLL,funcName);
							nameReader.Clear();
							if (functions.ContainsKey(funcName))
								throw new ParsingError("A function is already declared with the name \""+funcName+'"',this);
                            if (!currentMods.hasAccessorModifier())
                                currentMods=currentMods|Modifier.PRIVATE;
                            if (currentMods.HasFlag(Modifier.PULLABLE)||currentMods.HasFlag(Modifier.CONSTANT))
                                throw new ParsingError("Pullable and constant are not valid modifiers for functions",this);
                            if (this.inhFuncsToDefine.ContainsKey(funcName)) {
                            	Int32 i=-1;
                            	Console.WriteLine(this.inhFuncsToDefine[funcName].parameterTypes.Count.ToString()+" params vs " +nextFunctionParamTypes.Length.ToString());
                            	if (this.inhFuncsToDefine[funcName].parameterTypes.Count!=nextFunctionParamTypes.Length)
                            			throw new ParsingError("Expected func \""+funcName+"\" to have "+(this.inhFuncsToDefine[funcName].parameterTypes.Count==0?"no param types":"param types: "+merge(this.inhFuncsToDefine[funcName].parameterTypes.Select(x=>x.Item1),", "))+'.',this);
                            	Console.WriteLine(this.nextFunctionParamTypes.Length+" <- nextFunctionParamTypes.Length");
                            	while (++i!=nextFunctionParamTypes.Length) {
                            		if (inhFuncsToDefine[funcName].parameterTypes[i].Item1!=nextFunctionParamTypes[i].Item1||inhFuncsToDefine[funcName].parameterTypes[i].Item2!=nextFunctionParamTypes[i].Item2)
                            			throw new ParsingError("Expected func \""+funcName+"\" to have "+(this.inhFuncsToDefine[funcName].parameterTypes.Count==0?"no param types":"param types: "+merge(this.inhFuncsToDefine[funcName].parameterTypes.Select(x=>x.Item1),", "))+'.',this);
                            	}
                            }
                            Boolean isPrivate=currentMods.HasFlag(Modifier.PRIVATE);
                            this.functions.Add(funcName,new Function((this.nextType== FunctionType.SUNSET)?blocks.Keys.Last().startMemAddr:0,null,(UInt16)this.nextFunctionParamTypes.Length,this.nextType,cl,currentMods,new List<ValueTuple<String,VarType>>(this.nextFunctionParamTypes),(inhFuncsToDefine.ContainsKey(funcName)?inhFuncsToDefine[funcName].instanceID:instanceID),inhFuncsToDefine.ContainsKey(funcName)));
							
                        	if (currentMods.HasFlag(Modifier.STATIC)) {
                        		Function val=functions.Last().Value;
                        		staticFunctions[ID].Add(functions.Last().Key,new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>(val.memAddr,val.returnType,val.expectedParameterCount,val.functionType,val.callingConvention,val.modifier));
                        	} else if (!isPrivate) {
                        		if (inhFuncsToDefine.ContainsKey(funcName)) {
                            		Int32 idx=(Int32)this.functions.Last().Value.instanceID;
                            		instanceTable.RemoveAt(idx);
                            		instanceTable.Insert(idx,this.functions.Last().Value.memAddr);
                        		}
                            	else {
                            		++Parser.instanceID;
                            		instanceTable.Add(this.functions.Last().Value.memAddr);
                            	}
                        	}
                           this.functionReferences.Add(funcName,new List<Tuple<UInt32,UInt32>>());
						status=ParsingStatus.SEARCHING_NAME;
						if (this.nextType==FunctionType.SUNSET)
							this.nextExpectedKeywordTypes=new []{KeywordType.TYPE};
						this.searchingFunctionReturnType=true;
                            currentMods=Modifier.NONE;
							
						}
						
						break;
						
					case ParsingStatus.IN_COMMENT:
						
						if (this.isNewlineOrReturn(c))
							status=ParsingStatus.SEARCHING_NAME;
						
						break;
						
					case ParsingStatus.STOP_PARSING_IMMEDIATE:
						return this.compile();
						
					case ParsingStatus.SEARCHING_COLON:
						if (this.isFormOfBlankspace(c)) break;
						if (this.isColon(c)) {
							status=ParsingStatus.SEARCHING_NAME;
							break;
						}
						throw new ParsingError("Expected colon, got \""+c+'"',this);

                    case ParsingStatus.READING_TYPE_DEFINITION_NAME:

                        if (this.isValidNameChar(c))
                            nameReader.Append(c);
                        else if (this.isFormOfBlankspace(c)) {

                            status=ParsingStatus.SEARCHING_NAME;
                            nextExpectedKeywordTypes=new [] { KeywordType.TYPE_DEFINITION_ASSIGNEMENT };
                            rTypeDefinition=nameReader.ToString();
                            nameReader.Clear();
                            if (this.nameExists(rTypeDefinition))
                                throw new ParsingError("Can't acknowledge \""+rTypeDefinition+"\", name is in use",this);

                        }
                        else
                            throw new ParsingError("Got invalid character '"+c+"' while reading name \""+nameReader.ToString()+"\": \""+c+'"',this);


                        break;
                    case ParsingStatus.READING_LABEL:

                        if (this.isValidNameChar(c))
                            nameReader.Append(c);
                        else if (this.isFormOfBlankspace(c)) {

                            status=ParsingStatus.SEARCHING_NAME;
                            String labelName=nameReader.ToString();
                            nameReader.Clear();
                            if (this.nameExists(labelName))
                                throw new ParsingError("Can't define label \""+labelName+"\", name is in use",this);
                            if (blocks.Count!=0)
                                throw new ParsingError("Label \""+labelName+"\" cannot be defined because it is in a block",this);
                            this.labels.Add(labelName,this.memAddress);

                        }
                        else
                            throw new ParsingError("Got invalid character '"+c+"' while reading name \""+nameReader.ToString()+"\": \""+c+'"',this);

                        break;
						
				}
				
				++currentChar;
				if (data.Length>currentChar)
					nextChar=data[currentChar];
				
			}
			
			return compile();
			
		}
		
		private Byte[] compile () {
			if (inhVarsToDefine.Count!=0)
				throw new ParsingError("Missing inherited variable: \""+inhVarsToDefine.First().Key+"\" of type "+inhVarsToDefine.First().Value.Item2,this);
			if (inhArrsToDefine.Count!=0)
				throw new ParsingError("Missing inherited array: \""+inhArrsToDefine.First().Key+"\" of type "+inhArrsToDefine.First().Value.Item2,this);
			if (inhClassesToDefine.Count!=0)
				throw new ParsingError("Missing inherited class: \""+inhClassesToDefine.First().Key+"\" of type "+inhClassesToDefine.First().Value.Item2,this);
			if (inhFuncsToDefine.Count!=0)
				throw new ParsingError("Missing inherited function: \""+inhFuncsToDefine.First().Key+(inhFuncsToDefine.First().Value.parameterTypes.Count==0?"\" with no parameters":"\" with parameters "+merge(inhFuncsToDefine.First().Value.parameterTypes.Select(x=>x.Item1),", "))+" and "+(inhFuncsToDefine.First().Value.returnType==null?"no return value":"return type of "+inhFuncsToDefine.First().Value.returnType.Item1)+".",this);
			
			Console.WriteLine("Compiling: "+this.parserName);
			
			if (!@struct) {
				
				this.addBytes(new Byte[]{0x6A,0}); //PUSH 0EAX
				freeHeapsMemAddr=memAddress;
				this.freeHeaps();
				this.addByte(0x58); //POP  to set the exit code (return value) of process (HACK:: NOTICE::return value is an UNSIGNED value)
				this.addByte(0xC3); //Add RETN call to end of our exe, so no matter what happens in terms of the source, it should not be a blank application & will exit
			}
			
			inhTblAppendAfterIndex=(UInt32)appendAfter.Count;
            if (tableAddrIndex!=0) {
            	opcodes.RemoveRange((Int32)tableAddrIndex,4);
            	opcodes.InsertRange((Int32)tableAddrIndex,BitConverter.GetBytes((UInt32)(inhTblAppendAfterIndex)));
            	opcodes.RemoveRange((Int32)appendAfterIndexConst,4);
            	opcodes.InsertRange((Int32)appendAfterIndexConst,BitConverter.GetBytes((UInt32)(opcodes.Count+4)));
            }
			appendAfter.AddRange(instanceTable.SelectMany(x=>BitConverter.GetBytes(x)));
			this.setEsiFuncVar();
			this.fillEsiFuncReferences();
			opcodes.AddRange(appendAfter);
			this.updateVariableReferences();
			this.fillFunctionReferences();  
			this.fillHeapFreeReferences();
			this.fillConstantStringReferences();
			this.subtractInt32s();
            this.fillLabelReferences();
			
			if ((!(winApp))&&(this.toImport.Count!=0)) {
				
				if (this.setToWinAppIfDllReference)
					this.winApp=true;
				else
					throw new ParsingError("Can not reference DLL's on non-PE app ("+parserName+')',this);
				
			}
			
			if (winApp) {
				
				List<Tuple<String,UInt32>>funcMemAddrs=null;
				
				if (!(skipHdr)) {
					
					UInt32 importSectVirtualSize=0;
					
					if (writeImportSection) {
					
						if (this.toImport.Count>0)
							importOpcodes=this.getImportSection(out funcMemAddrs,out importSectVirtualSize);
						
						if (funcMemAddrs!=null)
							this.fillFuncMemAddrs(funcMemAddrs);
					
					}
					
					PEHeader hdr=PEHeaderFactory.newHdr(opcodes,importOpcodes,memAddress,-this.appendAfter.Count,importSectVirtualSize,this.gui);
					
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
            byteCountBeforeDataSect=(UInt32)finalBytes.Count;
            if (dataSectBytes.Count!=0)
                finalBytes.AddRange(dataSectBytes);
			
			compiledBytesFinalNo=(UInt32)finalBytes.Count;
			return finalBytes.ToArray();
			
		}
		
		internal void addByte (Byte b) {
			
			if (@struct) {
				if (referencedVariable==null)
					throw new ParsingError("A struct is limited to only variable declarations",this);
				else return;
			}
			else if (InStaticEnvironment()) {
                this.increaseDataSectDwordsByOpcodes();
                dataSectBytes.Add(b);
                return;
            }
			Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>> newDict=new Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>>(this.variables.Count);
			foreach (KeyValuePair<String,Tuple<UInt32,String,Modifier,UInt32>> kvp in this.variables) {
//				Console.WriteLine("For variable: "+kvp.Key+", updating mem address to: "+(kvp.Value.Item1+1).ToString("X"));
				newDict.Add(kvp.Key,new Tuple<UInt32,String,Modifier,UInt32>(kvp.Value.Item1+1,kvp.Value.Item2,kvp.Value.Item3,kvp.Value.Item4));
			}
			
			this.variables=new Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>>(newDict);
			
			Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>> newDict0=new Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>>(this.arrays.Count);
			foreach (KeyValuePair<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>> kvp in this.arrays) {
			
			
//					Console.WriteLine("For array: "+kvp.Key+", updating mem address to: "+(kvp.Value.Item1+1).ToString("X"));
				
				newDict0.Add(kvp.Key,new Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>(kvp.Value.Item1+1,kvp.Value.Item2,kvp.Value.Item3,kvp.Value.Item4,kvp.Value.Item5));
				
			}
			
			this.arrays=new Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>>(newDict0);
			
			Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>> newDict1=new Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>>(this.classes.Count);
			foreach (KeyValuePair<String,Tuple<UInt32,String,Class,Modifier,UInt32>> kvp in this.classes) {
				
				if (kvp.Value.Item1==0) newDict1.Add(kvp.Key,kvp.Value);
				
				else {
				
					Console.WriteLine("For class: "+kvp.Key+", updating mem address to: "+(kvp.Value.Item1+1).ToString("X"));
					
					newDict1.Add(kvp.Key,new Tuple<UInt32,String,Class,Modifier,UInt32>(kvp.Value.Item1+1,kvp.Value.Item2,kvp.Value.Item3,kvp.Value.Item4,kvp.Value.Item5));
				
				}

			}
			
			this.classes=new Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>>(newDict1);
			
			List<Tuple<UInt32,List<UInt32>>>newList=new List<Tuple<UInt32,List<UInt32>>>(this.stringsAndRefs.Count);
			foreach (Tuple<UInt32,List<UInt32>>str in this.stringsAndRefs) {
				
				newList.Add(new Tuple<UInt32,List<UInt32>>(str.Item1+1,str.Item2));
				
			}
			this.stringsAndRefs=new List<Tuple<UInt32,List<UInt32>>>(newList);

			opcodes.Add(b);
			++memAddress;
             this.increaseDwordsByOpcodes();
			
		}
		
		internal void addBytes (IEnumerable<Byte> bytes) {
			
			foreach (Byte b in bytes)
				this.addByte(b);
			
		}
		
		private void chkName (String name) {
			
			//HACK:: check var type
			
			KeywordType[] pKTs=this.nextExpectedKeywordTypes;
			Boolean wasSearchingFuncReturnType=searchingFunctionReturnType,expectsCaseOrDefault=this.blocks.Count>0&&this.blocks.Last().Key.switchBlock;
			this.nextExpectedKeywordTypes=new []{KeywordType.NONE};
			searchingFunctionReturnType=false;
			
			Console.WriteLine("Got name: \""+name+'"');
			
			if (!pKTs.Contains(KeywordType.NONE)||attemptingClassAccess||gettingClassItem||expectsCaseOrDefault)
				goto checkKeywords;
			
			if (this.variables.ContainsKey(name)) {
				
				this.lastReferencedVariable=name;
				Console.WriteLine("LRV:"+this.lastReferencedVariable);
				this.status=ParsingStatus.SEARCHING_NAME;
				this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
				this.lastReferencedVarType=VarType.NATIVE_VARIABLE;
				lastReferencedVariableIsLocal=lastReferencedVariableIsStatic=false;
				return;
				
			}
			
			if (this.arrays.ContainsKey(name)) {
				
				this.lastReferencedVariable=name;
				this.lastReferencedVarType=VarType.NATIVE_ARRAY;
				this.status=ParsingStatus.SEARCHING_NAME;
				this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
				lastReferencedVariableIsLocal=lastReferencedVariableIsStatic=false;
				return;
				
			}

              if (staticFunctions[ID].ContainsKey(name)) {
                
                lastReferencedVariableIsLocal=lastReferencedVariableIsStatic=false;
                
                if (staticFunctions[ID][name].Item3!=0) {
                    
                    status=(this.beginsParameters(this.nextChar))?ParsingStatus.READING_PARAMETERS:ParsingStatus.SEARCHING_PARAMETERS;
                    roundBracketBalance=1;
                    sharpbb=0;
                    this.waitingToExecute=new Executor(){internalStaticFunc=new Tuple<String,String,String>(ID,fileName,name)};

                }
                else this.CallStaticClassFunc(ID,fileName,name,new String[0]);
                return;
                
             }

			if (this.functions.ContainsKey(name)) {
				
				Console.WriteLine("PARAM CT: "+this.functions[name].expectedParameterCount.ToString());
				
				lastReferencedVariableIsLocal=false;
				
				if (this.functions[name].expectedParameterCount!=0) {
					
					status=(this.beginsParameters(this.nextChar))?ParsingStatus.READING_PARAMETERS:ParsingStatus.SEARCHING_PARAMETERS;
					roundBracketBalance=1;
					sharpbb=0;
					this.waitingToExecute=new Executor(){func=name};
					
				}
				else this.callFunction(name,new String[0]);
				return;
				
			}
			
			if (this.classes.ContainsKey(name)) {
				
				lastReferencedVariableIsLocal=lastReferencedVariableIsStatic=false;
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
                this.lastReferencedVariableIsStatic=false;
				this.lastReferencedVarType=this.getLocalVarHomeBlock(name).localVariables[name].Item1.Item2;
				this.status=ParsingStatus.SEARCHING_NAME;
				this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
				return;
				
			}
            
              if (staticInstances[ID].ContainsKey(name)) {
                    
                    this.lastReferencedVariable=name;
                    this.lastReferencedVariableIsLocal=false;
                    this.lastReferencedVariableIsStatic=false;
                    var instance=staticInstances[ID][name];
                    this.lastReferencedVarType=instance.Item2.Item2;
                    this.status=ParsingStatus.SEARCHING_NAME;
                    this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
                    
                    return;
                      
              }
			
			checkKeywords:
			
			if (!this.pvtNull()&&this.pvtContainsKey(name)) {
				
				if (wasSearchingFuncReturnType) {
                        String fn=this.functions.Last().Key;
                        Function f=functions[fn];
                        this.functions[fn]=new Function(f.memAddr,this.pvtGet(name),f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier,f.parameterTypes,f.instanceID,f.isInherited);
    				     if (staticFunctions[ID].ContainsKey(fn))
    				     	staticFunctions[ID][fn]=new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>(f.memAddr,this.pvtGet(name),f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier);
	                     if (this.inhFuncsToDefine.ContainsKey(fn)) {
							if ((this.inhFuncsToDefine[fn].returnType==null&&functions[fn].returnType!=null)||!this.inhFuncsToDefine[fn].returnType.Equals(this.functions[fn].returnType))
								throw new ParsingError("Expected "+(inhFuncsToDefine[fn].returnType==null?"no return type":"return type \""+inhFuncsToDefine[fn].returnType.Item1+"\" of \""+inhFuncsToDefine[fn].returnType.Item2+"\"")+ " for func \""+fn+"\".",this);
							this.inhFuncsToDefine.Remove(fn);
						}
                       status=ParsingStatus.SEARCHING_NAME;
					this.setExpectsBlock=1;
					return;
				}
                else if (!(String.IsNullOrEmpty(rTypeDefinition))) {
                    
                    this.acknowledgements.Add(rTypeDefinition,pvtGet(name));
                    this.keywordMgr.acknowledgements.Add(rTypeDefinition,pvtGet(name).Item1);
                    rTypeDefinition=null;
                    status=ParsingStatus.SEARCHING_NAME;
                    return;

                }
				status=ParsingStatus.SEARCHING_VARIABLE_NAME;
				Tuple<String,VarType>vt=this.pvtGet(name);
				this.lastReferencedVarType=vt.Item2;
				this.varType=vt.Item1;
				
				return;
				
			}

            else if (this.acknowledgements.ContainsKey(name)) {
                
                if (wasSearchingFuncReturnType) {
                    String fn=this.functions.Last().Key;
                    Function f=functions[fn];
                    this.functions[fn]=new Function(f.memAddr,this.acknowledgements[name],f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier,f.parameterTypes,f.instanceID,f.isInherited);
                    if (staticFunctions[ID].ContainsKey(fn))
                    	staticFunctions[ID][fn]=new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>(f.memAddr,this.acknowledgements[name],f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier);
                    status=ParsingStatus.SEARCHING_NAME;
                    if (this.inhFuncsToDefine.ContainsKey(fn)) {
						if ((this.inhFuncsToDefine[fn].returnType==null&&functions[fn].returnType!=null)||!this.inhFuncsToDefine[fn].returnType.Equals(this.functions[fn].returnType))
							throw new ParsingError("Expected "+(inhFuncsToDefine[fn].returnType==null?"no return type":"return type \""+inhFuncsToDefine[fn].returnType.Item1+"\" of \""+inhFuncsToDefine[fn].returnType.Item2+"\"")+ " for func \""+fn+"\".",this);
						this.inhFuncsToDefine.Remove(fn);
					}
                    this.setExpectsBlock=1;
                    return;
                }
                else if (!(String.IsNullOrEmpty(rTypeDefinition))) {
                    
                    this.acknowledgements.Add(rTypeDefinition,this.acknowledgements[name]);
                    this.keywordMgr.acknowledgements.Add(rTypeDefinition,this.acknowledgements[name].Item1);
                    rTypeDefinition=null;
                    status=ParsingStatus.SEARCHING_NAME;
                    return;

                }
                status=ParsingStatus.SEARCHING_VARIABLE_NAME;
                Tuple<String,VarType>vt=this.acknowledgements[name];
                this.lastReferencedVarType=vt.Item2;
                this.varType=name;
                
                return;
                
            }

            if (this.containsImportedClass(name)&&!attemptingClassAccess) {

				if (!pKTs.Contains(KeywordType.NONE)&&!pKTs.Contains(KeywordType.TYPE)) {
					if (pKTs.Length>1) {
						StringBuilder sb=new StringBuilder();
						foreach (KeywordType kt in pKTs)
							sb.Append('"'+kt.ToString()+"\", ");
						throw new ParsingError("Expected a keyword of any of the following types: "+String.Concat(sb.ToString().Take(sb.Length-2)),this);
					}
					else throw new ParsingError("Expected a keyword of type \""+pKTs[0].ToString()+'"',this);
				}
			
				this.varType=name;
				this.lastReferencedVarType=VarType.CLASS;
				status=ParsingStatus.SEARCHING_VARIABLE_NAME;
				
				if (wasSearchingFuncReturnType) {
                      String fn=this.functions.Last().Key;
                      Function f=this.functions[fn];
					this.functions[fn]=new Function(f.memAddr,new Tuple<String,VarType>(this.varType,VarType.CLASS),f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier,f.parameterTypes,f.instanceID,f.isInherited);
					if (this.inhFuncsToDefine.ContainsKey(fn)) {
						if ((this.inhFuncsToDefine[fn].returnType==null&&functions[fn].returnType!=null)||!this.inhFuncsToDefine[fn].returnType.Equals(this.functions[fn].returnType))
							throw new ParsingError("Expected "+(inhFuncsToDefine[fn].returnType==null?"no return type":"return type \""+inhFuncsToDefine[fn].returnType.Item1+"\" of \""+inhFuncsToDefine[fn].returnType.Item2+"\"")+ " for func \""+fn+"\".",this);
						this.inhFuncsToDefine.Remove(fn);
					}
					if (staticFunctions[ID].ContainsKey(fn))
                            staticFunctions[ID][fn]=new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>(f.memAddr,new Tuple<String,VarType>(this.varType,VarType.CLASS),f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier);
                       status=ParsingStatus.SEARCHING_NAME;
					this.setExpectsBlock=1;
				}
				lastReferencedVariableIsLocal=false;
				return;
				
			}
			else if (attemptingClassAccess) {
				
                  Boolean homeStatic=staticInstances[ID].ContainsKey(name)&&staticInstances[ID][name].Item2.Item2==VarType.CLASS;
                  if(!homeStatic)
                    this.tryCreateRestoreEsiFunc();
				this.attemptingClassAccess=false;
				
				if (lastReferencedClassInstance.Count!=0) {
					
					if (this.getOriginFinalClass(lastReferencedClassInstance,lastReferencedVariableIsLocal).classes.ContainsKey(name)||(lastReferencedClassInstance.Count==1&&isImportedClass(lastReferencedClassInstance.First())&&staticInstances[getImportedClass(lastReferencedClassInstance.First()).classID].Where(x=>x.Key==name&&x.Value.Item2.Item2==VarType.CLASS).Count()!=0)) {
						
						this.lastReferencedClassInstance.Add(name);
						gettingClassItem=true;
						this.status=ParsingStatus.SEARCHING_NAME;
						return;
						
					}
					else throw new ParsingError("\""+name+"\" does not exist in \""+merge(lastReferencedClassInstance,".")+'"',this);
					
				}
				else if (this.classes.ContainsKey(name)||this.importedClasses.Select(x=>x.className).Contains(name)||homeStatic) {
					
					this.lastReferencedVariable=name;
					this.status=ParsingStatus.SEARCHING_NAME;
					this.gettingClassItem=true;
					lastReferencedVariableIsLocal=false;
					this.lastReferencedClassInstance=new []{name}.ToList();
					return;
					
				}
				else if (this.isALocalVar(name)&&this.getLocalVarHomeBlock(name).localVariables[name].Item1.Item2==VarType.CLASS) {
					
					this.lastReferencedVariable=name;
					this.lastReferencedVariableIsLocal=true;
					this.gettingClassItem=true;
					this.status=ParsingStatus.SEARCHING_NAME;
					this.lastReferencedClassInstance=new []{name}.ToList();
					return;
					
				}
				else throw new ParsingError("Expected class instance, got: \""+name+'"',this);
				
			}
			else if (gettingClassItem) {

               Boolean isExternalStatic=lastReferencedClassInstance.Count==1&&isImportedClass(lastReferencedClassInstance.First());
			   gettingClassItem=false;
			   lastReferencedVariableIsFromClass=!isExternalStatic;
			   Class cl=this.getOriginFinalClass(lastReferencedClassInstance,lastReferencedVariableIsLocal);
                Console.WriteLine(cl.className+", " +cl.classID+", "+name+", "+cl.functions.ContainsKey(name)+", "+cl.functions.Count());
                if (isExternalStatic) {

                    lastReferencedVariableIsStatic=true;
                    this.lastReferencedVariable=name;
                    this.status=ParsingStatus.SEARCHING_NAME;
                    String scName=lastReferencedClassInstance.First();
                    if (!staticInstances[cl.classID].ContainsKey(name)) {
                        if (staticFunctions[cl.classID].ContainsKey(name)) {
                            this.status=ParsingStatus.SEARCHING_NAME;
                            if (staticFunctions[cl.classID][name].Item3!=0) {
                                
                                status=(this.beginsParameters(this.nextChar))?ParsingStatus.READING_PARAMETERS:ParsingStatus.SEARCHING_PARAMETERS;
                                roundBracketBalance=1;
                                sharpbb=0;
                                this.waitingToExecute=new Executor(){externalStaticFunc=new Tuple<Class,String>(cl,name)};
                                
                            }
                            else {

                                this.CallStaticClassFunc(cl,name,new String[0]);
                                this.lastReferencedClassInstance.Clear();
                                
                            }
                            lastReferencedVariableIsLocal=false;
                            return;

                        }
                        throw new ParsingError(scName+" does not contain the static instance \""+name+'"',this);
                    }
                    this.lastReferencedVarType=staticInstances[cl.classID][name].Item2.Item2;
                    return;

                }
		        else if (cl.variables.ContainsKey(name)) {
					
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
					
					this.status=ParsingStatus.SEARCHING_NAME;
					lastReferencedVariableIsFromClass=false;
					
					if (cl.functions[name].expectedParameterCount!=0) {
						
						status=(this.beginsParameters(this.nextChar))?ParsingStatus.READING_PARAMETERS:ParsingStatus.SEARCHING_PARAMETERS;
						roundBracketBalance=1;
						sharpbb=0;
						this.waitingToExecute=new Executor(){classFunc=new Tuple<IEnumerable<String>,String,Boolean>(lastReferencedClassInstance,name,lastReferencedVariableIsLocal)};
						
					}
					else {
						
						this.callClassFunc(this.lastReferencedClassInstance,name,new String[0],lastReferencedVariableIsLocal);
						this.lastReferencedClassInstance.Clear();
						
					}
					lastReferencedVariableIsLocal=false;
					return;
					
				}
				else if (cl.arrays.ContainsKey(name)) {
					
					this.lastReferencedVariable=name;
					Console.WriteLine("LRV:"+this.lastReferencedVariable);
					this.status=ParsingStatus.SEARCHING_NAME;
					this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
					this.lastReferencedVarType=VarType.NATIVE_ARRAY;
					return;	
					
				}
                else throw new ParsingError("Invalid class item: \""+name+'"',this);
				
			}
			
			foreach (Keyword kw in this.keywordMgr.getKeywords()) {
				
				if (kw.name==name) {
					
					Console.WriteLine("Exec: "+kw.name);
					Type kwt=kw.GetType();
					
					if (expectsCaseOrDefault&&kwt!=typeof(KWDefault)&&kwt!=typeof(KWCase))
						throw new ParsingError("Expected only \""+KWCase.constName+"\" and \""+KWDefault.constName+"\" blocks in a \""+KWSwitch.constName+"\" block",this);
					
					if (!pKTs.Contains(KeywordType.NONE)&&!pKTs.Contains(kw.type)) {
						if (pKTs.Length>1) {
							StringBuilder sb=new StringBuilder();
							foreach (KeywordType kt in pKTs)
								sb.Append('"'+kt.ToString()+"\", ");
							throw new ParsingError("Expected a keyword of any of the following types: "+String.Concat(sb.ToString().Take(sb.Length-2)),this);
						}
						else throw new ParsingError("Expected a keyword of type \""+pKTs[0].ToString()+'"',this);
					}
					
					if (kw.name==KWElse.constName&&!expectsElse)
						throw new ParsingError("Got \""+KWElse.constName+"\", but was not expecting an else reference",this);
					else if (kw.hasParameters) {
						
						status=(this.beginsParameters(this.nextChar))?ParsingStatus.READING_PARAMETERS:ParsingStatus.SEARCHING_PARAMETERS;
						waitingToExecute=new Executor(){kw=kw};
						sharpbb=0;
						roundBracketBalance=1;
						
					}
					else
						this.execKeyword(kw,new String[0]);
					
					if (wasSearchingFuncReturnType) {
                           String fn=this.functions.Last().Key;
                           Function f= this.functions[fn];
						this.functions[fn]=new Function(f.memAddr,new Tuple<String,VarType>(this.varType,this.lastReferencedVarType),f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier,f.parameterTypes,f.instanceID,f.isInherited);
						if (this.inhFuncsToDefine.ContainsKey(fn)) {
							if ((this.inhFuncsToDefine[fn].returnType==null&&functions[fn].returnType!=null)||!this.inhFuncsToDefine[fn].returnType.Equals(this.functions[fn].returnType))
								throw new ParsingError("Expected "+(inhFuncsToDefine[fn].returnType==null?"no return type":"return type \""+inhFuncsToDefine[fn].returnType.Item1+"\" of \""+inhFuncsToDefine[fn].returnType.Item2+"\"")+ " for func \""+fn+"\".",this);
							this.inhFuncsToDefine.Remove(fn);
						}
						if (staticFunctions[ID].ContainsKey(fn))
                            staticFunctions[ID][fn]=new Tuple<UInt32,Tuple<String,VarType>,UInt16,FunctionType,CallingConvention,Modifier>(f.memAddr,new Tuple<String,VarType>(this.varType,this.lastReferencedVarType),f.expectedParameterCount,f.functionType,f.callingConvention,f.modifier);
                           status=ParsingStatus.SEARCHING_NAME;
						this.setExpectsBlock=1;
					}
                    else if (!(String.IsNullOrEmpty(rTypeDefinition))&&kwt!=typeof(KWAs)) {
                        
                        this.acknowledgements.Add(rTypeDefinition,new Tuple<String,VarType>(this.varType,this.lastReferencedVarType));
                        this.keywordMgr.acknowledgements.Add(rTypeDefinition,this.varType);
                        rTypeDefinition=null;
                        status=ParsingStatus.SEARCHING_NAME;

                    }
                    lastReferencedVariableIsLocal=lastReferencedVariableIsStatic=false;
					if (kw.type==KeywordType.INCREMENT||kw.type==KeywordType.DECREMENT)
						this.lastReferencedClassInstance.Clear();
					return;
					
				}
				
			}
			
					
			if (name.Length==1) {
				
				Char c=name[0];
				
				if (this.opensBlock(c)) {
					
					if (!(this.expectsBlock))
						throw new ParsingError("Got \""+c+"\" but did not expect block",this);
					
					if (wasSearchingFuncReturnType) {
						String fn=this.functions.Last().Key;
						if (this.inhFuncsToDefine.ContainsKey(fn)) {
							if (this.inhFuncsToDefine[fn].returnType!=null)
								throw new ParsingError("Expected "+(inhFuncsToDefine[fn].returnType==null?"no return type":"return type \""+inhFuncsToDefine[fn].returnType.Item1+"\" of \""+inhFuncsToDefine[fn].returnType.Item2+"\"")+ " for func \""+fn+"\".",this);
							this.inhFuncsToDefine.Remove(fn);
						}
					}
					
					this.updateBlockBalances(c);
					
					this.expectsBlock=false;
					status=ParsingStatus.SEARCHING_NAME;
					lastReferencedVariableIsLocal=false;
					return;
					
				}
				else if (this.closesBlock(c)) {
					
					if (this.blockBracketBalances.Count==0)
						throw new ParsingError("Tried to close block, when no block detected",this);
					
					this.updateBlockBalances(c);
					
					status=ParsingStatus.SEARCHING_NAME; 
					lastReferencedVariableIsLocal=false;
					return;
					
				}
					
			}
			
			Console.WriteLine(variables.ContainsKey(name));
			Console.WriteLine(!pKTs.Contains(KeywordType.NONE));
			Console.WriteLine(attemptingClassAccess);
			Console.WriteLine(gettingClassItem);
			Console.WriteLine(expectsCaseOrDefault);
			throw new ParsingError("Unexpected name: \""+name+'"',this);
			
		}
		
		private static String ModsToLegibleString (Modifier mod) {
			StringBuilder sb=new StringBuilder();
			if (mod.hasAccessorModifier())
				sb.Append(mod.HasFlag(Modifier.PRIVATE)?"private ":mod.HasFlag(Modifier.LOCAL)?"local ":mod.HasFlag(Modifier.PULLABLE)?"pullable ":"public ");
			if (mod.HasFlag(Modifier.CONSTANT))
				sb.Append("constant ");
			if (mod.HasFlag(Modifier.STATIC))
				sb.Append("static ");
			String str=sb.ToString();
			if (str.EndsWith(" "))
				str=str.Substring(0,str.Length-1);
			return str;
		}
			
		private void registerVariable (String varName) {

			if (this.containsImportedClass(this.varType)) {
				
				this.registerClassInstance(varName);
				return;
				
			}

			if (!(currentMods.hasAccessorModifier()))
                currentMods=currentMods|Modifier.PRIVATE;

			Console.WriteLine("Registering variable "+varName+" (a type of \""+this.varType+"\"), memAddress: "+memAddress.ToString("X"));
			
			if (this.nameExists(varName))
				throw new ParsingError("The name \""+varName+"\" is already in use",this);
		
			this.tryIncreaseBlockVarCount();
			
			//when classes are a thing, make sure they are accounted for here
			//if (class) -> appendAfter.addRange ... class or struct size.. because, the pointers are 4 bytes, but the actual struct could and probably is greater or different than 4bytes

            UInt32 vtbs=keywordMgr.getVarTypeByteSize(this.varType);
            Tuple<String,VarType> vt=new Tuple<String,VarType>(this.varType,VarType.NATIVE_VARIABLE);
            if (currentMods.HasFlag(Modifier.STATIC)) {
                
                staticInstances[ID].Add(varName,new Tuple<UInt32,Tuple<String,VarType>,Modifier,Class>((UInt32)dataSectBytes.Count,vt,currentMods,null));
                dataSectBytes.AddRange(new Byte[vtbs]);

            }
			else if (this.blocks.Count==0) {//not local var
            	Boolean isPrivate=currentMods.HasFlag(Modifier.PRIVATE);
				this.defineTimeOrder.Add(varName);
				this.variables.Add(varName,new Tuple<UInt32,String,Modifier,UInt32>(memAddress+(UInt32)appendAfter.Count,this.varType,currentMods,(isPrivate?0:instanceID)));
				this.appendAfterIndex.Add(varName,(UInt32)appendAfter.Count);
				if (!isPrivate) {
					if (inhVarsToDefine.ContainsKey(varName)) {
						Int32 idx=(Int32)this.inhVarsToDefine[varName].Item4;
						var pv=this.variables[varName];
						this.variables[varName]=new Tuple<uint, string, Modifier, uint>(pv.Item1,pv.Item2,pv.Item3,(UInt32)idx);
		        		instanceTable.RemoveAt(idx);
		        		instanceTable.Insert(idx,(UInt32)(appendAfter.Count));
		        		if (!currentMods.Equals(inhVarsToDefine[varName].Item3)) throw new ParsingError("Expected \""+varName+"\" to be defined with mods \""+ModsToLegibleString(inhVarsToDefine[varName].Item3)+'"',this);
						if (this.varType!=inhVarsToDefine[varName].Item2) throw new ParsingError("Expected \""+varName+"\" to be defined of type \""+inhVarsToDefine[varName].Item2+'"',this);
						inhVarsToDefine.Remove(varName);
						Console.WriteLine(varName+" successfully being defined as a inherited var.");
		    		}
					else {
						++instanceID;
						instanceTable.Add((UInt32)appendAfter.Count);
					}
				}
				this.appendAfter.AddRange(new Byte[vtbs]);
				this.variableReferences.Add(varName,new List<UInt32>());
			}
			else {//should be local var
				this.pseudoStack.push(new LocalVar(varName));
				this.getCurrentBlock().localVariables.Add(varName,new Tuple<Tuple<String,VarType>>(vt));
				this.lastReferencedVariableIsLocal=true;
				this.offsetEBPs(4);
			}
			this.lastReferencedVariable=varName;
            if (currentMods.HasFlag(Modifier.CONSTANT)) {
                nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT };
                constantBeingSet=varName;
                constants.Add(varName,new Tuple<UInt32,Tuple<String,VarType>>(0,null));
            }
            currentMods=Modifier.NONE;
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void processValue (String value) {
			
			Console.WriteLine("------------ processValue ------------");
			Console.WriteLine("is local: "+this.referencedVariableIsLocal.ToString()+", is static: "+this.referencedVariableIsStatic+", var name: "+this.referencedVariable+", referenced var type: "+this.referencedVarType.ToString());
			String type;
            Modifier mods;
            Class cl=null;
			if (this.referencedVariableIsFromClass) {

                cl=getOriginFinalClass(this.lastReferencedClassInstance,referencedVariableIsLocal);
                mods=getClassOriginItemMod(lastReferencedClassInstance,referencedVariable,this.referencedVarType,this.referencedVariableIsLocal);
				throwIfCantAccess(mods,referencedVariable,cl.path,false);
                type=cl.getVarType(this.referencedVariable).Item1;
            } 
            else if (staticInstances[ID].ContainsKey(referencedVariable)) {
                type=staticInstances[ID][referencedVariable].Item2.Item1;
                mods=staticInstances[ID][referencedVariable].Item3;
            }
            else if (this.referencedVariableIsStatic) {

                Console.WriteLine("~ Static start (type/mods)");
                cl=getOriginFinalClass(this.lastReferencedClassInstance,referencedVariableIsLocal);
                type=staticInstances[cl.classID][referencedVariable].Item2.Item1;
                mods=staticInstances[cl.classID][referencedVariable].Item3;
                Console.WriteLine("~ Static finish (type/mods)");

            }
            else if (!(this.referencedVariableIsLocal)) {
				type=(this.referencedVarType==VarType.NATIVE_ARRAY||this.referencedVarType==VarType.NATIVE_ARRAY_INDEXER)?this.arrays[this.referencedVariable].Item2:(this.referencedVarType==VarType.CLASS?this.classes[this.referencedVariable].Item2:this.variables[this.referencedVariable].Item2);
                mods=modsOf(this.referencedVariable,this.referencedVarType);
            }
			else {
				type=this.getLocalVarHomeBlock(this.referencedVariable).localVariables[this.referencedVariable].Item1.Item1;
                mods=Modifier.NONE;
            }
			
            Console.WriteLine("-- processValue: successfully set mods and type ("+mods.ToString()+", "+type+')');
            ThrowIfInstRefFromStaticEnv(referencedVariable);

            if(constantBeingSet!=null) {

                UInt32 constantValue;
                Tuple<String,VarType>returnType=getConstantValue(value,out constantValue);
                constants[constantBeingSet]=new Tuple<UInt32,Tuple<String,VarType>>(constantValue,returnType);
                goto done;

            }
            else if (mods.HasFlag(Modifier.CONSTANT))
                throw new ParsingError("Can only set constants immediately at definition time ("+this.referencedVariable+')',this);

//			Console.WriteLine("referencingArray: "+referencingArray.ToString());
//			Console.WriteLine("this.arrays[referencedVariable]: "+this.arrays[referencedVariable].Item2);

			//HACK:: check variable type
			if (this.referencedVarType==VarType.NATIVE_ARRAY) {
				
				if (this.isNativeArrayCreationIndicator(value[0])) {
				    
					Console.WriteLine("Making array named \""+this.referencedVariable+"\" of array type \""+type+"\" with value \""+value+"\".");
					String adjustedValue=value.Substring(1);
					const String HL="HeapAlloc";
					this.referenceDll(Parser.KERNEL32,HL);
					
					if (!this.splitsParameters(adjustedValue[0])) {
						
						this.addByte(0x51); //PUSH ECX
						
						if (Parser.processHeapVar==UInt32.MaxValue)
							this.setProcessHeapVar();
						
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
                           ReferenceRefdFunc(HL,2);
						this.addBytes(new Byte[]{0xFF,0x15,0,0,0,0});//CALL FUNC HeapAlloc
	//					Console.WriteLine(this.referencedVariable);
	//					Console.WriteLine("------------");
	//					foreach (String s in this.arrays.Select(x=>x.Key))
	//						Console.WriteLine(" - "+s);
	//					Console.WriteLine("------------");
						if (this.referencedVariableIsLocal) {
							
							if (this.getLocalVarHomeBlock(this.referencedVariable)!=this.getCurrentBlock())
								this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
							this.addBytes(new Byte[]{0x89,0x45,this.pseudoStack.getVarEbpOffset(this.referencedVariable)}); //MOV [EBP+-OFFSET],EAX
							
						}
						else {
							
							if (referencedVariableIsFromClass) {
								
								this.addByte(0x91);//XCHG EAX,ECX
								this.moveClassOriginItemAddrIntoEax(this.lastReferencedClassInstance,this.referencedVariable,VarType.NATIVE_ARRAY,lastReferencedVariableIsFromClass);
								this.addBytes(new Byte[]{0x89,8});//MOV [EAX],ECX
								
							}
							else {
								
								if (addEsiToLocalAddresses)
									this.addBytes(new Byte[]{0x89,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[referencedVariable])));
								else {
									this.arrayReferences[this.referencedVariable].Add((UInt32)(opcodes.Count+1));
									this.addBytes(new Byte[]{0xA3,0,0,0,0});//STORE ALLOCATED PTR TO VARIABLE
								}
								
							}
						
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
					}
					else {
						
						if (adjustedValue.Length<=2||!this.beginsParameters(adjustedValue[1])) throw new ParsingError("Invalid syntax for initializing array by set of values: expected format \"#,(value,value0,value1,...)\"",this);
						
						List<String>@params=parseParameters(adjustedValue.Substring(2));
						Int32 bytesToReserve=(Int32)((@params.Count*keywordMgr.getVarTypeByteSize(type))+8),stackSpace=(Int32)(@params.Count*4);
						if (bytesToReserve>SByte.MaxValue)
							this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes(bytesToReserve))); //PUSH DWORD
						else
							this.addBytes(new Byte[]{0x6A,(Byte)bytesToReserve}); //PUSH SBYTE
						
						this.addBytes(new Byte[]{0x6A,8}); //PUSH 8
						this.pushProcessHeapVar();
						
                           ReferenceRefdFunc(HL,2); 
						this.addBytes(new Byte[]{0xFF,0x15,0,0,0,0});//CALL FUNC HeapAlloc
						if (this.referencedVariableIsLocal) {
							if (this.getLocalVarHomeBlock(this.referencedVariable)!=this.getCurrentBlock())
								this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
							this.addBytes(new Byte[]{0x89,0x45,this.pseudoStack.getVarEbpOffset(this.referencedVariable)}); //MOV [EBP+-OFFSET],EAX
						}
						else {
							if (referencedVariableIsFromClass) {
								this.addByte(0x91);//XCHG EAX,ECX
								this.moveClassOriginItemAddrIntoEax(this.lastReferencedClassInstance,this.referencedVariable,VarType.NATIVE_ARRAY,lastReferencedVariableIsFromClass);
								this.addBytes(new Byte[]{0x89,8});//MOV [EAX],ECX
							}
							else {
								if (addEsiToLocalAddresses)
									this.addBytes(new Byte[]{0x89,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[referencedVariable])));
								else {
									this.arrayReferences[this.referencedVariable].Add((UInt32)(opcodes.Count+1));
									this.addBytes(new Byte[]{0xA3,0,0,0,0});//STORE ALLOCATED PTR TO VARIABLE
								}
							}
						}
						Tuple<String,VarType>arrayMemberType=this.getVarType(type);
						UInt32 byteSize=keywordMgr.getVarTypeByteSize(arrayMemberType.Item1);
						this.addBytes(new Byte[]{0x6A,0});//PUSH 0
						if (@params.Count>SByte.MaxValue)
							this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes(@params.Count)));
						else
							this.addBytes(new Byte[]{0x6A,(Byte)(@params.Count)});
						this.callSetArrayValue(this.referencedVariable);
						this.addBytes(new Byte[]{0x6A,4});//PUSH 4
						this.addBytes(new Byte[]{0x6A,(Byte)(byteSize)});
						this.callSetArrayValue(this.referencedVariable);
						UInt32 i=8,call2BSetValue=0,call1BSetValue=0;
						foreach (String str in @params) {
							if (i>SByte.MaxValue)
								this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes(i)));
							else
								this.addBytes(new Byte[]{0x6A,(Byte)(i)});
							this.tryConvertVars(arrayMemberType,this.pushValue(str),str);
							switch (byteSize) {
								case 4:
									this.callSetArrayValue(this.referencedVariable);
									break;
								case 2:
									if (call2BSetValue==0) {
										
										UInt32 jmpOCA=(UInt32)(this.opcodes.Count+1);
										this.addBytes(new Byte[]{0xE9,0,0,0,0});//JUMP DWORD OFFSET
										call2BSetValue=memAddress;
										this.pushValue(referencedVariable);
										this.addByte(0x5A);//POP EDX
										this.addBytes(new Byte[]{0x8B,0x44,0x24,4});//MOV EAX,[ESP+4]
										this.addBytes(new Byte[]{3,0x54,0x24,8});//ADD EDX,[ESP+8]
										this.addBytes(new Byte[]{0x66,0x89,2});//MOV [EDX],AX
										this.addBytes(new Byte[]{0xC2}.Concat(BitConverter.GetBytes((UInt16)8))); //RETN 8
										
										Byte[] arr=BitConverter.GetBytes(memAddress-call2BSetValue);
										Byte i0=0;
										while (i0!=4) {
											
											this.opcodes[(Int32)(jmpOCA+i0)]=arr[i0];
											++i0;
											
										}
										
									}
									this.addBytes(new Byte[]{0xE8}.Concat(BitConverter.GetBytes((Int32)call2BSetValue-(Int32)memAddress-5)));//CALL DWORD OFFSET
									break;
								case 1:
									if (call1BSetValue==0) {
										
										UInt32 jmpOCA=(UInt32)(this.opcodes.Count+1);
										this.addBytes(new Byte[]{0xE9,0,0,0,0});//JUMP DWORD OFFSET
										call1BSetValue=memAddress;
										this.pushValue(referencedVariable);
										this.addByte(0x5A);//POP EDX
										this.addBytes(new Byte[]{0x8B,0x44,0x24,4});//MOV EAX,[ESP+4]
										this.addBytes(new Byte[]{3,0x54,0x24,8});//ADD EDX,[ESP+8]
										this.addBytes(new Byte[]{0x88,2});//MOV [EDX],AL
										this.addBytes(new Byte[]{0xC2}.Concat(BitConverter.GetBytes((UInt16)8))); //RETN 8
										
										Byte[] arr=BitConverter.GetBytes(memAddress-call1BSetValue);
										Byte i0=0;
										while (i0!=4) {
											
											this.opcodes[(Int32)(jmpOCA+i0)]=arr[i0];
											++i0;
											
										}
										
									}
									this.addBytes(new Byte[]{0xE8}.Concat(BitConverter.GetBytes((Int32)call1BSetValue-(Int32)memAddress-5)));//CALL DWORD OFFSET
									break;
							}
							i+=byteSize;
						}
					}
					
				}
				else if (this.arrays.ContainsKey(value)) {
					
					if (referencedVariableIsFromClass) {
						this.pushValue(value);
						this.moveClassOriginItemAddrIntoEax(this.lastReferencedClassInstance,this.referencedVariable,VarType.NATIVE_ARRAY,lastReferencedVariableIsFromClass);
						this.addBytes(new Byte[]{0x8B,0x14,0x24}); //MOV EDX,[ESP]
						this.addBytes(new Byte[]{0x89,0x10}); //MOV [EAX],EDX
						this.addBytes(new Byte[]{0x83,0xC4,4}); //ADD ESP,4
						//FIXME:: probable bug on next line, it may apply for all classes of that type
						this.getOriginFinalClass(this.lastReferencedClassInstance,lastReferencedVariableIsLocal).arrays[this.referencedVariable]=new Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>(this.arrays[value].Item1,this.arrays[value].Item2,this.arrays[value].Item3,this.arrays[value].Item4,this.arrays[value].Item5);
					}
					else {
						this.arrayReferences[value].Add((UInt32)this.opcodes.Count+1);
						this.addBytes(new Byte[]{0xA1,0,0,0,0}); //MOV EAX,[PTR]
						if (addEsiToLocalAddresses) {
							this.addBytes(new Byte[]{0x89,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[this.referencedVariable]))); //MOV [PTR+ESI],EAX
						}
						else {
							this.arrayReferences[this.referencedVariable].Add((UInt32)this.opcodes.Count+1);
							this.addBytes(new Byte[]{0xA3,0,0,0,0}); //MOV [PTR+ESI],EAX
						}
					}
					goto done;
					
				}
				else if (this.isValidFunction(value)||this.hasClassAccessorOutsideParentheses(value)) {
					
					if (referencedVariableIsLocal) {
						
						this.pushValue(value);
						if (this.getLocalVarHomeBlock(this.referencedVariable)!=this.getCurrentBlock())
							this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
						this.addBytes(new Byte[]{0x8F,0x45,this.pseudoStack.getVarEbpOffset(this.referencedVariable)}); //POP [EBP+-OFFSET]
						
					}
					else {
						if (this.arrays[this.referencedVariable].Item1!=0) {
							
							const String HF="HeapFree";
							this.referenceDll(Parser.KERNEL32,HF);
							this.arrayReferences[this.referencedVariable].Add((UInt32)(opcodes.Count+2));
							this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0});//push pMemory
							this.addBytes(new Byte[]{0x6A,0});//push Flags
							this.pushProcessHeapVar();//push hHeap
                                ReferenceRefdFunc(HF,2);
							this.addBytes(new Byte[]{0xFF,0x15,0,0,0,0});//call HeapFree
							
						}
						else {
							
							Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>_refdVar=this.arrays[this.referencedVariable];
							this.arrays[this.referencedVariable]=new Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>(this.memAddress+(UInt32)(appendAfter.Count),_refdVar.Item2,_refdVar.Item3,_refdVar.Item4,_refdVar.Item5);
							this.appendAfterIndex.Add(referencedVariable,(UInt32)appendAfter.Count);
							this.appendAfter.AddRange(new Byte[4]);
							
						}
						
						this.pushValue(value);
						if (addEsiToLocalAddresses) {
							this.addBytes(new Byte[]{0x8F,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[this.referencedVariable])));
						}
						else {
							this.arrayReferences[this.referencedVariable].Add((UInt32)(opcodes.Count+2));
							this.addBytes(new Byte[]{0x8F,5,0,0,0,0}); // POP [PTR]
						}
					}
				}
				else if ((this.isALocalVar(value)&&this.getLocalVarHomeBlock(value).localVariables[value].Item1.Item2==VarType.NATIVE_ARRAY)) {
					
					if (referencedVariableIsFromClass) {
						this.pushValue(value);
						this.moveClassOriginItemAddrIntoEax(this.lastReferencedClassInstance,this.referencedVariable,VarType.NATIVE_ARRAY,lastReferencedVariableIsFromClass);
						this.addBytes(new Byte[]{0x8B,0x14,0x24}); //MOV EDX,[ESP]
						this.addBytes(new Byte[]{0x89,0x10}); //MOV [EAX],EDX
						this.addBytes(new Byte[]{0x83,0xC4,4}); //ADD ESP,4
						//FIXME:: probable bug on next line, it may apply for all classes of that type
						this.getOriginFinalClass(this.lastReferencedClassInstance,lastReferencedVariableIsLocal).arrays[this.referencedVariable]=new Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>(this.arrays[value].Item1,this.arrays[value].Item2,this.arrays[value].Item3,this.arrays[value].Item4,this.arrays[value].Item5);
					}
					else {
						
						this.pushValue(value);
						this.addByte(0x58);//POP EAX
						if (addEsiToLocalAddresses) {
							this.addBytes(new Byte[]{0x89,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[this.referencedVariable]))); //MOV [PTR+ESI],EAX
						}
						else {
							this.arrayReferences[this.referencedVariable].Add((UInt32)this.opcodes.Count+1);
							this.addBytes(new Byte[]{0xA3,0,0,0,0}); //MOV [PTR],EAX
						}
					}
					
				}
				else throw new ParsingError("Invalid array assignment value: \""+value+'"',this);
				
			}
			else if (this.referencedVarType==VarType.NATIVE_ARRAY_INDEXER) {
				
				tryConvertVars(new Tuple<String,VarType>(type,referencedVarType),this.pushValue(value),value);
				this.addByte(0x5A);//POP EDX 
				this.addByte(0x58);//POP EAX
				UInt32 size=this.keywordMgr.getVarTypeByteSize(varType);
				this.addBytes(size==1?new Byte[]{0x88,0x10}://MOV DWORD [EAX],EDX
				              size==2?new Byte[]{0x66,0x89,0x10}:
				              /*size==4*/new Byte[]{0x89,0x10}
				             );
				
			}
			else if (this.referencedVarType==VarType.NATIVE_VARIABLE&&referencedVariableIsFromClass) {
				
				this.moveClassOriginItemAddrIntoEax(this.lastReferencedClassInstance,this.referencedVariable,this.referencedVarType,this.referencedVariableIsLocal);
				
				UInt32 size=keywordMgr.getVarTypeByteSize(varType);
				
				this.addByte(0x50);//PUSH EAX
				Tuple<String,VarType> tpl=this.pushValue(value);
				this.tryConvertVars(new Tuple<String,VarType>(type,this.referencedVarType),tpl,value);
				this.addByte(0x5A);//POP EDX
				this.addByte(0x58);//POP EAX
				//EDX has VALUE, EAX has PTR
                if (acknowledgements.ContainsKey(type))
                    type=ackRootOf(type).Item1;
				
				if (tpl.Item1==KWBoolean.constName&&type!=KWBoolean.constName)
					throw new ParsingError("You can only apply \""+KWBoolean.constTrue+"\" and +\""+KWBoolean.constFalse+"\" to boolean variables",this);
				
				if (type!=KWString.constName&&tpl.Item1==KWString.constName)
					throw new ParsingError("Can't convert \""+tpl.Item1+"\" to a string (\""+KWString.constName+"\").",this);
				
				if (type==KWString.constName&&tpl.Item1!=KWString.constName)
					throw new ParsingError("Can't convert a string (\""+KWString.constName+"\") to \""+tpl.Item1+"\".",this);
					
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
				
				this.moveClassOriginItemAddrIntoEax(this.lastReferencedClassInstance,this.referencedVariable,this.referencedVarType,referencedVariableIsLocal);
				
				this.addByte(0x50);//PUSH EAX
				Tuple<String,VarType> tpl=this.pushValue(value);
				this.tryConvertVars(new Tuple<String,VarType>(type,this.referencedVarType),tpl,value);
				this.addByte(0x5A);//POP EDX
				this.addByte(0x58);//POP EAX
				
				this.addBytes(new Byte[]{
					              	0x89,0x10 //MOV DWORD [EAX],EDX
					              });
				
			}
			
			else if (this.referencedVarType==VarType.CLASS) {
				
				Tuple<String,VarType> tpl=this.pushValue(value);
				this.tryConvertVars(new Tuple<String,VarType>(type,this.referencedVarType),tpl,referencedVariable);
				if (this.referencedVariableIsLocal) {
					if (this.getLocalVarHomeBlock(this.referencedVariable)!=this.getCurrentBlock())
						this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{0x8F,0x45,this.pseudoStack.getVarEbpOffset(this.referencedVariable)}); //POP [EBP+-OFFSET]
				}
				else {

				     if (mods.HasFlag(Modifier.STATIC))
                                this.addBytes(new Byte[]{0x8F,5,}.Concat(BitConverter.GetBytes((UInt32)(PEHeaderFactory.dataSectAddr+staticInstances[referencedVariableIsStatic?cl.classID:ID][this.referencedVariable].Item1)))); // POP DWORD [PTR]
					else if (addEsiToLocalAddresses)
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
                //this.debugLine(referencedVariable+" processValue type result: "+tpl.Item1+" (this may not be the type of \""+referencedVariable+"\")");
				Console.WriteLine("value: \""+value+"\",type: "+type);
                this.tryConvertVars(new Tuple<String,VarType>(type,this.referencedVarType),tpl,value);
                if (acknowledgements.ContainsKey(type))
                    type=ackRootOf(type).Item1;
				if (tpl.Item1==KWBoolean.constName&&type!=KWBoolean.constName)
					throw new ParsingError("You can only apply \""+KWBoolean.constTrue+"\" and \""+KWBoolean.constFalse+"\" to boolean variables",this);
				
				if (type!=KWString.constName&&tpl.Item1==KWString.constName)
					throw new ParsingError("Can't convert \""+type+"\" to a string (\""+KWString.constName+"\").",this);
				
				if (type==KWString.constName&&tpl.Item1!=KWString.constName)
					throw new ParsingError("Can't convert a string (\""+KWString.constName+"\") to \""+tpl.Item1+"\".",this);
				
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
							this.addBytes(new Byte[]{0x33,0xD2}); // XOR EDX,EDX
							this.addByte(0x58); // POP EAX
							this.addBytes(new Byte[]{0x66,0x8B,0xD0}); // MOV DX,AX
							this.addBytes(new Byte[]{0x66,0x89,0x96}.Concat(BitConverter.GetBytes(appendAfterIndex[this.referencedVariable]))); // MOV [PTR+ESI],DX
						}
						else {
							this.addBytes(new Byte[]{0x33,0xD2}); // XOR EDX,EDX
							this.addByte(0x58); // POP EAX
							this.addBytes(new Byte[]{0x66,0x8B,0xD0}); // MOV DX,AX
							this.variableReferences[this.referencedVariable].Add((UInt32)this.opcodes.Count+3);
							this.addBytes(new Byte[]{0x66,0x89,0x15,0,0,0,0}); // MOV [PTR],DX
						}
						
					}
					
					else if (type==KWInteger.constName||type==KWString.constName) {

                                if (mods.HasFlag(Modifier.STATIC)) {
                                    // TODO:: UNDONE (NEED TO REPLACE dataSectAddr WITH REFERENCE, SET TO WHATEVER DATA SECT ADDRESS ENDS UP BEING,NAME IT DATA SECT MEMORY ADDRESSES)
                                    this.addBytes(new Byte[]{0x8F,5,}.Concat(BitConverter.GetBytes((UInt32)(PEHeaderFactory.dataSectAddr+staticInstances[referencedVariableIsStatic?cl.classID:ID][this.referencedVariable].Item1)))); // POP DWORD [PTR]
                                }

						else if (this.addEsiToLocalAddresses)
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
			
            done:
		   status=ParsingStatus.SEARCHING_NAME;
            constantBeingSet=null;
		   this.lastReferencedClassInstance.Clear();
			
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
			
			foreach (KeyValuePair<Class,List<OpcodeIndexReference>>references in this.staticClassReferences) {
				
				foreach (OpcodeIndexReference index in references.Value) {
				
					Byte[]memAddrBytes=BitConverter.GetBytes(references.Key.skeletonIndex+PEHeaderFactory.dataSectAddr);
					
					Byte i=0;
					while (i!=4) {
						SetStaticInclusiveByte(index:index,b:memAddrBytes[i],indexOffset:i);
						++i;
					}
				}
				
			}
			
			if (processHeapVar!=UInt32.MaxValue) {
				foreach (OpcodeIndexReference index in this.procHeapVarRefs) {
					
					Byte[]memAddrBytes=BitConverter.GetBytes(processHeapVar+PEHeaderFactory.dataSectAddr);
					
					Byte i=0;
					while (i!=4) {
						SetStaticInclusiveByte(index,memAddrBytes[i],i);
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
				throw new ParsingError("The name \""+arrayName+"\" is already in use",this);

			if (!(currentMods.hasAccessorModifier()))
                currentMods=currentMods|Modifier.PRIVATE;
            lastReferencedVariableIsStatic=false;
			this.tryIncreaseBlockVarCount();

            var vt=new Tuple<Tuple<String,VarType>>(new Tuple<String,VarType>(this.varType,VarType.NATIVE_ARRAY));
			if (currentMods.HasFlag(Modifier.STATIC)) {
                
                staticInstances[ID].Add(arrayName,new Tuple<UInt32,Tuple<String,VarType>,Modifier,Class>((UInt32)dataSectBytes.Count,vt.Item1,currentMods,null));
                dataSectBytes.AddRange(new Byte[4]);

            }
			else if (this.blocks.Count==0) {
            	Boolean isPrivate=currentMods.HasFlag(Modifier.PRIVATE);
                
				this.defineTimeOrder.Add(arrayName);
				this.arrays.Add(arrayName,new Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>(this.memAddress+(UInt32)(appendAfter.Count),this.varType,this.style,currentMods,(isPrivate?0:instanceID)));
				this.appendAfterIndex.Add(arrayName,(UInt32)appendAfter.Count);
				if (!isPrivate) { 
					if (inhArrsToDefine.ContainsKey(arrayName)) {
						Int32 idx=(Int32)this.inhArrsToDefine[arrayName].Item5;
						var pa=this.arrays[arrayName];
						this.arrays[arrayName]=new Tuple<uint, string, ArrayStyle, Modifier, uint>(pa.Item1,pa.Item2,pa.Item3,pa.Item4,(UInt32)idx);
						instanceTable.RemoveAt(idx);
						instanceTable.Insert(idx,(UInt32)appendAfter.Count);
						if (!currentMods.Equals(inhArrsToDefine[arrayName].Item4)) throw new ParsingError("Expected \""+arrayName+"\" to be defined with mods \""+ModsToLegibleString(inhArrsToDefine[arrayName].Item4)+'"',this);
						if (this.varType!=inhArrsToDefine[arrayName].Item2) throw new ParsingError("Expected \""+arrayName+"\" to be defined of type \""+inhArrsToDefine[arrayName].Item2+'"',this);
						inhArrsToDefine.Remove(arrayName);
						Console.WriteLine(arrayName+" successfully being defined as a inherited array.");
					}
					else {
						++instanceID;
						instanceTable.Add((UInt32)appendAfter.Count); //....
					}
				}
//						this.debugLine(referencedVariable+','+appendAfter.Count.ToString());
				this.appendAfter.AddRange(new Byte[4]);
				this.arrayReferences.Add(arrayName,new List<UInt32>());
	//			this.appendAfter.AddRange(new Byte[keywordMgr.getVarTypeByteSize(this.varType)]);
			}
			else {//should be local var
				this.pseudoStack.push(new LocalVar(arrayName));
				this.getCurrentBlock().localVariables.Add(arrayName,vt);
				this.lastReferencedVariableIsLocal=true;
				this.offsetEBPs(4);
			}
			if (currentMods.HasFlag(Modifier.CONSTANT)) {
                nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT };
                constantBeingSet=arrayName;
                constants.Add(arrayName,new Tuple<uint, Tuple<string, VarType>>(0,null));
            }
            else nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.MODIFIER,KeywordType.FUNCTION,KeywordType.TYPE };
			this.lastReferencedVariable=arrayName;
			this.lastReferencedVarType=VarType.NATIVE_ARRAY;
             currentMods=Modifier.NONE;
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private List<Byte> getImportSection (out List<Tuple<String,UInt32>>funcMemAddrs,out UInt32 virtualSize) {
			
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
			
			virtualSize=(UInt32)(opcodes.Count);
			while (opcodes.Count%512!=0)
				opcodes.Add(0x00);
			
			return opcodes;
			
		}
		
		private void fillFuncMemAddrs (List<Tuple<String,UInt32>>funcMemAddrs) {
			
			Byte i;
			Byte[]memAdd;
			
			foreach (Tuple<String,UInt32>funcMemAddr in funcMemAddrs) {
				
				Console.WriteLine(funcMemAddr.Item1);
				foreach (OpcodeIndexReference pos in this.referencedFuncPositions[funcMemAddr.Item1]) {
					
                    Console.WriteLine(pos.type.ToString()+", "+funcMemAddr.Item1+", "+pos.index.ToString());
                    //if (pos.type==OpcodeIndexType.DATA_SECT_REFERENCE) Console.ReadKey();

					i=0;
					memAdd=BitConverter.GetBytes(funcMemAddr.Item2);
					while (i!=4) {
						
                        SetStaticInclusiveByte(pos,memAdd[i],i);

						++i;
						
					}
					
				}
				
			}
			
		}
		
		internal void referenceDll (String dllName,String funcName) {
			
			dllName=dllName.ToUpper();
			
            const String ext=".DLL";
			if (!(dllName.EndsWith(ext,StringComparison.CurrentCulture)))
				dllName+=ext;
			
			if (!(this.toImport.ContainsKey(dllName))) {
				
				this.toImport.Add(dllName,new List<String>());
				this.toImport[dllName].Add(funcName);
				
			}
			else
				if (!(this.toImport[dllName].Contains(funcName)))
					this.toImport[dllName].Add(funcName);
			
			if (!(this.referencedFuncPositions.ContainsKey(funcName)))
				this.referencedFuncPositions.Add(funcName,new List<OpcodeIndexReference>());
			
		}
		
		internal void setProcessHeapVar () {
			
			const String GPH="GetProcessHeap";
            if (processHeapVar!=UInt32.MaxValue) return;

            if (blocks.Count!=0) {

                // TODO:: set pheapvar at start
                return;

            }
            
			this.referenceDll(Parser.KERNEL32,GPH);
			ReferenceRefdFunc(GPH,2);
			Parser.processHeapVar=(UInt32)Parser.dataSectBytes.Count;
            this.procHeapVarRefs.Add(GetStaticInclusiveOpcodesCount(7));
			Parser.dataSectBytes.AddRange(new Byte[4]);
				
			this.addBytes(new Byte[]{
			              	
			              	0xFF,0x15, //CALL FUNC
			              	0,0,0,0, //MEM ADDR TO GetProcessHeap
			              	
			              	0xA3, //MOV EAX TO
			              	0,0,0,0 //MEM ADDR TO processHeapVar
			              	
			              });
			
		}
		
		internal void pushProcessHeapVar () {
			
			if (processHeapVar==UInt32.MaxValue)
				setProcessHeapVar();
			
			this.procHeapVarRefs.Add(GetStaticInclusiveOpcodesCount(2));
			this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0}); //PUSH DWORD [PTR]
			
		}
		
		/// <summary>
		/// First push byte index and value
		/// this works for 4 byte member arrays
		/// </summary>
		private void callSetArrayValue (String arrayName) {
			
			Console.WriteLine(arrayName);
			
			if (!(this.setArrayValueFuncPtrs.ContainsKey(arrayName))) {
				
				
				if (this.isALocalVar(arrayName)) {
					
					this.setArrayValueFuncPtrs.Add(arrayName,GetStaticInclusiveAddress()+2);
					
//					this.localVarEBPPositions[this.getLocalVarHomeBlock(arrayName)][arrayName].Add((UInt32)(this.opcodes.Count+8));
					
					this.addBytes(new Byte[] {
					              	
						            0xEB,10, // JMP 10 BYTES
					              	0x5A, //POP EDX
					              	0x5B, //POP EBX
					              	0x58, //POP EAX
					              	0x52, //PUSH EDX
					              	3,0x45,this.pseudoStack.getVarEbpOffset(arrayName), //ADD EAX,[EBP+-OFFSET]
					              	0x89,0x18, //MOV EBX TO [EAX]
					              	0xC3 //RETN
					              	
					              });
					
					this.getLocalVarHomeBlock(arrayName).restoreArraySetValueFuncs.Add(arrayName);
					
				}
				
				else {
				
					
					if (addEsiToLocalAddresses) {
						this.setArrayValueFuncPtrs.Add(arrayName,GetStaticInclusiveAddress()+2);
                        Console.WriteLine(this.appendAfterIndex[arrayName].ToString());
						this.addBytes(new Byte[] {
					              		
							            0xEB,0x0D, // JMP 14 BYTES
						              	0x5A, //POP EDX
						              	0x5B, //POP EBX
						              	0x58, //POP EAX
						              	0x52, //PUSH EDX
						              	3,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[arrayName])).Concat(new Byte[]{ //ADD VALUE AT [PTR+ESI] TO EAX
						              	0x89,0x18, //MOV EBX TO [EAX]
						              	0xC3 //RETN
						              	
						              }));
					}
					else if (referencedVariableIsFromClass) {
						//note: this assumes this function will only be called from processValue during array initialization
						// ^ or more specifically, that this.lastReferencedClassInstance && referencedVariableIsFromClass are accurate
						
						UInt32 sMemAddr=this.memAddress,opcodesCtPtr=(UInt32)this.opcodes.Count+1;
						this.addBytes(new Byte[]{0xE9,0,0,0,0});
						this.setArrayValueFuncPtrs.Add(arrayName,GetStaticInclusiveAddress());
						this.moveClassOriginItemAddrIntoEax(this.lastReferencedClassInstance,arrayName,VarType.NATIVE_ARRAY,lastReferencedVariableIsFromClass);
						this.addBytes(new Byte[]{0x8B,0}); //MOV EAX,[EAX]
						this.addBytes(new Byte[]{3,0x44,0x24,8});//ADD EAX,[ESP+8]
						this.addBytes(new Byte[]{0x8B,0x5C,0x24,4});//MOV EBX,[ESP+4]
						this.addBytes(new Byte[]{0x89,0x18});//MOV [EAX],EBX
						this.addBytes(new Byte[]{0xC2}.Concat(BitConverter.GetBytes((UInt16)8)));//RETN 8
						Byte i=0;
						Byte[]t=BitConverter.GetBytes(this.memAddress-sMemAddr-5);
						while (i!=4) {
							
							this.opcodes[(Int32)opcodesCtPtr+i]=t[i];
							++i;
							
						}
						
						
					}
					else {
						this.setArrayValueFuncPtrs.Add(arrayName,GetStaticInclusiveAddress()+2);
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
				
			}
			
			Console.WriteLine("Calling setArrayValue: memAddress: "+memAddress.ToString("X")+", setArrayValueFuncPtrs[arrayName]: "+setArrayValueFuncPtrs[arrayName].ToString("X"));
			this.addBytes(new Byte[]{0xE8}.Concat(BitConverter.GetBytes((Int32)setArrayValueFuncPtrs[arrayName]-(Int32)GetStaticInclusiveAddress()-5)));
			
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
				
				throw new ParsingError("Invalid array indexer: Array \""+referencedArray+"\" does not exist",this);
				
			}
			else return false;
			
		}
		
		
		internal Boolean nameExists (String name) {
			
			//HACK:: check var type
			
			foreach (String str in keywordMgr.getKeywords().Select(x=>x.name))
				if (str==name) return true;
			
			return this.arrays.ContainsKey(name)        ||
				this.variables.ContainsKey(name)        ||
				name==KWBoolean.constFalse              ||
				name==KWBoolean.constTrue               ||
				this.functions.ContainsKey(name)        ||
				this.isALocalVar(name)                  ||
				name==Parser.NULL_STR                   ||
				this.containsImportedClass(name)        ||
                this.acknowledgements.ContainsKey(name) ||
				name==Parser.THIS_STR                   ||
                staticInstances[ID].ContainsKey(name)   ||
                this.labels.ContainsKey(name)            ;
			
		}
		
		/// <summary>
		/// Pushes the memory address address to an index in an array
		/// </summary>
		/// <param name="arrName">name of array</param>
		/// <param name="indexer">value inside first set of square brackets (not including first and last square brackets)</param>
		/// <param name="noName">if this is true,array address should be in EAX</param>
		/// <returns>new status to update</returns>
		private ParsingStatus indexArray (String arrName,String indexer,Byte recursionDepth=0,Boolean noName=false,UInt32 arrMemSize=0) {
			
			if (noName) {
				if (recursionDepth==0) {
					this.addByte(0x55);//PUSH EBP
					this.addBytes(new Byte[]{0x8B,0xEC});//MOV EBP,ESP
				}
				this.addByte(0x50);//PUSH EAX
			}
			
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
										this.indexArray(arrName0,indexer0,0);
										
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
					if (!noName) {
						if (addEsiToLocalAddresses) 
							this.addBytes(new Byte[]{3,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[arrName])));//ADD EAX,VALUE @ [ESI+PTR]
						else {
							this.arrayReferences[arrName].Add((UInt32)(this.opcodes.Count+2));
							this.addBytes(new Byte[]{3,5,0,0,0,0});//ADD EAX,VALUE @ PTR
						}
					
					}
					else {
						this.addBytes(new Byte[]{3,0x45,unchecked((Byte)(-(4+(4*recursionDepth))))});//ADD EAX,[EBP+-OFFSET]
					}
					
					this.addByte(0x50);//PUSH EAX
					
					Console.WriteLine(" ====== =============== =======");
					
				}
				else {
					
					sub=indexer.Substring(indexer.IndexOf('[')+1);
					arrName0=indexer.Split('[')[0].Replace(" ","");
					indexer0=(sub.EndsWith("]"))?String.Concat(sub.Take(sub.Length-1)):sub;
					
					this.indexArray(arrName0,indexer0,(Byte)(recursionDepth+1));
					
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
					this.addBytes(new Byte[]{0xB9}.Concat(BitConverter.GetBytes(arrMemSize==0?keywordMgr.getVarTypeByteSize((this.isALocalVar(arrName)?this.getLocalVarHomeBlock(arrName).localVariables[arrName].Item1.Item1:this.arrays[arrName].Item2)):arrMemSize))); // MOV ECX,BYTE SIZE OF ARR MEMBER
					this.addByte(0x52);//PUSH EDX
					this.addBytes(new Byte[]{0xF7,0xE1});//MUL ECX
					this.addByte(0x5A);//POP EDX
					this.addByte(0x92);//XCHG EAX,EDX
					
					if (!noName) {
						if (this.isALocalVar(arrName)) {
							if (this.getCurrentBlock()!=this.getLocalVarHomeBlock(arrName))
								this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
							this.addBytes(new Byte[]{3,0x55,this.pseudoStack.getVarEbpOffset(arrName)});
						}
						else {
							if (addEsiToLocalAddresses)
								this.addBytes(new Byte[]{3,0x96}.Concat(BitConverter.GetBytes(appendAfterIndex[arrName])));
							else {
								this.arrayReferences[arrName].Add((UInt32)(this.opcodes.Count+2));
								this.addBytes(new Byte[]{3,0x15,0,0,0,0});//ADD EDX,VALUE @ PTR
							}
						}
					}
					else {
						this.addBytes(new Byte[]{3,0x55,unchecked((Byte)(-(4+(4*recursionDepth))))});//ADD EDX,[EBP+-OFFSET]
					}
					this.addBytes(new Byte[]{0x83,0xC2,8});//ADD EDX,8
					if (recursionDepth!=0)
						this.addByte(0x92);//XCHG EAX,EDX
					else
						this.addByte(0x52);//PUSH EDX
				}
					
				
			}
					
			else {
				
				Console.WriteLine("Trying to pushValue: \""+indexer+'"');
				this.pushValue(indexer);//PUSH ......
				this.addByte(0x58);//POP EAX
				Byte arrMemByteSize=(Byte)(arrMemSize==0?(Byte)keywordMgr.getVarTypeByteSize((this.isALocalVar(arrName)?this.getLocalVarHomeBlock(arrName).localVariables[arrName].Item1.Item1:this.arrays[arrName].Item2)):arrMemSize);
				if (arrMemByteSize!=1)
					this.addBytes(new Byte[]{0x6B,0xC0,arrMemByteSize});//IMUL EAX BY ARRAY MEMBER BYTE SIZE
				this.addBytes(new Byte[]{0x83,0xC0,8}); //ADD 8 TO EAX
				if (this.isALocalVar(arrName)) { 
					
					if (this.getLocalVarHomeBlock(arrName)!=this.getCurrentBlock())
						this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{3,0x45,this.pseudoStack.getVarEbpOffset(arrName)});
				}
				else {
					if (!noName) {
						if (addEsiToLocalAddresses)
							this.addBytes(new Byte[]{3,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[arrName])));//ADD EAX,VALUE @ [ESI+PTR]
						else {
							this.arrayReferences[arrName].Add((UInt32)(this.opcodes.Count+2));
							this.addBytes(new Byte[]{3,5,0,0,0,0});//ADD EAX,VALUE @ PTR
						}
					}
					else {
//						this.addBytes(new Byte[]{3,4,0x24});//ADD EAX,[ESP]
						this.addBytes(new Byte[]{3,0x45,unchecked((Byte)(-(4+(4*recursionDepth))))});//ADD EAX,[EBP+-OFFSET]
					}
						
				}
				if (recursionDepth==0)
					this.addByte(0x50);//PUSH EAX
				
			}
			
			if (noName&&recursionDepth==0) {
				this.addByte(0x58);//POP EAX
				this.addBytes(new Byte[]{0x8B,0xE5});//MOV ESP,EBP
				this.addByte(0x5D);//POP EBP
				this.addByte(0x50);//PUSH EAX
			}
			
			return ParsingStatus.SEARCHING_NAME;
			
		}
		
		/// <summary>
		/// Push a value (constant number,var,array,array indexer) onto the stack
		/// </summary>
		internal Tuple<String,VarType> pushValue (String value,Boolean _gettingAddr=false) {
			
			//HACK:: check var type here
			
			Boolean gettingAddr;
			if (_gettingAddr) gettingAddr=true;
			else {
				gettingAddr=value.StartsWith("$",StringComparison.CurrentCulture);
				if (gettingAddr)
					value=value.Substring(1);
			}
			
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
			else if (value==Parser.THIS_STR) {
				
				if (addEsiToLocalAddresses) {
					int32sToSubtractByFinalOpcodesCount.Add((UInt32)(this.opcodes.Count+2));
					this.addBytes(new Byte[]{0x8D,0x86,0,0,0,0}); //LEA EAX,[ESI+-DWORD OFFSET]
					this.addByte(0x50);//PUSH EAX
				}
				else
					this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes(startingMemAddr)));
				return new Tuple<String,VarType>(className,VarType.CLASS);
				
			}
            else if (constants.ContainsKey(value)) {
                
                this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes(this.constants[value].Item1))); //PUSH DWORD
                return this.constants[value].Item2;

            }
			else if (@struct)
				throw new ParsingError("Can only apply constant values to a struct",this);
			else if (this.isALocalVar(value)) {
				
				Block localVarHomeBlock=this.getLocalVarHomeBlock(value);
				
				this.refEbp(localVarHomeBlock,2);
				if (gettingAddr) {
					this.addBytes(new Byte[]{0x8D,0x45,pseudoStack.getVarEbpOffset(value)}); //LEA EAX,[EBP+-OFFSET]
					this.addByte(0x50); //PUSH EAX
                    return PTR;
				}
				else {
					this.addBytes(new Byte[]{0xFF,0x75,pseudoStack.getVarEbpOffset(value)});
				    return localVarHomeBlock.localVariables[value].Item1;
                }
				
			}
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
                    return PTR;
					
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
					
					this.addBytes(new Byte[]{0x31,0xDB}); //XOR EBX,EBX
					if (addEsiToLocalAddresses)
						this.addBytes(new Byte[]{0x66,0x8B,0x9E}.Concat(BitConverter.GetBytes(this.appendAfterIndex[value]))); //MOV BX,[PTR+ESI]
					else {
						this.variableReferences[value].Add((UInt32)this.opcodes.Count+3);
						this.addBytes(new Byte[]{0x66,0x8B,0x1D,0,0,0,0}); //MOV BX,[PTR]
					}
					this.addByte(0x53); //PUSH EBX
					
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
					
                    if (addEsiToLocalAddresses) {
                        this.addBytes(new Byte[] { 0x8D,0x86}.Concat(BitConverter.GetBytes(appendAfterIndex[value])));//LEA EAX,[ESI+DWORD]
                        this.addByte(0x50);//PUSH EAX
                    }
                    else {
                        this.arrayReferences[value].Add((UInt32)this.opcodes.Count+1);
    					this.addBytes(new Byte[]{0x68,0,0,0,0}); //PUSH DWORD
                    }
					return PTR;
					
				}
				if (addEsiToLocalAddresses)
					this.addBytes(new Byte[]{0xFF,0xB6}.Concat(BitConverter.GetBytes(appendAfterIndex[value])));
				else {
					this.arrayReferences[value].Add((UInt32)this.opcodes.Count+2);
					this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0}); //PUSH DWORD [PTR]
				}
				return new Tuple<String,VarType>(this.arrays[value].Item2,VarType.NATIVE_ARRAY);
				
			}
             else if (staticInstances[ID].ContainsKey(value)) {
                
                var instance=staticInstances[ID][value];
                UInt32 sAddr=PEHeaderFactory.dataSectAddr+instance.Item1;
                if (gettingAddr) {
                    this.addBytes(new Byte[]{0x68 }.Concat(BitConverter.GetBytes(sAddr))); // PUSH DWORD
                    return PTR;
                }
                this.addBytes(new Byte[]{0xFF,0x35 }.Concat(BitConverter.GetBytes(sAddr))); // PUSH DWORD [PTR]
                return instance.Item2;
                       
             }
			else if (this.indicatesMathOperation(value)) {
				
				Console.WriteLine(" ! value: "+value);
				//NOTE:: math is also parsed & calculated at Parser#indexArray and Parser#pushArrValue
				
				if (gettingAddr)
					value='$'+value;
				
				return this.parseMath(value,delegate(String str){ if (this.isArrayIndexer(str))this.pushArrValue(str); else this.pushValue(str); },pushValue=>(this.isArrayIndexer(pushValue))?this.pushArrValue(pushValue):this.pushValue(pushValue));
				
			}
			else if (this.isFuncWithParams(value)) {
				
				this.throwIfAddr(gettingAddr,value);
				
				String funcName=value.Split('(')[0];
				
                  if (staticFunctions[ID].ContainsKey(funcName)) {

                    var function=staticFunctions[ID][funcName];
                    if (function.Item2==null)
                        throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained",this);

                    String unparsedParams0=value.Substring(value.IndexOf('(')+1);
                    List<String>@params0=parseParameters(unparsedParams0);
                    this.CallStaticClassFunc(ID,fileName,funcName,@params0.ToArray());
                    this.addByte(0x50); //PUSH EAX
                    return function.Item2;
                    
                  }
				if (functions[funcName].returnType==null)
					throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained",this);
				
				String unparsedParams=value.Substring(value.IndexOf('(')+1);
				List<String>@params=parseParameters(unparsedParams);
				this.callFunction(funcName,@params.ToArray());
				this.addByte(0x50); //PUSH EAX
				return this.functions[funcName].returnType;
				
			}
             else if (staticFunctions[ID].ContainsKey(value)) {

                var function=staticFunctions[ID][value];
                if (gettingAddr) {
                    this.addBytes(new Byte[]{0xB8 }.Concat(BitConverter.GetBytes(function.Item1)));
                    return FUNC_PTR;
                }
                if (function.Item2==null)
                    throw new ParsingError("Function \""+value+"\" has no return value, therefore its return value can't be obtained",this);
                CallStaticClassFunc(ID,fileName,value,new String[0]);
                this.addByte(0x50); //PUSH EAX
                return function.Item2;

             }
			else if (functions.ContainsKey(value)) {
				
				if (gettingAddr) {
					
					if (addEsiToLocalAddresses) {
						this.int32sToSubtractByFinalOpcodesCount.Add((UInt32)this.opcodes.Count+2);
						this.addBytes(new Byte[]{0x8D,0x86}.Concat(BitConverter.GetBytes(this.functions[value].memAddr)));
						this.addByte(0x50);//PUSH EAX
					}
					else
						this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes((UInt32)(this.functions[value].memAddr))));
					return FUNC_PTR;
					
				}
				
				String funcName=value;
				
				if (functions[funcName].returnType==null)
					throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained",this);
				
				this.callFunction(funcName,new String[0]);
				this.addByte(0x50); //PUSH EAX
				return this.functions[funcName].returnType;
				
			}
			else if (this.keywordMgr.getKeywords().Where(x=>x.type==KeywordType.NATIVE_CALL_WITH_RETURN_VALUE).Select(x=>x.name).Contains(value)) {
				
				this.throwIfAddr(gettingAddr,value);
				
				Keyword[] query=this.keywordMgr.getKeywords().Where(x=>x.type==KeywordType.NATIVE_CALL_WITH_RETURN_VALUE&&x.name==value).ToArray();
				if (query.Length==0)
					throw new ParsingError("Function \""+value+"\" has no return value, therefore its return value can't be obtained",this);
				Keyword kw=query.First();
				this.execKeyword(kw,new String[0]);
				this.addByte(0x50); //PUSH EAX
				return kw.outputType;
				
			}
			else if (value.Contains('(')&&this.keywordMgr.getKeywords().Where(x=>x.type==KeywordType.NATIVE_CALL_WITH_RETURN_VALUE).Select(x=>x.name).Contains(value.Split('(')[0])&&value.Contains(')')&&!value.Substring(value.LastIndexOf(')')+1).Any(x=>this.isMathOperator(x))) {
				
				this.throwIfAddr(gettingAddr,value);
				
				String funcName=value.Split('(')[0];
				
				Keyword[] query=this.keywordMgr.getKeywords().Where(x=>x.type==KeywordType.NATIVE_CALL_WITH_RETURN_VALUE&&x.name==funcName).ToArray();
				if (query.Length==0)
					throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained",this);
				Keyword kw=query.First();
				
				String unparsedParams=value.Substring(value.IndexOf('(')+1);
				List<String>@params=parseParameters(unparsedParams);
				this.execKeyword(kw,@params.ToArray());
				this.addByte(0x50); //PUSH EAX
				return kw.outputType;
				
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
					return PTR;
					
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
				Byte[]chars=new Byte[innerText.Length+1];//+1 = Null Terminator Byte
				UInt16 i=0;
				foreach (Byte ch in innerText.Select(x=>(Byte)x)) {
					
					chars[i]=ch;
					
					++i;
					
				}
				
                  if (InStaticEnvironment()) {
                    this.addBytes(new Byte[]{0xB8 }.Concat(BitConverter.GetBytes((UInt32)(PEHeaderFactory.dataSectAddr+dataSectBytes.Count+appendAfterStaticFunc.Count+5)))); // MOV EAX,DWORD
                    dwordsToIncByOpcodesUntilStaticFuncEnd.Add((UInt32)dataSectBytes.Count()-4);
                    this.addByte(0x50); //PUSH EAX
                    appendAfterStaticFunc.AddRange(chars);
                  }
				else if (addEsiToLocalAddresses) {
					
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
			else if (value.Any(x=>this.accessingClass(x))&&(this.classes.ContainsKey(value.Split(Parser.accessorChar).First())||(this.isALocalVar(value.Split(Parser.accessorChar).First()))||this.pvClassInstanceOrigin.Contains(value.Split(Parser.accessorChar).First())||this.isImportedClass(value.Split(Parser.accessorChar).First())||staticInstances[ID].ContainsKey(value.Split(Parser.accessorChar).First()))) {
                    
				if (clearNextPvOrigin) pvClassInstanceOrigin.Clear();
				
				//HACK:: sub parsing
				List<String>accessors=new List<String>();
				Boolean inQuotes=false;
				Int16 rbb=0,sbb=0;
				StringBuilder sb=new StringBuilder();
				foreach (Char c in value) {
					
					if (c=='"') inQuotes=!inQuotes;
					else if (!inQuotes) {
						
						if (this.beginsParameters(c))++rbb;
						else if (this.endsParameters(c))--rbb;
						else if (c=='[')++sbb;
						else if (c==']')--sbb;
						
						if (rbb<0||sbb<0) Console.WriteLine("Unbalanced "+(sbb<0?"square parentheses":"parentheses")+" in \""+value+'"');
						
					}
					
					if (c!='.') sb.Append(c);
					else if (c=='.'&&inQuotes) sb.Append(c);
					else if (c=='.'&&rbb!=0) sb.Append(c);
					else if (c=='.'&&sbb!=0) sb.Append(c);
					else if (rbb==0&&sbb==0) {
						
						accessors.Add(sb.ToString());
						sb.Clear();
						
					}
					
				}
				accessors.Add(sb.ToString());
				String first=accessors.First();
				Class initialClass;
				
				Boolean local=false,imported=this.isImportedClass(first),staticHome=staticInstances[ID].ContainsKey(first);
				if (this.isALocalVar(first)) {
					
					local=true;
					if (this.getLocalVarHomeBlock(first).localVariables[first].Item1.Item2!=VarType.CLASS)
						throw new ParsingError("Not a class, can't read member of: \""+first+'"',this);
					
					initialClass=this.importedClasses.Where(x=>x.className==this.getLocalVarHomeBlock(first).localVariables[first].Item1.Item1).First();
					
				}
                    else if (imported)
                        initialClass=this.importedClasses.Where(x=>x.className==first).First();
				else if (this.pvClassInstanceOrigin.Count>1) {
					local=this.isALocalVar(pvClassInstanceOrigin.First());
					initialClass=this.moveClassOriginIntoEax(pvClassInstanceOrigin,local);
				}
                  else if (staticHome) initialClass=staticInstances[ID][first].Item4;
				else initialClass=this.classes[first].Item3;
                    
				String pValue=accessors[1];
				Boolean classOriginRecursor=accessors.Count>2&&initialClass.classes.ContainsKey(pValue);
				clearNextPvOrigin=!classOriginRecursor&&pvClassInstanceOrigin.Count==0;
                
                    Console.WriteLine("clearNextPvOrigin: "+clearNextPvOrigin);
				
				if (clearNextPvOrigin)
					pvClassInstanceOrigin=new List<String>(new String[]{first});
				if (this.isALocalVar(first)) {
					if (this.getLocalVarHomeBlock(first)!=this.getCurrentBlock())
						this.refEbp(this.getLocalVarHomeBlock(first),2);
					this.addBytes(new Byte[]{0x8B,0x45,this.pseudoStack.getVarEbpOffset(first)}); //MOV [EBP+-OFFSET],EAX
					
				}
                else if (imported) {
                    if (staticInstances[initialClass.classID].ContainsKey(pValue))
                        this.addBytes(new Byte[]{0xA1}.Concat(BitConverter.GetBytes((UInt32)(PEHeaderFactory.dataSectAddr+staticInstances[initialClass.classID][pValue].Item1)))); // MOV EAX,[PTR]
                    else if (!staticFunctions[initialClass.classID].ContainsKey(pValue)&&!isFuncWithParams(pValue,initialClass,true))
                        throw new ParsingError("Does not exist in \""+initialClass.className+"\": "+pValue,this);
                }
                else if (staticHome) {
                    this.addBytes(new Byte[]{0xA1}.Concat(BitConverter.GetBytes((UInt32)(PEHeaderFactory.dataSectAddr+staticInstances[ID][first].Item1)))); // MOV EAX,[PTR]
               

                }
				else if (this.pvClassInstanceOrigin.Count<2&&!classOriginRecursor) {
					
                          if (addEsiToLocalAddresses)
						this.addBytes(new Byte[]{0x8B,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[first])));//MOV EAX,DWORD[PTR+ESI]
					else  {
						this.addByte(0xA1);//MOV EAX,DWORD[FOLLOWING PTR]
						this.classReferences[first].Add(this.getOpcodesCount());
						this.addBytes(new Byte[]{0,0,0,0});
					}
					
				}

				if (classOriginRecursor) {
					if (pvClassInstanceOrigin.Count==0)
						this.pvClassInstanceOrigin.Add(first);
					this.pvClassInstanceOrigin.Add(pValue);
					Tuple<String,VarType>retVal=this.pushValue(value.Substring(first.Length+1),gettingAddr);
					if (pvClassInstanceOrigin.Count!=0)
						this.pvClassInstanceOrigin.Clear();
					return retVal;
					
				}
                  else if (imported&&staticInstances[initialClass.classID].ContainsKey(pValue)) {
                        
                        var inst=staticInstances[initialClass.classID][pValue];

                        throwIfCantAccess(inst.Item3,pValue,initialClass.path,true);
                        if (initialClass.constants.ContainsKey(pValue)) {
                    
                           this.addBytes(new Byte[]{0x68 }.Concat(BitConverter.GetBytes(initialClass.constants[pValue].Item1))); // PUSH DWORD
                           return initialClass.constants[pValue].Item2;

                        }
                        this.addBytes(new Byte[]{0xBF}.Concat(BitConverter.GetBytes(PEHeaderFactory.dataSectAddr+inst.Item1))); // MOV EDI,DWORD
                        UInt32 sz=this.keywordMgr.getVarTypeByteSize(inst.Item2.Item1);
    				  
                        
                        this.addBytes(sz==1?
                                  new Byte[]{0x31,0xC0, //XOR EAX,EAX
                                    0x8A,7, //MOV AL,[EDI]
                                    0x50}: //PUSH EAX
                                  sz==2?
                                  new Byte[]{0x31,0xC0, //XOR EAX,EAX
                                    0x66,0x8B,7, //MOV AX,[EDI]
                                    0x50}: //PUSH EAX
                                  new Byte[]{0xFF,0x37}/*PUSH DWORD[EDI]*/);
                        return inst.Item2;

                  }
                  else if (imported&&staticFunctions[initialClass.classID].ContainsKey(pValue)) {

                    var function=staticFunctions[initialClass.classID][pValue];
                    throwIfCantAccess(function.Item6,pValue,initialClass.path,true);
                    if (gettingAddr) {
                    
                        this.addByte(5);//ADD EAX,FOLLOWING DWORD
                        this.addBytes(BitConverter.GetBytes(function.Item1)); //DWORD HERE
                        return FUNC_PTR;
                        
                    }
                    Tuple<String,VarType>retType=function.Item2;
                    if (retType==null)
                        throw new ParsingError("Function \""+pValue+"\" has no return value, therefore its return value can't be obtained",this);
                    Console.WriteLine("ORIGIN: "+merge(pvClassInstanceOrigin,"."));
                    
                    this.CallStaticClassFunc(initialClass,pValue,new String[0]);
                    this.addByte(0x50);//PUSH EAX
                    
                    return retType;

                  }
                  else if (imported&&isFuncWithParams(pValue,initialClass,true)) {

                    String funcName=pValue.Split('(')[0];
                    var function=staticFunctions[initialClass.classID][funcName];
                    throwIfCantAccess(function.Item6,pValue,initialClass.path,true);
                    this.throwIfAddr(gettingAddr,value);
                    
                    if (function.Item2==null)
                        throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained",this);
                    
                    String unparsedParams=pValue.Substring(pValue.IndexOf('(')+1);
                    Byte roundBracketBalance=1,sharpBracketBalance=0;
                    List<String>@params=new List<String>();
                    StringBuilder paramBuilder=new StringBuilder();
                    //HACK:: sub parsing
                    inQuotes=false;
                    foreach (Char c in unparsedParams) {
                        
                        if (c=='"') inQuotes=!inQuotes;
                        
                        if (!inQuotes) {
                            if (c=='(') ++roundBracketBalance;
                            else if (c==')') --roundBracketBalance;
                            else if (c=='<') ++sharpBracketBalance;
                            else if (c=='>') --sharpBracketBalance;
                            else if (c==','&&roundBracketBalance==1&&sharpBracketBalance==0) {

                                @params.Add(paramBuilder.ToString());
                                paramBuilder.Clear();
                                
                            }
                        }
                        
                        if (roundBracketBalance==0) {
                            @params.Add(paramBuilder.ToString());
                            break;
                        }
                        else if (!(c==','&&roundBracketBalance==1&&sharpBracketBalance==0)) paramBuilder.Append(c);
                        
                    }
                    Console.WriteLine("unparsedParams: \""+unparsedParams+'"');
                    Console.WriteLine("3");
                    this.CallStaticClassFunc(initialClass,funcName,@params.ToArray());
                    this.addByte(0x50); //PUSH EAX
                    return function.Item2;

                  }
				else if (initialClass.variables.ContainsKey(pValue)) {


                        throwIfCantAccess(initialClass.variables[pValue].Item3,pValue,initialClass.path,true);
                        throwIfStatic(initialClass.variables[pValue].Item3,pValue);
    				  if (initialClass.constants.ContainsKey(pValue)) {
                    
                           this.addBytes(new Byte[]{0x68 }.Concat(BitConverter.GetBytes(initialClass.constants[pValue].Item1))); // PUSH DWORD
                           return initialClass.constants[pValue].Item2;

                        }
                        if (initialClass.variables[pValue].Item3.HasFlag(Modifier.PRIVATE)||initialClass.parserUsed.@struct) {
                                                                         
	    					this.addByte(5);//ADD EAX,FOLLOWING DWORD
	    					this.addBytes(BitConverter.GetBytes(initialClass.variables[pValue].Item1+initialClass.opcodePortionByteSize)); //DWORD HERE
	                    }
                        else {
                        	// push esi, push edx, xor esi esi, add eax edx, add eax [eax+esi+fn.instanceId*4], call eax, pop esi, pop edx
                        	this.addBytes(new Byte[]{0x56,0x52,0x33,0xF6,0xFF,0xD0,3,0xC2,3,0x84,0x30}.Concat(BitConverter.GetBytes(initialClass.variables[pValue].Item4*4)).Concat(new Byte[]{0x5A,0x5E}));
                  		}
    					this.addBytes(new Byte[]{0x8B,0xF8}); //MOV EDI,EAX
    					UInt32 sz=this.keywordMgr.getVarTypeByteSize(initialClass.variables[pValue].Item2);
    					if (gettingAddr) {
    						this.addByte(0x57);//PUSH EDI
                            return PTR;
                        }
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

                    throwIfCantAccess(initialClass.classes[pValue].Item4,pValue,initialClass.path,true);
                    throwIfStatic(initialClass.classes[pValue].Item4,pValue);
				    if (initialClass.constants.ContainsKey(pValue)) {
                    
                           this.addBytes(new Byte[]{0x68 }.Concat(BitConverter.GetBytes(initialClass.constants[pValue].Item1)));
                           return initialClass.constants[pValue].Item2;

                        }
                    if (initialClass.classes[pValue].Item4.HasFlag(Modifier.PRIVATE)||initialClass.parserUsed.@struct) {
                    	
						this.addByte(5);//ADD EAX,FOLLOWING DWORD
						this.addBytes(BitConverter.GetBytes(initialClass.classes[pValue].Item1+initialClass.opcodePortionByteSize)); //DWORD HERE
				
                    }   
                    else {
                    	// push esi, push edx, xor esi esi, add eax edx, add eax [eax+esi+fn.instanceId*4], call eax, pop esi, pop edx
            			this.addBytes(new Byte[]{0x56,0x52,0x33,0xF6,0xFF,0xD0,3,0xC2,3,0x84,0x30}.Concat(BitConverter.GetBytes(initialClass.classes[pValue].Item5*4)).Concat(new Byte[]{0x5A,0x5E}));
                    }
    				
					this.addBytes(new Byte[]{0x8B,0xF8}); //MOV EDI,EAX
					UInt32 sz=this.keywordMgr.getVarTypeByteSize(initialClass.classes[pValue].Item2);
					if (gettingAddr) {
						this.addByte(0x57);//PUSH EDI
						return PTR;
					}
					this.addBytes(new Byte[]{0xFF,0x37}/*PUSH DWORD[EDI]*/);
					
					UInt32 size=keywordMgr.getVarTypeByteSize(varType);
					return new Tuple<String,VarType>(initialClass.classes[pValue].Item2,VarType.CLASS);
					
				}
				else if (initialClass.functions.ContainsKey(pValue)) {
					
                    throwIfCantAccess(initialClass.functions[pValue].modifier,pValue,initialClass.path,true);
                    throwIfStatic(initialClass.functions[pValue].modifier,pValue);
					if (gettingAddr) {
					
						this.addByte(5);//ADD EAX,FOLLOWING DWORD
						this.addBytes(BitConverter.GetBytes(initialClass.functions[pValue].memAddr)); //DWORD HERE
						this.addByte(0x50);//PUSH EAX
						return FUNC_PTR;
						
					}
					Tuple<String,VarType>retType=initialClass.functions[pValue].returnType;
					Console.WriteLine("-------------> Getting Address: "+gettingAddr);
					if (retType==null)
						throw new ParsingError("Function \""+pValue+"\" has no return value, therefore its return value can't be obtained",this);
					Console.WriteLine("ORIGIN: "+merge(pvClassInstanceOrigin,"."));
					
					this.callClassFunc(pvClassInstanceOrigin,pValue,new String[0],local);
					this.addByte(0x50);//PUSH EAX
					
					return retType;
					
				}
				else if (this.isFuncWithParams(pValue,initialClass)) {
                    
                    String funcName=pValue.Split('(')[0];

                    throwIfCantAccess(initialClass.functions[funcName].modifier,funcName,initialClass.path,true);
                    throwIfStatic(initialClass.functions[funcName].modifier,funcName);
					this.throwIfAddr(gettingAddr,value);
					
					
					if (initialClass.functions[funcName].returnType==null)
						throw new ParsingError("Function \""+funcName+"\" has no return value, therefore its return value can't be obtained",this);
					
					String unparsedParams=pValue.Substring(pValue.IndexOf('(')+1);
					Byte roundBracketBalance=1,sharpBracketBalance=0;
					List<String>@params=new List<String>();
					StringBuilder paramBuilder=new StringBuilder();
					//HACK:: sub parsing
					inQuotes=false;
					foreach (Char c in unparsedParams) {
						
						if (c=='"') inQuotes=!inQuotes;
						
						if (!inQuotes) {
							if (c=='(') ++roundBracketBalance;
							else if (c==')') --roundBracketBalance;
							else if (c=='<') ++sharpBracketBalance;
							else if (c=='>') --sharpBracketBalance;
							else if (c==','&&roundBracketBalance==1&&sharpBracketBalance==0) {
								
								@params.Add(paramBuilder.ToString());
								paramBuilder.Clear();
								
							}
						}
						
						if (roundBracketBalance==0) {
							@params.Add(paramBuilder.ToString());
							break;
						}
						else if (!(c==','&&roundBracketBalance==1&&sharpBracketBalance==0)) paramBuilder.Append(c);
						
					}
					Console.WriteLine("unparsedParams: \""+unparsedParams+'"');
                    Console.WriteLine("3");
					this.callClassFunc(pvClassInstanceOrigin,funcName,@params.ToArray(),local,true,initialClass);
					this.addByte(0x50); //PUSH EAX
					return initialClass.functions[funcName].returnType;
					
				}
				else if (this.isArrayIndexer(pValue)) {

					String arrName=pValue.Split('[')[0];
                    throwIfCantAccess(initialClass.arrays[pValue].Item4,pValue,initialClass.path,true);
                    throwIfStatic(initialClass.arrays[pValue].Item4,pValue);
					if (!(initialClass.arrays.ContainsKey(arrName)))
						throw new ParsingError("Array does not exist in \""+initialClass.className+"\": \""+arrName+'"',this);
					if (initialClass.arrays[arrName].Item4.HasFlag(Modifier.PRIVATE)||initialClass.parserUsed.@struct) {
						this.addByte(5);//ADD EAX,FOLLOWING DWORD
						this.addBytes(BitConverter.GetBytes(initialClass.arrays[arrName].Item1+initialClass.opcodePortionByteSize)); //DWORD HERE
					}
					else {
						// push esi, push edx, xor esi esi, add eax edx, add eax [eax+esi+fn.instanceId*4], call eax, pop esi, pop edx
            			this.addBytes(new Byte[]{0x56,0x52,0x33,0xF6,0xFF,0xD0,3,0xC2,3,0x84,0x30}.Concat(BitConverter.GetBytes(initialClass.arrays[arrName].Item5*4)).Concat(new Byte[]{0x5A,0x5E}));
					}
					if (gettingAddr) {
						
						this.addByte(0x50);//PUSH EAX
						return PTR;
						
						
					}
					this.addBytes(new Byte[]{0x8B,0}); //MOV EAX,[EAX]
					return this.pushArrValue(value,gettingAddr,true,this.keywordMgr.getVarTypeByteSize(initialClass.arrays[arrName].Item2));
					
				}
				else if (initialClass.arrays.ContainsKey(pValue)) {

                    throwIfCantAccess(initialClass.arrays[pValue].Item4,pValue,initialClass.path,true);
                    throwIfStatic(initialClass.arrays[pValue].Item4,pValue);
				  if (initialClass.constants.ContainsKey(pValue)) {
                
                       this.addBytes(new Byte[]{0x68 }.Concat(BitConverter.GetBytes(initialClass.constants[pValue].Item1)));
                       return initialClass.constants[pValue].Item2;

                    }
                    if (initialClass.arrays[pValue].Item4.HasFlag(Modifier.PRIVATE)||initialClass.parserUsed.@struct) {
                    	this.addByte(5);//ADD EAX,FOLLOWING DWORD
						this.addBytes(BitConverter.GetBytes(initialClass.arrays[pValue].Item1+initialClass.opcodePortionByteSize)); //DWORD HERE		
                    }
                    else {
                    	// push esi, push edx, xor esi esi, add eax edx, add eax [eax+esi+fn.instanceId*4], call eax, pop esi, pop edx
            			this.addBytes(new Byte[]{0x56,0x52,0x33,0xF6,0xFF,0xD0,3,0xC2,3,0x84,0x30}.Concat(BitConverter.GetBytes(initialClass.classes[pValue].Item5*4)).Concat(new Byte[]{0x5A,0x5E}));
                    }
					
					if (gettingAddr) {
						this.addByte(0x50);//PUSH EAX
						return PTR;
					}
					else {
						this.addBytes(new Byte[]{0xFF,0x30}); //PUSH [EAX]
						return new Tuple<String,VarType>(initialClass.arrays[pValue].Item2,VarType.NATIVE_ARRAY);
					}
						
					
					
				}
				else throw new ParsingError("Item \""+pValue+"\" is not accessible and may not exist from \""+initialClass.className+'"',this);
				
			}
			
			else if (this.tryCheckIfValidArrayIndexer(value)) { 
				
				return this.pushArrValue(value,gettingAddr);
				
			}
			
			else throw new ParsingError("Invalid value: \""+value+'"',this);
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
			foreach (KeyValuePair<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>> array in this.arrays) {
				
				if (doneMemAddrs.Contains(array.Value.Item1))
					continue;
				
				this.arrayReferences[array.Key].Add((UInt32)this.opcodes.Count+2);
				this.addBytes(new Byte[]{0xFF,0x35,0,0,0,0});//push pMemory
				this.addBytes(new Byte[]{0x6A,0});//push Flags
				this.pushProcessHeapVar();//push hHeap
				ReferenceRefdFunc(HF,2);
				this.addBytes(new Byte[]{0xFF,0x15,0,0,0,0});
				doneMemAddrs.Add(array.Value.Item1);
				
			}
			
		}
		
		private void execKeyword (Keyword kw,String[] @params) {
			
			KeywordResult res=kw.execute(this,@params);
			
			this.status=res.newStatus;
			this.addBytes(res.newOpcodes);
			if (res.action!=null)
				res.action();
			
			if (kw.type==KeywordType.ASSIGNMENT||kw.type==KeywordType.DECREMENT||kw.type==KeywordType.INCREMENT)
				this.resetLastReferencedVar();
			
		}
		
		internal void addBlock (Block block,Byte setExpectsBlock=1,Boolean staticIncl=false) {
			
			block.nestedLevel=this.nestedLevel;
			if (this.blocks.Count!=0)
				this.blocks.Keys.Last().addChild(block);
			this.blocks.Add(block,0);
			this.blockBracketBalances.Add(block,0);
			this.blockVariablesCount.Add(block,0);
			this.blockAddrBeforeAppendingReferences.Add(block,new List<Tuple<UInt32,Int16>>());
			this.blocksClosed.Add(block,new List<Block>());
			this.localVarEBPPositionsToOffset.Add(block,new List<UInt32>());
			this.setExpectsBlock=setExpectsBlock;
			//NOTE:: first param to ENTER is a word(short), second one is a byte
			if (block.addEnterAutomatically)
				this.enterBlock(block,0,staticIncl);
			
			Console.WriteLine("Blocks count: "+this.blocks.Count.ToString());
			++this.nestedLevel;
			
		}
		
		private void onBlockClosed (Block block) {
			
			Console.WriteLine("onBlockClosed called (Hash code: "+block.GetHashCode().ToString()+')');
			Console.WriteLine("Mem addr @ start of onBlockClosed: "+this.memAddress.ToString());
			
			this.lastBlockClosed=block;
              Boolean staticFunc=InStaticEnvironment();
              UInt32 endAddress=GetStaticInclusiveAddress();
			
			if (block.shouldXOREAX) this.addBytes(new Byte[]{0x31,0xC0}); //XOR EAX,EAX
			UInt32 beforeAppendingMemAddr=endAddress;
			
			this.addByte(0xC9); // LEAVE
			this.pseudoStack.pop((UInt16)(1+block.localVariables.Count));//pseudo pop ebp and local vars
			this.addBytes(block.opcodesToAddOnBlockEnd);
			
			if (block.onBlockEnd!=null)
				block.onBlockEnd.Invoke();
              endAddress=GetStaticInclusiveAddress(staticFunc);
			
			this.blocks[block]=endAddress;
			Console.WriteLine("onBlockClosed: "+endAddress.ToString("X")+','+block.startMemAddr.ToString("X"));
			//Console.WriteLine(memAddress.ToString("X")+'-'+block.startMemAddr.ToString("X")+'='+(memAddress-block.startMemAddr).ToString());
			
			
			Byte[] memAddr=BitConverter.GetBytes((Int32)endAddress-(Int32)block.startMemAddr);
			Byte i;
             foreach (Block b in this.blocks.Select(x=>x.Key)) {
				
				Console.WriteLine("Found block: "+b.startMemAddr.ToString("X")+" (This block: "+block.startMemAddr.ToString("X")+')');
				
			}
			foreach (OpcodeIndexReference index in block.blockMemPositions) {
				
				Console.WriteLine("Block Mem Pos @ "+index.ToString());
                  Console.WriteLine("Data sect bytes #: "+dataSectBytes.Count);
				
				i=0;
				
				while (i!=4) {
					
                     SetStaticInclusiveByte(index,memAddr[i],i);
                     ++i;
					
				}
				
				
			}
              Console.WriteLine("RVA positions ... ");
			foreach (Tuple<UInt32,UInt32>RVAindex in block.blockRVAPositions) {
				
				memAddr=BitConverter.GetBytes((Int32)GetStaticInclusiveAddress(staticFunc)-(Int32)RVAindex.Item2);
				
				i=0;
				
				while (i!=4) {

					if (!staticFunc)
                        opcodes[(Int32)RVAindex.Item1+i]=memAddr[i];
                     else
                          dataSectBytes[(Int32)RVAindex.Item1+i]=memAddr[i];
					++i; 
					
				}
				
			}
              Console.WriteLine("Indexes and offsets ... ");
			foreach (Tuple<UInt32,Int16>indexAndOffset in this.blockAddrBeforeAppendingReferences[block]) {
				
			  i=0;
		      Int32 addrOfJump;
               if (!staticFunc) addrOfJump=BitConverter.ToInt32(new Byte[]{this.opcodes[(Int32)indexAndOffset.Item1],this.opcodes[(Int32)indexAndOffset.Item1+1],this.opcodes[(Int32)indexAndOffset.Item1+2],this.opcodes[(Int32)indexAndOffset.Item1+3]},0);
			  else addrOfJump=BitConverter.ToInt32(new Byte[]{dataSectBytes[(Int32)indexAndOffset.Item1],dataSectBytes[(Int32)indexAndOffset.Item1+1],dataSectBytes[(Int32)indexAndOffset.Item1+2],dataSectBytes[(Int32)indexAndOffset.Item1+3]},0);
                Byte[]memAddr0=BitConverter.GetBytes((Int32)beforeAppendingMemAddr-(addrOfJump+indexAndOffset.Item2)-2);//minus constant 2 at end to jump over XOR EAX,EAX
				while (i!=4) {
					

                     //SetStaticInclusiveByte(index
                     if (!staticFunc)
                        opcodes[(Int32)indexAndOffset.Item1+i]=memAddr0[i];
                     else
                        dataSectBytes[(Int32)indexAndOffset.Item1+i]=memAddr0[i];
					++i;
					
				}
				
			}
			
			Console.WriteLine("blockBracketBalances ct: "+blockBracketBalances.Count.ToString());
			this.blocks.Remove(block);
			if (this.blocks.Count!=0) {
				Block parentBlock=this.blocks.Last().Key;
				this.blocksClosed[parentBlock].Add(block);
				this.blocksClosed[parentBlock].AddRange(this.blocksClosed[block]);
			}
			this.blockBracketBalances.Remove(block);
			Byte[]varCt=BitConverter.GetBytes((UInt16)(this.blockVariablesCount[block]*4));
             if (staticFunc) {
                Parser.dataSectBytes[(Int32)this.enterPositions[block]]=varCt[0];
                Parser.dataSectBytes[(Int32)this.enterPositions[block]+1]=varCt[1];
             }
             else {
			    this.opcodes[(Int32)this.enterPositions[block]]=varCt[0];
			    this.opcodes[(Int32)this.enterPositions[block]+1]=varCt[1];
             }
			this.blockVariablesCount.Remove(block);
			this.enterPositions.Remove(block);
			this.blocksClosed.Remove(block);
			foreach (String str in this.setArrayValueFuncPtrs.Keys.ToArray().Where(x=>block.restoreArraySetValueFuncs.Contains(x)))
				this.setArrayValueFuncPtrs.Remove(str);
			if (this.blocks.Count==0)
				this.localVarEBPPositionsToOffset.Clear();
			Console.WriteLine("blockBracketBalances post ct: "+blockBracketBalances.Count.ToString());
			Console.WriteLine("Searching again: ");
			foreach (Block b in this.blocks.Select(x=>x.Key)) {
				
				Console.WriteLine("Found block: "+b.startMemAddr.ToString("X")+" (This block: "+block.startMemAddr.ToString("X")+", Same block: "+(b==block).ToString()+')');
				
			}
			Console.WriteLine("New Block No.: "+blockBracketBalances.Count.ToString());
			--this.nestedLevel;
			
			if (block.afterBlockClosedOpcodes!=null)
				this.addBytes(block.afterBlockClosedOpcodes);

              if (block.afterBlockClosedFunc!=null)
                  block.afterBlockClosedFunc();
			
			Console.WriteLine("Mem addr @ end of onBlockClosed: "+this.memAddress.ToString("X"));
//			this.debugLine("block start addr:"+block.startMemAddr.ToString("X"));
			
		}
		
		private void updateBlockBalances (Char c) {
			
			Console.WriteLine(this.blockBracketBalances.Count.ToString());
			Dictionary<Block,UInt16>newDict=new Dictionary<Block,UInt16>(this.blockBracketBalances.Count);
			Block toClose=null;
			foreach (KeyValuePair<Block,UInt16> kvp in this.blockBracketBalances) {
				
				if (!kvp.Key.hasParentheses)
					continue;
				
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
				throw new ParsingError("Function does not exist: \""+functionName+'"',this);
			
			Boolean restoreEsiCondition=functions[functionName].functionType==FunctionType.DLL_REFERENCED&&addEsiToLocalAddresses&&inFunction;
            if (!functions[functionName].modifier.HasFlag(Modifier.STATIC)&&InStaticEnvironment()&&functions[functionName].functionType!=FunctionType.DLL_REFERENCED)
                throw new ParsingError("Can't access non-static function from a static environment (\""+functionName+"\" was called)",this);
			
			Console.WriteLine(" == callFunction: "+functionName+" == ");
			foreach (String str in @params)
				Console.WriteLine(str);
			Console.WriteLine(" == total params: "+@params.Length.ToString()+ " == ");
			
			
			foreach (String s in @params) {
				
				Console.WriteLine(" P- "+s);
				
			}
			
			if (this.functions[functionName].expectedParameterCount!=@params.Length)
				throw new ParsingError("Expected \""+this.functions[functionName].expectedParameterCount.ToString()+"\" parameters for \""+functionName+"\", got \""+@params.Length+'"',this);
			
			if (restoreEsiCondition) {
				pseudoStack.push(new EsiPtr());
				this.addByte(0x56);//PUSH ESI
			}
			
			if (this.functions[functionName].parameterTypes.Count!=0) {
				UInt16 i=(UInt16)(this.functions[functionName].parameterTypes.Count-1);
				foreach (String str in @params.Reverse()) {
					
					Console.WriteLine("Pushing: "+str);
					this.tryConvertVars(vtToTpl(this.functions[functionName].parameterTypes[i]),this.pushValue(str),str);
					if (i==0) break;
					--i;
					
				}
			}
			
			if (restoreEsiCondition) {
				this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+2));
				this.addBytes(new Byte[]{0x8B,0x75,this.pseudoStack.getLatestEsiOffset()}); //MOV ESI,[EBP+-OFFSET]
			}
			
			this.addBytes((this.functions[functionName].functionType==FunctionType.SUNSET)?new Byte[]{0xE8,0,0,0,0}:new Byte[]{0xFF,0x15,0,0,0,0}); //CALL Mem Addr
			if (this.functions[functionName].functionType==FunctionType.SUNSET)
				this.functionReferences[functionName].Add(new Tuple<UInt32,UInt32>((UInt32)opcodes.Count-4,this.memAddress));
			else
                  ReferenceRefdFunc(functionName,-4);
			if (this.functions[functionName].callingConvention==CallingConvention.Cdecl)
				this.addBytes(new Byte[]{0x81,0xC4}.Concat(BitConverter.GetBytes((UInt32)this.functions[functionName].expectedParameterCount*4)));
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
					Console.WriteLine("Filling function ref @ "+tpl.Item1.ToString()+": "+this.functions[funcName].memAddr.ToString("X")+'-'+tpl.Item2.ToString("X")+'='+((Int32)this.functions[funcName].memAddr-(Int32)tpl.Item2).ToString());
					
					if (this.functions[funcName].functionType==FunctionType.SUNSET)
						memAddr=BitConverter.GetBytes(((Int32)this.functions[funcName].memAddr-(Int32)tpl.Item2));
					else 
						memAddr=BitConverter.GetBytes(this.functions[funcName].memAddr);
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
        internal OpcodeIndexReference GetStaticInclusiveOpcodesCount (Int64 offset) { 

            if (!InStaticEnvironment())
                return OpcodeIndexReference.NewCodeSectRef((UInt32)(this.opcodes.Count+offset));
            else
                return OpcodeIndexReference.NewDataSectRef((UInt32)(Parser.dataSectBytes.Count+offset));

        }

		internal OpcodeIndexReference GetStaticInclusiveOpcodesCount () {  return GetStaticInclusiveOpcodesCount(0); }
		
		internal Tuple<String,VarType> getVarType (String value) {
			
			Console.WriteLine("getVarType: \""+value+'"');
			
			//HACK:: check var type here
			if (!pvtNull()) {
				if (pvtContainsKey(value))
					return pvtGet(value);
				else if (value.EndsWith("#")) {
				    String substr=value.Substring(0,value.Length-1);
				    if(pvtContainsKey(substr))
						return new Tuple<String,VarType>(pvtGet(substr).Item1,VarType.NATIVE_ARRAY);
				}
			}

            if (acknowledgements.ContainsKey(value))
			    return new Tuple<String,VarType>(value,acknowledgements[value].Item2);
			else if (value.EndsWith("#")) {
                String substr=value.Substring(0,value.Length-1);
                if(acknowledgements.ContainsKey(substr))
                    return new Tuple<String,VarType>(substr,VarType.NATIVE_ARRAY);
            }

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
				
				foreach (String str in keywordMgr.classWords) {
				
					if (newValue==str)
						return new Tuple<String,VarType>(str,VarType.NATIVE_ARRAY);
					
				}
				
			}
			
			//CLASS
			if (this.containsImportedClass(value)) {
				
				return new Tuple<String,VarType>(value,VarType.CLASS);
				
			}
			
			throw new ParsingError("Not a var type: \""+value+'"',this);
				
			
		}
		
		/// <summary>
		/// Sets new parsing status
		/// </summary>
		private void exec (Executor executor,String[]@params) {
			
			Console.WriteLine("Exec: ");
			foreach (String s in @params)
				Console.WriteLine(" - "+s);
			
			if (!(String.IsNullOrEmpty(executor.func)))
				this.callFunction(executor.func,@params);
			else if (executor.kw!=null)
				this.execKeyword(executor.kw,@params);
			else if (executor.classFunc!=null) {
				this.callClassFunc(executor.classFunc,@params);
				this.lastReferencedClassInstance.Clear();
			}
              else if (executor.externalStaticFunc!=null) {
                   this.CallStaticClassFunc(executor.externalStaticFunc.Item1,executor.externalStaticFunc.Item2,@params);
                   this.lastReferencedClassInstance.Clear();
              }
              else if (executor.internalStaticFunc!=null)
                   this.CallStaticClassFunc(executor.internalStaticFunc.Item1,executor.internalStaticFunc.Item2,executor.internalStaticFunc.Item3,@params);
			
		}
		
		internal void tryConvertVars (Tuple<String,VarType>to,Tuple<String,VarType>from,String refVar) {
			
			//HACK:: check var type here
  
			Console.WriteLine("tryConvertVars: "+to.Item1+','+to.Item2.ToString()+" - "+from.Item1+','+from.Item2.ToString());
			
			if (from.Item2==VarType.NATIVE_VARIABLE) {
				if (keywordMgr.getVarTypeByteSize(to.Item1)<keywordMgr.getVarTypeByteSize(from.Item1))
					throw new ParsingError("Can't convert \""+to.Item1+"\" to \""+from.Item1+'"',this);
            }
            if (to.Item2==VarType.NATIVE_ARRAY_INDEXER) {
				tryConvertVars(getVarType(to.Item1),from,refVar);
            	return;
            }
			if (from.Item2==VarType.NATIVE_ARRAY_INDEXER) {
				tryConvertVars(to,getVarType(from.Item1),refVar);
            	return;
            }
            if (from.Item2==VarType.CLASS&&to.Item2==VarType.CLASS&&from.Item1!=to.Item1) {
            	// if from inherits to...
            	//   then its ok to convert but the memory addresses wont align so that has to be accounted for in the code.
            	Class parent=importedClasses.Where(x=>x.className==to.Item1).First(),child=importedClasses.Where(x=>x.className==from.Item1).First();
            	if (!child.inheritedClasses.Contains(parent))
            		throw new ParsingError("Cannot convert class \""+from.Item1+"\" to \""+to.Item1+'"',this);
            }
			else if (from.Item2!=VarType.NONE&&((from.Item2==VarType.CLASS&&to.Item2!=VarType.CLASS)||(to.Item2==VarType.CLASS&&from.Item2!=VarType.CLASS)))
				throw new ParsingError("Can't cross convert classes with other var types! (\""+from.Item1+"\" to \""+to.Item1+"\")",this);
			else if (from.Item2==VarType.NATIVE_VARIABLE&&to.Item2==VarType.NATIVE_VARIABLE&&(from.Item1==KWString.constName||to.Item1==KWString.constName)&&from.Item1!=to.Item1)
				throw new ParsingError("Can't convert \""+from.Item1+"\" to \""+to.Item1+'"',this);
			else if (from.Item2!=to.Item2&&keywordMgr.getVarTypeByteSize(to.Item1)!=4)// What this does is allow pointers of native arrays etc to be moved into native integers!
				throw new ParsingError("Can't convert \""+to.Item2.ToString()+"\" of \""+to.Item1.ToString()+"\" to \""+from.Item2.ToString()+"\" of \""+from.Item1+'"',this);
			else if ((acknowledgements.ContainsKey(from.Item1))&&from.Item1!=to.Item1)
                throw new ParsingError("Can't convert \""+from.Item1+"\" to \""+to.Item1+"\", did you mean to cast(x,y)?",this);
            
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
				throw new ParsingError("The name \""+varName+"\" is already in use",this);

    		if (!(currentMods.hasAccessorModifier()))
                    currentMods=currentMods|Modifier.PRIVATE;
    		
			

			this.tryIncreaseBlockVarCount();
    			
            var vt=new Tuple<Tuple<String,VarType>>(new Tuple<String,VarType>(this.varType,VarType.CLASS));
            Class cl=this.importedClasses.Where(x=>x.className==this.varType).First();
            if (currentMods.HasFlag(Modifier.STATIC)) {
                
                staticInstances[ID].Add(varName,new Tuple<UInt32,Tuple<String,VarType>,Modifier,Class>((UInt32)dataSectBytes.Count,vt.Item1,currentMods,cl));
                dataSectBytes.AddRange(new Byte[4]);

            }
			else if (this.blocks.Count==0) {//not local var
            	Boolean isPrivate=currentMods.HasFlag(Modifier.PRIVATE);
				this.defineTimeOrder.Add(varName);
                UInt32 addr=memAddress+(UInt32)appendAfter.Count;
                this.classes.Add(varName,new Tuple<UInt32,String,Class,Modifier,UInt32>(addr,this.varType,cl,currentMods,(isPrivate?0:instanceID)));
                if (this.inhClassesToDefine.ContainsKey(varName)) {
                	Int32 idx=(Int32)this.inhClassesToDefine[varName].Item5;
					var pc=this.classes[varName];
					this.classes[varName]=new Tuple<uint, string, Class, Modifier, uint>(pc.Item1,pc.Item2,pc.Item3,pc.Item4,(UInt32)idx);
	        		instanceTable.RemoveAt(idx);
	        		instanceTable.Insert(idx,(UInt32)(appendAfter.Count));
	        		if (!currentMods.Equals(inhClassesToDefine[varName].Item4)) throw new ParsingError("Expected \""+varName+"\" to be defined with mods \""+ModsToLegibleString(inhClassesToDefine[varName].Item4)+'"',this);
					if (this.varType!=inhClassesToDefine[varName].Item2) throw new ParsingError("Expected \""+varName+"\" to be defined of type \""+inhClassesToDefine[varName].Item2+'"',this);
					inhClassesToDefine.Remove(varName);
					Console.WriteLine(varName+" successfully being defined as a inherited class.");
				}else { 
                	++instanceID;
                	instanceTable.Add((UInt32)appendAfter.Count); // ....
                }
				this.appendAfterIndex.Add(varName,(UInt32)appendAfter.Count);
				this.appendAfter.AddRange(new Byte[4]);
				this.classReferences.Add(varName,new List<UInt32>());
			}
			else {//should be local var
				this.pseudoStack.push(new LocalVar(varName));
				this.getCurrentBlock().localVariables.Add(varName,vt);
				this.lastReferencedVariableIsLocal=true;
				this.offsetEBPs(4);
			}
            if (currentMods.HasFlag(Modifier.CONSTANT)) {
                nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT };
                constantBeingSet=varName;
                constants.Add(varName,new Tuple<uint, Tuple<string, VarType>>(0,null));
            }
			this.lastReferencedVariable=varName;
            currentMods=Modifier.NONE;
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void tryIncreaseBlockVarCount () {
			
			Block b;
			if (this.tryGetCurrentBlock(out b))
				++this.blockVariablesCount[b];
			
		}
		
		internal Block getCurrentBlock () { return this.blocks.Last().Key; }
		
		private void enterBlock (Block block,Int32 offset=0,Boolean staticIncl=false) {
			
              if (staticIncl) {
                this.enterPositions.Add(block,(UInt32)(Parser.dataSectBytes.Count+1+offset));
                dataSectBytes.AddRange(new Byte[]{0xC8,0,0,0 });
              }
			else {
                this.enterPositions.Add(block,(UInt32)(this.opcodes.Count+1+offset));
                this.addBytes(new Byte[]{0xC8,0,0,0}); //ENTER 0,0 (first parameter 0 should be overwritten later if local variables are introduced)
              }
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
			
			this.addBytes(new Byte[]{0xE9}.Concat(BitConverter.GetBytes((Int32)gotoMemAddr-(Int32)(GetStaticInclusiveAddress()+5))));
			
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
					if (!incBalanceChars.Contains(previousCharacter)&&!this.isMathOperator(previousCharacter)&&(this.isValidNameChar(previousCharacter))) {
						
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
		
		/// <param name="noName">If true, the memory address to the array should be in EAX, and arrMemSize should be set</param>
		private Tuple<String,VarType> pushArrValue (String value,Boolean gettingAddr=false,Boolean noName=false,UInt32 arrMemSize=0) {
			
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
			this.indexArray(arrName,(sub.EndsWith("]"))?String.Concat(sub.Take(sub.Length-1)):sub,0,noName,arrMemSize);
			
			UInt32 varTypeByteSize=(arrMemSize!=0?arrMemSize:(this.isALocalVar(arrName))?this.keywordMgr.getVarTypeByteSize(this.getLocalVarHomeBlock(arrName).localVariables[arrName].Item1.Item1):this.keywordMgr.getVarTypeByteSize(this.arrays[arrName].Item2));
			String varType=(this.isALocalVar(arrName))?this.getLocalVarHomeBlock(arrName).localVariables[arrName].Item1.Item1:this.arrays[arrName].Item2;
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
					throw new ParsingError("Unexpected: \""+slack+'"',this);
				
			}
			
			this.writeStrOpcodes("PAV E");
			
			return new Tuple<String,VarType>(varType,VarType.NATIVE_ARRAY_INDEXER);
			
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
					throw new ParsingError("Unexpected math operator \""+op+"\" (?!)",this);
				
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
		
		internal Int32 getAppendAfterCount () {
			
			return this.appendAfter.Count; 
			
		}
		
		internal Boolean containsImportedClass (String name) {
			
			return this.importedClasses.Select(x=>x.className).Contains(name);
			
		}
		
		internal Dictionary<String,Tuple<UInt32,String,Modifier,UInt32>>getVariables () {
			
			return this.variables;
			
		}
		
		internal Dictionary<String,Tuple<UInt32,String,Class,Modifier,UInt32>>getClasses () {
			
			return this.classes;
			
		}
		
		#if DEBUG
		
		internal void debugLine (String str) {
			
			Console.WriteLine(str);
			Console.ReadKey(true);
			
		}
		
		#endif
		
		internal Boolean isFuncWithParams (String value,Class sendingClass=null,Boolean sendingClassStatic=false) {

			return value.Contains('(')&&(sendingClass==null?functions.ContainsKey(value.Split('(')[0]):sendingClassStatic?staticFunctions[sendingClass.classID].ContainsKey(value.Split('(')[0])&&value.Contains(')')&&!value.Substring(value.LastIndexOf(')')+1).Any(x=>this.isMathOperator(x))&&!this.hasClassAccessorOutsideParentheses(value):sendingClass.functions.ContainsKey(value.Split('(')[0]))&&value.Contains(')')&&!value.Substring(value.LastIndexOf(')')+1).Any(x=>this.isMathOperator(x))&&!this.hasClassAccessorOutsideParentheses(value);
			
		}
		
		internal Dictionary<String,Function>getFunctions () {
			
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
							throw new ParsingError("Unbalanced parentheses in \""+str+'"',this);
						
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
		
		private void moveClassItemAddrIntoEax (String classInstance,String item,VarType vt,Boolean classInstanceAlreadyInEax=false,Class cl=null) {
			
            Boolean imported=isImportedClass(classInstance);

			if (!classInstanceAlreadyInEax&&!imported)
				this.moveClassInstanceIntoEax(classInstance);
			
			if (cl==null)
				cl=this.getClassFromInstanceName(classInstance);
		
            Console.WriteLine("Class instance: "+classInstance+", item: "+item+", imported: "+imported);
			
            if (imported) { 
            	this.addByte(0xB8); // MOV EAX,FOLLOWING DWORD
                this.addBytes(BitConverter.GetBytes(PEHeaderFactory.dataSectAddr+staticInstances[cl.classID][item].Item1));
            	return;
            }
            // Not Static:
            switch (vt) {
		    		
				case VarType.NATIVE_VARIABLE:
            		var v=cl.variables[item];
            		if (v.Item3.HasFlag(Modifier.PRIVATE)||cl.parserUsed.@struct)
            			this.addBytes(new Byte[]{5}.Concat(BitConverter.GetBytes(v.Item1+cl.opcodePortionByteSize))); //ADD EAX,DWORD HERE
            		else {
            			// push esi
            			// push edx
            			// xor esi,esi
            			// add eax,edx
            			// add eax,[eax+esi+fn.instanceId*4]
            			// call eax
            			// pop esi
            			// pop edx
            			this.addBytes(new Byte[]{0x56,0x52,0x33,0xF6,0xFF,0xD0,3,0xC2,3,0x84,0x30}.Concat(BitConverter.GetBytes((UInt32)(v.Item4*4))).Concat(new Byte[]{0x5A,0x5E}));
            			
            		}
            		break;
					
				case VarType.CLASS:
            		var c=cl.classes[item];
            		if (c.Item4.HasFlag(Modifier.PRIVATE)||cl.parserUsed.@struct)
						this.addBytes(new Byte[]{5}.Concat(BitConverter.GetBytes(cl.classes[item].Item1+cl.opcodePortionByteSize))); //ADD EAX,DWORD HERE
            		else {
            			// push esi
            			// push edx
            			// xor esi,esi
            			// add eax,edx
            			// add eax,[eax+esi+fn.instanceId*4]
            			// call eax
            			// pop esi
            			// pop edx
            			this.addBytes(new Byte[]{0x56,0x52,0x33,0xF6,0xFF,0xD0,3,0xC2,3,0x84,0x30}.Concat(BitConverter.GetBytes((UInt32)(c.Item5*4))).Concat(new Byte[]{0x5A,0x5E}));
            			
            		}
            		break;
					
				case VarType.FUNCTION:
					Function fn=cl.functions[item];
					if (fn.modifier.HasFlag(Modifier.PRIVATE))
						this.addBytes(new Byte[]{5}.Concat(BitConverter.GetBytes(fn.memAddr))); //ADD EAX,DWORD HERE
					else {
						// push esi
						// push edx
						// xor esi esi
						// call EAX
						// add esi,edx
						// add EAX [EAX+ESI+fn.instanceId*4]
						// pop edx
						// pop esi
						this.addBytes(new Byte[]{0x56,0x52,0x33,0xF6,0xFF,0xD0,3,0xF2,3,0x84,0x30}.Concat(BitConverter.GetBytes(fn.instanceID*4)).Concat(new Byte[]{0x5A,0x5E}));
					}
					break;
				
				case VarType.NATIVE_ARRAY:
					var a=cl.arrays[item];
					if (a.Item4.HasFlag(Modifier.PRIVATE))
						this.addBytes(new Byte[]{5}.Concat(BitConverter.GetBytes(cl.arrays[item].Item1+cl.opcodePortionByteSize))); // ADD EAX,DWORD HERE
					else {
						// push esi
						// push edx
						// xor esi esi
						// call EAX
						// add esi,edx
						// add EAX [EAX+ESI+fn.instanceId*4]
						// pop edx
						// pop esi
						this.addBytes(new Byte[]{0x56,0x52,0x33,0xF6,0xFF,0xD0,3,0xF2,3,0x84,0x30}.Concat(BitConverter.GetBytes(a.Item5*4)).Concat(new Byte[]{0x5A,0x5E}));
					}
					break;
					
				default:
					throw new ParsingError("Invalid VarType (?!) ("+vt.ToString()+')',this);
					
			}
			
		}
		
		private void throwIfAddr (Boolean gettingAddr,String value) {
			
			if (gettingAddr)
				throw new ParsingError("Can't get address of: \""+value+'"',this);
			
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
		
		internal void moveClassInstanceIntoEax (String classInstance) {
			
			Console.WriteLine("[classInstance: \""+classInstance+"\"]");

			if (!(this.isALocalVar(classInstance))) {
				
                  if (staticInstances[ID].ContainsKey(classInstance))
                       this.addBytes(new Byte[]{0xA1 }.Concat(BitConverter.GetBytes(PEHeaderFactory.dataSectAddr+staticInstances[ID][classInstance].Item1))); // MOV EAX,DWORD [PTR]
                  else if (addEsiToLocalAddresses)
					this.addBytes(new Byte[]{0x8B,0x86}.Concat(BitConverter.GetBytes(this.appendAfterIndex[classInstance])));//MOV EAX,DWORD[PTR+ESI]
				else {
					this.addByte(0xA1);//MOV EAX,DWORD[FOLLOWING PTR]
					this.classReferences[classInstance].Add(this.getOpcodesCount());
					this.addBytes(new Byte[]{0,0,0,0});
				}
				
			}
			else {
				
				Block localVarHomeBlock=this.getLocalVarHomeBlock(classInstance);
				this.refEbp(localVarHomeBlock,2);
				this.addBytes(new Byte[]{0x8B,0x45,this.pseudoStack.getVarEbpOffset(classInstance)}); //MOV [EBP+-OFFSET],EAX
				
			}
			
		}
		
		private void callClassFunc (IEnumerable<String>classInstance,String func,String[]parameters,Boolean originLocal,Boolean classInstanceAlreadyInEax=false,Class lastClassOnOriginIfInstanceInEax=null) {
			
			foreach (String s in parameters)
				Console.WriteLine("Param - \""+s+'"');
			
			Class cl=this.getOriginFinalClass(classInstance,originLocal);
			
			if(!cl.functions.ContainsKey(func)) throw new Exception("Func:"+func+", origin: "+merge(classInstance,"."));
			
            Modifier mods=cl.functions[func].modifier;
            throwIfCantAccess(mods,func,cl.path,true);
            if (!this.containsImportedClass(classInstance.Last()))
                throwIfStatic(mods,func);

			if (cl.functions[func].expectedParameterCount!=parameters.Length)
				throw new ParsingError("Expected \""+cl.functions[func].expectedParameterCount+"\" parameters for \""+func+"\", got \""+parameters.Length+'"',this);
			
			this.addByte(0x56);//PUSH ESI
			if (classInstanceAlreadyInEax)
				this.addByte(0x50);//PUSH EAX
			
			foreach (String s in parameters.Reverse())
				this.pushValue(s); //FIXME:: tryConvertVars here
			
			if (classInstanceAlreadyInEax) {
				
				Int32 i=(parameters.Length*4);
				if (i<SByte.MaxValue)
					this.addBytes(new Byte[]{0x8B,0x44,0x24,(Byte)(i)}); //MOV EAX,[ESP+-OFFSET]
				else
					this.addBytes(new Byte[]{0x8B,0x84,0x24}.Concat(BitConverter.GetBytes(i))); //MOV EAX,[ESP+-DWORD OFFSET]
			}
				
			
			if (!classInstanceAlreadyInEax) {
				this.moveClassOriginIntoEax(classInstance,originLocal);
				this.moveClassOriginItemAddrIntoEax(classInstance.ToList(),func,VarType.FUNCTION,originLocal,true);
			}
			else
				this.moveClassItemAddrIntoEax(null,func,VarType.FUNCTION,true,lastClassOnOriginIfInstanceInEax);
			this.addBytes(new Byte[]{0xFF,0xD0}); //CALL EAX
			if (classInstanceAlreadyInEax)
				this.addBytes(new Byte[]{0x83,0xC4,4});//ADD ESP,4
			this.addByte(0x5E);//POP ESI
			
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void callClassFunc (Tuple<IEnumerable<String>,String,Boolean>tpl,String[]parameters) {
			
			this.callClassFunc(tpl.Item1,tpl.Item2,parameters,tpl.Item3);
			
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
			
			if (addEsiToLocalAddresses&&restoreEsiFuncAddr==0&&opcodes.Count!=0&&blocks.Count==0&&!@struct) {
				
				// Create restore esi function
				this.addBytes(new Byte[]{0x8B,0x44,0x24,4});//MOV EAX,[ESP+4]
//				this.addBytes(new Byte[]{0x8D,0x44,0x24,4});//LEA EAX,[ESP+4]
				esiFuncVarIndex=(UInt32)(this.opcodes.Count+2);
				this.addBytes(new Byte[]{0x89,0x86,0,0,0,0});//MOV [ESI+-DWORD],EAX
				this.addBytes(new Byte[]{0xEB,7});//JMP 7 BYTES
				restoreEsiFuncAddr=memAddress;
				this.addBytes(new Byte[]{0x8D,0x35,0,0,0,0});//LEA ESI,[PTR]
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
			
			foreach (Int32 index in this.esiFuncReferences) {
				
				Byte[] bytes=BitConverter.GetBytes(((Int32)restoreEsiFuncAddr)-BitConverter.ToInt32(new Byte[]{opcodes[(Int32)index],opcodes[(Int32)index+1],opcodes[(Int32)index+2],opcodes[(Int32)index+3]},0)-5);
				Byte i=0;
				while (i!=4) {
					
					this.opcodes[(Int32)(i+index)]=bytes[(Int32)i];
					++i;
					
				}
				
			}
			
		}
		
		public Dictionary<String,Tuple<UInt32,String,ArrayStyle,Modifier,UInt32>> getArrays () {
			
			return this.arrays;
			
		}
		
		/// <returns>The final class type</returns>
		public Class moveClassOriginIntoEax (IEnumerable<String>origin,Boolean originLocal) {
			
			String fc=origin.First();
			this.moveClassInstanceIntoEax(fc);
			Console.WriteLine(originLocal.ToString());
			Class pc=GetClassByName(fc,originLocal);
			foreach (String s in origin.Skip(1)) {
				
				this.moveClassItemAddrIntoEax(null,s,VarType.CLASS,true,pc);
				this.addBytes(new Byte[]{0x8B,0,}); //MOV EAX,[EAX]
				pc=pc.classes[s].Item3;
				
			}
			return pc;
			
		}
		
		/// <returns>The final class type</returns>
		public Class getOriginFinalClass (IEnumerable<String>origin,Boolean originLocal) {
			
			String fc=origin.First();
			Console.WriteLine("Fc: "+fc+", originLocal: "+originLocal.ToString()+", origin.Count(): "+origin.Count().ToString());
                Boolean fcImported=isImportedClass(fc);
			Class pc=GetClassByName(fc,originLocal);
            if (origin.Count()==1) return pc;
                if (fcImported) {
                    origin=origin.Skip(1);
                    pc=staticInstances[pc.classID][origin.First()].Item4;
                }
			foreach (String s in origin.Skip(1))
				pc=pc.classes[s].Item3;
			return pc;
			
		}
		
		/// <returns>The final class type</returns>
		public Class moveClassOriginItemAddrIntoEax (List<String>origin,String item,VarType vt,Boolean originLocal,Boolean originAlreadyInEax=false) {
			
			String fc=origin.First();
                Boolean fcImported=isImportedClass(fc);
			Class pc;
			if (!originAlreadyInEax) {
                     Int32 skipCount=1;
				pc=GetClassByName(fc,originLocal);
                    if (!fcImported) this.moveClassInstanceIntoEax(fc);
                    else {

                        ++skipCount;
                        this.moveClassItemAddrIntoEax(fc,origin[1],VarType.NONE,false,pc);
                        if (origin.Count!=1) pc=staticInstances[pc.classID][origin[1]].Item4;

                    }
				Console.WriteLine(originLocal.ToString());
				if (origin.Count!=1) {
					foreach (String s in origin.Skip(skipCount).Take(origin.Count-1)) {
						
						this.moveClassItemAddrIntoEax(null,s,VarType.CLASS,true,pc);
						this.addBytes(new Byte[]{0x8B,0,}); //MOV EAX,[EAX]
						pc=pc.classes[s].Item3;
						
					}
				}
			}
			else pc=this.getOriginFinalClass(origin,originLocal);
			this.moveClassItemAddrIntoEax(null,item,vt,true,pc);
			return pc;
			
		}
		
		public static String merge (IEnumerable<String> strings,String append="") {
			
			StringBuilder sb=new StringBuilder();
			
			foreach (String s in strings)
				sb.Append(s+append);
			
			return String.Concat(sb.ToString().Take(sb.Length-append.Length));
			
		}
		
		internal void closeBlock (Block b) {
			
			this.onBlockClosed(b);
			
		}
		
		internal void setByte (UInt32 index,Byte newByte) { this.opcodes[(Int32)index]=newByte; }
		
		private Boolean indicatesMathOperation (String value) {
			
			if (value.Any(x=>this.isMathOperator(x))) {
			
				//HACK:: sub parsing
				Int16 rbb=0,sbb=0;
				Boolean inQuotes=false;
				foreach (Char c in value) {
					
					if (c=='"') inQuotes=!inQuotes;
					else if (!inQuotes) {
						
						if (this.beginsParameters(c))++rbb;
						else if (this.endsParameters(c))--rbb;
						else if (c=='[')++sbb;
						else if (c==']')--sbb;
						
						if (rbb<0||sbb<0) Console.WriteLine("Unbalanced "+(sbb<0?"square parentheses":"parentheses")+" in \""+value+'"');
						
						if (this.isMathOperator(c)&&rbb==0&&sbb==0) return true;
						
					}
					
				}
			
			}
			
			return false;
			
		}
		
		/// <summary>
		/// passedVarTypes.ContainsKey
		/// </summary>
		internal Boolean pvtContainsKey (String key) {
			
			return this.passedVarTypes.Select(x=>x.Item1).Contains(key);
			
		}
		/// <summary>
		/// passedVarTypes[String index]
		/// </summary>
		public Tuple<String,VarType> pvtGet (String key) {
			
			return this.passedVarTypes.Where(x=>x.Item1==key).First().Item2;
			
		}
		/// <summary>
		/// passedVarTypes==null
		/// </summary>
		public Boolean pvtNull () {
			
			return this.passedVarTypes==null;
			
		}
		
		private List<String> parseParameters (String unparsedParams) {
			
			Byte roundBracketBalance=1,sharpBracketBalance=0;
			List<String>@params=new List<String>();
			StringBuilder paramBuilder=new StringBuilder();
            Boolean inQuotes=false;
			//HACK:: sub parsing
			foreach (Char c in unparsedParams) {
				
                if (inQuotes&&c!='"') continue;
                else if (c=='"') inQuotes=!inQuotes;
				else if (c=='(') ++roundBracketBalance;
				else if (c==')') --roundBracketBalance;
				else if (c=='<') ++sharpBracketBalance;
				else if (c=='>') --sharpBracketBalance;
				else if (c==','&&roundBracketBalance==1&&sharpBracketBalance==0) {
					
					@params.Add(paramBuilder.ToString());
					paramBuilder.Clear();
					
				}
				
				if (roundBracketBalance==0) {
					@params.Add(paramBuilder.ToString());
					break;
				}
				else if (!(c==','&&roundBracketBalance==1&&sharpBracketBalance==0)) paramBuilder.Append(c);
				
			}
			return @params;
			
		}
		
		private void refEbp (Block localVarHomeBlock,UInt32 offset) {
			
			if (localVarHomeBlock!=this.getCurrentBlock())
				this.localVarEBPPositionsToOffset[this.getCurrentBlock()].Add((UInt32)(this.opcodes.Count+offset));
			
		}
		
		private void offsetEBPs (SByte offset) {
			
			foreach (UInt32 index in this.localVarEBPPositionsToOffset.Where(x=>this.getCurrentBlock().hasChild(x.Key)).SelectMany(x=>x.Value))
					this.offsetEBP(4,index);
			
		}
		
        internal Boolean tryGetAcknowledgement (String varType,out Tuple<String,VarType>type) {
            type=null;
            if (!(acknowledgements.ContainsKey(varType)))
                return false;
            type=acknowledgements[varType];
            return true;
        }

        private Tuple<String,VarType> ackRootOf (String ack) {

            if (!(this.acknowledgements.ContainsKey(ack)))
                throw new ParsingError("Not an acknowledgement: "+ack,this);

            Tuple<String,VarType>root=this.acknowledgements[ack];
            if (this.acknowledgements.ContainsKey(root.Item1))
                return ackRootOf(root.Item1);
            else
                return root;

        }

        private void fillLabelReferences () {

            Byte[] addr;
            Byte i;

            foreach (KeyValuePair<String,List<Tuple<UInt32,UInt32>>>kvp in this.labelReferences) {

                if (!(labels.ContainsKey(kvp.Key)))
                    throw new ParsingError("Label was referenced but does not exist: \""+kvp.Key+'"',this);

                foreach (Tuple<UInt32,UInt32>tpl in kvp.Value) {

                    addr=BitConverter.GetBytes((Int32)labels[kvp.Key]-((Int32)tpl.Item2+5));

                    i=0;
                    while (i!=4) {

                        opcodes[(Int32)(tpl.Item1+i)]=addr[i];
                        ++i;

                    }

                }

            }


        }

        private void throwIfCantAccess (Modifier mods,String instanceName,String classFilePath,Boolean getting) {

            Char[] arr={'\\','/' };
            instanceName=this.trimCurrentPath(instanceName);
            classFilePath=this.trimCurrentPath(classFilePath);

            if ((mods.HasFlag(Modifier.PRIVATE)&&classFilePath!=fileName)||(mods.HasFlag(Modifier.LOCAL)&&((classFilePath.Contains('\\')||classFilePath.Contains('/'))&&merge(fileName.Split(arr).AllButLast(),"/")!=merge(classFilePath.Split(arr).AllButLast(),"/")))||(mods.HasFlag(Modifier.PULLABLE)&&!getting))
                throw new ParsingError("Can't access \""+instanceName+"\" from \""+classFilePath.Split(arr).Last()+"\": exists, but inaccessible due to its modifiers. \n\nCalling path: "+merge(fileName.Split(new []{'/','\\' }).AllButLast(),"/")+"\nCalled path: "+merge(classFilePath.Split(new []{'/','\\' }).AllButLast(),"/")+'\n',this);
            
        }

        private void throwIfStatic (Modifier mods,String instanceName) {

            if (mods.HasFlag(Modifier.STATIC))
                throw new ParsingError("Tried to access a static instance \""+instanceName+"\" from a class instance (use class name)",this);

        }

        /// <summary>
        /// Doesn't throw all possible ParsingErrors (check for anything invalid beforehand if necessary)
        /// </summary>
        /// <param name="vt">Should be CLASS || FUNCTION || NATIVE_ARRAY || NATIVE_VARIABLE</param>
        public Modifier getClassOriginItemMod (List<String>origin,String item,VarType vt,Boolean originLocal) {
            
            String prevC,fc=prevC=origin.First();
            Class pc=GetClassByName(fc,originLocal);
            if (origin.Count!=1) {
                foreach (String s in origin.Skip(1).Take(origin.Count-1)) {
                    pc=isImportedClass(prevC)?staticInstances[pc.classID][s].Item4:pc.classes[s].Item3; 
                    prevC=s;
                }
            }
            else pc=this.getOriginFinalClass(origin,originLocal);

            if (isImportedClass(origin.Last())) return staticInstances[pc.classID][item].Item3;
            switch (vt) {

                case VarType.CLASS: 
                    return pc.classes[item].Item4;
                case VarType.FUNCTION:
                    return pc.functions[item].modifier;
                case VarType.NATIVE_ARRAY:
                    return pc.arrays[item].Item4;
                case VarType.NATIVE_VARIABLE:
                    return pc.variables[item].Item3;
                default:
                    throw new ParsingError("Invalid VarType (?!): "+vt.ToString(),this);

            }

        }

        public Tuple<String,VarType> getConstantValue (String value,out UInt32 constValue) {

            //constants:
            UInt32 _value;
            Int32 _value0;
            if (UInt32.TryParse(value,out _value)) {
                String rv;
                constValue=_value;
                if (_value<=SByte.MaxValue)
                    return new Tuple<String,VarType>(KWByte.constName,VarType.NATIVE_VARIABLE);
                else if (_value<=UInt16.MaxValue)
                    rv=KWShort.constName;
                else rv=KWInteger.constName;
                
                return new Tuple<String,VarType>(rv,VarType.NATIVE_VARIABLE);
                
            }
            else if (Int32.TryParse(value,out _value0)) {
                
                //TODO:: SIGNED VARIABLES
                constValue=unchecked((UInt32)_value0);
                return new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
                
            }
            else if (value==KWBoolean.constFalse) {
                
                constValue=0;
                return new Tuple<String,VarType>(KWBoolean.constName,VarType.NATIVE_VARIABLE);

            }
            else if (value==KWBoolean.constTrue) {
                
                constValue=1;
                return new Tuple<String,VarType>(KWBoolean.constName,VarType.NATIVE_VARIABLE);
                
            }
            else if (value==Parser.NULL_STR) {
                
                constValue=0;
                return new Tuple<String,VarType>(Parser.NULL_STR,VarType.NONE);
                
            }
            else if (constants.ContainsKey(value)) {
                
                constValue=this.constants[value].Item1;
                return this.constants[value].Item2;

            }

            throw new ParsingError("Not constant value: "+value,this);

        }

        internal Dictionary<String,Tuple<UInt32,Tuple<String,VarType>>> getConstants () { return this.constants; }

        private Modifier modsOf (String varName,VarType varType) {

            switch (varType) {

                case VarType.CLASS:
                    return this.classes[varName].Item4;
                case VarType.FUNCTION:
                    return this.functions[varName].modifier;
                case VarType.NATIVE_ARRAY_INDEXER:
                case VarType.NATIVE_ARRAY:
                    return this.arrays[varName].Item4;
                case VarType.NATIVE_VARIABLE:
                    return this.variables[varName].Item3;
                case VarType.NONE:
                    throw new ParsingError("Can't get var type of void/null variable: "+varName,this);
                default:
                    throw new ParsingError("Can't get var type of: "+varName,this);

            }

        }

        private Boolean isImportedClass (String name) { return this.importedClasses.Select(x=>x.className).Contains(name); }
        /// <param name="name">No checks or exceptions if this is not a valid imported class</param>
        private Class getImportedClass  (String name) { return this.importedClasses.Where(x=>x.className==name).First(); }

        internal String classIDOf (String name) { return getImportedClass(name).classID;}

        private void increaseDwordsByOpcodes () {

            Byte i;

            foreach (UInt32 index in this.dwordsToIncByOpcodes) {

                Console.WriteLine(":::::: "+index.ToString()+','+this.opcodes.Count.ToString());

                Byte[] arr=new Byte[4];
                i=0;
                while (i!=4) {
                    arr[i]=this.opcodes[(Int32)(i+index)];
                    ++i;
                }
                arr=BitConverter.GetBytes(BitConverter.ToUInt32(arr,0)+1);
                i=0;
                while (i!=4) {
                    this.opcodes[(Int32)(i+index)]=arr[i];
                    ++i;
                }

            }

        }

        private void increaseDataSectDwordsByOpcodes () {

            Byte i;

            foreach (UInt32 index in this.dwordsToIncByOpcodesUntilStaticFuncEnd) {

                Console.WriteLine(":::::: "+index.ToString()+','+dataSectBytes.Count.ToString());

                Byte[] arr=new Byte[4];
                i=0;
                while (i!=4) {
                    arr[i]=dataSectBytes[(Int32)(i+index)];
                    ++i;
                }
                arr=BitConverter.GetBytes(BitConverter.ToUInt32(arr,0)+1);
                i=0;
                while (i!=4) {
                    dataSectBytes[(Int32)(i+index)]=arr[i];
                    ++i;
                }

            }

        }

        private String trimCurrentPath (String path) {

            return path.StartsWith(Environment.CurrentDirectory)?path.Substring(Environment.CurrentDirectory.Length+1):path;

        }
        
        /// <param name="varName">Variable name of variable trying to be referenced.</param>
        private void ThrowIfInstRefFromStaticEnv (String varName) {

            if (InStaticEnvironment()&&this.variables.ContainsKey(varName)&&!this.variables[varName].Item3.HasFlag(Modifier.STATIC))
                throw new ParsingError("Tried to access non-static instance variable \""+varName+"\" from a static environment",this);

        }

        private Class GetClassByName (String fc,Boolean originLocal) {

            return isImportedClass(fc)?getImportedClass(fc):(originLocal)?this.importedClasses.Where(x=>x.className==this.getLocalVarHomeBlock(fc).localVariables[fc].Item1.Item1).First():this.classes.ContainsKey(fc)?this.classes[fc].Item3:staticInstances[ID][fc].Item4;

        }

        internal Boolean InStaticEnvironment () {
            return this.inFunction&&!this.inConstructor&&this.functions.Count>0&&this.functions.Last().Value.modifier.HasFlag(Modifier.STATIC);
        }

        internal UInt32 GetStaticInclusiveAddress (Boolean inStaticEnvironment) {
            return inStaticEnvironment?(UInt32)(PEHeaderFactory.dataSectAddr+dataSectBytes.Count()):memAddress;
        }

        internal UInt32 GetStaticInclusiveAddress () {
            return GetStaticInclusiveAddress(InStaticEnvironment());
        }

        private void CallStaticClassFunc (String _ID,String path,String funcName,String[]parameters) {

            var function=staticFunctions[_ID][funcName];
            Modifier mods=function.Item6;
            throwIfCantAccess(mods,funcName,path,false);

            if (function.Item3!=parameters.Length)
                throw new ParsingError("Expected \""+function.Item3+"\" parameters for \""+funcName+"\", got \""+parameters.Length+'"',this);
            
            foreach (String s in parameters.Reverse())
                this.pushValue(s); //FIXME:: tryConvertVars here

            if (addEsiToLocalAddresses) {
                // need to mov eax,addr then call eax because there is no
                // way to calculate the address without using ESI which
                // would then cost more instructions and processor time
                // anyway
                this.addBytes(new Byte[]{0xB8 }.Concat(BitConverter.GetBytes(function.Item1))); // MOV EAX ... DWORD
                this.addBytes(new Byte[]{0xFF,0xD0 }); // CALL EAX
            }
            else this.addBytes(new Byte[]{0xE8 }.Concat(BitConverter.GetBytes((Int32)function.Item1-(Int32)GetStaticInclusiveAddress()-5)));
            status=ParsingStatus.SEARCHING_NAME;

        }

        private void CallStaticClassFunc (Class cl,String funcName,String[]parameters) { CallStaticClassFunc(cl.classID,cl.path,funcName,parameters); }

        private void SetStaticInclusiveByte (OpcodeIndexReference index,Byte b,Int32 indexOffset=0) {

            Int32 idx=index.GetIndexAsInt();
            if (index.type==OpcodeIndexType.CODE_SECT_REFERENCE) this.opcodes[idx+indexOffset]=b;
            else if (index.type==OpcodeIndexType.DATA_SECT_REFERENCE) Parser.dataSectBytes[idx+indexOffset]=b;
                // ^ else if for the off chance a new OpcodeIndexType is introduced
        }

        internal Boolean InDataSect () {

            return parserName=="Child parser"||InStaticEnvironment();

        }

        internal OpcodeIndexReference GetDataSectInclusiveOpcodesCount (Int64 offset) {

            if (!InDataSect())
                return OpcodeIndexReference.NewCodeSectRef((UInt32)(this.opcodes.Count+offset));
            else
                return OpcodeIndexReference.NewDataSectRef((UInt32)(Parser.dataSectBytes.Count+offset+opcodes.Count));

        }

        internal static UInt32 GetSkeletonAddress (String classID) {
            return PEHeaderFactory.dataSectAddr+Parser.classSkeletons[classID];
        }

        internal static UInt32 GetSkeletonAddress (Class cl) {
            return GetSkeletonAddress(cl.classID);
        }

        internal void ReferenceRefdFunc (String functionName,Int64 offset) {

            if (InStaticEnvironment())
                this.referencedFuncPositions[functionName].Add(GetStaticInclusiveOpcodesCount(offset));
            else this.referencedFuncPositions[functionName].Add(GetDataSectInclusiveOpcodesCount(offset));

        }
        
        internal Tuple<String,VarType> vtToTpl (ValueTuple<String,VarType>vt) {
        	return new Tuple<String,VarType>(vt.Item1,vt.Item2);
        }
        
        #region Character parsing helpers
        //TODO:: make all this static

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
		
		internal Boolean isColon (Char c) {
			
			return c==':';
			
		}
		
		internal Boolean isValidNameChar (Char c) {
			
			return Char.IsLetterOrDigit(c)||this.isUnderscore(c);
			
		}
		
		internal Boolean startsPassingTypes (Char c) {
			
			return c=='<';
			
		}
		
		internal Boolean endsPassingTypes (Char c) {
			
			return c=='>';
			
		}

        private Boolean indicatesLabel (Char c) {

            return c=='~';

        }
        
        private Boolean endsInherit (Char c) {
        	
        	return c=='.';
        	
        }
        
        private Boolean continuesInherit (Char c) {
        	
        	return c==',';
        	
        }
		
		#endregion
		
		/// <summary>
		/// will most likely have unexpected outcome (due to collateral damage) if used
		/// </summary>
		internal void clearOpcodes () {
			opcodes.Clear();
			memAddress=startingMemAddr;
			tableAddrIndex=0;
		}
		
	}
	
}
