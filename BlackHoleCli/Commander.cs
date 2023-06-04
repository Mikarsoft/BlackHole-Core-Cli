using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System;

namespace BlackHoleCli
{
    public static class Commander
    {
        private static readonly JsonSerializerOptions _options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        private static string MainCommand { get; set; } = string.Empty;
        private static string SubCommand { get; set; } = string.Empty;
        private static string ThirdCommand { get; set; } = string.Empty;

        public static void ScanSolution(string[] commandText)
        {
            int exitCodeNum = 0;
            if (CheckCommands(commandText))
            {
                string currentDir = Directory.GetCurrentDirectory();
                string[] files = Directory.GetFiles(currentDir, "*.sln");

                if (files.Length == 1)
                {
                    List<ReferencedProjectInfo> projects = new List<ReferencedProjectInfo>();

                    string[] solutionLines = File.ReadAllLines(files[0]);
                    projects = ReferencedProjects(solutionLines, currentDir);

                    List<string> ProjectsWithBlackHole = BlackHoleInitializationProjects(projects, currentDir);

                    if (ProjectsWithBlackHole.Count == 1)
                    {
                        string workingDir = Path.Combine(currentDir, "BhDir");
                        Console.WriteLine($"Building project {Path.GetFileName(ProjectsWithBlackHole[0]).Replace(".csproj","")}..");
                        Console.WriteLine(" ");
                        int buildResult = BuildCommand("dotnet", $"build {ProjectsWithBlackHole[0]} /p:OutputPath={workingDir}");

                        if (buildResult == 0)
                        {
                            Console.WriteLine("Build was successful", Console.ForegroundColor = ConsoleColor.Green);
                            Console.WriteLine("-");

                            string bhAssemblyPath = Path.Combine(workingDir, "BlackHole.dll");
                            string projectAssemblyPath = Path.Combine(workingDir, "BlackHoleTesting.dll");

                            bool assemblyTest = BlackHoleAssemblyTest(bhAssemblyPath, "add-migration");

                            if (assemblyTest)
                            {
                                Console.WriteLine("\t BlackHole Assembly is correct", Console.ForegroundColor = ConsoleColor.Green);
                                Console.WriteLine(" ");

                                CreateCommandSettings(workingDir, commandText, Path.GetDirectoryName(ProjectsWithBlackHole[0]));

                                Console.WriteLine("Begin running..", Console.ForegroundColor = ConsoleColor.White);
                                Console.WriteLine(" ");

                                exitCodeNum = RunCommand("dotnet", $"exec {projectAssemblyPath}");

                                if (exitCodeNum == 0)
                                {
                                    Console.WriteLine($"Exit code {exitCodeNum}. Project run finished successfully", Console.ForegroundColor = ConsoleColor.Green);
                                    Console.WriteLine(" ");
                                }
                                else
                                {
                                    Console.WriteLine($"Error {exitCodeNum}. Project contains errors. Run failed", Console.ForegroundColor = ConsoleColor.Red);
                                    Console.WriteLine(" ");
                                }
                            }
                            else
                            {
                                exitCodeNum = 401;
                                Console.WriteLine("BlackHole Assembly is incorrect or missing", Console.ForegroundColor = ConsoleColor.Red);
                                Console.WriteLine(" ");
                            }
                        }
                        else
                        {
                            exitCodeNum = 502;
                            Console.WriteLine($"Error {exitCodeNum}. Project {ProjectsWithBlackHole[0]}, build failed", Console.ForegroundColor = ConsoleColor.Red);
                            Console.WriteLine(" ");
                        }

                        Console.WriteLine("");

                        if (!CleanDirectory(workingDir))
                        {
                            int warningCode = 301;
                            Console.WriteLine($"Warning {warningCode}. Could not delete working directory", Console.ForegroundColor = ConsoleColor.Yellow);
                            Console.WriteLine(" ");
                            exitCodeNum = exitCodeNum == 0 ? warningCode : exitCodeNum;
                        }
                    }
                    else
                    {
                        exitCodeNum = 501;
                        Console.WriteLine($"Error {exitCodeNum}. Zero or More than One, Startup projects that use BlackHole.dll found in this solution", Console.ForegroundColor = ConsoleColor.Red);
                        Console.WriteLine(" ");
                    }

                    if (exitCodeNum == 0)
                    {
                        Console.WriteLine($"Exit code {exitCodeNum}. Process has finished successfully", Console.ForegroundColor = ConsoleColor.White);
                        Console.WriteLine(" ");
                    }
                    else
                    {
                        Console.WriteLine($"Exit code {exitCodeNum}. Process has finished with Errors", Console.ForegroundColor = ConsoleColor.White);
                        Console.WriteLine(" ");
                    }
                }
                else
                {
                    exitCodeNum = 404;
                    Console.WriteLine($"Error {exitCodeNum}. Zero or More than One, Solution files found in this directory.", Console.ForegroundColor = ConsoleColor.White);
                    Console.WriteLine(" ");
                }
            }
            else
            {
                exitCodeNum = 500;
                Console.WriteLine($"Error {exitCodeNum}. The Command is wrong", Console.ForegroundColor = ConsoleColor.Red);
                Console.WriteLine("-", Console.ForegroundColor = ConsoleColor.White);
                Console.WriteLine("BlackHole supported main commands => update , drop , parse");
                Console.WriteLine("\t BlackHole main command 'update' => update , Is used to update the database according to your projects Entities and Settings");
                Console.WriteLine("\t BlackHole main command 'drop'   => drop , Is used to drop all the tables and the database");
                Console.WriteLine("\t BlackHole main command 'parse'  => parse , Is used to read an existing database and create Entities in your project according to the database tables");
                Console.WriteLine("-");
                Console.WriteLine("\t BlackHole argument 'force' => --force or -f , Is used to complete the process without requiring user input, selecting 'Y' in all user prompts");
                Console.WriteLine("\t BlackHole argument 'save'  => --save or -s , Is used to save the database sql and migratons into an sql file at BlackHole's defautl datapath");
                Console.WriteLine("-");
                Console.WriteLine("\t BlackHole Cli Command Example 1 => bhl update");
                Console.WriteLine("\t BlackHole Cli Command Example 2 => bhl update -f");
                Console.WriteLine("\t BlackHole Cli Command Example 3 => bhl update -s");
                Console.WriteLine("\t BlackHole Cli Command Example 4 => bhl update -f -s ");
            }

        }

        public static bool CheckCommands(string[] arguments)
        {
            bool mainCommand = false;
            bool subCommand = false;
            bool thirdCommand = false;

            if(arguments.Length == 1)
            {
                mainCommand = MainCommandSwitch(arguments[0]);
                return mainCommand;
            }

            if(arguments.Length == 2)
            {
                mainCommand = MainCommandSwitch(arguments[0]);
                subCommand = SubCommandSwitch(arguments[1]);
                return mainCommand && subCommand;
            }

            if (arguments.Length == 3)
            {
                mainCommand = MainCommandSwitch(arguments[0]);
                subCommand = SubCommandSwitch(arguments[1]);
                thirdCommand = ThirdCommandSwitch(arguments[2]);
                return mainCommand && subCommand && thirdCommand;
            }

            return false;
        }

        private static bool MainCommandSwitch(string maincom)
        {
            bool correct = false;

            switch (maincom.ToLower())
            {
                case "update":
                    MainCommand = maincom.ToLower();
                    correct = true;
                    break;
                case "drop":
                    MainCommand = maincom.ToLower();
                    correct = true;
                    break;
                case "parse":
                    MainCommand = maincom.ToLower();
                    correct = true;
                    break;
                default:
                    correct = false;
                    break;
            }

            if (!correct)
            {
                Console.WriteLine($"Unrecognized main command", Console.ForegroundColor = ConsoleColor.Red);
                Console.WriteLine(" ");
            }

            return correct;
        }

        private static bool SubCommandSwitch(string subcom)
        {
            bool correct = false;

            switch (subcom.ToLower())
            {
                case "--save":
                    SubCommand = "savesql";
                    correct = true;
                    break;
                case "--force":
                    SubCommand = "forceaction";
                    correct = true;
                    break;
                case "-s":
                    SubCommand = "savesql";
                    correct = true;
                    break;
                case "-f":
                    SubCommand = "forceaction";
                    correct = true;
                    break;
                default:
                    correct = false;
                    break;
            }

            if (!correct)
            {
                Console.WriteLine($"Unrecognized second argument", Console.ForegroundColor = ConsoleColor.Red);
                Console.WriteLine(" ");
            }

            return correct;
        }

        private static bool ThirdCommandSwitch(string subcom)
        {
            bool correct = false;

            switch (subcom.ToLower())
            {
                case "--save":
                    ThirdCommand = "savesql";
                    correct = ThirdCommand != SubCommand;
                    break;
                case "--force":
                    ThirdCommand = "forceaction";
                    correct = ThirdCommand != SubCommand;
                    break;
                case "-s":
                    ThirdCommand = "savesql";
                    correct = ThirdCommand != SubCommand;
                    break;
                case "-f":
                    ThirdCommand = "forceaction";
                    correct = ThirdCommand != SubCommand;
                    break;
                default:
                    correct = false;
                    break;
            }

            if (!correct)
            {
                Console.WriteLine($"Error at the third argument", Console.ForegroundColor = ConsoleColor.Red);
                Console.WriteLine(" ");
            }

            return correct;
        }

        static bool CreateCommandSettings(string WorkDirPath, string[] arguments, string? projectDirectory)
        {
            try
            {
                Console.WriteLine("Saving command settings..", Console.ForegroundColor = ConsoleColor.White);

                BHCommandProperties commandSettings = new BHCommandProperties
                {
                    ProjectPath = projectDirectory,
                    CliCommand = MainCommand,
                    SettingMode = SubCommand,
                    ExtraMode = ThirdCommand
                };

                string jsonString = JsonSerializer.Serialize(commandSettings, _options);
                File.WriteAllText(Path.Combine(WorkDirPath, "blackHole_Cli_command.json"), jsonString);

                Console.WriteLine("\t Command settings have been saved.", Console.ForegroundColor = ConsoleColor.Green);
                Console.WriteLine(" ");

                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message, Console.ForegroundColor = ConsoleColor.Red);
                Console.WriteLine(" ");
                return false;
            }
        }

        static bool GetStartupProject(string projectNode, string solutionFilePath)
        {
            try
            {
                string? solutionDirectory = Path.GetDirectoryName(solutionFilePath);

                if(solutionDirectory != null)
                {
                    string projectPath = projectNode;

                    string projectFullPath = Path.GetFullPath(Path.Combine(solutionDirectory, projectPath));

                    string projectExtension = Path.GetExtension(projectFullPath);

                    if (projectExtension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        string? projectDirectory = Path.GetDirectoryName(projectFullPath);

                        if(projectDirectory != null)
                        {
                            string launchSettingsPath = Path.Combine(projectDirectory, "Properties", "launchSettings.json");

                            if (File.Exists(launchSettingsPath))
                            {
                                return true;
                            }

                            string programFilePath = Path.Combine(projectDirectory, "Program.cs");

                            if (File.Exists(programFilePath))
                            {
                                return true;
                            }

                            string StartupFilePath = Path.Combine(projectDirectory, "Startup.cs");

                            if (File.Exists(StartupFilePath))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while detecting the startup project: {ex.Message}", Console.ForegroundColor = ConsoleColor.Yellow);
            }

            return false;
        }

        static bool CleanDirectory(string dirPath)
        {
            bool success = false;

            try
            {
                Console.WriteLine("Cleaning temporary directory..", Console.ForegroundColor = ConsoleColor.White);

                DirectoryInfo di = new DirectoryInfo(dirPath);
                di.Delete(true);

                Console.WriteLine("\t Directory has been cleaned", Console.ForegroundColor = ConsoleColor.Green);
                Console.WriteLine(" ");

                success = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message, Console.ForegroundColor = ConsoleColor.Yellow);
                Console.WriteLine(" ");
                success = false;
            }

            return success;
        }

        static bool BlackHoleAssemblyTest(string bhAssemblyPath, string commandText)
        {
            bool success = false;

            try
            {
                Console.WriteLine("Testing BlackHole assembly and version..", Console.ForegroundColor = ConsoleColor.White);

                Assembly? projectAssembly = Assembly.Load(File.ReadAllBytes(bhAssemblyPath));

                if(projectAssembly != null)
                {
                    Type? targetType = projectAssembly.GetType("BlackHole.Statics.CliCommand");

                    if (targetType != null)
                    {
                        PropertyInfo? property = targetType.GetProperty("BHRun");

                        if (property != null && property.PropertyType == typeof(string))
                        {
                            success = true;
                        }
                        else
                        {
                            Console.WriteLine("\t Possibly wrong BlackHole version. BlackHole Cli Supports versions from 6.0.1 and above", Console.ForegroundColor = ConsoleColor.Yellow);
                            Console.WriteLine(" ");
                        }
                    }
                    else
                    {
                        Console.WriteLine("\t Possibly wrong BlackHole version. BlackHole Cli Supports versions from 6.0.1 and above", Console.ForegroundColor = ConsoleColor.Yellow);
                        Console.WriteLine(" ");
                    }
                }
                else
                {
                    Console.WriteLine("BlackHole assembly was not found in the build",Console.ForegroundColor = ConsoleColor.Red);
                    Console.WriteLine(" ");
                }
                projectAssembly = null;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message, Console.ForegroundColor = ConsoleColor.Red);
                Console.WriteLine(" ");
                success = false;
            }

            return success;
        }

        static List<string> BlackHoleInitializationProjects(List<ReferencedProjectInfo> projectsInfos, string slnPath)
        {
            List<string> projectsToInitialize = new List<string>();

            foreach (ReferencedProjectInfo project in projectsInfos)
            {
                if(GetStartupProject(project.ProjectPath, slnPath))
                {
                    XmlDocument projDefinition = new XmlDocument();
                    projDefinition.Load(project.ProjectPath);
                    XmlNodeList? packageNodes = projDefinition.SelectNodes("//PackageReference");


                    if (packageNodes != null)
                    {
                        foreach (XmlNode packageNode in packageNodes)
                        {
                            var packageAttr = packageNode.Attributes?["Include"];

                            if (packageAttr != null)
                            {
                                string packageName = packageAttr.Value;

                                if (packageName.ToLower() == "blackhole.core.orm")
                                {
                                    projectsToInitialize.Add(project.ProjectPath);
                                }
                            }

                        }
                    }

                    XmlNodeList? projectNodes = projDefinition.SelectNodes("//ProjectReference");

                    if (projectNodes != null)
                    {
                        foreach (XmlNode projectNode in projectNodes)
                        {
                            var projectAttr = projectNode.Attributes?["Include"];

                            if (projectAttr != null)
                            {
                                string projectName = projectAttr.Value;

                                if (Path.GetFileName(projectName).Replace(".csproj", "").ToLower() == "blackhole")
                                {
                                    projectsToInitialize.Add(project.ProjectPath);
                                }
                            }
                        }
                    }
                }
            }
            return projectsToInitialize;
        }

        static List<ReferencedProjectInfo> ReferencedProjects(string[] allLines, string projectSolutionPath)
        {
            List<ReferencedProjectInfo> projectLines = new List<ReferencedProjectInfo>();
            foreach(string slnLine in allLines)
            {
                if (slnLine.ToLower().StartsWith("project"))
                {
                    string[] projectInfo = slnLine.Split(",");
                    ReferencedProjectInfo referencedProjectInf = new ReferencedProjectInfo();

                    if(projectInfo.Length == 3)
                    {
                        string[] projectName = projectInfo[0].Split("=");
                        referencedProjectInf.ProjectPath = Path.Combine(projectSolutionPath,projectInfo[1].Replace(@"""","").Trim());

                        if(projectName.Length == 2)
                        {
                            referencedProjectInf.ProjectName = projectName[1];
                            projectLines.Add(referencedProjectInf);
                        }
                    }
                }
            }
            return projectLines;
        }

        static int RunCommand(string command, string arguments)
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                if (e.Data != null && e.Data.StartsWith("_bhLog_"))
                {
                    Console.WriteLine(e.Data.Replace("_bhLog_",""), Console.ForegroundColor = ConsoleColor.White);
                }
            });

            process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data, Console.ForegroundColor = ConsoleColor.Red);
                }
            });

            process.WaitForExit();
            return process.ExitCode;
        }

        static int BuildCommand(string command, string arguments)
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                if (e.Data != null && e.Data.Length < 200)
                {
                    Console.WriteLine(e.Data, Console.ForegroundColor = ConsoleColor.White);
                }
            });

            process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data, Console.ForegroundColor = ConsoleColor.Red);
                }
            });

            process.WaitForExit();
            return process.ExitCode;
        }

        public static void ReadlinePipe()
        {
            using (var pipeServer = new NamedPipeServerStream("BlackHolePipe", PipeDirection.Out))
            {
                pipeServer.WaitForConnection();

                using (var writer = new StreamWriter(pipeServer))
                {
                    Console.Write("Are you sure you want to proceed? (Y/N): ");
                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                    string result = "no";
                    if (key.Key == ConsoleKey.Y)
                    {
                        // User confirmed, proceed with the action
                        result = "yes";
                        // ... Continue with your logic
                    }
                    else
                    {
                        // User canceled, abort the action
                        Console.WriteLine("Action canceled!");
                        // ... Handle cancellation logic
                    }

                    writer.WriteLine(result);
                    writer.Flush();
                }
            }
        }
    }
}
