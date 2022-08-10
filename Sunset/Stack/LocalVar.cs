/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/17/2021
 * Time: 12:31 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Sunset.Stack {
	
	public class LocalVar : IPseudoStackItem {
		
		public ItemType type { set;  get; }
		public readonly String varName;
		public LocalVar (String varName) { 
			this.varName=varName; 
			type=ItemType.LOCAL_VAR;
		}
		
	}
	
}
