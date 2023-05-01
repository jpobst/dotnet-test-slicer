using NUnit.Engine;
using System.Xml;

namespace DotNet.Test.Slicer;

public static class NUnitTestDiscoverer
{
		public static List<string> GetFilteredTestCases (string testAssembly, string? filterString)
		{
			// Set up the test assembly
			var package = new TestPackage (testAssembly);
			package.AddSetting ("WorkDirectory", Path.GetDirectoryName (testAssembly));

			using var engine = TestEngineActivator.CreateInstance ();

			// Set up the filter
			var filterService = engine.Services.GetService<ITestFilterService> ();
			var builder = filterService.GetTestFilterBuilder ();

			if (!string.IsNullOrWhiteSpace (filterString))
				builder.SelectWhere (filterString);
			
			var filter = builder.GetFilter ();

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
}
