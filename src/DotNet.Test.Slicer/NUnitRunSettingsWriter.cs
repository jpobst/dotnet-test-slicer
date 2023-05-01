using System.Xml;

namespace DotNet.Test.Slicer;

public static class NUnitRunSettingsWriter
{
	public static void WriteWithTestCaseFilter (List<string> tests, string outFile)
	{
		// If we leave the filter blank it will run all tests rather than none.
		if (tests.Count == 0)
			tests.Add ("dotnet-slicer-dummy-test-name");

		var filter = BuildFilter (tests);
		using var xw = XmlWriter.Create (outFile);

		xw.WriteStartElement ("RunSettings");
		xw.WriteStartElement ("NUnit");

		xw.WriteElementString ("Where", filter);

		xw.WriteEndElement ();
		xw.WriteEndElement ();
	}

	private static string BuildFilter (List<string> tests)
	{
		// We're going to quote our test names with single quotes, but we'll need to escape them if the test name contains them.
		// We also have to escape backslashes in test names.
		var result = string.Join (" or ", tests.Select (s => $"test == '{s.Replace (@"\", @"\\").Replace ("'", @"\'")}'"));

		return result;
	}
}
