using CodeCentral.Helpers;
using SingleBuildExampleTester;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExampleRangeTester.Helpers {
    public static class TrashCleaner {
        public static void Clean(string path) {
            List<Regex> defaultElementsToIgnore = ConfigHelper.SolutionItemsToIgnore;
            RemoveFileSystemItems(path, defaultElementsToIgnore, Directory.GetDirectories(path, "*", SearchOption.AllDirectories), dir => FileSystemHelper.SafeDeleteDirectory(dir));
            RemoveFileSystemItems(path, defaultElementsToIgnore, Directory.GetFiles(path, "*", SearchOption.AllDirectories), file => FileSystemHelperEx.SaveDeleteFile(file));
        }
        static void RemoveFileSystemItems(string path, List<Regex> removePatterns, string[] fsItems, Action<string> removeAction) {
            foreach (string item in fsItems) {
                foreach (Regex pattern in removePatterns)
                    if (pattern.IsMatch(item.ToLower())) {
                        removeAction(item);
                    }
            }
        }
    }
}
