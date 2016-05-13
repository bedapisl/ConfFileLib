using System;
using System.Collections;
using System.Collections.Generic;


namespace ConfLib
{
	public class Section : Dictionary<string, Option>
	{
		public string identifier {get; set;}
		public RequiredStatus required {get; set;}
		public string description {get; set;}
		public uint numOptions {get{return this.numOptions;}}
		public bool isKnown {get; internal set;}

		internal bool loaded = false;
		internal ConfigurationFile confFile = null;

		protected List<string> optionsOrder = new List<string>();
		protected string comment;
		protected int lineNumber;

		public Section(string identifier, string description = "", RequiredStatus required = RequiredStatus.Required)
		{
			this.identifier = identifier;
			this.description = description;
			this.required = required;
		}

		public void Add(Option o)
		{
			base.Add(o.identifier, o);
		}
	
		internal void LoadSection(List<string> lines, bool strict, int lineNumber, ConfigurationFile confFile)
		{
			if(this.loaded)
			{
				HandleError("Section " + this.identifier + " was defined multiple times", lineNumber, ExceptionType.MultipleSectionDefinitions);
				return;
			}

			foreach(var option in this)
			{
				option.Value.loaded = false;
			}

			this.confFile = confFile;
			this.lineNumber = lineNumber;
			this.comment = Parser.GetOptionValuesComment(lines[0]).Item3;
			this.optionsOrder.Clear();
			
			for(int i=1; i<lines.Count; ++i)
			{
				Tuple<string, string, string> optionValuesComment = Parser.GetOptionValuesComment(lines[i]);
				string identifier = optionValuesComment.Item1;
				if(identifier == "")
				{
					continue;
				}

				Option option = null;
				try
				{
					option = this[identifier];
				}
				catch(KeyNotFoundException)
				{	
					if(strict)
					{
						HandleError("Unknown option: " + optionValuesComment.Item1, lineNumber + i, ExceptionType.UnknownOption);
					}
					option = new StringOption(optionValuesComment.Item1);
					option.isKnown = false;
					Add(identifier, option);
				}

				option.LoadOption(lines[i], optionValuesComment.Item2, optionValuesComment.Item3, lineNumber + i, this);
				optionsOrder.Add(identifier);
			}

			this.loaded = true;

			CheckRequiredLoaded();
		}

		internal void ResolveLinks()
		{
			foreach(var o in this)
			{
				o.Value.ResolveLinks();
			}
		}

		internal void HandleError(string errorMessage, int lineNumber, ExceptionType typeOfException)
		{
			if(this.confFile == null)
			{
				ConfigurationFile.ThrowException(lineNumber.ToString() + ": " + errorMessage, typeOfException);	
			}
			this.confFile.HandleError(errorMessage, lineNumber, typeOfException);
		}

		internal List<string> Save(bool defaults)
		{
			List<string> lines = new List<string>();

			lines.Add("[" + identifier + "]" + "\t" + comment);
			foreach(string optionIdentifier in optionsOrder)
			{
				lines.Add(this[optionIdentifier].Save(defaults));
			}
			return lines;
		}

		protected void CheckRequiredLoaded()
		{
			foreach(var o in this)
			{
				if((o.Value.required == RequiredStatus.Required) && (!o.Value.loaded))
				{
					HandleError("Required option " + o.Value.identifier + " is missing.", lineNumber, ExceptionType.MissingOption);
				}
				else if((o.Value.required == RequiredStatus.Optional) && (!o.Value.loaded))
				{
					o.Value.LoadDefaultValues();
				}
			}
		}
	}
}
