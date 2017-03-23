using LibGit2Sharp;
using SingleBuildExampleTester;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExampleRangeTester.Helpers {
    public static class BuildsHelper {
        public static List<ExampleBuild> GetBuilds(string branchName, string gitPath, string buildsFolder) {
            Repository repository = new Repository(gitPath);
            string canonicalNamePrefix = "refs/remotes/origin/";
            Regex regex = new Regex(canonicalNamePrefix + @"\d+.\d+.\d+");
            ExampleBuild startBuild = new ExampleBuild(branchName.Replace("+", ""));
            ExampleBuild endBuild = startBuild;
            string buildString = string.Empty;
            foreach (var branch in repository.Branches) {
                if (regex.IsMatch(branch.CanonicalName)) {
                    buildString = branch.CanonicalName.Replace(canonicalNamePrefix, string.Empty).Replace("+", "");
                    ExampleBuild build = new ExampleBuild(buildString);
                    if (build.CompareTo(startBuild) == 1 && (endBuild.CompareTo(build) == 1 || endBuild.CompareTo(startBuild) == 0)) {
                        endBuild = build;
                    }
                }
            }
            return GetValidatedBuilds(startBuild, endBuild, buildsFolder);
        }
        static List<ExampleBuild> GetValidatedBuilds(ExampleBuild startBuild, ExampleBuild endBuild, string buildsFolder) {
            List<ExampleBuild> parsedBuilds = new List<ExampleBuild>();
            if (startBuild.CompareTo(endBuild) == 0)
                endBuild = ExampleBuild.MaxValue;
            foreach (string dir in Directory.EnumerateDirectories(buildsFolder)) {
                string dirName = Path.GetFileName(dir);
                ExampleBuild folderBuild = null;
                try {
                    folderBuild = new ExampleBuild(dirName);
                }
                catch {
                    continue;
                }
                if (startBuild.CompareTo(folderBuild) != 1 && endBuild.CompareTo(folderBuild) == 1) {
                    parsedBuilds.Add(folderBuild);
                }
            }
            return parsedBuilds.OrderBy(b => b).ToList<ExampleBuild>();
        }
    }
}
