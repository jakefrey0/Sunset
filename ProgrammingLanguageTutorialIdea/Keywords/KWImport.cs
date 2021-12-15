/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 7/11/2021
 * Time: 12:59 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWImport : Keyword {
		
		public const String constName="import";
		
		public KWImport () : base(constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute (Parser sender,String[]@params) {
			
			if (@params.Length!=1)
				throw new ParsingError("Invalid param count for native call \""+KWImport.constName+"\" (expected 1)");
			
			String fp=@params[0];
			List<Tuple<String,Tuple<String,VarType>>>passedTypes=null;
			String passingTypesUnparsed=null;
			Int32 startsPassingTypesNo=fp.Where(x=>sender.startsPassingTypes(x)).Count();
			List<String>classWords=new List<String>();
			if (startsPassingTypesNo!=0&&startsPassingTypesNo==fp.Where(x=>sender.endsPassingTypes(x)).Count()) {
				
				Int32 sptIndex=fp.IndexOf('<')+1;
				passingTypesUnparsed=fp.Substring(sptIndex,fp.LastIndexOf('>')-sptIndex);
				fp=fp.Split('<')[0];
				passedTypes=new List<Tuple<String,Tuple<String,VarType>>>();
				foreach (String s in passingTypesUnparsed.Split(',')) {
					Tuple<String,VarType>vt=sender.getVarType(s);
					passedTypes.Add(new Tuple<String,Tuple<String,VarType>>(null,vt));
                    Console.WriteLine(vt.Item1+","+vt.Item2.ToString());
					if (vt.Item2==VarType.CLASS)
						classWords.Add(vt.Item1);
                    else if (vt.Item2==VarType.NATIVE_ARRAY) {
                        Tuple<String,VarType>varType=vt;
                        while (varType.Item2==VarType.NATIVE_ARRAY) {
                            varType=sender.getVarType(vt.Item1);
                            if (varType.Item2==VarType.CLASS)
                                classWords.Add(varType.Item1);
                        }   
                    }
				}
				
			}
			
			if (!(File.Exists(fp))) {
				
				fp+=".sunset";
				if (!File.Exists(fp)) {

                    if (!sender.hasAttatchedProject)
                        throw new ParsingError("Invalid param for \""+constName+"\", should be a valid filepath! (Got \""+fp+"\")");

                    fp=sender.attatchedProject.projPath+'\\'+fp;
                    if (!File.Exists(fp)) {
                        fp=String.Concat(fp.Take(fp.Length-7));
                        if (!File.Exists(fp))
                            throw new ParsingError("Invalid param for \""+constName+"\", should be a valid filepath! (Got \""+fp+"\")");
                    }

                }
				
			}

			String className=GetClassName(fp);
            Int32 initialDataSectBytesCount=Parser.dataSectBytes.Count;
			Parser childParser=new Parser("Child parser",fp,false,true,true,false,false){addEsiToLocalAddresses=true,gui=sender.gui,className=className,hasAttatchedProject=sender.hasAttatchedProject,attatchedProject=sender.attatchedProject};
            
            if (!String.IsNullOrEmpty(passingTypesUnparsed))
                className+='<'+passingTypesUnparsed+'>';
            className=className.Contains("\\")?className.Split('\\').Last():className;
            String path=Path.GetFullPath(fp),id=CreateClassID(fp,className);
			childParser.keywordMgr.classWords=classWords;

            if (!Parser.classSkeletons.ContainsKey(id)) {

                foreach (String cw_name in classWords)
                    childParser.importedClasses.AddRange(sender.importedClasses.Where(x=>x.className==cw_name));
                if (passedTypes!=null)
                    childParser.passedVarTypes=passedTypes;
                Byte[]data=childParser.parse(File.ReadAllText(fp));

			    Parser.dataSectBytes.AddRange(data.Take((Int32)childParser.byteCountBeforeDataSect));
                Parser.classSkeletons.Add(id,(UInt32)(Parser.dataSectBytes.Count-childParser.byteCountBeforeDataSect));

                if (childParser.toggledGui)
                    sender.gui=childParser.gui;

            }
			
			if (childParser.toImport!=null&&!sender.winApp) {
				
				if (sender.setToWinAppIfDllReference)
					sender.winApp=true;
				
				else
					throw new ParsingError("An import referenced a DLL, which are compatible with only Windows apps ("+fp+")");
				
			}
			
			foreach (KeyValuePair<String,List<String>>kvp in childParser.toImport) {
				
				if (!(sender.toImport.ContainsKey(kvp.Key))) {
					sender.toImport.Add(kvp.Key,new List<String>());
					Console.WriteLine("Imported \""+kvp.Key+'"');
				}
				
				foreach (String str in kvp.Value) {
					
					if (sender.toImport[kvp.Key].Contains(str))
						continue;
					
					sender.toImport[kvp.Key].Add(str);
					Console.WriteLine("Imported \""+str+"\" from \""+kvp.Key+'"');
					
				}
				
			}
			
			foreach (KeyValuePair<String,List<OpcodeIndexReference>>kvp in childParser.referencedFuncPositions) {
				
				if (!sender.referencedFuncPositions.ContainsKey(kvp.Key))
					sender.referencedFuncPositions.Add(kvp.Key,new List<OpcodeIndexReference>());

                foreach (OpcodeIndexReference i in kvp.Value) {
                    
                    if (i.type!=OpcodeIndexType.CODE_SECT_REFERENCE)
                        sender.referencedFuncPositions[kvp.Key].Add(i);
                }

			}
			
			sender.keywordMgr.classWords.AddRange(childParser.keywordMgr.classWords);
            foreach (KeyValuePair<String,String>kvp in childParser.keywordMgr.acknowledgements) {
                if (!sender.keywordMgr.acknowledgements.ContainsKey(kvp.Key))
                    sender.keywordMgr.acknowledgements.Add(kvp.Key,kvp.Value);
            }
			foreach (KeyValuePair<String,Tuple<String,VarType>>kvp in childParser.acknowledgements) {
                if (!sender.acknowledgements.ContainsKey(kvp.Key))
                    sender.acknowledgements.Add(kvp.Key,kvp.Value);
            }

			if (sender.importedClasses.Select(x=>x.className).Contains(className))
				throw new ParsingError("Class (with same name) already imported: \""+className+'"');

            Class cl;
		    if (Parser.classByIDs.ContainsKey(id))
                cl=Parser.classByIDs[id];
            else {
                cl=new Class(className,path,childParser.byteCountBeforeDataSect,childParser.@struct?ClassType.STRUCT:ClassType.NORMAL,childParser,childParser.memAddress,(UInt32)initialDataSectBytesCount,(UInt32)childParser.getAppendAfterCount(),id);
                Parser.classByIDs.Add(id,cl);
            }
            sender.importedClasses.Add(cl);
			sender.staticClassReferences.Add(cl,new List<OpcodeIndexReference>());

			if (!sender.keywordMgr.classWords.Contains(className))
				sender.keywordMgr.classWords.Add(className);

			return base.execute(sender, @params);
			
		}

        public static String CreateClassID (String fileName,String className) {

            return "path: \""+Path.GetFullPath(fileName)+"\" && name: \""+className+'"';
        }
		
        public static String GetClassName (String path) {

            return path.Split('.')[0].Split(new Char[]{'\\','/'}).Last();

        }

	}
}
