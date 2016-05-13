using System;
using System.Collections.Generic;

namespace ConfLib
{
	public abstract class ElementSuperBase<ValueType>
	{
		public ValueType elementValue 
		{
			get
			{
				return this.elementValueImpl;
			}
			set
			{
				isLink = false;
				this.elementValueImpl = value;
			}
		}

		public bool isLink {get; internal set;}
		
		internal string linkAddress;
		
		protected ValueType elementValueImpl;

		abstract public List<ValueType> GetLinkValue();
	
		abstract internal void ResolveLink(string sectionName, string optionName, ConfigurationFile confFile);

		abstract internal string Save();

	}

	public abstract class ElementBase<ValueType, OptionType> : ElementSuperBase<ValueType> where OptionType : Option
	{
		public OptionType linkedOption
		{
			get
			{
				return this.linkedOptionImpl;
			}
			set
			{
				isLink = true;
				linkedOptionImpl = value;
			}
		}

		protected OptionType linkedOptionImpl;

		protected bool onLinkPath = false;

		override public List<ValueType> GetLinkValue()
		{
			if(!isLink)
			{
				return new List<ValueType>{elementValue};
			}

			if(onLinkPath)
			{
				throw new CircleLinksException();
			}

			onLinkPath = true;

			List<ValueType> values = new List<ValueType>();

			List<object> elements = linkedOption.GetAllElements();

			foreach(object element in elements)
			{
				ElementSuperBase<ValueType> castedElement = (ElementSuperBase<ValueType>)(element);
				values.AddRange(castedElement.GetLinkValue());
			}
			return values;
		}

		override internal void ResolveLink(string sectionName, string optionName, ConfigurationFile confFile)
		{
			this.linkedOption = (OptionType)(confFile[sectionName][optionName]);
		}

		override internal string Save()
		{
			if(isLink)
			{
				return "${" + linkedOptionImpl.section.identifier + "#" + linkedOptionImpl.identifier + "}";
			}
			else
			{
				return this.elementValue.ToString();
			}
		}

		protected ElementBase() 
		{ 
			this.onLinkPath = false;
		}

	}

	public class BooleanElement : ElementBase<bool, BooleanOption>
	{	
		public BooleanElement()
		{
		}
		
		public BooleanElement(bool value)
		{
			this.elementValue = value;
			this.isLink = false;
		}

		public BooleanElement(BooleanOption link)
		{
			
			this.linkedOption = link;
			this.isLink = true;
		}
	}

	public class SignedElement : ElementBase<long, SignedOption>
	{		
		public SignedElement()
		{
		}

		public SignedElement(long value)
		{	

			this.elementValue = value;
			this.isLink = false;
		}

		public SignedElement(SignedOption link)
		{	
			this.linkedOption = link;
			this.isLink = true;
		
		}	
	}

	public class UnsignedElement : ElementBase<ulong, UnsignedOption>
	{	
		public UnsignedElement()
		{
		}
	
		public UnsignedElement(ulong value)
		{
			this.elementValue = value;
			this.isLink = false;
		}

		public UnsignedElement(UnsignedOption link)
		{	
			this.linkedOption = link;
			this.isLink = true;
		}	
	}

	public class FloatElement : ElementBase<double, FloatOption>
	{	
		public FloatElement()
		{
		}

		public FloatElement(double value)
		{
			this.elementValue = value;
			this.isLink = false;
		}

		public FloatElement(FloatOption link)
		{	
			this.linkedOption = link;
			this.isLink = true;
		}	
	}

	public class EnumElement : ElementBase<string, EnumOption>
	{	
		public EnumElement()
		{
		}

		public EnumElement(string value)
		{
			this.elementValue = value;
			this.isLink = false;
		}

		public EnumElement(EnumOption link)
		{	
			this.linkedOption = link;
			this.isLink = true;
		}	
	}

	public class StringElement : ElementBase<string, StringOption>
	{	
		public StringElement()
		{
		}

		public StringElement(string value)
		{
			this.elementValue = value;
			this.isLink = false;
		}

		public StringElement(StringOption link)
		{	
			this.linkedOption = link;
			this.isLink = true;
		}	
	}
}






