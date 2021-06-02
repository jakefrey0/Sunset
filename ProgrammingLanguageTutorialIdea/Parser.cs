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
		
		internal String varType; //to fix for classes later, maybe set to a Tuple<String,String>//Name,Origin or something? where Origin is something like the filename, or something to get the exact class that is being referred to
		internal Dictionary<String,List<UInt32>> variableReferences=new Dictionary<String,List<UInt32>>();//Name,(Index in the Opcodes List)
		
		private UInt32 memAddress;
		private readonly KeywordMgr keywordMgr;
			
		private List<Byte> opcodes=new List<Byte>(),importOpcodes=null,finalBytes=new List<Byte>(),appendAfter=new List<Byte>();
		private ParsingStatus status;
		private Dictionary<String,Tuple<UInt32,String>> variables=new Dictionary<String,Tuple<UInt32,String>>();//Name,(Mem Address,Var Type)
		
		public Parser (Boolean winApp=true) {
			
			memAddress=winApp?0x00401000:(UInt32)0;
			keywordMgr=new KeywordMgr();
			
		}
		
		public Byte[] parse (String data) {
			
			status=ParsingStatus.SEARCHING_NAME;
			StringBuilder nameReader=new StringBuilder();
			
			data+=' ';
			
			foreach (Char c in data) {
				
				Console.WriteLine(" - Checking: \""+c+"\",ParsingStatus: "+status.ToString());
				
				switch (status) {
					
					case ParsingStatus.SEARCHING_VALUE:
					case ParsingStatus.SEARCHING_VARIABLE_NAME:
					case ParsingStatus.SEARCHING_NAME:
						if (!this.isFormOfBlankspace(c)) {
							
							nameReader.Append(c);
							++status;
							
						}
						break;
					
					case ParsingStatus.READING_NAME:
						
						if (Char.IsLetterOrDigit(c)||this.refersToIncrementOrDecrement(c)) nameReader.Append(c);
						else {
							
							String prevLastReferencedVariable=lastReferencedVariable;
							
							String name=nameReader.ToString();
//							Console.WriteLine("Name: "+name);
							
							if ((this.refersToIncrementOrDecrement(name[0])||this.refersToIncrementOrDecrement(name[name.Length-1]))&&name.Length>2) {
								
								if (name.StartsWith(KWIncrease.constName)) {
									
									name=name.Substring(2,name.Length-2);
									this.chkName(name);
									this.chkName(KWIncrease.constName);
									lastReferencedVariable=null;
									
								}
								else if (name.StartsWith(KWDecrease.constName)) {
									
									name=name.Substring(2,name.Length-2);
									this.chkName(name);
									this.chkName(KWDecrease.constName);
									lastReferencedVariable=null;
									
								}
								
								else if (name.EndsWith(KWIncrease.constName)) {
									
									name=name.Substring(0,name.Length-2);
									this.chkName(name);
									this.chkName(KWIncrease.constName);
									lastReferencedVariable=null;
									
								}
								else if (name.EndsWith(KWDecrease.constName)) {
									
									name=name.Substring(0,name.Length-2);
									this.chkName(name);
									this.chkName(KWDecrease.constName);
									lastReferencedVariable=null;
									
								}
								else this.chkName(name);
								
							}
							else this.chkName(name);
							
							nameReader.Clear();
							
							if (prevLastReferencedVariable==lastReferencedVariable)
								lastReferencedVariable=null;
							
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
							
						}
						break;
						
				}
				
			}
			
			return compile();
			
		}
		
		private Byte[] compile () {
			
			this.addByte(0xC3); //Add RETN call to end of our exe, so no matter what happens in terms of the source, it should not be a blank application & will exit
			
			opcodes.AddRange(appendAfter);
			this.updateVariableReferences();
			
			PEHeader hdr=PEHeaderFactory.newHdr(opcodes,importOpcodes,memAddress,0);
			
			while(opcodes.Count%512!=0)
				opcodes.Add(0);
			
			finalBytes.AddRange(hdr.toBytes());
			finalBytes.AddRange(opcodes);
			if (importOpcodes!=null)
				finalBytes.AddRange(importOpcodes);
			
			return finalBytes.ToArray();
			
		}
		
		private void addByte (Byte b) {
			
			Dictionary<String,Tuple<UInt32,String>> newDict=new Dictionary<String,Tuple<UInt32,String>>(this.variables.Count);
			foreach (KeyValuePair<String,Tuple<UInt32,String>> kvp in this.variables) {
				Console.WriteLine("For variable: "+kvp.Key+", updating mem address to: "+(kvp.Value.Item1+1).ToString("X"));
				newDict.Add(kvp.Key,new Tuple<UInt32,String>(kvp.Value.Item1+1,kvp.Value.Item2));
			}
			
			this.variables=new Dictionary<String,Tuple<UInt32,String>>(newDict);
			
			opcodes.Add(b);
			++memAddress;
			
		}
		
		private void addBytes (IEnumerable<Byte> bytes) {
			
			foreach (Byte b in bytes)
				this.addByte(b);
			
		}
		
		private void chkName (String name) {
			
			Console.WriteLine("Got name: \""+name+'"');
			
			if (this.variables.ContainsKey(name)) {
				
				this.lastReferencedVariable=name;
				this.status=ParsingStatus.SEARCHING_NAME;
				return;
				
			}
			
			foreach (Keyword kw in this.keywordMgr.getKeywords().Where(x=>!x.hasParameters)) {
				
				if (kw.name==name) {
					
					KeywordResult res=kw.execute(this);
					this.status=res.newStatus;
					this.addBytes(res.newOpcodes);
					return;
					
				}
				
			}
			
			throw new ParsingError("Unexpected name: \""+name+'"');
			
		}
		
		private void registerVariable (String varName) {
			
			Console.WriteLine("Regestering variable "+varName+" (a type of"+this.varType+"), memAddress: "+memAddress.ToString("X"));
		
			this.variables.Add(varName,new Tuple<UInt32,String>(memAddress+(UInt32)appendAfter.Count,this.varType));
			this.variableReferences.Add(varName,new List<UInt32>());
			
			//HACK:: check variable type
			if (this.varType==KWByte.constName) {
				
				this.appendAfter.Add(0);
				
			}
			else if (this.varType==KWShort.constName) {
				
				this.appendAfter.AddRange(new Byte[2]);
				
			}
			else if (this.varType==KWInteger.constName) {
				
				this.appendAfter.AddRange(new Byte[4]);
				
			}
			
			this.lastReferencedVariable=varName;
			status=ParsingStatus.SEARCHING_NAME;
			
		}
		
		private void processValue (String value) {
			
			String type=this.variables[this.referencedVariable].Item2;
			
			//HACK:: check variable type
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
		
	}
	
}
