using NUnit.Engine;
using System.Xml;

namespace DotNet.Test.Slicer
{
	public static class NUnitTestSlicer
	{
		public static void Slice (string testAssembly, string? filterString, int sliceNumber, int totalSlices, string outFile)
		{
			var all_tests = GetFilteredTestCases (testAssembly, filterString);
			Console.WriteLine ($"Found {all_tests.Count} matching tests.");
			Console.WriteLine ();

			var sliced_tests = SliceTestCases (all_tests, sliceNumber, totalSlices);
			LogTestSlice (sliced_tests);

			var filter = BuildFilter (sliced_tests);

			OutputRunSettingsFile (filter, outFile);
		}

		private static List<string> GetFilteredTestCases (string testAssembly, string? filterString)
		{
			// Set up the test assembly
			var package = new TestPackage (testAssembly);
			package.AddSetting ("WorkDirectory", Path.GetDirectoryName (testAssembly));

			using var engine = TestEngineActivator.CreateInstance ();

			// Set up the filter
			TestFilter? filter = null;

			if (!string.IsNullOrWhiteSpace (filterString)) {
				var filterService = engine.Services.GetService<ITestFilterService> ();

				var builder = filterService.GetTestFilterBuilder ();
				builder.SelectWhere (filterString);
				filter = builder.GetFilter ();
			}

			// Do test discovery
			using ITestRunner runner = engine.GetRunner (package);
			var tests = runner.Explore (filter);

			// Extract test names
			var results = new List<string> ();
			var nodes = tests.SelectNodes ("//test-case");

			if (nodes is not null)
				foreach (var element in nodes.Cast<XmlElement> ())
					results.Add (element.GetAttribute ("fullname"));

			return results;
		}

		private static List<string> SliceTestCases (List<string> tests, int sliceNumber, int totalSlices)
		{
			// We need a canonical sort so all slices are picking from the same order
			tests.Sort ();

			var results = new List<string> ();

			for (var i = sliceNumber; i <= tests.Count; i += totalSlices)
				results.Add (tests [i - 1]);

			return results;
		}

		private static void LogTestSlice (List<string> tests)
		{
			Console.WriteLine ($"{tests.Count} tests chosen for this slice to run:");

			foreach (var s in tests)
				Console.WriteLine ($"- {s}");
		}

		private static string BuildFilter (List<string> tests)
		{
			// We're going to quote our test names with single quotes, but we'll need to escape them if the test name contains them
			var result = string.Join (" or ", tests.Select (s => $"test == '{s.Replace ("'", @"\'")}'"));

			return result;
		}

		private static void OutputRunSettingsFile (string filter, string outFile)
		{
			using var xw = XmlWriter.Create (outFile);

			xw.WriteStartElement ("RunSettings");
			xw.WriteStartElement ("NUnit");

			xw.WriteElementString ("Where", filter);

			xw.WriteEndElement ();
			xw.WriteEndElement ();
		}
	}
}
