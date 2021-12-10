/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/6/2021
 * Time: 6:07 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ProgrammingLanguageTutorialIdea.Stack;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWSwitch : Keyword {
		
		public const String constName="switch";
		
		public KWSwitch () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute (Parser sender,String[]@params) {
			
			if (@params.Length!=1) throw new ParsingError("Expected 1 parameter for \""+constName+'"');
			
			sender.pushValue(@params[0]);
			sender.pseudoStack.push(new SwitchVar());
			sender.addBlock(new Block(delegate{sender.pseudoStack.pop();},sender.GetStaticInclusiveAddress(),new Byte[0]/*ADD ESP,4*/){isLoopOrSwitchBlock=true,switchBlock=true,afterBlockClosedOpcodes=new Byte[]{0x83,0xC4,4}});
			
			return base.execute(sender,@params);
			
		}
		
	}
	
}
