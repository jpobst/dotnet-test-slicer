using System.Xml;

namespace DotNet.Test.Slicer;

public class TestDefinition
{
	public required Guid TestId { get; set; }
	public required string Name { get; set; }
	public required string ClassName { get; set; }

	public static TestDefinition FromXml (XmlElement element)
	{
		var id = Guid.Parse (element.GetAttribute ("id"));
		var name = element.GetAttribute ("name");
		var test_method_elem = element ["TestMethod"] ?? throw new Exception ("Whoops");
		var cls = test_method_elem.GetAttribute ("className");

		return new TestDefinition {
			TestId = id,
			Name = name,
			ClassName = cls,
		};
	}
}
