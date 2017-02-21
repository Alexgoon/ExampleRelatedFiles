using CodeCentral.Helpers;
using CodeCentral.Infrastructure;
using CodeCentral.Services;
using CodeCentral.Tester;
using CodeCentral.Tools;
using GitExampleHelper;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleRangeTester {
    class Program {
        static int Main(string[] args) {
            //args = new string[] { @"D:\progs\Jenkins\jobs\E5171-How-to-bind-a-dashboard-to-a-List-object_14.1.9-15.1.1\workspace", "CS_14.1.9-15.1.1", "Breaking commit", "codecentral-examples/E5171-How-to-bind-a-dashboard-to-a-List-object", "29" };

            string path = string.Empty;
            string range = string.Empty;
            string repoName = string.Empty;
            string commitMessage = string.Empty;
            int PRNumber = 0;
            try {
                path = args[0];
                range = args[1].Substring(3);
                commitMessage = args[2];
                repoName = args[3];
                PRNumber = int.Parse(args[4]);
            }
            catch (Exception) {
                Console.WriteLine("Please check if parameters are correct");
                return 1;
            }
            
            Console.WriteLine("Working folder path:" + path);
            Console.WriteLine("Build range:" + range);
            Console.WriteLine("Commit Message:" + commitMessage);
            Console.WriteLine("RepoName: " + repoName);
            Console.WriteLine("Pull Request Number: " + PRNumber);
            ExampleRangeTester tester = new ExampleRangeTester(path, range, commitMessage, repoName, PRNumber);

            if (!tester.TestExample()) {
                Console.WriteLine("Ready: TESTING FAILED");
                return 1;
            }
            Console.WriteLine("Ready: TESTING SUCCEEDED");
            return 0;
        }
    }
}
