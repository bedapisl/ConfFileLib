using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace ConfLib
{
	public enum OptionType
	{
		Boolean,
		Signed,
		Unsigned,
		Float,
		Enum,
		String
	}

	public abstract class Option
	{
		public string identifier {get; set;}
		public string description {get; set;}
		public RequiredStatus required {get; set;}
		public bool isKnown {get; internal set;}
		public OptionType type {get; internal set;}
		public char delimiter {get; set;}

		internal bool loaded = false;
		internal Section section = null;
	
		protected int lineNumber;
		protected string comment;
	
		virtual public BooleanOption ConvertToBoolean()
		{
			return (BooleanOption)this;
		}

		virtual public SignedOption ConvertToSigned()
		{
			return (SignedOption)this;
		}

		virtual public UnsignedOption ConvertToUnsigned()
		{
			return (UnsignedOption)this;
		}

		virtual public FloatOption ConvertToFloat()
		{
			return (FloatOption)this;
		}

		virtual public EnumOption ConvertToEnum()
		{
			return (EnumOption)this;
		}

		virtual public StringOption ConvertToString()
		{
			return (StringOption)this;
		}

		protected Option()
		{
			isKnown = true;
			delimiter = ',';
		}

		abstract protected void ParseValues(List<string> values);

		abstract internal void ResolveLinks();

		abstract internal string Save(bool defaults);

		abstract internal void LoadDefaultValues();
	
		abstract internal List<object> GetAllElements();

		internal void LoadOption(string originalLine, string valuesTogether, string comment, int lineNumber, Section section)
		{
			if(this.loaded)
			{
				HandleError("Option " + this.identifier + " was defined multiple times", lineNumber, ExceptionType.MultipleOptionDefinitions);
				return;
			}
			this.loaded = true;
			this.comment = comment;
			this.lineNumber = lineNumber;
			this.section = section;

			List<string> values = Parser.Split(valuesTogether, delimiter);
			ParseValues(values);
		}

		protected void HandleError(string errorMessage, int lineNumber, ExceptionType typeOfException)
		{	
			if(this.section == null)
			{
				ConfigurationFile.ThrowException(lineNumber.ToString() + ": " + errorMessage, typeOfException);	
			}
			this.section.HandleError(errorMessage, lineNumber, typeOfException);
		}
	}

	public abstract class BaseOption<ElementType, ValueType> : Option where ElementType : ElementSuperBase<ValueType>, new()		//tady by taky mozna mohlo byt IEnumerable
	{
		public List<ElementType> defaultValues {get; set;}

		protected List<ElementType> elements = new List<ElementType>();
		
		public int getNumElements()
		{
			return elements.Count;
		}

		public void AddElement(ElementType element)
		{
			elements.Add(element);
		}

		public void RemoveElement(int index)
		{
			elements.RemoveAt(index);
		}

		public ElementType this[int index]
		{
			get
			{
				return elements[index];
			}
			set
			{
				elements[index] = value;
			}
		}

		abstract protected ElementType CreateElement(string textValue);

		override internal void ResolveLinks()
		{
			if(!this.loaded)
			{
				return;
			}

			foreach(ElementType element in elements)
			{
				if(element.isLink)
				{
					string sectionName = element.linkAddress.Substring(0, element.linkAddress.IndexOf('#'));
					string optionName = element.linkAddress.Substring(element.linkAddress.IndexOf('#') + 1);

					try
					{
						//element.linkedOption = (typeof(element.linkedOption))(this.section.confFile[sectionName][optionName]);
						element.ResolveLink(sectionName, optionName, this.section.confFile);
					}
					catch(SystemException)
					{
						HandleError("${" + element.linkAddress + "} is invalid link.", this.lineNumber, ExceptionType.InvalidLink);
					}
				}
			}
		}

		override internal string Save(bool defaults)
		{
			string line = identifier + " = ";
			List<ElementType> elementsToSave = elements;
			if(defaults)
			{
				elements = defaultValues;
			}

			if(elementsToSave.Count > 0)
			{
				line = line + elementsToSave[0].Save();
			}

			for(int i=1; i<elementsToSave.Count; ++i)
			{
				line = line + delimiter.ToString() + elementsToSave[i].Save();
			}

			return line + "\t\t" + comment;
		}

		override internal void LoadDefaultValues()
		{
			this.elements = this.defaultValues;
		}

		override internal List<object> GetAllElements()
		{
			return elements.Cast<object>().ToList();
		}

		protected BaseOption(string identifier, string description, RequiredStatus required, List<ElementType> defaultValue) 
		{
			this.identifier = identifier;
			this.required = required;
			this.description = description;
			this.defaultValues = defaultValue;
		}


		//Loads values of elements from their text representation. Stores links to be resolved later.
		protected override void ParseValues(List<string> textValues)
		{
			elements.Clear();
			foreach(string v in textValues)
			{
				bool isLink = v[0] == '$' && v[1] == '{' && v[v.Length - 1] == '}';
				ElementType newElement = null;
				if(isLink)
				{
					newElement = new ElementType();
					newElement.linkAddress = v.Substring(2, v.Length - 3);
				}
				else
				{
					newElement = CreateElement(v);
				}
				if(newElement != null)
				{
					newElement.isLink = isLink;
					elements.Add(newElement);
				}
			}
		}
	}

	public abstract class NumericOption<ElementType, ValueType> : BaseOption<ElementType, ValueType> 
			where ElementType : ElementSuperBase<ValueType>, new() where ValueType : IComparable
	{
		protected NumericOption(string identifier, string description, RequiredStatus required, 
		                        List<ElementType> defaultValue, ValueType lowerBound, ValueType upperBound) 
								: base(identifier, description, required, defaultValue)
		{
			this.lowerBound = lowerBound;
			this.upperBound = upperBound;
		}

		public ValueType lowerBound {get; private set;}
		public ValueType upperBound {get; private set;}

		abstract protected override ElementType CreateElement(string textValue);

		protected int GetBase(ref string textValue)
		{
			int valueBase = 10;
			if(textValue.Length < 2)
			{
				valueBase = 10;
			}
			else if(textValue.Substring(0, 2) == "0x")
			{
				valueBase = 16;
				textValue = textValue.Substring(2);
			}
			else if(textValue.Substring(0, 2) == "0b")
			{
				valueBase = 2;
				textValue = textValue.Substring(2);
			}
			else if(textValue.Substring(0, 1) == "0")
			{
				valueBase = 8;
				textValue = textValue.Substring(1);
			}
			return valueBase;
		}

		protected ValueType CheckBounds(ValueType v)
		{
			if(v.CompareTo(lowerBound) < 0 || v.CompareTo(upperBound) > 1)
			{
				HandleError("Value " + v.ToString() + " out of bounds", this.lineNumber, ExceptionType.ValueOutOfBounds);
			}
			return v;
		}
	}

	public class BooleanOption : BaseOption<BooleanElement, bool>
	{
		public BooleanOption(string identifier, string description = "", RequiredStatus required = RequiredStatus.Required, 
		                     List<BooleanElement> defaultValue = default(List<BooleanElement>)) : base(identifier, description, required, defaultValue)
		{
			this.type = OptionType.Boolean;
		}

		public override BooleanOption ConvertToBoolean()
		{
			return null;
		}

		protected override BooleanElement CreateElement(string textValue)
		{
			List<string> positive = new List<string>{"1", "t", "y", "on", "yes", "enabled"};
			List<string> negative = new List<string>{"0", "f", "n", "off", "no", "disabled"};

			if(positive.Contains(textValue))
			{
				return new BooleanElement(true);
			}
			else if(negative.Contains(textValue))
			{
				return new BooleanElement(false);
			}
			else
			{
				HandleError("Cannot convert " + textValue + " to Boolean.", this.lineNumber, ExceptionType.ConversionError);
			}
			return null;
		}
	}

	public class SignedOption : NumericOption<SignedElement, long>
	{
		public SignedOption(string identifier, string description = "", RequiredStatus required = RequiredStatus.Required, 
		                    List<SignedElement> defaultValue = default(List<SignedElement>), 
		                    long lowerBound = long.MinValue, long upperBound = long.MaxValue) 
							: base(identifier, description, required, defaultValue, lowerBound, upperBound)
		{
			this.type = OptionType.Signed;
		}

		protected override SignedElement CreateElement(string textValue)
		{
			try 
			{
				int valueBase = GetBase(ref textValue);
				return new SignedElement(CheckBounds(Convert.ToInt64(textValue, valueBase)));
			}
			catch(SystemException)
			{
				HandleError("Cannot convert " + textValue + " to signed integer.", this.lineNumber, ExceptionType.ConversionError);
			}
			return null;
		}
	}

	public class UnsignedOption : NumericOption<UnsignedElement, ulong>
	{				
		public UnsignedOption(string identifier, string description = "", RequiredStatus required = RequiredStatus.Required, 
		                    List<UnsignedElement> defaultValue = default(List<UnsignedElement>), 
		               		ulong lowerBound = ulong.MinValue, ulong upperBound = ulong.MaxValue) : 
							base(identifier, description, required, defaultValue, lowerBound, upperBound)
		{
			this.type = OptionType.Unsigned;
		}

		protected override UnsignedElement CreateElement(string textValue)
		{
			try 
			{
				int valueBase = GetBase(ref textValue);
				return new UnsignedElement(CheckBounds(Convert.ToUInt64(textValue, valueBase)));
			}
			catch(SystemException)
			{
				HandleError("Cannot convert " + textValue + " to unsigned integer.", this.lineNumber, ExceptionType.ConversionError);
			}
			return null;
		}
	}

	public class FloatOption : NumericOption<FloatElement, double>
	{			
		public FloatOption(string identifier, string description = "", RequiredStatus required = RequiredStatus.Required, 
		                   	List<FloatElement> defaultValue = default(List<FloatElement>), 
		                   	double lowerBound = double.MinValue, double upperBound = double.MaxValue) : 
							base(identifier, description, required, defaultValue, lowerBound, upperBound)
		{
			this.type = OptionType.Float;
		}

		protected override FloatElement CreateElement(string textValue)
		{
			try 
			{
				return new FloatElement(CheckBounds(Convert.ToDouble(textValue)));
			}
			catch(Exception)
			{
				HandleError("Cannot convert " + textValue + " to float.", this.lineNumber, ExceptionType.ConversionError);
			}
			return null;
		}
	}

	public class EnumOption : BaseOption<EnumElement, string>
	{	
		public List<string> possibleValues {get; protected set;}

		public EnumOption(List<string> possibleValues, string identifier, string description = "", RequiredStatus required = RequiredStatus.Required, 
		                     List<EnumElement> defaultValue = default(List<EnumElement>)) : base(identifier, description, required, defaultValue)
		{
			this.possibleValues = possibleValues;
			this.type = OptionType.Enum;
		}

		protected override EnumElement CreateElement(string textValue)
		{
			if(possibleValues.Contains(textValue))
			{
				return new EnumElement(textValue);
			}
			HandleError(textValue + " is not a valid enum for this option.", this.lineNumber, ExceptionType.WrongEnumValue);
			return null;
		}
	}

	public class StringOption : BaseOption<StringElement, string>
	{		
		public StringOption(string identifier, string description = "", RequiredStatus required = RequiredStatus.Required, 
		                     List<StringElement> defaultValue = default(List<StringElement>)) : base(identifier, description, required, defaultValue)
		{
			this.type = OptionType.String;
		}

		protected override StringElement CreateElement(string textValue)
		{
			StringBuilder removedBackslashes = new StringBuilder(textValue.Substring(0, 1));
			List<char> backslashableChars = new List<char>{',',':',';'};
			for(int i=1; i<textValue.Length; ++i)
			{
				if(textValue[i - 1] == '\\' && backslashableChars.Contains(textValue[i]))
				{
					removedBackslashes[removedBackslashes.Length - 1] = textValue[i];			//Overwrite backslash
				}
				else
				{
					removedBackslashes.Append(textValue[i]);
				}
			}
			
			return new StringElement(removedBackslashes.ToString());
		}
	}
}
