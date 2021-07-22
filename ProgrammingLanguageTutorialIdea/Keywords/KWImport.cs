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
			
			if (!(File.Exists(@params[0]))) {
				
				@params[0]+=".sunset";
				if (!(File.Exists(@params[0])))
					throw new ParsingError("Invalid param for \""+constName+"\", should be a valid filepath!");
				
			}
			
			Int32 initialAppendAfterCount=sender.getAppendAfterCount();
			UInt32 startMemAddr=(UInt32)(sender.memAddress+initialAppendAfterCount);
			Parser childParser=new Parser("Child parser",false,true,true,false,false){addEsiToLocalAddresses=true,gui=sender.gui};
			Byte[]data=childParser.parse(File.ReadAllText(@params[0]));
			if (childParser.toggledGui)
				sender.gui=childParser.gui;
			if (!sender.@struct)
				sender.addBlockToAppendAfter(data);
			
			if (childParser.toImport!=null&&!sender.winApp) {
				
				if (sender.setToWinAppIfDllReference)
					sender.winApp=true;
				
				else
					throw new ParsingError("An import referenced a DLL, which are compatible with only Windows apps ("+@params[0]+")");
				
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
			
			foreach (KeyValuePair<String,List<UInt32>>kvp in childParser.referencedFuncPositions) {
				
				if (!sender.referencedFuncPositions.ContainsKey(kvp.Key))
					sender.referencedFuncPositions.Add(kvp.Key,new List<UInt32>());
				
				foreach (UInt32 i in kvp.Value) {
					
					sender.referencedFuncPositions[kvp.Key].Add((UInt32)(sender.getOpcodesCount()+initialAppendAfterCount+i));
					if (!sender.refdFuncsToIncreaseWithOpcodes.ContainsKey(kvp.Key))
						sender.refdFuncsToIncreaseWithOpcodes.Add(kvp.Key,new List<Int32>());
					sender.refdFuncsToIncreaseWithOpcodes[kvp.Key].Add(sender.referencedFuncPositions[kvp.Key].Count-1);
				}
			}
			
			String className=@params[0].Split('.')[0].Split(new Char[]{'\\','/'}).Last();
			className=className.Contains("\\")?className.Split('\\').Last():className;
			if (sender.importedClasses.Select(x=>x.className).Contains(className))
				throw new ParsingError("Class (with same name) already imported: \""+className+'"');
			Class cl=new Class(className,(UInt32)data.Length,childParser.@struct?ClassType.STRUCT:ClassType.NORMAL,startMemAddr,childParser,childParser.memAddress,(UInt32)initialAppendAfterCount,(UInt32)childParser.getAppendAfterCount());
			sender.importedClasses.Add(cl);
			sender.staticClassReferences.Add(cl,new List<UInt32>());
			
			if (!sender.keywordMgr.classWords.Contains(className))
				sender.keywordMgr.classWords.Add(className);
			
			return base.execute(sender, @params);
			
		}
		
	}
}
