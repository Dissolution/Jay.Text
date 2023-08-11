using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Jay.Text.Benchmarks;

var config = DefaultConfig.Instance
    .AddJob(Job
        .ShortRun
        .WithRuntime(ClrRuntime.Net48)
        .WithRuntime(CoreRuntime.Core20)
        .WithRuntime(CoreRuntime.Core21)
        .WithRuntime(CoreRuntime.Core31)
        .WithRuntime(CoreRuntime.Core60)
        .WithRuntime(CoreRuntime.Core70));


// var config = DefaultConfig.Instance
//     .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60))
//     .AddJob(Job.Default.WithRuntime(ClrRuntime.Net48));
//
// var summaries = BenchmarkSwitcher
//     .FromAssembly(Assembly.GetExecutingAssembly())
//     .Run(args, config);

var sum = BenchmarkRunner.Run<EqualsBenchmarks>(config);
openSummary(sum);
return;

static void openSummary(Summary summary)
{
    string path = Path.Combine(summary.ResultsDirectoryPath);
    Process.Start(new ProcessStartInfo
    {
        FileName = path,
        UseShellExecute = true,
    });
}