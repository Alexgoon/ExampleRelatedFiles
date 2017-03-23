using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleBuildExampleTester {
    class Program {
        static int Main(string[] args) {
            //args = new string[] { @"TestExample_15.1.5-15.1.7", "17068", @"\\corp\builds\release\Build.v16.2.Release\NetStudio.v16.2.2005\2017-03-10_17-55" };
            string exampleName = string.Empty;
            string build = string.Empty;
            string assemblySource = string.Empty;
            string examplePath = string.Empty;
            try {
                exampleName = args[0];
                build = args[1];
                assemblySource = args[2];
            }
            catch (Exception) {
                Console.WriteLine("Please check if parameters are correct");
                return 1;
            }
            string rootExamplesFolder = System.Configuration.ConfigurationManager.AppSettings["RootExamplesFolder"];
            if (exampleName.EndsWith("_latestBuild")) {
                examplePath = LatestBuildHelper.GetLatestExamplePath(exampleName, rootExamplesFolder);
            }
            else {
                examplePath = string.Format(@"{0}\{1}", rootExamplesFolder, exampleName);
            }
            SingleBuildExampleTester tester = new SingleBuildExampleTester(examplePath, build, assemblySource);
            return tester.ProcessExample() ? 0 : 1;
        }
    }
}
