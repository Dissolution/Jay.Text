using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Jay.Text.Benchmarks;

// var config = DefaultConfig.Instance
//     .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60))
//     .AddJob(Job.Default.WithRuntime(ClrRuntime.Net48));
//
// var summaries = BenchmarkSwitcher
//     .FromAssembly(Assembly.GetExecutingAssembly())
//     .Run(args, config);

var sum = BenchmarkRunner.Run<CopyBenchmarks>();
OpenSummary(sum);

static void OpenSummary(Summary summary)
{
    string path = Path.Combine(summary.ResultsDirectoryPath);
    Process.Start(new ProcessStartInfo
    {
        FileName = path,
        UseShellExecute = true,
    });
}