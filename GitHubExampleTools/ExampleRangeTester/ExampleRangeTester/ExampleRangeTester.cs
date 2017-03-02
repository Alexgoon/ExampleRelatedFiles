using CodeCentral.Helpers;
using CodeCentral.Infrastructure;
using CodeCentral.Services;
using CodeCentral.Tester;
using CodeCentral.Tools;
using GitExampleHelper;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using NLog.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleRangeTester {
    public class ExampleRangeTester : ExampleTool {
        protected List<SampleBuild> verifiedBuilds;
        protected string Builds;
        protected string CommitMessage = string.Empty;
        protected string baseWorkingFolder;
        protected string RepositoryFullName;
        protected int PullRequestNumber;
        string rootTempDirectoryPath;
        readonly string relativeTempOriginalVB = "tempOriginalVB";
        readonly string relativeTempTestingVB = "tempTestingVB";
        readonly string relativeTempOriginalCS = "tempOriginalCS";

        CredentialsHandler gitCredentialsHandler;

        protected DefaultExampleTesterConfigurationEx TesterConfig { get { return (DefaultExampleTesterConfigurationEx)BaseConfiguration; } }

        protected CredentialsHandler GitCredentialsHandler {
            get {
                if (gitCredentialsHandler == null)
                    gitCredentialsHandler = CreateGitCredentialsHandler();
                return gitCredentialsHandler;
            }
        }

        public ExampleRangeTester(string workingFolder, string builds, string commitMessage, string repoFullName, int PRNumber)
            : base(new DefaultExampleTesterConfigurationEx() { }, new DefaultDataRepositoryService(), new DefaultFileSystemService()) {

            baseWorkingFolder = workingFolder;
            Builds = builds;
            CommitMessage = commitMessage;
            RepositoryFullName = repoFullName;
            PullRequestNumber = PRNumber;
            verifiedBuilds = SampleBuild.ParseBuilds(builds, GetPlatformSpecificRemoteDXDependenciesDirectoryPath());
        }

        protected virtual CredentialsHandler CreateGitCredentialsHandler() {

            return new CredentialsHandler((url, usernameFromUrl, types) => new UsernamePasswordCredentials() { Username = TesterConfig.GitUserName, Password = TesterConfig.GitUserPassword, });
        }

        public bool ProcessExample() {
            rootTempDirectoryPath = FileSystemHelperEx.CreateTempFolder();
            bool result = false;
            try {
                result = ProcessExampleCore();
            }
            finally {
                FileSystemHelper.SafeDeleteDirectory(rootTempDirectoryPath);
            }
            return result;
        }

        protected bool ProcessExampleCore() {
            //CS testing

            PrepareCSForTesting();
            if (!TestExampleBuilds(Path.Combine(baseWorkingFolder, "CS"))) {
                return false;
            }

            //--VB part--
            //RevertGitToInitialState();

            PrepareVBForTesting();
            if (!TestExampleBuilds(rootTempDirectoryPath)) {
                //TODO: create fork, upload VB
                //TODO: create Pull Request 
                //TODO: comment about broken VB and add a link to PR
                return false;
            }

            //merge
            GitHubHelper.MergePullRequest(TesterConfig.GitHubToken, RepositoryFullName, PullRequestNumber); // Ex.R0 -> Ex.R1; localGit = R0'

            //solution: Pull: R0'->R1; copy V: R1->R1'; commit: R1'-> R2; push: R2->Ex.R2


            //update main example Repo
            GitHelper.CommitChanges(baseWorkingFolder, CommitMessage + "(VB)");
            GitHelper.PushToRemote(baseWorkingFolder, Builds, GitCredentialsHandler);

            return true;
        }

        protected void RevertGitToInitialState() {
            GitHelper.ForcedRevertBranchState(baseWorkingFolder, "refs/remotes/origin/" + Builds);
            GitHelper.CheckoutBranch(baseWorkingFolder, Builds);
        }

        protected bool TestExampleBuilds(string projectFolder) {
            ((DefaultExampleTesterConfigurationEx)BaseConfiguration).WorkingSolutionDirectoryPath = projectFolder;
            foreach (SampleBuild build in verifiedBuilds)
                try {
                    TestFileSet(build.StringValue);
                }
                catch (OperationFailedException ex) {
                    Console.WriteLine("Testing result: {1}{0}{2}{0}{3}.", Environment.NewLine, ex.GetType().Name, ex.Message, ex.StackTrace);
                    GitHubHelper.AddPullRequestComment(TesterConfig.GitHubToken, RepositoryFullName, PullRequestNumber, ex.Message);
                    return false;
                }
            return true;
        }

        protected void PrepareVBForTesting() {
            string sourceCSProjectPath = Path.Combine(baseWorkingFolder, "CS");
            string absoluteTempTestingVB = Path.Combine(rootTempDirectoryPath, relativeTempTestingVB);


            if (ShouldGenerateVB()) {
                GenerateVB(sourceCSProjectPath, rootTempDirectoryPath);
                FileSystemHelperEx.CopyFolderContent(rootTempDirectoryPath, absoluteTempTestingVB);
            }
            else {
                //if vb exists
                //FileSystemHelperEx.CopyFolderContent(vbResultFolder, rootTempDirectoryPath);
            }


            string absoluteTempOriginalVB = Path.Combine(rootTempDirectoryPath, relativeTempOriginalVB);
            

            //string vbResultFolder = Path.Combine(baseWorkingFolder, "VB");

            //FileSystemHelper.SafeClearDirectory(vbResultFolder, "*");
            //if (ShouldGenerateVB()) {
            //    GenerateVB(sourceCSProjectPath, rootTempDirectoryPath);
            //    FileSystemHelperEx.CopyFolderContent(rootTempDirectoryPath, vbResultFolder);
            //}
            //else
            //    FileSystemHelperEx.CopyFolderContent(vbResultFolder, rootTempDirectoryPath);
        }

        protected void PrepareCSForTesting() {
            string sourceCSProjectPath = Path.Combine(baseWorkingFolder, "CS");
            string absoluteTempOriginalCS = Path.Combine(rootTempDirectoryPath, relativeTempOriginalCS);
            Directory.CreateDirectory(absoluteTempOriginalCS);
            FileSystemHelperEx.CopyFolderContent(sourceCSProjectPath, absoluteTempOriginalCS);
        }

        protected bool ShouldGenerateVB() {
            if (!Directory.Exists(Path.Combine(baseWorkingFolder, "VB")))
                return true;
            Patch lastPatch = GitHelper.GetDifferencesWithLastCommit(baseWorkingFolder);
            foreach (var ptc in lastPatch) {
                if (ptc.Path.StartsWith(@"VB\"))
                    return false;
            }
            return true;
        }

        protected void GenerateVB(string source, string destination) {
            var instantVB = new InstantVB(destination, toolPath: "Instant VB.exe", toolSettingsFilePath: "Instant VB.dat");
            ExecuteExternalTool(instantVB, source, "[VB CONVERSION ERROR]");
        }

        protected void TestFileSet(string build) {
            string toolsVersion = "12.0";
            PrepareEnvironment(build);
            //FileSystemHelper.ClearUnpackArchive(Configuration.WorkingSolutionDirectoryPath, fileSetArchive);
            PrepareFileSet(build);
            Logger.Info(build + ": ");
            BuildFileSet(toolsVersion);
        }

        protected void PrepareEnvironment(string build) {
            string concreteBuildRemoteDXDependenciesDirectoryPath = GetConcreteBuildRemoteDXDependenciesDirectoryPath(build);
            string concreteBuildLocalDXDependenciesDirectoryPath = GetConcreteBuildLocalDXDependenciesDirectoryPath(build);

            if (!Directory.Exists(concreteBuildRemoteDXDependenciesDirectoryPath)) {
                throw new OperationFailedException(string.Format("[PREPARATION ERROR]{0}\tRemote DX assembly (or SDK) repository directory '{1}' was not found.", Environment.NewLine, concreteBuildRemoteDXDependenciesDirectoryPath), OperationResultVisibility.Internal);
            }

            if (!Directory.Exists(concreteBuildLocalDXDependenciesDirectoryPath)) {
                FileSystemService.CopyDirectory(concreteBuildRemoteDXDependenciesDirectoryPath, concreteBuildLocalDXDependenciesDirectoryPath);
            }
        }

        protected string GetConcreteBuildLocalDXDependenciesDirectoryPath(string build) {
            string localDirectoryPath = GetPlatformSpecificLocalDXDependenciesDirectoryPath();
            return GetConcreteBuildDirectoryPath(localDirectoryPath, build);
        }

        public string GetPlatformSpecificLocalDXDependenciesDirectoryPath() {
            return BaseConfiguration.LocalDXAssemblyDirectoryPath;
        }

        string GetConcreteBuildDirectoryPath(string parentDirectoryPath, string build) {
            return Path.Combine(parentDirectoryPath, build);
        }

        protected string GetConcreteBuildRemoteDXDependenciesDirectoryPath(string build) {
            string remoteDirectoryPath = GetPlatformSpecificRemoteDXDependenciesDirectoryPath();
            return GetConcreteBuildDirectoryPath(remoteDirectoryPath, build);
        }

        string GetPlatformSpecificRemoteDXDependenciesDirectoryPath() {
            return BaseConfiguration.RemoteDXAssemblyDirectoryPath;
        }

        protected virtual void PrepareFileSet(string build) {
            string concreteBuildLocalDXDependenciesDirectoryPath = GetConcreteBuildLocalDXDependenciesDirectoryPath(build);
            ConvertToAnotherBuild(concreteBuildLocalDXDependenciesDirectoryPath);
            UpdateReferencedAssemblies(concreteBuildLocalDXDependenciesDirectoryPath);
        }
    }

    public class SampleBuild : IComparable<SampleBuild> {

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

        public SampleBuild(int firstDigit, int secondDigit, int thirdDigit) {
            FirstDigit = firstDigit;
            SecondDigit = secondDigit;
            ThirdDigit = thirdDigit;
        }

        public SampleBuild(string stringValue) {
            int startSearchIndex = 0;
            int dotIndex = -1;

            FirstDigit = GetDigit(stringValue, ref startSearchIndex, ref dotIndex);
            SecondDigit = GetDigit(stringValue, ref startSearchIndex, ref dotIndex);
            ThirdDigit = GetDigit(stringValue, ref startSearchIndex, ref dotIndex);
        }


        static int GetDigit(string build, ref int startSearchIndex, ref int dotIndex) {
            startSearchIndex = dotIndex + 1;
            dotIndex = build.IndexOf('.', startSearchIndex);
            int digitLength = dotIndex - startSearchIndex;
            digitLength = digitLength < 0 ? build.Length - startSearchIndex : digitLength;
            return int.Parse(build.Substring(startSearchIndex, digitLength));
        }

        public override string ToString() {
            return string.Format("{0}.{1}.{2}", FirstDigit, SecondDigit, ThirdDigit);
        }

        public int CompareTo(SampleBuild other) {
            return Comparer<int>.Default.Compare(this.FirstDigit * 10000 + this.SecondDigit * 100 + this.ThirdDigit, other.FirstDigit * 10000 + other.SecondDigit * 100 + other.ThirdDigit);
        }

        public static List<SampleBuild> ParseBuilds(string builds, string buildsFolder) {
            List<SampleBuild> parsedBuilds = new List<SampleBuild>();
            SampleBuild startBuild;
            SampleBuild endBuild;

            int dashIndex = builds.IndexOf('-');
            if (dashIndex != -1) {
                startBuild = new SampleBuild(builds.Substring(0, dashIndex));
                endBuild = new SampleBuild(builds.Substring(dashIndex + 1, builds.Length - (dashIndex + 1)));
            }
            else {
                startBuild = new SampleBuild(builds.Substring(0, builds.IndexOf('+')));
                endBuild = new SampleBuild(999, 999, 999);
            }

            foreach (string dir in Directory.EnumerateDirectories(buildsFolder)) {
                string dirName = Path.GetFileName(dir);
                SampleBuild folderBuild = null;
                try {
                    folderBuild = new SampleBuild(dirName);
                }
                catch (Exception e) {
                    continue;
                }
                if (startBuild.CompareTo(folderBuild) != 1 && endBuild.CompareTo(folderBuild) != -1) {
                    parsedBuilds.Add(folderBuild);
                }
            }

            return parsedBuilds.OrderBy(b => b).ToList<SampleBuild>();
        }
    }

    public class DefaultExampleTesterConfigurationEx : DefaultExampleTesterConfiguration, IExampleToolConfiguration {
        public new string WorkingSolutionDirectoryPath { get; set; }
        public string GitHubToken {
            get { return System.Configuration.ConfigurationManager.AppSettings["GitHubToken"]; }
        }
        public string GitUserName {
            get { return System.Configuration.ConfigurationManager.AppSettings["GitBotUserName"]; }
        }
        public string GitUserPassword {
            get { return System.Configuration.ConfigurationManager.AppSettings["GitBotPassword"]; }
        }
    }
}
