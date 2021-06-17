/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/29/2021
 * Time: 11:39 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ProgrammingLanguageTutorialIdea {
	
	public enum ParsingStatus {
		
		SEARCHING_NAME,READING_NAME,
		SEARCHING_VARIABLE_NAME,READING_VARIABLE_NAME,
		SEARCHING_VALUE,READING_VALUE,
		SEARCHING_ARRAY_NAME,READING_ARRAY_NAME,
		READING_ARRAY_INDEXER,
		SEARCHING_PARAMETERS,READING_PARAMETERS,
		SEARCHING_FUNCTION_NAME,READING_FUNCTION_NAME
		
	}
	
}
