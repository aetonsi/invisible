using CliWrap;
using System.Diagnostics;

string? __FILE__ = System.Environment.ProcessPath ?? Process.GetCurrentProcess().ProcessName;
string? __PROJECT_NAME__ = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
string? __PROJECT_VERSION__ = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() ?? "UNKNOWN";


////// default values
string working_dir = Directory.GetCurrentDirectory();
bool show_output = false;
bool wait_for_exit = true;
string? stdout_file = null;
string? stderr_file = null;
//////

////// parse command line args
string[] invocationArguments = Environment.GetCommandLineArgs().Skip(1).ToArray();
while (invocationArguments.Length > 0 && invocationArguments[0].StartsWith("--"))
{
    int to_shift = 1;
    switch (invocationArguments[0])
    {
        case "--": goto stop_args;
        case "--version": Console.WriteLine(__PROJECT_NAME__ + " v" + __PROJECT_VERSION__); Environment.Exit(0); break;
        case "--help": goto print_help;
        case "--working-directory": working_dir = invocationArguments[1]; to_shift = 2; break;
        case "--show-output": show_output = true; break;
        case "--stdout": stdout_file = invocationArguments[1]; to_shift = 2; break;
        case "--stderr": stderr_file = invocationArguments[1]; to_shift = 2; break;
        case "--dont-wait": wait_for_exit = false; break;
        default: Console.Error.WriteLine($"Unrecognized option: {invocationArguments[0]}"); Environment.Exit(1); break;
    }
    invocationArguments = invocationArguments.Skip(to_shift).ToArray();
}
stop_args:;
// check args errors
if (invocationArguments.Length == 0)
{
    Console.Error.WriteLine("No executable specified.\n");
    Environment.Exit(1);
}
string program = invocationArguments[0];
string[] program_args = invocationArguments.Skip(1).ToArray();
//////

////// invocation
Command cmd = Cli.Wrap(program) // TODO #stdin8542102154 allow standard input
    .WithArguments(program_args, true)
    .WithWorkingDirectory(working_dir)
    .WithValidation(CommandResultValidation.None);
CommandTask<CommandResult> task;
CommandResult? result = null;

if (wait_for_exit)
{
    List<PipeTarget> stdout_targets = new List<PipeTarget>();
    List<PipeTarget> stderr_targets = new List<PipeTarget>();
    if (show_output)
    {
        stdout_targets.Add(PipeTarget.ToStream(Console.OpenStandardOutput()));
        stderr_targets.Add(PipeTarget.ToStream(Console.OpenStandardError()));
    }
    if (stdout_file != null) stdout_targets.Add(PipeTarget.ToFile(stdout_file));
    if (stderr_file != null) stderr_targets.Add(PipeTarget.ToFile(stderr_file));

    cmd = cmd.WithStandardOutputPipe(PipeTarget.Merge(stdout_targets.ToArray()));
    cmd = cmd.WithStandardErrorPipe(PipeTarget.Merge(stderr_targets.ToArray()));

    task = cmd.ExecuteAsync();
    result = await task;
}
else
{
    task = cmd.ExecuteAsync();
}

int exit_code = !wait_for_exit ? 0 : result.ExitCode;
Environment.Exit(exit_code);



///////////////////////////////////////


print_help:;
string f = Path.GetFileName(__FILE__ + " - " + __PROJECT_NAME__ + " v" + __PROJECT_VERSION__);
// TODO #helptxt8548745 move help to txt template file
Console.WriteLine(f +
    "\n\nInvokes a program silently." +
    "\nFeatures:" +
    "\n- can locate the executable via relative path, absolute path, or the system's PATH" +
    "\n- can run the program with a different starting directory (via parameter)" +
    "\n- waits for the program to be over (or doesn't, via parameter)" +
    "\n- exits with the invocation's exit code (or doesn't, via parameter)" +
    "\n- if requested (via parameters), outputs the invocation's output to the console or files (binary safe)" +
    "\n\nUsage:" +
    "\n    " + f + " [parameters] executable [executable arguments]" +
    "\n\nParameters: " +
    "\n    [--]                                   stops parsing arguments and start parsing executable" +
    "\n    [--version]                            prints version" +
    "\n    [--help]                               prints help" +
    "\n    [--working-directory <directory>]      runs the program with the given starting directory" +
    "\n    [--show-output]                        prints the invocation's output to console (both stdout and stderr)" +
    "\n    [--stdout <filepath>]                  appends the stdout stream to the given file" +
    "\n    [--stderr <filepath>]                  appends the stderr stream to the given file" +
    "\n    [--dont-wait]                          doesn't wait for the invoked program to be over, immediately quits with exit code 0" +
    "\n\nUsage examples (NB: escape special characters according to your shell's rules):" +
    "\n    " + f + " myprogram --with -its /arguments list" +
    "\n    " + f + " --dont-wait -- myscript.cmd /with /args" +
    "\n    " + f + " ..\\some\\program\\relative\\to\\the\\current\\directory.exe" +
    "\n    " + f + " cmd /c shutdown /p" +
    "\n    " + f + " --stdout out.txt --stderr err.txt --show-output cmd /c \"echo my output & echo my err >&2\"" +
    "\n    " + f + " logoff" +
    "\n    " + f + " c:\\absolute\\path\\to\\program.exe" +
    "");
Environment.Exit(0);
