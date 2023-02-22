# dotnet-test-slicer
Unit test slicer to support running NUnit tests on multiple test agents using 'dotnet test'.

A clever solution is [provided in the AZDO docs](https://learn.microsoft.com/en-us/azure/devops/pipelines/test/parallel-testing-any-test-runner?view=azure-devops) for how to slice up the tests in a test assembly for an arbitrary number of test agents: 
- Use `dotnet test --list-tests` to find all the tests
- Use a script to calculate which tests the agent should run, based on `$(System.JobPositionInPhase)` and `$(System.TotalJobsInPhase)`
- Pass those test names into `dotnet test` as a filter

Unfortunately there are issues with the provided approach:
- `dotnet test --list-tests` is not compatible with `--filter` so you have to run *all* tests in the test assembly which may not be desirable. (https://github.com/dotnet/sdk/issues/8643)
- Passing test names (including test parameters) on the command line hits limitations with escaping certain characters and argument limits.

While the approach is good, we need a more robust solution for figuring out which tests each test agent should run.

This dotnet global tool:
- Uses the `NUnit.Engine` NuGet package to find all tests in a test assembly using the desired filter query.
- Slices the test list for the current test agent.
- Outputs the desired tests into an NUnit-specific `.runsettings` file that can be passed to `dotnet test --settings foo.runsettings`, bypassing command line arguments.

## Usage

First install the global tool:
```
dotnet tool install -g dotnet-test-slicer
```

Run the slicer, providing the test assembly, an optional filter, and the slice parameters:

```
dotnet-test-slicer
  --test-assembly=MyUnitTests.dll
  --test-filter="cat != IgnoreTheseTests"
  --slice-number=1
  --total-slices=4
  --outfile=MyUnitTests.runsettings
```

Pass the `.runsettings` file to `dotnet test` to run the test slice:
```
dotnet test MyUnitTests.dll --settings MyUnitTests.runsettings
```
