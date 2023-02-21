using DotNet.Test.Slicer;

namespace dotnet_test_slicer
{
	class Program
	{
		/// <param name="testAssembly">The NUnit test assembly to slice</param>
		/// <param name="sliceNumber">The (1-based) index of the desired slice</param>
		/// <param name="totalSlices">The total number of desired slices</param>
		/// <param name="outfile">File to write the run settings file to</param>
		/// <param name="testFilter">Optional NUnit test filter query</param>
		static void Main (string testAssembly, int sliceNumber, int totalSlices, string outfile = "run-settings.txt", string? testFilter = null)
		{
			Console.WriteLine ("Arguments:");
			Console.WriteLine ($"- Test Assembly: {testAssembly}");
			Console.WriteLine ($"- Test Filter: {testFilter}");
			Console.WriteLine ($"- Slice Number: {sliceNumber}");
			Console.WriteLine ($"- Total Slices: {totalSlices}");
			Console.WriteLine ($"- RunSettings Output File: {outfile}");
			Console.WriteLine ();

			NUnitTestSlicer.Slice (testAssembly, testFilter, sliceNumber, totalSlices, outfile);
		}
	}
}
