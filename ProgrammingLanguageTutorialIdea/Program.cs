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
			
			Console.WriteLine(Marshal.SizeOf(typeof(PEHeader)));
			
			UInt32 memAddress=0x004001000;
			
			List<Byte> opcodes=new List<Byte>(),importOpcodes=null,finalBytes=new List<Byte>();
			opcodes.Add(0xC3);
			++memAddress;
			
			Console.WriteLine(memAddress.ToString("X"));
			PEHeader hdr=PEHeaderFactory.newHdr(opcodes,importOpcodes,memAddress,0);
			
			while(opcodes.Count%512!=0)
				opcodes.Add(0);
			
			finalBytes.AddRange(hdr.toBytes());
			finalBytes.AddRange(opcodes);
			if (importOpcodes!=null)
				finalBytes.AddRange(importOpcodes);
			
			const String outputFilename="thing.exe";
			
			File.WriteAllBytes(outputFilename,finalBytes.ToArray());
			
			UInt32 checkSum;
			Program.MapFileAndCheckSum(outputFilename,out checkSum,out checkSum);
			using (FileStream fs=File.Open(outputFilename,FileMode.Open)) {
				
				fs.Seek(216,SeekOrigin.Current);
				fs.Write(BitConverter.GetBytes(checkSum),0,4);
				
			}
			
			halt:goto halt;
			
		}
		
	}
	
}