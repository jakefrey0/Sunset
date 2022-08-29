/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 7/11/2021
 * Time: 2:34 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Sunset.Keywords {
	
	public class KWEnum : Keyword {
		
		public const String constName="ENUM";
		
		public KWEnum () : base (constName,KeywordType.CLASS_TYPE_SETTER) { }
		
		public override KeywordResult execute(Parser sender,String[]@params) {
			
			if (sender.namesChked!=1)
				throw new ParsingError("Enum must be the first and only keyword of the file.",sender);
			sender.@enum=true;
			
			return base.execute(sender,@params);
			
		}
		
	}
	
}

