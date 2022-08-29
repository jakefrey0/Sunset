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

namespace Sunset {
	
	internal class Program {
		
		[DllImport("ImageHlp.dll")]
		static extern private UInt32 MapFileAndCheckSum (String Filename,out UInt32 HeaderSum,out UInt32 CheckSum);
		private static TextWriter tw=Console.Out;
		private static Boolean silenced=false,showStackTrace=false;
		
		public static void Main (String[] args) {

            SunsetProject sp=new SunsetProject();
            Boolean hasAttatchedProject=false;

			if (args.Length!=0) {
				
                String farg=args.First();

                if (farg=="help") {
    				Console.WriteLine("\nTo compile, set the argument to the entry file.");
    				Console.WriteLine("An example would be \"C:\\fakepath\\MySourceFile.Sunset\"");
    				Console.WriteLine("\n-- Flags --\n");
    				Console.WriteLine("Flags are optional arguments that can be added when compiling");
    				Console.WriteLine("They can be added before or after the file path argument");
    				Console.WriteLine("The flags (don't actually print the double quotes):\n");
    				Console.WriteLine(" - \"-v\" ~ this stands for \"verbose\" and will print all compiler debug output\n");
    				Console.WriteLine(" - \"-s\" ~ this stands for \"silence\" and will disable error/compiling result output\n");
                    Console.WriteLine(" - \"-st\" ~ this stands for \"stack trace\" and will show the stack trace on exception \n");
                    Console.WriteLine("\nTo make a project (optional), use arguments \"mkproj [name] [path]\"");
    				return;
                }
                else if (farg=="mkproj") {
                    
                    if (args.Length<=2) {
                        Console.WriteLine("Argument \"mkproj\" expects additional arguments: project name, and the directory");
                        return;
                    }
                    String dir=Parser.merge(args.Skip(2)," "),pName=args[1],mainFn=mainFn=pName+".sunset";
                    sp=new SunsetProject(){name=pName,mainFn=mainFn,projPath=dir+"\\"+pName+"\\" };
                    TryCreateDir(dir);
                    dir=dir+"\\"+pName+"\\";
                    TryCreateDir(dir);
                    TryCreateDir(dir+"\\bin\\");
                    TryCreateFile(dir+pName+".sunproj",sp.ToBytes());
                    TryCreateFile(dir+mainFn,File.ReadAllBytes("Important/DemoProject.sunset"));
                    Console.WriteLine("Created project succesfully: "+pName+ "\n @ \n"+dir);
                    return;

                }
				
			}
			
			Program.processFlags(args,out args);
			
			if (args.Length!=1)
				Program.exitWithError("Expected 1 argument (path of file), with optional flags. Set argument to \"help\" to see flags. Got "+args.Length.ToString()+" args.",1);
			if (!(File.Exists(args[0])))
				Program.exitWithError("Invalid filepath: \""+args[0]+'"',2);

            String projPath=Path.GetDirectoryName(args[0]);

            if (args[0].EndsWith(".sunproj",StringComparison.CurrentCulture)) {
                
                hasAttatchedProject=true;
                sp=SunsetProjectHelper.LoadProject(args[0]);
                sp.projPath=projPath;
                args[0]=projPath+"/"+sp.mainFn;

            }
			
			Parser psr=new Parser("Main parser",args[0]){className=args[0].Split('.')[0].Split(new Char[]{'\\','/'}).Last(),attatchedProject=sp,hasAttatchedProject=hasAttatchedProject};
			
			String outputFilename=hasAttatchedProject?projPath+"/bin/"+sp.name+".exe":args[0].Contains('.')?args[0].Split('\\').Last().Split('/').Last().Split('.').First()+".exe":"output.exe",sourceFilename=args[0];

			try {
				File.WriteAllBytes(outputFilename,psr.parse(File.ReadAllText(sourceFilename)));
				Program.enableOutput();
			}
			catch (ParsingError ex) {
				
				Program.enableOutput();
				if (showStackTrace)
				    Console.WriteLine("Error compiling: "+ex.ToString());
				else
				    Console.WriteLine("\nThere was an error in your code: "+ex.Message);
				return;
				
			}
			catch (IOException ex) {
				
				Program.enableOutput();
				if (showStackTrace)
				    Console.WriteLine("Error compiling: "+ex.ToString());
				else
				    Console.WriteLine("There was an error writing to the file: "+ex.Message);
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
			
			Console.WriteLine((hasAttatchedProject?"\n\nDone compiling project "+sp.name+",\nSource file: ":"\n\nDone compiling,\nSource file: ")+sourceFilename+"\nOutput file: "+outputFilename+"\nChecksum: "+checkSum.ToString("X")+"\nAt: "+DateTime.Now.ToString()+'\n');
			
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
            Program.enableOutput();
			Console.WriteLine("\n\n[!] FATAL: "+str+'\n');
			Console.ForegroundColor=ConsoleColor.Gray;
			Environment.Exit(exitCode);
			
		}
		
		private static void processFlags (String[] args,out String[] newArgs) {
			
			if (!args.Contains("-v"))
				Program.disableOutput();
			Program.silenced=args.Contains("-s");
            Program.showStackTrace=args.Contains("-st");
			newArgs=args.Where(x=>x!="-s"&&x!="-v"&&x!="-dg"&&x!="-st").ToArray();
			
		}
		
		private static void disableOutput () {
			
			Console.SetOut(TextWriter.Null);
			Console.SetError(TextWriter.Null);
			
		}
		
        private static void TryCreateDir (String dir) {

             if (!Directory.Exists(dir)) {
                try {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception e) {
                    exitWithError("Error creating project: "+e.Message,Int32.MaxValue);
                }
            }

        } 
             
        private static void TryCreateFile (String fn,Byte[] data) {

             if (!File.Exists(fn)) {
                try {
                    FileStream fs=File.Create(fn);
                    fs.Write(data,0,data.Length);
                    fs.Close();
                }
                catch (Exception e) {
                    exitWithError("Error creating project: "+e.Message,Int32.MaxValue-1);
                }
            }
            else exitWithError("Project already exists: "+fn,Int32.MaxValue-2);

        }

	}

    public static class Helpers {

        public static IEnumerable<T> AllButLast<T> (this IEnumerable<T> instances) { return instances.Take(instances.Count()-1); }
		
	 	public static IEnumerable<T> TakeUntil<T> (this IEnumerable<T> instances,Func<T,Boolean>predicate) {
			foreach (T instance in instances) {
				if (predicate(instance)) break;
				yield return instance;
			}
		}

    }
	
}