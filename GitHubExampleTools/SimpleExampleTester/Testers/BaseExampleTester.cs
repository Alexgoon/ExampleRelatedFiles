using CodeCentral.Helpers;
using CodeCentral.Infrastructure;
using CodeCentral.Services;
using CodeCentral.Tester;
using CodeCentral.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SingleBuildExampleTester {
    public abstract class BaseExampleTester : ExampleTool {
        protected string baseWorkingFolder;
        protected readonly string relativeTempOriginalVB = "tempOriginalVB";
        protected readonly string relativeTempTestingVB = "tempTestingVB";
        protected readonly string relativeTempTestingCS = "tempTestingCS";
        protected string rootTempDirectoryPath;
        //ExampleMetadata metadata;
        ExampleInfoControllerBase exampleInfoController;
        protected BaseDefaultExampleTesterConfiguration BaseTesterConfig {
            get { return (BaseDefaultExampleTesterConfiguration)this.BaseConfiguration; }
        }
        protected string SourceCSPath {
            get {
                string defaultCSPath = Path.Combine(baseWorkingFolder, "CS");
                if (Directory.Exists(defaultCSPath))
                    return defaultCSPath;
                return baseWorkingFolder;
            }
        }
        protected string SourceVBPath {
            get { return Path.Combine(baseWorkingFolder, "VB"); }
        }
        protected ExampleInfoControllerBase ExampleInfoController {
            get {
                if (exampleInfoController == null) {
                    exampleInfoController = CreateExampleInfoController();
                }
                return exampleInfoController;
            }
        }
        public BaseExampleTester(string workingFolder)
            : this(workingFolder, new BaseDefaultExampleTesterConfiguration()) {
        }
        public BaseExampleTester(string workingFolder, BaseDefaultExampleTesterConfiguration config)
            : base(config, new DefaultDataRepositoryService(), new DefaultFileSystemService()) {
            baseWorkingFolder = workingFolder;
            InitTempDir();
        }
        public abstract bool ProcessExample();
        protected virtual void PrepareForCSTesting() {
            PrepareForTesting(SourceCSPath, relativeTempTestingCS);
        }
        protected virtual void PrepareForVBTesting() {
            PrepareForTesting(Path.Combine(baseWorkingFolder, "VB"), relativeTempTestingVB);
        }
        void PrepareForTesting(string sourceProjectPath, string relativeTemp) {
            string absoluteTemp = Path.Combine(rootTempDirectoryPath, relativeTemp);
            Directory.CreateDirectory(absoluteTemp);
            FileSystemHelperEx.CopyFolderContent(sourceProjectPath, absoluteTemp);
            BaseTesterConfig.WorkingSolutionDirectoryPath = absoluteTemp;
        }
        void InitTempDir() {
            rootTempDirectoryPath = FileSystemHelperEx.CreateTempFolder();
        }
        protected void TestFileSet(string build) {
            string toolsVersion = "14.0";
            PrepareEnvironment(build);
            PrepareFileSet(build);
            Logger.Info(build + ": ");
            BuildFileSet(toolsVersion);
        }
        protected virtual void PrepareEnvironment(string build) {
            string concreteBuildRemoteDXDependenciesDirectoryPath = GetConcreteBuildRemoteDXDependenciesDirectoryPath(build);
            string concreteBuildLocalDXDependenciesDirectoryPath = GetConcreteBuildLocalDXDependenciesDirectoryPath(build);
            if (!Directory.Exists(concreteBuildRemoteDXDependenciesDirectoryPath)) {
                throw new OperationFailedException(string.Format("[PREPARATION ERROR]{0}\tRemote DX assembly (or SDK) repository directory '{1}' was not found.", Environment.NewLine, concreteBuildRemoteDXDependenciesDirectoryPath), OperationResultVisibility.Internal);
            }
            if (!Directory.Exists(concreteBuildLocalDXDependenciesDirectoryPath)) {
                CopyAssembliesToLocalFolder(concreteBuildRemoteDXDependenciesDirectoryPath, concreteBuildLocalDXDependenciesDirectoryPath);
            }
            if (ExampleInfoController.IsWindows10AppsPlatformExample) {
                RegisterDXWinSdk(concreteBuildLocalDXDependenciesDirectoryPath);
            }
        }
        protected virtual void CopyAssembliesToLocalFolder(string concreteBuildRemoteDXDependenciesDirectoryPath, string concreteBuildLocalDXDependenciesDirectoryPath) {
            SearchOption searchOptions;
            if (ExampleInfoController.IsWindows10AppsPlatformExample)
                searchOptions = SearchOption.AllDirectories;
            else
                searchOptions = SearchOption.TopDirectoryOnly;
            FileSystemService.CopyDirectory(concreteBuildRemoteDXDependenciesDirectoryPath, concreteBuildLocalDXDependenciesDirectoryPath, searchOptions, new string[] { @"\w+.dll", @"\w+.exe" });
        }
        protected string GetConcreteBuildLocalDXDependenciesDirectoryPath(string build) {
            string localDirectoryPath = GetPlatformSpecificLocalDXDependenciesDirectoryPath();
            return GetConcreteBuildDirectoryPath(localDirectoryPath, build);
        }
        public virtual string GetPlatformSpecificLocalDXDependenciesDirectoryPath() {
            if (ExampleInfoController.IsWindows10AppsPlatformExample)
                return BaseConfiguration.LocalDXUWPSDKsDirectoryPath;
            return BaseConfiguration.LocalDXAssemblyDirectoryPath;
        }
        string GetConcreteBuildDirectoryPath(string parentDirectoryPath, string build) {
            return Path.Combine(parentDirectoryPath, build);
        }
        protected virtual string GetConcreteBuildRemoteDXDependenciesDirectoryPath(string build) {
            string remoteDirectoryPath = GetPlatformSpecificRemoteDXDependenciesDirectoryPath();
            return GetConcreteBuildDirectoryPath(remoteDirectoryPath, build);
        }
        protected string GetPlatformSpecificRemoteDXDependenciesDirectoryPath() {
            if (ExampleInfoController.IsWindows10AppsPlatformExample)
                return BaseConfiguration.RemoteDXUWPSDKsDirectoryPath;
            return BaseConfiguration.RemoteDXAssemblyDirectoryPath;
        }
        protected virtual void PrepareFileSet(string build) {
            string concreteBuildLocalDXDependenciesDirectoryPath = GetConcreteBuildLocalDXDependenciesDirectoryPath(build);
            ConvertToAnotherBuild(concreteBuildLocalDXDependenciesDirectoryPath);
            UpdateReferencedAssemblies(concreteBuildLocalDXDependenciesDirectoryPath);
        }
        protected static void RegisterDXWinSdk(string dxWinSdkDirectoryPath) {
            var batchFileRunner = new ToolRunner();
            foreach (var registrationBatchFilePath in FileSystemHelper.FindBatchFiles(dxWinSdkDirectoryPath)) {
                batchFileRunner.SetContext(registrationBatchFilePath, string.Empty, (int)TimeSpan.FromMinutes(5).TotalSeconds);
                ExecuteExternalTool(batchFileRunner, null, "[DX WIN SDK REGISTRATION]");
            }
        }
        protected virtual ExampleInfoControllerBase CreateExampleInfoController() {
            return new ExampleInfoControllerBase(baseWorkingFolder, BaseTesterConfig.MetadataFileName);
        }
    }

    public class BaseDefaultExampleTesterConfiguration : DefaultExampleTesterConfiguration, IExampleToolConfiguration {
        public new string WorkingSolutionDirectoryPath { get; set; }
        public string MetadataFileName {
            get { return System.Configuration.ConfigurationManager.AppSettings["MetadataFileName"]; }
        }
    }

    public class ExampleMetadata {
        public string Platform {
            get;
            set;
        }
    }

    public class ExampleBuild : IComparable<ExampleBuild> {
        public string StringValue {
            get { return ToString(); }
        }
        public int FirstDigit {
            get;
            protected set;
        }
        public int SecondDigit {
            get;
            protected set;
        }
        public int ThirdDigit {
            get;
            protected set;
        }
        public ExampleBuild(int firstDigit, int secondDigit, int thirdDigit) {
            FirstDigit = firstDigit;
            SecondDigit = secondDigit;
            ThirdDigit = thirdDigit;
        }
        public ExampleBuild(string stringValue) {
            int startSearchIndex = 0;
            int dotIndex = -1;
            FirstDigit = GetDigit(stringValue, ref startSearchIndex, ref dotIndex);
            SecondDigit = GetDigit(stringValue, ref startSearchIndex, ref dotIndex);
            ThirdDigit = GetDigit(stringValue, ref startSearchIndex, ref dotIndex);
        }
        int GetDigit(string build, ref int startSearchIndex, ref int dotIndex) {
            startSearchIndex = dotIndex + 1;
            dotIndex = build.IndexOf('.', startSearchIndex);
            int digitLength = dotIndex - startSearchIndex;
            digitLength = digitLength < 0 ? build.Length - startSearchIndex : digitLength;
            return int.Parse(build.Substring(startSearchIndex, digitLength));
        }
        public override string ToString() {
            return string.Format("{0}.{1}.{2}", FirstDigit, SecondDigit, ThirdDigit);
        }
        public int CompareTo(ExampleBuild other) {
            return Comparer<int>.Default.Compare(this.FirstDigit * 10000 + this.SecondDigit * 100 + this.ThirdDigit, other.FirstDigit * 10000 + other.SecondDigit * 100 + other.ThirdDigit);
        }
        public static ExampleBuild MaxValue {
            get { return new ExampleBuild(int.MaxValue, int.MaxValue, int.MaxValue); }
        }
    }
}
