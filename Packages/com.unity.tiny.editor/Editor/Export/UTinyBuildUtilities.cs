#if NET_4_6
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.Tiny
{
    public static class UTinyBuildUtilities
    {
        #region Fields

        private static HashSet<string> s_NpmInstallDirectories = new HashSet<string>();

        #endregion

        private static FileInfo ZipProgramFile()
        {
            return new FileInfo(TinyPreferences.Default7zPath());
        }

        private static bool RunNpmInstall(DirectoryInfo workingDirectory)
        {
            // Run 'npm install' only if we didn't run it in this path already
            var nodeModulesDir = workingDirectory.GetDirectories().FirstOrDefault(d => d.Name == "node_modules");
            if (nodeModulesDir == null || !s_NpmInstallDirectories.Contains(workingDirectory.FullName))
            {
                // note: npm fails (outputs in the error stream) the first time it runs
                if (!RunInShell("npm install --no-progress", new ShellProcessArgs()
                    {
                        WorkingDirectory = workingDirectory,
                        ExtraPaths = TinyPreferences.NodeDirectory.AsEnumerable(),
                        ThrowOnError = false
                    }))
                {
                    if (!RunInShell("npm install --no-progress", new ShellProcessArgs()
                        {
                            WorkingDirectory = workingDirectory,
                            ExtraPaths = TinyPreferences.NodeDirectory.AsEnumerable()
                        }))
                    {
                        return false;
                    }
                }

                // Remember working directory successfully ran npm install
                s_NpmInstallDirectories.Add(workingDirectory.FullName);
            }
            return true;
        }

        public static bool RunNode(DirectoryInfo workingDirectory, string jsProgram, params string[] arguments)
        {
            // Install modules as needed
            if (!RunNpmInstall(workingDirectory))
            {
                return false;
            }

            // run the program
            var command = $"node \"{jsProgram ?? "index.js"}\" {string.Join(" ", arguments)}";
            return RunInShell(command, new ShellProcessArgs()
            {
                WorkingDirectory = workingDirectory,
                ExtraPaths = TinyPreferences.NodeDirectory.AsEnumerable()
            });
        }
        
        public static Process RunNodeNoWait(DirectoryInfo workingDirectory, string jsProgram, string arguments, DataReceivedEventHandler outputReceived = null, DataReceivedEventHandler errorReceived = null)
        {
            // Install modules as needed
            if (!RunNpmInstall(workingDirectory))
            {
                return null;
            }

            // Run the program
            return UTinyShell.RunNoWait("node", $"{jsProgram ?? "index.js"} {string.Join(" ", arguments)}",
                new ShellProcessArgs()
                {
                    WorkingDirectory = workingDirectory,
                    ExtraPaths = TinyPreferences.NodeDirectory.AsEnumerable()
                }, outputReceived, errorReceived);
        }

        public static bool ZipFolder(DirectoryInfo folder, string zipPath)
        {
            File.Delete(zipPath);

            var zip = ZipProgramFile();
            return RunInShell($"\"{zip.FullName}\" a \"{zipPath}\" \"{folder.FullName}\"", ShellProcessArgs.Default);
        }

        public static bool ZipPaths(string[] toZip, string zipPath)
        {
            File.Delete(zipPath);

            var zip = ZipProgramFile();
            var paths = string.Join(" ", toZip.Select(path => $"\"{path}\""));
            return RunInShell($"\"{zip.FullName}\" a \"{zipPath}\" {paths}", ShellProcessArgs.Default);
        }

        public static bool UnzipFile(string zipPath, DirectoryInfo destFolder)
        {
            if (!destFolder.Exists)
            {
                destFolder.Create();
            }
            var zip = ZipProgramFile();
            return RunInShell($"\"{zip.FullName}\" x -y -o. \"{zipPath}\"", new ShellProcessArgs()
            {
                WorkingDirectory = destFolder
            });
        }

        /// <summary>
        /// Deletes a directory and all of its contents.
        /// This method will throw if a file or directory cannot be deleted.
        /// </summary>
        public static void PurgeDirectory(DirectoryInfo dir)
        {
            if (!dir.Exists)
            {
                return;
            }
            foreach (var d in dir.GetDirectories())
            {
                PurgeDirectory(d);
            }
            foreach (var f in dir.GetFiles())
            {
                f.Delete();
            }
            dir.Delete();
        }

        /// <summary>
        /// Runs the given command in the OS shell (cmd on Windows, bash on Mac/Linux).
        /// </summary>
        public static bool RunInShell(string command, ShellProcessArgs processArgs, bool outputOnErrorOnly = true)
        {
            var output = UTinyShell.RunInShell(command, processArgs);

            if (!output.Succeeded)
            {
                Debug.LogError($"{UTinyConstants.ApplicationName}: {output.FullOutput}");
            }
            else if (!outputOnErrorOnly)
            {
                Debug.Log($"{UTinyConstants.ApplicationName}: {output.FullOutput}");
            }

            return output.Succeeded;
        }
    }
}
#endif // NET_4_6