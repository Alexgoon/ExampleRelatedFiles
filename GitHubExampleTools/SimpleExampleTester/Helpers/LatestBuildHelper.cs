using SingleBuildExampleTester;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleBuildExampleTester {
    public static class LatestBuildHelper {
        public static string GetLatestExamplePath(string exampleName, string rootExamplesFolder) {
            string result = null;
            ExampleBuild maxBuild = new ExampleBuild(int.MinValue, int.MinValue, int.MinValue);
            foreach (string dir in Directory.GetDirectories(rootExamplesFolder)) {
                if (dir.Contains(exampleName)) {
                    string stringBuildValue = dir.Replace(exampleName + "_", string.Empty).Replace("+", string.Empty);
                    ExampleBuild build = new ExampleBuild(stringBuildValue);
                    if (build.CompareTo(maxBuild) == 1) {
                        maxBuild = build;
                        result = dir;
                    }
                }
            }
            return result;
        }
    }
}
