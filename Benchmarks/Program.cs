using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public static class Program
    {
        public static void Main(params string?[] args)
        {
            var summary = BenchmarkRunner.Run<EqualsBenchmarks>();
            //var logFileDirectory = Path.GetDirectoryName(summary.LogFilePath);
            //Process.Start(logFileDirectory).Dispose();

            Console.WriteLine("Press Enter to close this window.");
            Console.ReadLine();
        }
    }
}
