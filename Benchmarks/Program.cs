using System.Diagnostics;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

var config = DefaultConfig.Instance
    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60))
    .AddJob(Job.Default.WithRuntime(ClrRuntime.Net48));

var summaries = BenchmarkSwitcher
    .FromAssembly(Assembly.GetExecutingAssembly())
    .Run(args, config);

foreach (var summary in summaries)
{
    string path = Path.Combine(summary.ResultsDirectoryPath);
    Process.Start(new ProcessStartInfo
    {
        FileName = path,
        UseShellExecute = true,
    });
}