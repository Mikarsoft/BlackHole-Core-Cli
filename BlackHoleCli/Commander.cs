using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace BlackHoleCli
{
    public static class Commander
    {
        private static readonly JsonSerializerOptions _options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        public static void ScanSolution(string[] commandText)
        {
            int exitCodeNum = 0;
            string currentDir = Directory.GetCurrentDirectory();
            string[] files = Directory.GetFiles(currentDir, "*.sln");

            List<ReferencedProjectInfo> projects = new List<ReferencedProjectInfo>();

            if (files.Length == 1)
            {
                string[] solutionLines = File.ReadAllLines(files[0]);
                projects = ReferencedProjects(solutionLines, currentDir);
                
                List<string> ProjectsWithBlackHole = BlackHoleInitializationProjects(projects, currentDir);

                if(ProjectsWithBlackHole.Count == 1)
                {
                    string workingDir = Path.Combine(currentDir, "BhDir");
                    var buildResult = RunCommand("dotnet", $"build {ProjectsWithBlackHole[0]} /p:OutputPath={workingDir}");

                    if (buildResult == 0)
                    {
                        Console.Write("", Console.ForegroundColor = ConsoleColor.Green);
                        Console.WriteLine("");
                        string bhAssemblyPath = Path.Combine(workingDir, "BlackHole.dll");
                        string projectAssemblyPath = Path.Combine(workingDir, "BlackHoleTesting.dll");

                        bool assemblyTest = BlackHoleAssemblyTest(bhAssemblyPath, "add-migration");

                        if (assemblyTest)
                        {
                            Console.Write("BlackHole Assembly is correct", Console.ForegroundColor = ConsoleColor.Green);
                            Console.WriteLine("");
                            Console.WriteLine("");

                            Console.WriteLine("Begin running..", Console.ForegroundColor = ConsoleColor.White);
                            Console.WriteLine("");
                            CreateCommandSettings(workingDir, commandText);
                            exitCodeNum = RunCommand("dotnet", $"exec {projectAssemblyPath}");

                            if(exitCodeNum == 0)
                            {
                                Console.Write($"Exit code {exitCodeNum}. Project run finished successfully", Console.ForegroundColor = ConsoleColor.White);
                                Console.WriteLine("");
                            }
                            else
                            {
                                exitCodeNum = 505;
                                Console.Write($"Error {exitCodeNum}. Project contains errors. Run failed", Console.ForegroundColor = ConsoleColor.Red);
                                Console.WriteLine("");
                            }

                            Console.WriteLine("");
                        }
                        else
                        {
                            exitCodeNum = 401;
                            Console.Write("BlackHole Assembly is incorrect or missing", Console.ForegroundColor = ConsoleColor.Red);
                            Console.WriteLine("");
                        }
                    }
                    else
                    {
                        exitCodeNum = 502;
                        Console.Write($"Error {exitCodeNum}. Project {ProjectsWithBlackHole[0]}, build failed", Console.ForegroundColor = ConsoleColor.Red);
                        Console.WriteLine("");
                    }

                    Console.WriteLine("");

                    if (!CleanDirectory(workingDir))
                    {
                        int warningCode = 301;
                        Console.WriteLine($"Warning {warningCode}. Could not delete working directory", Console.ForegroundColor = ConsoleColor.Yellow);
                        Console.WriteLine("");
                        exitCodeNum = exitCodeNum == 0 ? warningCode : exitCodeNum;
                    }
                }
                else
                {
                    exitCodeNum = 501;
                    Console.WriteLine($"Error {exitCodeNum}. Zero or More than One, Startup projects that use BlackHole.dll found in this solution", Console.ForegroundColor = ConsoleColor.Red);
                    Console.WriteLine("");
                }

                Console.WriteLine("");

                if (exitCodeNum == 0)
                {
                    Console.Write($"Exit code {exitCodeNum}. Process has finished successfully", Console.ForegroundColor = ConsoleColor.White);
                    Console.WriteLine("");
                }
                else
                {
                    Console.Write($"Exit code {exitCodeNum}. Process has finished with Errors", Console.ForegroundColor = ConsoleColor.White);
                    Console.WriteLine("");
                }
            }
            else
            {
                exitCodeNum = 404;
                Console.Write($"Error {exitCodeNum}. Zero or More than One, Solution files found in this directory.", Console.ForegroundColor = ConsoleColor.White);
                Console.WriteLine("");
            }
        }

        static void CreateCommandSettings(string WorkDirPath, string[] arguments)
        {
            BHCommandProperties commandSettings = new BHCommandProperties
            {
                ProjectPath = WorkDirPath,
                CliCommand = "aaaa",
                SettingMode = "--aaaaa--"
            };

            string jsonString = JsonSerializer.Serialize(commandSettings, _options);
            File.WriteAllText(Path.Combine(WorkDirPath,"blackHole_Cli_command.json"), jsonString);
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
                Console.Write("Cleaning temporary directory", Console.ForegroundColor = ConsoleColor.White);
                Console.WriteLine("");

                DirectoryInfo di = new DirectoryInfo(dirPath);
                di.Delete(true);

                Console.Write("Directory has been cleaned", Console.ForegroundColor = ConsoleColor.Green);
                Console.WriteLine("");

                success = true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message, Console.ForegroundColor = ConsoleColor.Yellow);
                Console.WriteLine("");
                success = false;
            }

            Console.WriteLine("");
            return success;
        }

        static bool BlackHoleAssemblyTest(string bhAssemblyPath, string commandText)
        {
            bool success = false;

            try
            {
                Console.Write("Testing BlackHole assembly and version", Console.ForegroundColor = ConsoleColor.White);
                Console.WriteLine("");

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
                            Console.WriteLine("Possibly wrong BlackHole version. BlackHole Cli Supports versions from 6.0.1 and above", Console.ForegroundColor = ConsoleColor.Yellow);
                            Console.WriteLine("");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Possibly wrong BlackHole version. BlackHole Cli Supports versions from 6.0.1 and above", Console.ForegroundColor = ConsoleColor.Yellow);
                        Console.WriteLine("");
                    }
                }
                else
                {
                    Console.WriteLine("BlackHole assembly was not found in the build",Console.ForegroundColor = ConsoleColor.Red);
                    Console.WriteLine("");
                }
                projectAssembly = null;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message, Console.ForegroundColor = ConsoleColor.Red);
                Console.WriteLine("");
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
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                if(e.Data != null)
                {
                    Console.WriteLine(e.Data, Console.ForegroundColor = ConsoleColor.White);
                    Console.WriteLine("");
                }
            });

            process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data, Console.ForegroundColor = ConsoleColor.Red);
                    Console.WriteLine("");
                }
            });

            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
