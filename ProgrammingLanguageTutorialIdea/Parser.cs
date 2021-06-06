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

namespace ProgrammingLanguageTutorialIdea {
	
	public class Parser {
		
		public String lastReferencedVariable {
			
			private set;
			get;
			
		}
		public String referencedVariable;
		
		internal VarType lastReferencedVarType=VarType.NONE,referencedVarType=VarType.NONE;
		internal String varType; //to fix for classes later, maybe set to a Tuple<String,String>//Name,Origin or something? where Origin is something like the filename, or something to get the exact class that is being referred to
		internal Dictionary<String,List<UInt32>> variableReferences=new Dictionary<String,List<UInt32>>(),//Name,(Index in the Opcodes List)
												 arrayReferences=new Dictionary<String,List<UInt32>>();//Name,(Index in the Opcodes List)
		
		internal Dictionary<String,List<String>> toImport;//DllName,Functions
		internal Dictionary<String,List<UInt32>> referencedFuncPositions;//FuncName,Opcode pos
		
		private UInt32 memAddress;
		private readonly KeywordMgr keywordMgr;
			
		private List<Byte> opcodes=new List<Byte>(),importOpcodes=null,finalBytes=new List<Byte>(),appendAfter=new List<Byte>();
		private ParsingStatus status;
		private Dictionary<String,Tuple<UInt32,String>> variables=new Dictionary<String,Tuple<UInt32,String>>();//Name,(Mem Address,Var Type)
		private Dictionary<String,Tuple<UInt32,String,ArrayStyle>> arrays=new Dictionary<String,Tuple<UInt32,String,ArrayStyle>>();//Name,(Ptr To Mem Address of Heap Handle(Dynamic) or Mem Block(Static),Array Var Type,ArrayStyle(Dynamic or Static))
		
		private Tuple<UInt32,List<UInt32>> processHeapVar;//Mem Addr, References
		
		private ArrayStyle style;//TODO:: do static memory block, and also allow this to be changed outside of compiler in the code
		
		private Boolean winApp;
		private Dictionary<String,UInt32> setArrayValueFuncPtrs;
		
		private KeywordType[] nextExpectedKeywordTypes=new []{KeywordType.NONE};
		
		private const String KERNEL32="KERNEL32.DLL";
		
		public Parser (Boolean winApp=true) {
			
			memAddress=winApp?0x00401000:(UInt32)0;
			keywordMgr=new KeywordMgr();
			style=winApp?ArrayStyle.DYNAMIC_MEMORY_HEAP:ArrayStyle.STATIC_MEMORY_BLOCK;
			this.winApp=winApp;
			toImport=new Dictionary<String,List<String>>();
			this.referencedFuncPositions=new Dictionary<String,List<UInt32>>();
			this.setArrayValueFuncPtrs=new Dictionary<String,UInt32>();
			
		}
		
		public Byte[] parse (String data) {
			
			status=ParsingStatus.SEARCHING_NAME;
			StringBuilder nameReader=new StringBuilder();
			UInt16 squareBracketBalance=0;
			String arrName=null;
			
			data+=' ';
			
			foreach (Char c in data) {
				
				Console.WriteLine(" - Checking: \""+c+"\",ParsingStatus: "+status.ToString());
				
				switch (status) {
					
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
							
							this.declareArray(nameReader.ToString());
							
							nameReader.Clear();
							
							this.resetLastReferencedVar();
							
							
						}
						else if (this.beginsArrayIndexer(c)) {
							
							arrName=nameReader.ToString();
							nameReader.Clear();
							++squareBracketBalance;
							status=ParsingStatus.READING_ARRAY_INDEXER;
							
						}
						else if (Char.IsLetterOrDigit(c)||this.refersToIncrementOrDecrement(c)) nameReader.Append(c);
						else {
							
							String prevLastReferencedVariable=lastReferencedVariable;
							
							String name=nameReader.ToString();
//							Console.WriteLine("Name: "+name);
							
							if ((this.refersToIncrementOrDecrement(name[0])||this.refersToIncrementOrDecrement(name[name.Length-1]))&&name.Length>2) {
								
								if (name.StartsWith(KWIncrease.constName)) {
									
									name=name.Substring(2,name.Length-2);
									this.chkName(name);
									this.chkName(KWIncrease.constName);
									this.resetLastReferencedVar();
									
								}
								else if (name.StartsWith(KWDecrease.constName)) {
									
									name=name.Substring(2,name.Length-2);
									this.chkName(name);
									this.chkName(KWDecrease.constName);
									this.resetLastReferencedVar();
									
								}
								
								else if (name.EndsWith(KWIncrease.constName)) {
									
									name=name.Substring(0,name.Length-2);
									this.chkName(name);
									this.chkName(KWIncrease.constName);
									this.resetLastReferencedVar();
									
								}
								else if (name.EndsWith(KWDecrease.constName)) {
									
									name=name.Substring(0,name.Length-2);
									this.chkName(name);
									this.chkName(KWDecrease.constName);
									this.resetLastReferencedVar();
									
								}
								else this.chkName(name);
								
							}
							else this.chkName(name);
							
							nameReader.Clear();
							
							if (prevLastReferencedVariable==lastReferencedVariable)
								this.resetLastReferencedVar();
							
						}
						
						break;
						
					case ParsingStatus.READING_VARIABLE_NAME:
						
						if (Char.IsLetterOrDigit(c)) nameReader.Append(c);
						else {
							
							this.registerVariable(nameReader.ToString());
							nameReader.Clear();
							this.nextExpectedKeywordTypes=new KeywordType[]{KeywordType.ASSIGNMENT,KeywordType.DECREMENT,KeywordType.INCREMENT};
							
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
							arrName=null;
							this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.DECREMENT,KeywordType.INCREMENT};
							this.lastReferencedVarType=VarType.NATIVE_ARRAY_INDEXER;
							
						}
						else nameReader.Append(c);
						
						break;
						
				}
				
			}
			
			return compile();
			
		}
		
		private Byte[] compile () {
			
			//TODO:: add code to free all allocated memory here,maybe call HeapFree with lpMem null? not sure of the functionality on that but it might free all mem allocated to the heap
			this.addByte(0xC3); //Add RETN call to end of our exe, so no matter what happens in terms of the source, it should not be a blank application & will exit
			
			opcodes.AddRange(appendAfter);
			this.updateVariableReferences();
			
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
		
		private void addByte (Byte b) {
			
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
			
			if (processHeapVar!=null){
				
				this.processHeapVar=new Tuple<UInt32,List<UInt32>>(this.processHeapVar.Item1+1,this.processHeapVar.Item2);
				
			}
			
			opcodes.Add(b);
			++memAddress;
			
		}
		
		private void addBytes (IEnumerable<Byte> bytes) {
			
			foreach (Byte b in bytes)
				this.addByte(b);
			
		}
		
		private void chkName (String name) {
			
			KeywordType[] pKTs=this.nextExpectedKeywordTypes;
			this.nextExpectedKeywordTypes=new []{KeywordType.NONE};
			
			Console.WriteLine("Got name: \""+name+'"');
			
			if (!pKTs.Contains(KeywordType.NONE))
				goto checkKeywords;
			
			if (this.variables.ContainsKey(name)) {
				
				this.lastReferencedVariable=name;
				this.status=ParsingStatus.SEARCHING_NAME;
				this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT,KeywordType.INCREMENT,KeywordType.DECREMENT};
				this.lastReferencedVarType=VarType.NATIVE_VARIABLE;
				return;
				
			}
			
			if (this.arrays.ContainsKey(name)) {
				
				this.lastReferencedVariable=name;
				this.lastReferencedVarType=VarType.NATIVE_ARRAY;
				this.status=ParsingStatus.SEARCHING_NAME;
				this.nextExpectedKeywordTypes=new []{KeywordType.ASSIGNMENT|KeywordType.INCREMENT|KeywordType.DECREMENT};
				return;
				
			}
			
			checkKeywords:
			foreach (Keyword kw in this.keywordMgr.getKeywords().Where(x=>!x.hasParameters)) {
				
				if (kw.name==name) {
					
					if (!pKTs.Contains(KeywordType.NONE)&&!pKTs.Contains(kw.type)) {
						if (pKTs.Length>1) {
							StringBuilder sb=new StringBuilder();
							foreach (KeywordType kt in pKTs)
								sb.Append('"'+kt.ToString()+"\", ");
							throw new ParsingError("Expected a keyword of any of the following types: "+String.Concat(sb.ToString().Take(sb.Length-2)));
						}
						else throw new ParsingError("Expected a keyword of type \""+pKTs[0].ToString()+'"');
					}
					
					KeywordResult res=kw.execute(this);
					this.status=res.newStatus;
					this.addBytes(res.newOpcodes);
					return;
					
				}
				
			}
			
			throw new ParsingError("Unexpected name: \""+name+'"');
			
		}
		
		private void registerVariable (String varName) {
			
			Console.WriteLine("Registering variable "+varName+" (a type of \""+this.varType+"\"), memAddress: "+memAddress.ToString("X"));
			
			if (this.nameExists(varName))
				throw new ParsingError("The name \""+varName+"\" is already in use");
		
			this.variables.Add(varName,new Tuple<UInt32,String>(memAddress+(UInt32)appendAfter.Count,this.varType));
			this.variableReferences.Add(varName,new List<UInt32>());
			
			//TODO:: when classes are a thing, make sure they are accounted for here
			//if (class) -> appendAfter.addRange ... class or struct size.. because, the pointers are 4 bytes, but the actual struct could and probably is greater or different than 4bytes
			this.appendAfter.AddRange(new Byte[keywordMgr.getVarTypeByteSize(this.varType)]);
			
			this.lastReferencedVariable=varName;
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void processValue (String value) {
			
			//TODO:: fix processValue so it uses Parser#pushValue
			
			//TODO:: make it so that bytes/shorts/ints can be parsed into bytes shortes ints just by typing the number in the code ie myFunction(1300) will parse 1300 into a short, in all check variable types
			
			String type=(this.referencedVarType==VarType.NATIVE_ARRAY||this.referencedVarType==VarType.NATIVE_ARRAY_INDEXER)?this.arrays[this.referencedVariable].Item2:this.variables[this.referencedVariable].Item2;
			
//			Console.WriteLine("referencingArray: "+referencingArray.ToString());
//			Console.WriteLine("this.arrays[referencedVariable]: "+this.arrays[referencedVariable].Item2);
			
			//HACK:: check variable type
			if (this.referencedVarType==VarType.NATIVE_ARRAY) {
				
				Console.WriteLine("Making array named \""+this.referencedVariable+"\" of array type \""+type+"\" with value \""+value+"\".");
				
				this.addByte(0x51); //PUSH ECX
				
				if (this.processHeapVar==null)
					this.setProcessHeapVar();
				
				this.addByte(0xBB);//MOV FOLLOWING UINT32 TO EBX
				this.addBytes(BitConverter.GetBytes(keywordMgr.getVarTypeByteSize(type)));//UINT32 HERE
				this.addBytes(new Byte[]{0x6B,0xDB,Byte.Parse(value.Substring(1))});//IMUL EAX BY BYTE TODO:: update this for pushValue
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
				Tuple<UInt32,String,ArrayStyle>_refdVar=this.arrays[this.referencedVariable];
				this.arrays[this.referencedVariable]=new Tuple<UInt32,String,ArrayStyle>(this.memAddress+(UInt32)(appendAfter.Count),_refdVar.Item2,_refdVar.Item3);
				this.appendAfter.AddRange(new Byte[4]);
				this.arrayReferences[this.referencedVariable].Add((UInt32)(opcodes.Count+1));
				this.addBytes(new Byte[]{0xA3,0,0,0,0,//STORE ALLOCATED PTR TO VARIABLE
				              				0x59,//POP ECX
				              				0x6A,0,//PUSH 0
				              				0x6A});//PUSH FOLLOWING BYTE TODO:: update this for pushValue, since pushValue will push the value then just cut off the addBytes here, remove the following addByte and replace it all with just a processValue call
				this.addByte(Byte.Parse(value.Substring(1)));//ARRAY LENGTH
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
			
			else if (this.referencedVarType==VarType.NATIVE_ARRAY_INDEXER) {
				
				this.addByte(0x58);//POP EAX
				this.pushValue(value);
				this.addByte(0x5A);//POP EDX
				this.addBytes(new Byte[]{0x89,0x10});//MOV [EAX],EDX
				
			}
			
			else {
				
//				Console.WriteLine("Did not make an array");
			
				if (type==KWByte.constName) {
					
					Byte num;
					if (!(Byte.TryParse(value,out num)))
						throw new ParsingError("Expected a number 0-255, got \""+value+'"');
					
					this.addByte(num);
					
				}
				else if (type==KWShort.constName) {
					
					UInt16 num;
					if (!(UInt16.TryParse(value,out num)))
						throw new ParsingError("Expected a number 0-65535, got \""+value+'"');
					
					this.addBytes(BitConverter.GetBytes(num));
					
				}
				
				else if (type==KWInteger.constName) {
					
					UInt32 num;
					if (!(UInt32.TryParse(value,out num)))
					    throw new ParsingError("Expected a number 0-4294967295, got \""+value+'"');
					    
					this.addBytes(BitConverter.GetBytes(num));
					
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
			
			this.arrays.Add(arrayName,new Tuple<UInt32,String,ArrayStyle>(0,this.varType,this.style));
			this.arrayReferences.Add(arrayName,new List<UInt32>());
			
//			this.appendAfter.AddRange(new Byte[keywordMgr.getVarTypeByteSize(this.varType)]);
			
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
			
			Console.WriteLine("Calling setArrayValue: memAddress: "+memAddress.ToString("X")+", setArrayValueFuncPtrs[arrayName]: "+setArrayValueFuncPtrs[arrayName].ToString("X"));
			this.addBytes(new Byte[]{0xE8}.Concat(BitConverter.GetBytes((Int32)setArrayValueFuncPtrs[arrayName]-(Int32)memAddress-5)));
			
		}
		
		private void resetLastReferencedVar () {
			
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
				
				throw new ParsingError("Invalid array indexer: Array \""+referencedArray+"\" does not exist");
				
			}
			else return false;
			
		}
		
		
		internal Boolean nameExists (String name) {
			
			foreach (String str in keywordMgr.getKeywords().Select(x=>x.name))
				if (str==name) return true;
			
			return this.arrays.ContainsKey(name) ||
				this.variables.ContainsKey(name);
			
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
				
				String sub=indexer.Substring(indexer.IndexOf('[')+1);
				String arrName0=indexer.Split('[')[0].Replace(" ",""),
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
				this.arrayReferences[arrName].Add((UInt32)(this.opcodes.Count+2));
				this.addBytes(new Byte[]{3,0x15,0,0,0,0});//ADD EDX,VALUE @ PTR
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
				Byte arrMemByteSize=(Byte)keywordMgr.getVarTypeByteSize(this.arrays[arrName].Item2);
				if (arrMemByteSize!=1)
					this.addBytes(new Byte[]{0x6B,0xC0,arrMemByteSize});//IMUL EAX BY ARRAY MEMBER BYTE SIZE
				this.addBytes(new Byte[]{0x83,0xC0,8}); //ADD 8 TO EAX
				this.arrayReferences[arrName].Add((UInt32)(this.opcodes.Count+2));
				this.addBytes(new Byte[]{3,5,0,0,0,0});//ADD EAX,VALUE @ PTR
				if (!recursing)
					this.addByte(0x50);//PUSH EAX
				
			}
			
			return ParsingStatus.SEARCHING_NAME;
			
		}
		
		/// <summary>
		/// Push a value (constant number,var,array,array indexer) onto the stack
		/// </summary>
		private void pushValue (String value) {
			
			//maybe return the var type, or set it to this.varType ?
			
			//HACK:: CHECK VAR TYPE HERE
			
			//constants:
			UInt32 _value;
			if (UInt32.TryParse(value,out _value)) {
				
				if (_value<=Byte.MaxValue) {
					
					Console.WriteLine(" PUSHING BYTE VALUE!@!!");
					this.addBytes(new Byte[]{0x6A,(Byte)(_value)});//PUSH BYTE _value
					return;
					
				}
				else if (_value<=UInt16.MaxValue)
					/*.. var type == short */;
				
				//Words & Dwords use the same push opcode
				
				this.addBytes(new Byte[]{0x68}.Concat(BitConverter.GetBytes(_value)));
				
				return;
				
			}
			else if (this.variables.ContainsKey(value)) {
				
				//TODO:: push value
				
			}
			else if (this.arrays.ContainsKey(value)) {
				
				//TODO:: push ptr
				
			}
			else if (this.tryCheckIfValidArrayIndexer(value)) {
				
				//TODO:: push value at index
				
			}
			else throw new ParsingError("Invalid value: \""+value+'"');
			
		}
		
		internal UInt32 getOpcodesCount () { return (UInt32)this.opcodes.Count; }
		
		internal String getVariablesType (String varName) {
			
			return this.variables[varName].Item2;
			
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
		
		
	}
	
}
