using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleRangeTester {
    public static class FileSystemHelperEx {
        public static string CreateTempFolder(string folderName) {
            return Directory.CreateDirectory(Path.Combine(@"D:\", folderName + Guid.NewGuid().ToString())).FullName;
        }

        public static void CopyFolderContent(string source, string dest) {
            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(source, dest));
            foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(source, dest), true);
        }

        public static string CopyWorkingFolderIntoTemp(string copyFrom, string tempFolderBaseName) {
            string csTempDirectoryPath = FileSystemHelperEx.CreateTempFolder(tempFolderBaseName);
            FileSystemHelperEx.CopyFolderContent(copyFrom, csTempDirectoryPath);
            return csTempDirectoryPath;
        }
    }
}
