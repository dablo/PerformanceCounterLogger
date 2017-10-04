using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;

namespace PerformanceCounterLogger
{
    class Program
    {
        static void Main(string[] args)
        {

            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            HostFactory.Run(x =>
            {
                x.Service<PerformanceCounterCollector>(s =>
                {
                    s.ConstructUsing(name => new PerformanceCounterCollector(1000));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                    s.WhenPaused(tc => tc.Pause());
                    s.WhenContinued(tc => tc.Continue());
                });
                x.RunAsLocalSystem();
                x.SetDescription("Log performance counters to the eventlog with Serilog");
                x.SetDisplayName("Performance Counter Logger");
                x.SetServiceName("PerformanceCounterLogger");
            });
        }
    }

    class PerformanceCounterCollector
    {
        private readonly int interval;
        private readonly List<PerformanceCounter> counters;
        private readonly PerformanceCounterCategory networkingCounters;
        private readonly PerformanceCounterCategory TCPv4Counters;
        private readonly PerformanceCounterCategory ThreadCounters;
        private readonly PerformanceCounterCategory dataCounters;
        private readonly PerformanceCounterCategory sqlCounters;

        public PerformanceCounterCollector(int interval)
        {
            this.interval = interval;

            counters = new List<PerformanceCounter>();
            networkingCounters = new PerformanceCounterCategory(".NET CLR Networking 4.0.0.0");
            TCPv4Counters = new PerformanceCounterCategory("TCPv4");
            ThreadCounters = new PerformanceCounterCategory(".NET CLR LocksAndThreads");
            dataCounters = new PerformanceCounterCategory(".NET CLR Data");
            sqlCounters = new PerformanceCounterCategory(".NET Data Provider for SqlServer");
        }

        public bool Enabled { get; private set; }

        public void Initialize()
        {
            Log.Information("Initializing Performance Counters");
            if (PerformanceCounterCategory.InstanceExists("_global_", networkingCounters.CategoryName))
            {
                Log.Information($"Adding {networkingCounters.CategoryName} Performance Counters");
                counters.AddRange(networkingCounters.GetCounters("_global_"));
            }

            if (PerformanceCounterCategory.Exists(ThreadCounters.CategoryName))
            {
                Log.Information($"Adding {ThreadCounters.CategoryName} Performance Counters");
                counters.AddRange(ThreadCounters.GetCounters("_Global_"));
            }

            if (PerformanceCounterCategory.Exists(dataCounters.CategoryName) && dataCounters.InstanceExists("_Global_"))
            {
                Log.Information($"Adding {dataCounters.CategoryName} Performance Counters");
                counters.AddRange(dataCounters.GetCounters("_Global_"));
            }

            if (PerformanceCounterCategory.Exists(sqlCounters.CategoryName) && sqlCounters.InstanceExists("_Global_"))
            {
                Log.Information($"Adding {sqlCounters.CategoryName} Performance Counters");
                counters.AddRange(sqlCounters.GetCounters("_Global_"));
            }

            Log.Information($"Adding {TCPv4Counters.CategoryName} Performance Counters");
            counters.AddRange(TCPv4Counters.GetCounters());

            Log.Information($"Added {counters.Count} Performance Counters");

        }

        public void Start()
        {
            Log.Information("Starting Performance Counter Collector");

            if (!counters.Any())
            {
                Initialize();
            }

            Enabled = true;
            while (Enabled)
            {
                foreach (var counter in counters)
                {
                    Log.Information($"PerformanceCounter Custom Metrics Custom Key:{counter.CategoryName}\\{counter.CounterName} Custom Value:{counter.RawValue}");
                }

                Thread.Sleep(1000);
            }
        }

        public void Stop()
        {
            Log.Information("Stopping Performance Counter Collector");
            Enabled = false;
        }

        public void Pause()
        {
            Log.Information("Pausing Performance Counter Collector");
            Stop();
        }
        public void Continue()
        {
            Log.Information("Resume Performance Counter Collector");
            Start();
        }
    }
}
