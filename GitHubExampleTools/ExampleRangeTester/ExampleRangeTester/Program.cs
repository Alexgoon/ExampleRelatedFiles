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

            //args = new string[] { @"C:\Users\russkov.alexander\Desktop\New folder (15)\Project TestExample_15.1.5+", "15.1.5+", "Some commit", "codecentral-examples/TestExample", "36", "Alexgoon", "https://github.com/Alexgoon/TestExample.git" };

            List<System.Text.RegularExpressions.Regex> defaultElementsToIgnore = ConfigHelper.SolutionItemsToIgnore;

            string path = string.Empty;
            string range = string.Empty;
            string prHeader = string.Empty;
            string repoName = string.Empty;
            int PRNumber = 0;
            string pullRequestCreator = string.Empty;
            string pullRequestCreatorGit = string.Empty;
            
            try {
                path = args[0];
                range = args[1];
                prHeader = args[2];
                repoName = args[3];
                PRNumber = int.Parse(args[4]);
                pullRequestCreator = args[5];
                pullRequestCreatorGit = args[6];
            }
            catch (Exception) {
                Console.WriteLine("Please check if parameters are correct");
                return 1;
            }
            ExampleRangeTester tester = new ExampleRangeTester(path, range, prHeader, repoName, PRNumber, pullRequestCreator, pullRequestCreatorGit);

            if (!tester.ProcessExample()) {
                Console.WriteLine("Ready: TESTING FAILED");
                return 1;
            }
            Console.WriteLine("Ready: TESTING SUCCEEDED");
            return 0;
        }
    }
}
