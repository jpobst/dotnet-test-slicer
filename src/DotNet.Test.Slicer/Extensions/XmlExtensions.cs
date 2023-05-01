using System.IO;
using System.Xml;

namespace DotNet.Test.Slicer;

static class XmlExtensions
{
	public static bool TryRemoveSingleNode (this XmlDocument doc, string xpath, XmlNamespaceManager nsmgr)
	{
		if (doc.SelectSingleNode (xpath, nsmgr) is XmlNode node && node.ParentNode is not null) {
			node.ParentNode.RemoveChild (node);
			return true;
		}

		return false;
	}
}
