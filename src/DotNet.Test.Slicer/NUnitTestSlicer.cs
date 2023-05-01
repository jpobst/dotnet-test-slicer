namespace DotNet.Test.Slicer;

public static class NUnitTestSlicer
{
	public static void SliceAndOutput (string testAssembly, string? filterString, int sliceNumber, int totalSlices, string outFile, string balanceFile)
	{
		var balancer = TestBalancer.FromXml (balanceFile);
		var all_tests = NUnitTestDiscoverer.GetFilteredTestCases (testAssembly, filterString);

		Console.WriteLine ($"Found {all_tests.Count} matching tests.");
		Console.WriteLine ();

		var manager = SliceTestCases (all_tests, totalSlices, balancer);
		var slice = manager.Slices.Single (s => s.SliceId == sliceNumber);

		LogTestSlice (balancer, slice);

		NUnitRunSettingsWriter.WriteWithTestCaseFilter (slice.Tests.Select (t => t.Name).ToList (), outFile);
	}

	public static List<string> Slice (string testAssembly, string? filterString, int sliceNumber, int totalSlices, TestBalancer balancer)
	{
		var all_tests = NUnitTestDiscoverer.GetFilteredTestCases (testAssembly, filterString);
		var sliced_tests = SliceTestCases (all_tests, totalSlices, balancer);

		return sliced_tests.Slices.Single (s => s.SliceId == sliceNumber).Tests.Select (t => t.Name).ToList ();
	}

	private static SliceManager SliceTestCases (List<string> tests, int totalSlices, TestBalancer balancer)
	{
		var tests_and_durations = tests.Select (t => balancer.GetTestDuration (t)).ToArray ();

		// We need a canonical sort so all slices are picking from the same order
		tests_and_durations = tests_and_durations.OrderByDescending (t => t.Duration).ThenBy (t => t.Name).ToArray ();
		tests.Sort ();

		var slices = new SliceManager (totalSlices);

		foreach (var test in tests_and_durations)
			slices.AddTest (test);

		return slices;
	}

	private static void LogTestSlice (TestBalancer balancer, SliceClass slice)
	{
		Console.WriteLine ($"{slice.Tests.Count} tests chosen for this slice to run:");

		var max = slice.Tests.Max (t => t.Duration).ToString ().Length;

		foreach (var s in slice.Tests.OrderBy (t => t.IsEstimatedDuration).ThenByDescending (t => t.Duration).ThenBy (t => t.Name)) {
			var timing_prefix = balancer.IsEmpty ? "" : (s.IsEstimatedDuration ? "?" : s.Duration + "ms").PadLeft (max + 2) + " ";
			Console.WriteLine ($"- {timing_prefix}{s.Name}");
		}

		Console.WriteLine ();

		if (!balancer.IsEmpty) {
			Console.WriteLine ($"Estimated Duration: {slice.Tests.Sum (t => t.Duration)}ms");
			Console.WriteLine ();
		}

		if (!balancer.IsEmpty && balancer.UntimedTests.Any ()) {
			Console.WriteLine ($"Timing data is missing for {balancer.UntimedTests.Count} tests in test assembly:");

			foreach (var test in balancer.UntimedTests)
				Console.WriteLine ($"- {test}");

			Console.WriteLine ();
		}
	}

	class SliceManager
	{
		public List<SliceClass> Slices { get; } = new List<SliceClass> ();

		public SliceManager (int sliceCount)
		{
			for (var i = 1; i <= sliceCount; i++)
				Slices.Add (new SliceClass { SliceId = i });
		}

		public void AddTest (TestAndDuration test)
		{
			var shortest_slice = Slices.OrderBy (s => s.ExpectedDuration).First ();

			shortest_slice.Tests.Add (test);
		}
	}

	class SliceClass
	{
		public int SliceId { get; init; }
		public List<TestAndDuration> Tests { get; } = new List<TestAndDuration> ();
		public int ExpectedDuration => Tests.Sum (t => t.Duration);
	}

	public class TestAndDuration
	{
		public string Name { get; set; }

		public int Duration { get; set; }

		public bool IsEstimatedDuration { get; set; }

		public TestAndDuration (string name, int duration, bool isEstimatedDuration)
		{
			Name = name;
			Duration = duration;
			IsEstimatedDuration = isEstimatedDuration;
		}
	}
}
