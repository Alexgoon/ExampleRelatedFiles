using CodeCentral.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleBuildExampleTester {
    public class SingleBuildExampleTester : BaseExampleTester {
        string BaseSourceAssemblyPath;
        string Build;
        public SingleBuildExampleTester(string workingFolder, string build, string baseSourceAssemblyPath)
            : base(workingFolder) {
            BaseSourceAssemblyPath = baseSourceAssemblyPath;
            Build = build;
        }
        public override bool ProcessExample() {
            if (ExampleInfoController.IsDevExtremePlatformExample)
                return true;
            string csPath = Path.Combine(baseWorkingFolder, "CS");
            string vbPath = Path.Combine(baseWorkingFolder, "VB");
            bool result = true;
            if (Directory.Exists(csPath)) {
                PrepareForCSTesting();
                result = GetTestFileSetResult(Build);
            }
            if (Directory.Exists(vbPath)) {
                PrepareForVBTesting();
                result = result && GetTestFileSetResult(Build);
            }
            return result;
        }
        bool GetTestFileSetResult(string build) {
            try {
                TestFileSet(build);
            }
            catch (OperationFailedException ex) {
                Logger.Error("Testing result: {1}{0}{2}{0}{3}.", Environment.NewLine, ex.GetType().Name, ex.Message, ex.StackTrace);
                return false;
            }
            return true;
        }
        protected override string GetConcreteBuildRemoteDXDependenciesDirectoryPath(string build) {
            if (ExampleInfoController.IsWindows10AppsPlatformExample)
                return Path.Combine(BaseSourceAssemblyPath, "W");
            return BaseSourceAssemblyPath;
        }
        protected override void CopyAssembliesToLocalFolder(string concreteBuildRemoteDXDependenciesDirectoryPath, string concreteBuildLocalDXDependenciesDirectoryPath) {
            base.CopyAssembliesToLocalFolder(concreteBuildRemoteDXDependenciesDirectoryPath, concreteBuildLocalDXDependenciesDirectoryPath);
            CopyProjectConverter(concreteBuildRemoteDXDependenciesDirectoryPath, concreteBuildLocalDXDependenciesDirectoryPath);
        }
        void CopyProjectConverter(string concreteBuildRemoteDXDependenciesDirectoryPath, string concreteBuildLocalDXDependenciesDirectoryPath) {
            string projectConverterSourcePath = Path.Combine(concreteBuildRemoteDXDependenciesDirectoryPath, @"Tools\ProjectConverter.exe");
            string projectConverterDestinationPath = Path.Combine(concreteBuildLocalDXDependenciesDirectoryPath, @"ProjectConverter.exe");
            File.Copy(projectConverterSourcePath, projectConverterDestinationPath);
        }

        public override string GetPlatformSpecificLocalDXDependenciesDirectoryPath() {
            return Path.Combine(base.GetPlatformSpecificLocalDXDependenciesDirectoryPath(), "SingleBuild");
        }
    }
}
