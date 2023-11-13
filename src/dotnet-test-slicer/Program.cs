using System.CommandLine;
using System.Runtime.InteropServices;
using DotNet.Test.Slicer;

namespace dotnet_test_slicer;

class Program
{
	static async Task Main (params string [] args)
	{
		var root_command = BuildCommandLine ();

		await root_command.InvokeAsync (args);
	}

	static RootCommand BuildCommandLine ()
	{
		var root = new RootCommand ("Utilities for working with CI unit tests.");

		// dotnet-test-slicer slice
		var slice_verb = new Command ("slice", "Create a slice of unit tests to run on a parallelized CI agent.");

		var test_assembly_option = new Option<string> ("--test-assembly", "The NUnit test assembly to slice.") { IsRequired = true };
		var slice_number_option = new Option<int> ("--slice-number", "The (1-based) index of the desired slice.") { IsRequired = true };
		var total_slices_option = new Option<int> ("--total-slices", "The total number of desired slices.") { IsRequired = true };
		var runsettings_output_file_option = new Option<string> ("--outfile", () => "run-settings.txt", "File to write the .runsettings file to.");
		var test_filter_option = new Option<string> ("--test-filter", () => "", "Optional NUnit test filter query.");
		var balance_file_option = new Option<string> ("--balance-file", () => "", "Optional test timings file used for load balancing.");

		slice_verb.Add (test_assembly_option);
		slice_verb.Add (slice_number_option);
		slice_verb.Add (total_slices_option);
		slice_verb.Add (runsettings_output_file_option);
		slice_verb.Add (test_filter_option);
		slice_verb.Add (balance_file_option);

		slice_verb.SetHandler (
			RunSliceVerb,
			test_assembly_option, slice_number_option, total_slices_option, runsettings_output_file_option, test_filter_option, balance_file_option
		);

		root.Add (slice_verb);

		// dotnet-test-slicer balance
		var balance_verb = new Command ("balance", "Create a test timings file used to load balance tests.");

		var balance_input_dir_option = new Option<string> ("--input-directory", "The directory containing the .trx files to consume.") { IsRequired = true };
		var balance_output_file_option = new Option<string> ("--outfile", () => "balance.xml", "File to write the balance file to.");

		balance_verb.Add (balance_input_dir_option);
		balance_verb.Add (balance_output_file_option);

		balance_verb.SetHandler (
			RunBalanceVerb,
			balance_input_dir_option, balance_output_file_option
		);

		root.Add (balance_verb);

		// dotnet-test-slicer retry
		var retry_verb = new Command ("retry", "Create a .runsettings file containing all tests that failed a previous test run.");

		var retry_input_trx_option = new Option<string> ("--trx", "The .trx file containing the previous run. (Can also be a directory of multiple .trx files.)") { IsRequired = true };
		var retry_runsettings_output_file_option = new Option<string> ("--outfile", () => "run-settings.txt", "File to write the .runsettings file to.");
		var retry_output_trx_file_option = new Option<string> ("--out-trx", () => "", "File to write updated .trx file to. (Will replace input if not specified, not valid if --trx is a directory.)");

		retry_verb.Add (retry_input_trx_option);
		retry_verb.Add (retry_runsettings_output_file_option);
		retry_verb.Add (retry_output_trx_file_option);

		retry_verb.SetHandler (
			RunRetryVerb,
			retry_input_trx_option, retry_runsettings_output_file_option, retry_output_trx_file_option
		);

		root.Add (retry_verb);

		return root;
	}

	static void RunSliceVerb (string testAssembly, int sliceNumber, int totalSlices, string outfile, string testFilter, string balanceFile)
	{
		Console.WriteLine ($".NET Runtime Version: {RuntimeInformation.FrameworkDescription}");
		Console.WriteLine ();

		Console.WriteLine ("Arguments:");
		Console.WriteLine ($"- Test Assembly: {testAssembly}");
		Console.WriteLine ($"- Slice Number: {sliceNumber}");
		Console.WriteLine ($"- Total Slices: {totalSlices}");
		Console.WriteLine ($"- RunSettings Output File: {outfile}");
		Console.WriteLine ($"- Test Filter: {testFilter}");
		Console.WriteLine ($"- Balance File: {balanceFile}");
		Console.WriteLine ();

		NUnitTestSlicer.SliceAndOutput (testAssembly, testFilter, sliceNumber, totalSlices, outfile, balanceFile);
	}

	static void RunBalanceVerb (string inputDirectory, string outfile)
	{
		Console.WriteLine ("Arguments:");
		Console.WriteLine ($"- Input Directory: {inputDirectory}");
		Console.WriteLine ($"- Balanced Output File: {outfile}");
		Console.WriteLine ();

		var balancer = new TestBalancer ();
		var trx_files = Directory.GetFiles (inputDirectory, "*.trx");
		var test_count = 0;

		Console.WriteLine ($"Found {trx_files.Length} .trx files.");

		foreach (var file in trx_files) {
			var trx = new TrxFile (file);
			var passed_tests = trx.GetExecutedTests ().Where (t => t.Result == TestResult.Passed).ToArray ();

			test_count += passed_tests.Length;

			foreach (var test in passed_tests)
				balancer.AddTestExecution (test);
		}

		Console.WriteLine ($"Found {test_count} successful test executions for {balancer.BalanceEntries.Count} tests.");

		balancer.Save (outfile);
	}

	static void RunRetryVerb (string trxFile, string outfile, string updatedTrx)
	{
		Console.WriteLine ("Arguments:");
		Console.WriteLine ($"- Input .trx: {trxFile}");
		Console.WriteLine ($"- RunSettings Output File: {outfile}");
		Console.WriteLine ($"- Adjusted .trx Output File: {updatedTrx}");
		Console.WriteLine ();

		var all_failed_tests = new List<ExecutedTest> ();
		var trx_files = new List<string> ();

		if (Directory.Exists (trxFile))
			foreach (var file in Directory.EnumerateFiles (trxFile, "*.trx"))
				trx_files.Add (file);
		else
			trx_files.Add (trxFile);

		foreach (var file in trx_files) {
			var trx = new TrxFile (file);

			Console.WriteLine ($"Found file: {file}");
			Console.WriteLine ();

			// First we need to collect the failed tests
			var failed_tests = trx.GetExecutedTests ().Where (t => t.Result == TestResult.Failed).ToArray ();

			Console.WriteLine ($"Found {failed_tests.Length} failed test(s) to retry:");

			foreach (var test in failed_tests)
				Console.WriteLine ($"- {test.FullName}");

			Console.WriteLine ();

			all_failed_tests.AddRange (failed_tests);

			// Remove failed tests from the .trx file so they don't show up as failed in CI reports
			trx.RemoveFailedTests ();
			trx.Save (updatedTrx);
		}

		// Write the new .runsettings file
		NUnitRunSettingsWriter.WriteWithTestCaseFilter (all_failed_tests.Select (t => t.FullName).ToList (), outfile);
	}
}
