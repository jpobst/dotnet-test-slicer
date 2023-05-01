using System.Xml;

namespace DotNet.Test.Slicer;

public class ExecutedTest
{
	public required Guid TestId { get; set; }
	public required string Name { get; set; }
	public required string ClassName { get; set; }
	public required TestResult Result { get; set; }
	public required int ElapsedMilliseconds { get; set; }

	public string FullName => $"{ClassName}.{Name}";

	public static ExecutedTest FromXml (XmlElement element, Dictionary<Guid, TestDefinition> definitions)
	{
		var id = Guid.Parse (element.GetAttribute ("testId"));
		var duration = TimeSpan.Parse (element.GetAttribute ("duration"));
		var outcome = Enum.Parse<TestResult> (element.GetAttribute ("outcome"));
		var definition = definitions [id];

		return new ExecutedTest {
			TestId = id,
			Name = definition.Name,
			ClassName = definition.ClassName,
			Result = outcome,
			ElapsedMilliseconds = (int)duration.TotalMilliseconds
		};
	}
}

public enum TestResult
{
	Passed,
	Failed,
	NotExecuted
}
