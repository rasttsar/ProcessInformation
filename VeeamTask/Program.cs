using System;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;

namespace VeeamTask
{
    class Program
    {
        [SupportedOSPlatform("windows")]
        static void Main(string[] args)
        {
            String pathToExecutable;
            String intervalString;
            int intervalLength;
            Process launchedProcess;
            Console.WriteLine("Enter path to the executable:");
            pathToExecutable = Console.ReadLine();
            Console.WriteLine("-------------------");
            Console.WriteLine("Enter interval for measuring(in milliseconds):");
            intervalString = Console.ReadLine();
            Console.WriteLine("-------------------");

            while (!int.TryParse(intervalString, out intervalLength))
            {
                Console.WriteLine("Incorrect format, reenter interval:");
                intervalString = Console.ReadLine();
                Console.WriteLine("-------------------");
            }

            using (StreamWriter sW = new(new FileStream("process.log", FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                sW.WriteLine("measurement,time_passed,working_set,private_bytes,handle_count,cpu_usage");
                while (true)
                {
                    try
                    {
                        launchedProcess = Process.Start(pathToExecutable);
                        break;
                    }
                    catch (Exception e) when (e is Win32Exception || e is FileNotFoundException || e is ObjectDisposedException)
                    {
                        Console.WriteLine("Application could not be started");
                        Console.WriteLine(e);
                        Console.WriteLine("-------------------");
                        Console.WriteLine("Reenter path to executable");
                        pathToExecutable = Console.ReadLine();
                        Console.WriteLine("-------------------");
                    }
                }

                try
                {
                    Console.WriteLine("Process is running.");
                    PerformanceCounter totalCpu = new("Process", "% Processor Time", "_Total");
                    PerformanceCounter processCpu = new("Process", "% Processor Time", launchedProcess.ProcessName);
                    _ = totalCpu.NextValue();
                    _ = processCpu.NextValue();
                    Stopwatch sw = new();
                    sw.Start();
                    int loop = 0;
                    long elapsedMilliseconds = 0;
                    long workingSet = 0;
                    long virtualMemory = 0;
                    int handleCount = 0;
                    float cpuUsage = 0;
                    while (true)
                    {
                        elapsedMilliseconds = sw.ElapsedMilliseconds;
                        launchedProcess.Refresh();
                        workingSet = launchedProcess.WorkingSet64;
                        virtualMemory = launchedProcess.VirtualMemorySize64;
                        handleCount = launchedProcess.HandleCount;
                        cpuUsage = processCpu.NextValue() / totalCpu.NextValue();
                        sW.WriteLine(loop + "," + elapsedMilliseconds + "," + workingSet + "," + virtualMemory + "," + handleCount + "," + cpuUsage.ToString("0.00"));
                        ++loop;
                        long timeWasted = sw.ElapsedMilliseconds - elapsedMilliseconds;
                        Thread.Sleep((int)(intervalLength - timeWasted > 0 ? intervalLength - timeWasted : 0));
                    }
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("Process has ended.");
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
