/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/13/2021
 * Time: 12:04 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ProgrammingLanguageTutorialIdea {
	
	internal class Program {
		
		[DllImport("ImageHlp.dll")]
		static extern private UInt32 MapFileAndCheckSum (String Filename,out UInt32 HeaderSum,out UInt32 CheckSum);
		private static TextWriter tw=Console.Out;
		private static Boolean silenced=false;
		
		public static void Main (String[] args) {
			
			if (args.Length!=0&&args.First()=="help") {
				
				Console.WriteLine("\nTo compile, set the argument to the entry file.");
				Console.WriteLine("An example would be \"C:\\fakepath\\MySourceFile.Sunset\"");
				Console.WriteLine("\n-- Flags --\n");
				Console.WriteLine("Flags are optional arguments that can be added when compiling");
				Console.WriteLine("They can be added before or after the file path argument");
				Console.WriteLine("The flags (don't actually print the double quotes):\n");
				Console.WriteLine(" - \"-v\" ~ this stands for \"verbose\" and will print all compiler debug output\n");
				Console.WriteLine(" - \"-s\" ~ this stands for \"silence\" and will disable error/compiling result output\n");
				return;
				
			}
			
			Program.processFlags(args,out args);
			
			if (args.Length!=1)
				Program.exitWithError("Expected 1 argument (path of file), with optional flags. Set argument to \"help\" to see flags.",1);
			if (!(File.Exists(args[0])))
				Program.exitWithError("Invalid filepath: \""+args[0]+'"',2);
			
			Parser psr=new Parser("Main parser"){className=args[0].Split('.')[0].Split(new Char[]{'\\','/'}).Last()};
			
			String outputFilename=args[0].Contains('.')?args[0].Split('\\').Last().Split('/').Last().Split('.').First()+".exe":"output.exe",sourceFilename=args[0];

			try {
				File.WriteAllBytes(outputFilename,psr.parse(File.ReadAllText(sourceFilename)));
				Program.enableOutput();
			}
			catch (ParsingError ex) {
				
				Program.enableOutput();
				#if DEBUG
				Console.WriteLine("Error compiling: "+ex.ToString());
				#else
				Console.WriteLine("There was an error in your code: "+ex.Message);
				#endif
				return;
				
			}
			catch (IOException ex) {
				
				Program.enableOutput();
				#if DEBUG
				Console.WriteLine("Error compiling: "+ex.ToString());
				#else
				Console.WriteLine("There was an error writing to the file: "+ex.Message);
				#endif
				return;
				
			}
			catch (Exception ex) {
				
				Program.enableOutput();
				Console.WriteLine("Unexpected exception: "+ex.ToString());
				return;
				
			}
			
			UInt32 checkSum;
			Program.MapFileAndCheckSum(outputFilename,out checkSum,out checkSum);
			using (FileStream fs=File.Open(outputFilename,FileMode.Open)) {
				
				fs.Seek(216,SeekOrigin.Current);
				fs.Write(BitConverter.GetBytes(checkSum),0,4);
				
			}
			
			Console.WriteLine("\n\nDone compiling,\nSource file: "+sourceFilename+"\nOutput file: "+outputFilename+"\nChecksum: "+checkSum.ToString("X")+"\nAt: "+DateTime.Now.ToString()+'\n');
			
		}
		
		private static void enableOutput () {
			
			if (Program.silenced) {
				Program.disableOutput();
				return;
			}
			
			Console.SetOut(Program.tw);
			Console.SetError(Program.tw);
			
		}
		
		private static void exitWithError (String str,Int32 exitCode) {
			
			Console.ForegroundColor=ConsoleColor.Red;
			Console.WriteLine("\n\n[!] FATAL: "+str+'\n');
			Console.ForegroundColor=ConsoleColor.Gray;
			Environment.Exit(exitCode);
			
		}
		
		private static void processFlags (String[] args,out String[] newArgs) {
			
			if (!args.Contains("-v"))
				Program.disableOutput();
			Program.silenced=args.Contains("-s");
			newArgs=args.Where(x=>x!="-s"&&x!="-v").ToArray();
			
		}
		
		private static void disableOutput () {
			
			Console.SetOut(TextWriter.Null);
			Console.SetError(TextWriter.Null);
			
		}
		
	}
	
}