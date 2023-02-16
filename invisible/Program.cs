// TODO #CLIWRAP implement everything via https://github.com/Tyrrrz/CliWrap !!!

﻿using System.Diagnostics;

string __FILE__ = System.Environment.ProcessPath ?? Process.GetCurrentProcess().ProcessName;
Func<string, string[], string, bool, Process> my_process = (program, args, working_dir, redirectStreams) =>
{
    ProcessStartInfo psi = new ProcessStartInfo
    {
        FileName = program,
        WorkingDirectory = working_dir,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardInput = false, // TODO #stdin8542102154 allow standard input somehow?
        RedirectStandardOutput = redirectStreams,
        RedirectStandardError = redirectStreams,
        WindowStyle = ProcessWindowStyle.Hidden,
    };
    foreach (string arg in args) psi.ArgumentList.Add(arg);
    Process p = new Process();
    p.StartInfo = psi;
    return p;
};



int exitCode = 0;
string[] invocationArguments = Environment.GetCommandLineArgs().Skip(1).ToArray();
string working_dir = Directory.GetCurrentDirectory();
bool no_output = true;
bool always_success = false;
bool dont_wait = false;

while (invocationArguments.Length > 0 && invocationArguments[0].StartsWith("--"))
{
    int to_shift = 1;
    switch (invocationArguments[0])
    {
        case "--": goto stop_args;
        case "--help": goto print_help;
        case "--working-directory": working_dir = invocationArguments[1]; to_shift = 2; break;
        case "--show-output": no_output = false; break;
        case "--always-success": always_success = true; break;
        case "--dont-wait": dont_wait = true; break;
        default: Console.Error.WriteLine($"Unrecognized option: {invocationArguments[0]}"); Environment.Exit(1); break;
    }
    invocationArguments = invocationArguments.Skip(to_shift).ToArray();
}
stop_args:;

if (invocationArguments.Length == 0)
{
    Console.Error.WriteLine("No executable specified. Showing --help.\n");
    if (!always_success) exitCode = 1;
    goto print_help;
}

string program = invocationArguments[0];
string[] program_args = invocationArguments.Skip(1).ToArray();

Process p = my_process(program, program_args, working_dir, !no_output);
p.Start();

if (!dont_wait)
{
    if (!no_output)
    {
        p.ErrorDataReceived += (sender, args) => Console.Error.Write(args.Data != null ? args.Data + Environment.NewLine : "");
        p.OutputDataReceived += (sender, args) => Console.Write(args.Data != null ? args.Data + Environment.NewLine : "");
        p.BeginErrorReadLine();
        p.BeginOutputReadLine();
    }
    p.WaitForExit();
}

exitCode = always_success || dont_wait ? 0 : p.ExitCode;
Environment.Exit(exitCode);





print_help:;
string f = Path.GetFileName(__FILE__);
// TODO #helptxt8548745 move help to txt template file
Console.WriteLine(f + "\n\nInvokes a program silently." +
    "\nFeatures:" +
    "\n- can be passed any number of arguments to the program" +
    "\n- can locate the executable via relative path, absolute path, or the system's PATH" +
    "\n- runs the program in the current directory (or the given directory, via parameter)" +
    "\n- waits for the program to be over (or doesn't, via parameter)" +
    "\n- exits with the invocation's exit code (or doesn't, via parameter)" +
    "\n- if requested via parameter, outputs the invocation's output" +
    "\n\nUsage:" +
    "\n    " + f + " [parameters] executable [executable arguments]" +
    "\n\nParameters: " +
    "\n    [--]                                   stops parsing arguments and start parsing executable" +
    "\n    [--working-directory directory]        runs the program in the given directory" +
    "\n    [--show-output]                        shows the invocation's shell output" +
    "\n    [--always-success]                     always quits with exit code 0, ignoring the invocation's exit code" +
    "\n    [--dont-wait]                          doesn't wait for the invoked program to be over, immediately quits with exit code 0" +
    "\n\nUsage examples:" +
    "\n    " + f + " myprogram --with -its /arguments list" +
    "\n    " + f + " --always-success --dont-wait -- myprogram --with -its /arguments list" +
    "\n    " + f + " ..\\some\\program\\relative\\to\\the\\current\\directory.exe" +
    "\n    " + f + " cmd /c shutdown /p" +
    "\n    " + f + " logoff" +
    "\n    " + f + " c:\\absolute\\path\\to\\php.exe -r \"file_put_contents('my_output_file.txt', \\\"double quoted string with spaces\\\");\"" +
    "\n\nPLEASE NOTE:" +
    "\nYou have to C-style escape double quotes if you want to pass them literally to the executable, like the php example above. Please also take into consideration the current shell's escape rules for quotes." +
    "\n\nPLEASE NOTE:" +
    "\nDue to output buffering differences between stdout and stderr, if the invocation sends data to both streams, --show-output will probably print things NOT in the correct order. The only way to be sure to have order, is to have a single stream, that is, merging the streams with redirection with 2>&1 (remember to escape this correctly)." +
    "");
Environment.Exit(exitCode);
