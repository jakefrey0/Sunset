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
		
		private UInt32 memAddress;
		private readonly KeywordMgr keywordMgr;
			
		private List<Byte> opcodes=new List<Byte>(),importOpcodes=null,finalBytes=new List<Byte>();
		private ParsingStatus status;
		
		public Parser (Boolean winApp=true) {
			
			memAddress=winApp?0x004001000:(UInt32)0;
			keywordMgr=new KeywordMgr();
			
		}
		
		public Byte[] parse (String data) {
			
			status=ParsingStatus.SEARCHING_NAME;
			StringBuilder nameReader=new StringBuilder();
			
			foreach (Char c in data) {
				
				switch (status) {
						
					case ParsingStatus.SEARCHING_NAME:
						if (!this.isFormOfBlankspace(c)) {
							
							nameReader.Append(c);
							status=ParsingStatus.READING_NAME;
							
						}
						break;
						
					case ParsingStatus.READING_NAME:
						
						if (Char.IsLetterOrDigit(c)) nameReader.Append(c);
						else {
							
							this.chkName(nameReader.ToString());
							nameReader.Clear();
							
						}
						
						break;
						
				}
				
			}
			
			return compile();
			
		}
		
		private Byte[] compile () {
			
			this.addByte(0xC3); //Add RETN call to end of our exe, so no matter what happens in terms of the source, it should not be a blank application & will exit
			
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
			
			opcodes.Add(b);
			++memAddress;
			
		}
		
		private void addBytes (IEnumerable<Byte> bytes) {
			
			foreach (Byte b in bytes)
				this.addByte(b);
			
		}
		
		private void chkName (String name) {
			
			Console.WriteLine("Got name: \""+name+'"');
			
			foreach (Keyword kw in this.keywordMgr.getKeywords().Where(x=>!x.hasParameters)) {
				
				if (kw.name==name) {
					
					KeywordResult res=kw.execute();
					this.status=res.newStatus;
					this.addBytes(res.newOpcodes);
					return;
					
				}
				
			}
			
			throw new ParsingError("Unexpected name: \""+name+'"');
			
		}
		
		private Boolean isFormOfBlankspace (Char c) {
			
			return c==' '||c=='\n'||c=='\r'||c=='\t';
			
		}
		
	}
	
}
