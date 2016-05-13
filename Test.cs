using System;
using System.IO;
using System.Collections.Generic;

namespace ConfLib
{
	public class Test
	{
		public static void Main()
		{
			ConfigurationFile f = new ConfigurationFile(false);
			Section first = new Section("Sekce 1", "");
			first.Add(new StringOption("Option 1", "", RequiredStatus.Required, null));
			first.Add(new StringOption("oPtion 1"));
			StringElement defaultLinkElement = new StringElement(first["Option 1"].ConvertToString());
			StringElement defaultValueElement = new StringElement("Blabol");
			first.Add(new StringOption("Default option", "", RequiredStatus.Optional, new List<StringElement>{defaultLinkElement, defaultValueElement}));

			f.Add(first);

			Section second = new Section("$Sekce::podsekce", "");
			for(int i=1; i<6; ++i)
			{
				second.Add(new StringOption("Option " + i.ToString()));
			}
			second["Option 2"].delimiter = ':';
			f.Add(second);

			Section third = new Section("Cisla", "");
			third.Add(new SignedOption("cele"));
			third.Add(new UnsignedOption("cele_bin"));
			third.Add(new UnsignedOption("cele_hex"));
			//one option is mising

			for(int i=1; i<5; ++i)
			{
				third.Add(new FloatOption("float" + i.ToString()));
			}
			f.Add(third);

			Section fourth = new Section("Other", "");
			fourth.Add(new BooleanOption("bool1"));
			fourth.Add(new EnumOption(new List<string>{"on", "baf"}, "bool2"));
			fourth.Add(new EnumOption(new List<string>{"nothing"}, "bool3"));

			f.Add(fourth);

			f.Load("/home/beda/c_sharp/doporucene_postupy_v_programovani/ukol4/konfiguracni_soubory_knihovna/konfiguracni_soubory_knihovna/test.ini", true);

			List<string> theSuperLink = f["$Sekce::podsekce"]["Option 4"].ConvertToString()[1].GetLinkValue();

			f["Cisla"]["cele"].ConvertToSigned()[0].elementValue = 33;
			f["$Sekce::podsekce"]["Option 3"].ConvertToString().AddElement(defaultLinkElement);

			foreach(string error in f.errors)
			{
				Console.WriteLine(error);
			}
			f.Save("/home/beda/c_sharp/doporucene_postupy_v_programovani/ukol4/konfiguracni_soubory_knihovna/konfiguracni_soubory_knihovna/vystup.ini", true);

		}
	}
}
