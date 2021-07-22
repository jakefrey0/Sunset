#define NO_OUTPUT
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

namespace ProgrammingLanguageTutorialIdea {
	
	internal class Program {
		
		[DllImport("ImageHlp.dll")]
		static extern private UInt32 MapFileAndCheckSum (String Filename,out UInt32 HeaderSum,out UInt32 CheckSum);
		private static TextWriter tw=Console.Out;
		
		public static void Main (String[] args) {
			
			#if NO_OUTPUT
			Console.SetOut(TextWriter.Null);
			Console.SetError(TextWriter.Null);
			#endif
			
			if (args.Length!=1)
				Program.exitWithError("Expected 1 argument (path of file)",1);
			if (!(File.Exists(args[0])))
				Program.exitWithError("Invalid filepath: \""+args[0]+'"',2);
			
			Parser psr=new Parser("Main parser");
			
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
			
			Console.SetOut(Program.tw);
			Console.SetError(Program.tw);
			
		}
		
		private static void exitWithError (String str,Int32 exitCode) {
			
			Console.ForegroundColor=ConsoleColor.Red;
			Console.WriteLine("\n\n[!] FATAL: "+str+'\n');
			Console.ForegroundColor=ConsoleColor.Gray;
			Environment.Exit(exitCode);
			
		}
		
	}
	
}