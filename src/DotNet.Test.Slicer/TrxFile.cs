using System.Xml;

namespace DotNet.Test.Slicer;

public class TrxFile
{
	private readonly XmlDocument doc;
	private readonly string filename;

	public TrxFile (string filename)
	{
		this.filename = filename;

		doc = new XmlDocument ();
		doc.Load (filename);
	}

	public IEnumerable<string> GetTestNames ()
	{
		var test_nodes = doc.GetElementsByTagName ("UnitTestResult");

		foreach (XmlElement elem in test_nodes)
			yield return elem.GetAttribute ("testName");
	}

	public IEnumerable<string> GetPassedTestNames ()
	{
		var test_nodes = doc.GetElementsByTagName ("UnitTestResult");

		foreach (XmlElement elem in test_nodes)
			if (elem.GetAttribute ("outcome") == "Passed")
				yield return elem.GetAttribute ("testName");
	}

	public IEnumerable<string> GetFailedTestNames ()
	{
		var test_nodes = doc.GetElementsByTagName ("UnitTestResult");

		foreach (XmlElement elem in test_nodes)
			if (elem.GetAttribute ("outcome") == "Failed")
				yield return elem.GetAttribute ("testName");
	}

	public IEnumerable<ExecutedTest> GetExecutedTests ()
	{
		var definitions = GetTestDefinitions ().ToDictionary (t => t.TestId);

		var test_nodes = doc.GetElementsByTagName ("UnitTestResult");

		foreach (XmlElement elem in test_nodes)
			yield return ExecutedTest.FromXml (elem, definitions);
	}

	public IEnumerable<TestDefinition> GetTestDefinitions ()
	{
		var test_nodes = doc.GetElementsByTagName ("UnitTest");

		foreach (XmlElement elem in test_nodes)
			yield return TestDefinition.FromXml (elem);
	}

	public void RemoveFailedTests ()
	{
		var failed_tests = GetExecutedTests ().Where (t => t.Result == TestResult.Failed).ToArray ();

		var nsmgr = new XmlNamespaceManager (doc.NameTable);
		nsmgr.AddNamespace ("test", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010");

		foreach (var test in failed_tests) {
			// Remove from <Results>
			doc.TryRemoveSingleNode ($"/test:TestRun/test:Results/test:UnitTestResult[@testId = '{test.TestId}']", nsmgr);

			// Remove from <TestDefinitions>
			doc.TryRemoveSingleNode ($"/test:TestRun/test:TestDefinitions/test:UnitTest[@id = '{test.TestId}']", nsmgr);

			// Remove from <TestEntries>
			doc.TryRemoveSingleNode ($"/test:TestRun/test:TestEntries/test:TestEntry[@testId = '{test.TestId}']", nsmgr);
		}

		// Update <ResultSummary>
		if (doc.SelectSingleNode ("/test:TestRun/test:ResultSummary/test:Counters", nsmgr) is XmlElement elem) {
			elem.SetAttribute ("failed", "0");
			elem.SetAttribute ("total", (int.Parse (elem.GetAttribute ("total")) - failed_tests.Length).ToString ());
			elem.SetAttribute ("executed", (int.Parse (elem.GetAttribute ("executed")) - failed_tests.Length).ToString ());
		}
	}

	public void Save (string? filename = null)
	{
		if (string.IsNullOrWhiteSpace (filename))
			doc.Save (this.filename);
		else
			doc.Save (filename);
	}
}
