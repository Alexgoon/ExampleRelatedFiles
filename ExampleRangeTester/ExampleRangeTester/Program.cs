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

            //args = new string[] { @"D:\progs\Jenkins\jobs\E5171-How-to-bind-a-dashboard-to-a-List-object\workspace", "CS_14.1.9-15.1.1" };
            Console.WriteLine("arg0: " + args[0]);
            Console.WriteLine("arg1: " + args[1]);
            Console.WriteLine("arg2: " + args[2]);

            string path = string.Empty;
            string range = string.Empty;
            string language = string.Empty;
            string commitMessage = string.Empty;
            try {
                path = args[0];
                language = args[0].Substring(0, 2);
                range = args[1].Substring(3);
                commitMessage = args[2];
            }
            catch (Exception) {
                Console.WriteLine("Please check if parameters are correct");
                return 1;
            }

            Console.WriteLine("path: " + path);
            Console.WriteLine("range: " + range);

            ExampleRangeTester tester = new ExampleRangeTester(path, range, commitMessage, language == "CS");

            if (!tester.TestExample()) {
                Console.WriteLine("Ready: TESTING FAILED");
                return 1;
            }
            Console.WriteLine("Ready: TESTING SUCCEEDED");
            return 0;
        }
    }
}
