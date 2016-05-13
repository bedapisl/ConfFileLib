using System;
using System.Collections.Generic;

namespace ConfLib
{
	public class Parser
	{
		
		public static char[] whitespaces = {' ', '\t'};

		//Expect input to have format "x = y ; z", returns tuple(x,y,z). If some part is missing returns empty string instead of the missing part. 
		public static Tuple<string, string, string> GetOptionValuesComment(string s)
		{
			string comment = "";
			string values = "";
			if(s.Contains(";"))
			{
				comment = s.Substring(s.IndexOf(';'));
				s = s.Substring(0, s.IndexOf(';'));
			}

			if(s.Contains("="))
			{
				values = s.Substring(s.IndexOf('=') + 1);
				s = s.Substring(0, s.IndexOf('='));
			}

			string optionIdentifier = s;

			return new Tuple<string, string, string>(RemoveWhitespaces(optionIdentifier), RemoveWhitespaces(values), comment);
		}

		
		// Splits string by delimiter, if the delimiter is not after backslash, and removes whitespaces if they are not after backslashes.
		public static List<string> Split(string s, char delimiter)
		{
			List<string> parts = new List<string>();
			int partStart = 0;
			for(int i=0; i<s.Length; ++i)
			{
				bool IsNonbackslashedDelimiter = s[i] == delimiter && i > 0 && s[i - 1] != '\\';
				if(IsNonbackslashedDelimiter)
				{
					parts.Add(RemoveWhitespaces(s.Substring(partStart, i - partStart)));
					partStart = i + 1;
				}
			}
			
			if(partStart < s.Length)
			{
				parts.Add(RemoveWhitespaces(s.Substring(partStart, s.Length - partStart)));
			}
			
			return parts;
		}


		//Removes whitespaces at the beginning and end of string, but whitespaces with preceeding backslash are counted as normal characters.
		public static string RemoveWhitespaces(string s)
		{
			int valueStart = 0;
			for(int i=0; i<s.Length; ++i)
			{
				if(!IsWhitespace(s[i]))
				{
					valueStart = i;
					break;
				}
			}

			int valueEnd = 0;

			for(int i=s.Length - 1; i >= valueStart; --i)
			{
				bool isNormalCharOrBackslashedWhitespace = !IsWhitespace(s[i]) || (i > 0 && s[i - 1] == '\\');
				if(isNormalCharOrBackslashedWhitespace)
				{
					valueEnd = i + 1;
					break;
				}
			}
			
			return s.Substring(valueStart, valueEnd - valueStart);
		}

		public static bool IsWhitespace(char c)
		{
			return Array.Exists(whitespaces, element => element == c);
		}
	}
}
