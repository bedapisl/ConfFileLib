using System;
using System.IO;
using System.Collections.Generic;

namespace ConfLib
{
	public enum RequiredStatus
	{
		Required,
		Optional
	}

	public enum ExceptionType
	{
		BadFormat,
		UnknownOption,
		UnknownSection,
		MultipleSectionDefinitions,
		MultipleOptionDefinitions,
		MissingSection,
		MissingOption,
		ConversionError,
		WrongEnumValue,
		InvalidLink,
		ValueOutOfBounds
	}

	public class ConfigurationFile : Dictionary<string, Section>
	{
		public bool throwOnError {get; set;}
		public int getNumSections() {return this.Count;}
		public List<string> errors {get; internal set;}

		private List<string> sectionsOrder = new List<string>();
		private List<string> linesBeforeFirstSection = new List<string>();
		
		public ConfigurationFile(bool throwOnError = true)
		{
			this.throwOnError = throwOnError;
			this.errors = new List<string>();
		}

		public void Load(StreamReader input, bool strict)
		{
			List<string> lines = new List<string>();

			while(!input.EndOfStream)
			{
				lines.Add(input.ReadLine());
			}
			
			Load(lines, strict);
		}

		public void Load(string filename, bool strict)
		{
			List<string> lines = new List<string>(File.ReadLines(filename));

			Load(lines, strict);
		}

		public void Save(StreamWriter output, bool defaults)
		{
			List<string> lines = Save(defaults);
			foreach(string line in lines)
			{
				output.WriteLine(line);
			}
		}

		public void Save(string filename, bool defaults)
		{
			StreamWriter output = new StreamWriter(filename);
			Save(output, defaults);
			output.Close();
		}
	
		public void Add(Section s)
		{
			base.Add(s.identifier, s);
		}

		internal static void ThrowException(string message, ExceptionType typeOfException)
		{
			switch(typeOfException)
			{
				case (ExceptionType.BadFormat):
					throw new BadFormatException(message);
					break;

				case(ExceptionType.UnknownOption):
					throw new UnknownOptionException(message);
					break;

				case(ExceptionType.UnknownSection):
					throw new UnknownSectionException(message);
					break;

				case(ExceptionType.MultipleSectionDefinitions):
					throw new MultipleSectionDefinitionsException(message);
					break;

				case(ExceptionType.MultipleOptionDefinitions):
					throw new MultipleOptionDefinitionsException(message);
					break;

				case(ExceptionType.MissingSection):
					throw new MissingSectionException(message);
					break;

				case(ExceptionType.MissingOption):
					throw new MissingOptionException(message);
					break;
	
				case(ExceptionType.ConversionError):
					throw new ConversionErrorException(message);
					break;
	
				case(ExceptionType.WrongEnumValue):
					throw new WrongEnumValueException(message);
					break;
	
				case(ExceptionType.InvalidLink):
					throw new InvalidLinkException(message);
					break;
				
				case(ExceptionType.ValueOutOfBounds):
					throw new ValueOutOfBoundsException(message);
					break;
			}
		}


		internal void HandleError(string errorMessage, int lineNumber, ExceptionType typeOfException)
		{
			errorMessage = "Line " + lineNumber.ToString() + ": " + errorMessage;
			if(throwOnError)
			{
				ThrowException(errorMessage, typeOfException);	
			}
			else
			{
				errors.Add(errorMessage);
			}
		}

		private void Load(List<string> lines, bool strict)
		{
			errors.Clear();
			sectionsOrder.Clear();

			bool firstSectionStarted = false;
			foreach(var s in this)
			{
				s.Value.loaded = false;
			}

			List<string> accumulator = new List<string>();
			for(int i=0; i<lines.Count; ++i)
			{
				string line = lines[i];
				List<string> parts = new List<string>(line.Split(';'));
				if(parts[0].Length > 0 && parts[0][0] == '[' && parts[0].Contains("]"))
				{
					if(!firstSectionStarted)
					{
						linesBeforeFirstSection = accumulator;
						firstSectionStarted = true;
					}
					else
					{
						LoadSection(accumulator, strict, i - accumulator.Count + 1);
					}
					accumulator = new List<string>();
				}

				accumulator.Add(line);

				//Dont want anything but whitespaces and comments outside sections
				if(!firstSectionStarted && strict && string.IsNullOrWhiteSpace(parts[0]))
				{	
					HandleError("Unknown characters before first section", i, ExceptionType.BadFormat);
				}
			}

			LoadSection(accumulator, strict, lines.Count - accumulator.Count + 1);

			CheckRequiredLoaded();
			ResolveLinks();
		}

		private void CheckRequiredLoaded()
		{
			foreach(var s in this)
			{
				if((s.Value.required == RequiredStatus.Required) && (!s.Value.loaded))
				{
					HandleError("Required section " + s.Value.identifier + " is missing.", 1, ExceptionType.MissingSection);
				}
			}
		}

		private void ResolveLinks()
		{
			foreach(var section in this)
			{
				section.Value.ResolveLinks();
			}
		}

		private void LoadSection(List<string> lines, bool strict, int lineNumber)
		{
			string identifier = lines[0].Substring(1, lines[0].IndexOf(']') - 1);
			Section section = null;
			try
			{
				section = this[identifier];
			}
			catch (KeyNotFoundException)
			{
				if(strict)
				{
					HandleError("Unknown section: " + identifier, lineNumber, ExceptionType.UnknownSection);
				}
				
				section = new Section(identifier);
				section.isKnown = false;
				Add(identifier, section);
			}
		
			section.LoadSection(lines, strict, lineNumber, this);
			sectionsOrder.Add(identifier);
		}

		private List<string> Save(bool defaults)
		{
			List<string> lines = linesBeforeFirstSection;
			foreach(string identifier in sectionsOrder)
			{
				lines.AddRange(this[identifier].Save(defaults));
			}
			return lines;
		}
	}

	public class ConfLibException : Exception
	{
		public ConfLibException()
		{
		}

		public ConfLibException(string message)
			: base(message)
		{
		}

		public ConfLibException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}

	public class BadFormatException : ConfLibException
	{
		public BadFormatException(string message) : base(message) { }
	}

	public class UnknownOptionException : ConfLibException
	{
		public UnknownOptionException(string message) : base(message) { }
	}
		
	public class UnknownSectionException : ConfLibException
	{
		public UnknownSectionException(string message) : base(message) { }
	}

	public class MultipleSectionDefinitionsException : ConfLibException
	{
		public MultipleSectionDefinitionsException(string message) : base(message) { }
	}
		
	public class MultipleOptionDefinitionsException : ConfLibException
	{
		public MultipleOptionDefinitionsException(string message) : base(message) { }
	}

	public class MissingSectionException : ConfLibException
	{
		public MissingSectionException(string message) : base(message) { }
	}

	public class MissingOptionException : ConfLibException
	{
		public MissingOptionException(string message) : base(message) { }
	}

	public class ConversionErrorException : ConfLibException
	{
		public ConversionErrorException(string message) : base(message) { }
	}
		
	public class WrongEnumValueException : ConfLibException
	{
		public WrongEnumValueException(string message) : base(message) { }
	}

	public class InvalidLinkException: ConfLibException
	{
		public InvalidLinkException(string message) : base(message) { }
	}

	public class ValueOutOfBoundsException : ConfLibException
	{
		public ValueOutOfBoundsException(string message) : base(message) { }
	}

	public class CircleLinksException : ConfLibException
	{
		public CircleLinksException() { }
	}
}

