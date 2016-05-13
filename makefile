all:
	dmcs ConfFile.cs Element.cs Option.cs Section.cs Parser.cs Test.cs -out:ConfLibrary.exe  --debug

clean:
	rm ConfLibrary.exe







