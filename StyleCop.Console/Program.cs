using DocoptNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StyleCop.Console
{
    public class Program
    {
        private static int _encounteredViolations;

        private static bool m_LastPrinted = false;

        private static HashSet<string> m_BadPaths { get; } = new HashSet<string>
        {
            @"\obj\Debug\",
            @"\obj\Release\",
            @"\bin\Debug\",
            @"\bin\Release\",
            @"\packages\"
        };

        private const string Usage = @"
StyleCop.

Usage:
    StyleCop --help
    StyleCop [--path=<path>] [--settings=<settingsPath>]

Options:
    -h --help                   Show this screen
    -p=<path> --path=<path>     Root folder to search in [default: .]
    --settings=<settingsPath>   Path to settings file";

        public static int Main(string[] args)
        {
            try
            {
                var arguments = new Docopt().Apply(Usage, args, exit: true);

                var projectPath = (string)arguments["--path"].Value;
                var settingsLocation = arguments["--settings"]?.Value as string;

                if (settingsLocation == null)
                {
                    settingsLocation = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "Settings.StyleCop");
                }

                if (!File.Exists(settingsLocation))
                {
                    Log();
                    Log($"ERROR: Invalid path specified for Settings.StyleCop \"{settingsLocation}\"!");
                    Log(Usage);
                    return (int)ExitCode.Failed;
                }

                if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
                {
                    Log();
                    Log($"ERROR: Invalid path specified \"{projectPath}\"!");
                    return (int)ExitCode.Failed;
                }

                var searchOption = SearchOption.AllDirectories;

                return ProcessFolder(settingsLocation, projectPath, searchOption);
            }
            catch (Exception ex)
            {
                Log($"An unhandled exception occured: {ex}");
                return (int)ExitCode.Failed;
            }
        }

        private static void Log(string msg = "")
        {
            System.Console.WriteLine(msg);
        }

        private static int ProcessFolder(string settings, string projectPath, SearchOption searchOption)
        {
            Log($"Checking folder: {new FileInfo(projectPath).FullName}");
            var console = new StyleCopConsole(settings, false, null, null, true);
            var project = new CodeProject(0, projectPath, new Configuration(null));

            var files = Directory.EnumerateFiles(projectPath, "*.cs", searchOption).ToList();
            Log($"Checking {files.Count} files");

            files = GetCSharpFiles(files);

            foreach (var file in files)
            {
                console.Core.Environment.AddSourceCode(project, file, null);
            }

            console.OutputGenerated += OnOutputGenerated;
            console.ViolationEncountered += OnViolationEncountered;
            console.Start(new[] { project }, true);
            console.OutputGenerated -= OnOutputGenerated;
            console.ViolationEncountered -= OnViolationEncountered;

            if (_encounteredViolations > 0)
            {
                Log("Finished with errors");
                return (int)ExitCode.Failed;
            }

            Log("Success");
            return (int)ExitCode.Passed;
        }

        private static List<string> GetCSharpFiles(List<string> files)
        {
            var output = new List<string>();
            foreach (var file in files)
            {
                var isBad = false;
                foreach (var badPath in m_BadPaths)
                {
                    if (file.Contains(badPath))
                    {
                        isBad = true;
                        break;
                    }
                }

                if (!isBad)
                {
                    output.Add(file);
                }
            }

            return output;
        }

        private static void OnOutputGenerated(object sender, OutputEventArgs e)
        {
            m_LastPrinted = false;
        }

        private static void OnViolationEncountered(object sender, ViolationEventArgs e)
        {
            if (!m_LastPrinted)
            {
                m_LastPrinted = true;
                Log(e.SourceCode.Path);
            }

            _encounteredViolations++;
            WriteLineViolationMessage(string.Format("  Line {0}: {1} ({2})", e.LineNumber, e.Message,
                e.Violation.Rule.CheckId));
        }

        private static void WriteLineViolationMessage(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            Log(message);
            System.Console.ResetColor();
        }
    }
}