using System;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;

namespace ProcessInformation
{
    class Program
    {
        [SupportedOSPlatform("windows")]
        static void Main(string[] args)
        {
            String pathToExecutable;
            String intervalString;
            int intervalLength = 0;
            Process launchedProcess;
            Console.WriteLine("Enter path to the executable:");
            pathToExecutable = Console.ReadLine();
            Console.WriteLine("-------------------");
            Console.WriteLine("Enter interval for measuring(in milliseconds):");
            intervalString = Console.ReadLine();
            Console.WriteLine("-------------------");

            // Parsing interval length
            while (!int.TryParse(intervalString, out intervalLength) || intervalLength <= 0)
            {
                Console.WriteLine("Incorrect format, reenter interval:");
                intervalString = Console.ReadLine();
                Console.WriteLine("-------------------");
            }

            // Using to automatically close and dispose the file object
            using (StreamWriter sW = new(new FileStream("process.log", FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                // Header of csv file
                sW.WriteLine("measurement,time_passed,working_set,private_bytes,handle_count,cpu_usage");
                // Looping until process is started
                while (true)
                {
                    try
                    {
                        // Starting the process
                        launchedProcess = Process.Start(pathToExecutable);
                        // Exception was not thrown we can continue to the measurement
                        break;
                    }
                    // Process could not be started (permissions/file not existing...), asking the user to reenter the executable
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
                    // Setting up performance counters to get information about CPU usage.
                    PerformanceCounter totalCpu = new("Process", "% Processor Time", "_Total");
                    PerformanceCounter processCpu = new("Process", "% Processor Time", launchedProcess.ProcessName);
                    // First call of NextValue always returns 0.
                    _ = totalCpu.NextValue();
                    _ = processCpu.NextValue();
                    // Using StopWwatch for measuring time.
                    Stopwatch sw = new();
                    sw.Start();
                    int loop = 1;
                    long elapsedMilliseconds = 0;
                    long workingSet = 0;
                    long virtualMemory = 0;
                    int handleCount = 0;
                    float cpuUsage = 0;
                    Thread.Sleep(intervalLength);
                    while (true)
                    {
                        elapsedMilliseconds = sw.ElapsedMilliseconds;
                        // Refresh information about the running process.
                        launchedProcess.Refresh();
                        // Getting necessary information
                        workingSet = launchedProcess.WorkingSet64;
                        virtualMemory = launchedProcess.VirtualMemorySize64;
                        handleCount = launchedProcess.HandleCount;
                        cpuUsage = processCpu.NextValue() / totalCpu.NextValue();
                        // Logging the information
                        sW.WriteLine(loop + "," + elapsedMilliseconds + "," + workingSet + "," + virtualMemory + "," + handleCount + "," + cpuUsage.ToString("0.00"));
                        ++loop;
                        // Since PerformanceCounters take a long time (around 20 ms) I try to adjust waiting to correspon more to the interval length
                        long timeWasted = sw.ElapsedMilliseconds - elapsedMilliseconds;
                        Thread.Sleep((int)(intervalLength - timeWasted > 0 ? intervalLength - timeWasted : 0));
                    }
                }
                // Process already ended
                catch (InvalidOperationException)
                {
                    Console.WriteLine("Process has ended.");
                }
                // Different exception - prints information about it to the user
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
