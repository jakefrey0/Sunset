/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 8/9/2021
 * Time: 5:11 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Sunset.Keywords {
	
	/// <summary>
	/// Note, GetProcessHeap stores the process heap after it is retrieved for the first time (so re calling it isn't detrimental)
	/// </summary>
	public class KWGetProcessHeap : Keyword {
		
		public const String constName="getProcessHeap";
		
		public KWGetProcessHeap () : base (constName,KeywordType.NATIVE_CALL_WITH_RETURN_VALUE) { }
		
		public override KeywordResult execute(Parser sender,String[] @params) {
			
			sender.pushProcessHeapVar();
			outputType=new Tuple<String,VarType>(KWInteger.constName,VarType.NATIVE_VARIABLE);
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_NAME,newOpcodes=new Byte[]{0x58}/*POP EAX*/};
			
		}
		
	}
	
}
