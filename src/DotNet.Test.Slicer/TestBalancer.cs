using System.Xml;

namespace DotNet.Test.Slicer;

public class TestBalancer
{
	private static readonly XmlWriterSettings xml_settings = new XmlWriterSettings { Indent = true };
	private bool average_calculated;
	private int average;

	public Dictionary<string, BalanceEntry> BalanceEntries { get; } = new Dictionary<string, BalanceEntry> ();
	public List<string> UntimedTests { get; } = new List<string> ();

	public bool IsEmpty => BalanceEntries.Count == 0;

	public void AddTestExecution (ExecutedTest execution)
	{
		if (!BalanceEntries.TryGetValue (execution.FullName, out var entry)) {
			entry = new BalanceEntry (execution.FullName);
			BalanceEntries.Add (execution.FullName, entry);
		}

		entry.ExecutionDurations.Add (execution.ElapsedMilliseconds);
		average_calculated = false;
	}

	public NUnitTestSlicer.TestAndDuration GetTestDuration (string name)
	{
		if (BalanceEntries.TryGetValue (name, out var entry))
			return new NUnitTestSlicer.TestAndDuration (name, entry.ExecutionAverage, false);

		if (!average_calculated) {
			average = IsEmpty ? 1 : (int) BalanceEntries.Select (b => b.Value.ExecutionAverage).Average ();
			average_calculated = true;
		}

		UntimedTests.Add (name);
		return new NUnitTestSlicer.TestAndDuration (name, average, true);
	}

	public static TestBalancer FromXml (string filename)
	{
		var balancer = new TestBalancer ();

		if (string.IsNullOrWhiteSpace (filename))
			return balancer;

		var doc = new XmlDocument ();
		doc.Load (filename);

		if (doc.DocumentElement is not null)
			foreach (XmlElement elem in doc.DocumentElement) {
				var name = elem.GetAttribute ("name");
				var duration = int.Parse (elem.GetAttribute ("duration"));

				var entry = new BalanceEntry (name);
				entry.ExecutionDurations.Add (duration);

				balancer.BalanceEntries.Add (name, entry);
			}

		return balancer;
	}

	public void Save (string outFile)
	{
		using var xw = XmlWriter.Create (outFile, xml_settings);

		xw.WriteStartElement ("tests");

		foreach (var entry in BalanceEntries.OrderBy (b => b.Value.Name)) {
			xw.WriteStartElement ("test");
			xw.WriteAttributeString ("name", entry.Value.Name);
			xw.WriteAttributeString ("duration", entry.Value.ExecutionAverage.ToString ());
			xw.WriteEndElement ();
		}

		xw.WriteEndElement ();
	}
}

public class BalanceEntry
{
	public string Name { get; set; }

	public List<int> ExecutionDurations { get; } = new ();

	public int ExecutionAverage => (int) ExecutionDurations.Average ();

	public BalanceEntry (string name)
	{
		Name = name;
	}
}
