# Process Information 

Program for running a process and periodically collecting data about it.

## Setup 
You need to have .NET 5.0 installed on your computer.
If you have Visual Studio 2019 or higher installed, you can work with solution from within it. You first need to add Nuget Package **System.Diagnostics.PerformanceCounter** for the program to work.

If you wish to work from command line, first go to the project folder
```console
cd ProcessInformation
```

To restore Nuget packages either run
```console
dotnet add package System.Diagnostics.PerformanceCounter
```

To then build and run the program just run
```console
dotnet build
dotnet run
```
In the program you have to enter path to the executable or the program(notepad.exe) and then enter interval length. After the process finishes, results of the measurement are in the file *process.txt* in the project folder.
