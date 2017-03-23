using CodeCentral.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleBuildExampleTester {
    public static class FileSystemHelperEx {
        public static string CreateTempFolder() {
            return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        }
        public static void CopyFolderContent(string source, string dest, string[] elementsToExclude = null) {
            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories)) {
                if (elementsToExclude != null && elementsToExclude.FirstOrDefault(el => dirPath.Contains(el)) != null)
                    continue;
                Directory.CreateDirectory(dirPath.Replace(source, dest));
            }
            foreach (string filePath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories)) {
                if (elementsToExclude != null && elementsToExclude.FirstOrDefault(el => filePath.Contains(el)) != null)
                    continue;
                File.Copy(filePath, filePath.Replace(source, dest), true);
            }
        }
        public static void SaveDeleteFile(string filePath) {
            if (!File.Exists(filePath))
                return;
            FileInfo fi = new FileInfo(filePath);
            fi.SafeDelete();
        }
        public static bool SafeClearDirectory(string directoryPath, string[] elementsToExclude = null) {
            if (!Directory.Exists(directoryPath)) {
                return false;
            }
            var cleaningDirectoryInfo = new DirectoryInfo(directoryPath);
            foreach (FileSystemInfo fsInfo in cleaningDirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories)) {
                if (!fsInfo.Exists || (elementsToExclude != null && elementsToExclude.FirstOrDefault(el => fsInfo.FullName.Contains(el)) != null)) {
                    continue;
                }
                var directoryInfo = fsInfo as DirectoryInfo;
                if (directoryInfo != null) {
                    foreach (var innerFile in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)) {
                        innerFile.SafeDelete();
                    }
                    SupportCenter.Common.Executor.ExecuteWithRetryingInCaseOf<SystemException>(() => directoryInfo.Delete(recursive: true), retryCount: 3);
                }
                else {
                    fsInfo.SafeDelete();
                }
            }
            return true;
        }
        public static bool SafeMoveFiles(string directorySourcePath, string directoryDestinationPath, string[] elementsToExclude = null) {
            if (!Directory.Exists(directorySourcePath)) {
                return false;
            }
            var cleaningDirectoryInfo = new DirectoryInfo(directorySourcePath);
            foreach (FileSystemInfo fsInfo in cleaningDirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly)) {
                if (!fsInfo.Exists || (elementsToExclude != null && elementsToExclude.Contains(fsInfo.Name, StringComparer.OrdinalIgnoreCase))) {
                    continue;
                }
                fsInfo.Attributes = FileAttributes.Normal;
                var directoryInfo = fsInfo as DirectoryInfo;
                var fileInfo = fsInfo as FileInfo;
                if (directoryInfo != null) {
                    SupportCenter.Common.Executor.ExecuteWithRetryingInCaseOf<SystemException>(() => directoryInfo.MoveTo(System.IO.Path.Combine(directoryDestinationPath, directoryInfo.Name)), retryCount: 3);
                }
                else if (fileInfo != null) {
                    SupportCenter.Common.Executor.ExecuteWithRetryingInCaseOf<SystemException>(() => fileInfo.MoveTo(System.IO.Path.Combine(directoryDestinationPath, fileInfo.Name)), retryCount: 5);
                }
            }
            return true;
        }
    }
}
