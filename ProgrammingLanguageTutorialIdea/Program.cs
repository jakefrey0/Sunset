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
		
		public static void Main (String[] args) {
			
			Parser psr=new Parser();
			
			const String outputFilename="thing.exe",sourceFilename="source.txt";
			
			try {
				File.WriteAllBytes(outputFilename,psr.parse(File.ReadAllText(sourceFilename)));
			}
			catch (ParsingError ex) {
				
				#if DEBUG
				Console.WriteLine("Error compiling: "+ex.ToString());
				#else
				Console.WriteLine("There was an error in your code: "+ex.Message);
				#endif
				
			}
			catch (IOException ex) {
				
				#if DEBUG
				Console.WriteLine("Error compiling: "+ex.ToString());
				#else
				Console.WriteLine("There was an error writing to the file: "+ex.Message);
				#endif
				
			}
			catch (Exception ex) {
				
				Console.WriteLine("Unexpected exception: "+ex.ToString());
				
			}
			
			UInt32 checkSum;
			Program.MapFileAndCheckSum(outputFilename,out checkSum,out checkSum);
			using (FileStream fs=File.Open(outputFilename,FileMode.Open)) {
				
				fs.Seek(216,SeekOrigin.Current);
				fs.Write(BitConverter.GetBytes(checkSum),0,4);
				
			}
			
			Console.WriteLine("\n\nDone compiling,\nSource file: "+sourceFilename+"\nOutput file: "+outputFilename+"\nChecksum: "+checkSum.ToString("X")+'\n');
			
		}
		
	}
	
}