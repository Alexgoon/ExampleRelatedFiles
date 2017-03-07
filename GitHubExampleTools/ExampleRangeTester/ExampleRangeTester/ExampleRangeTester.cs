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
        protected string PRHeader = string.Empty;
        protected string baseWorkingFolder;
        protected string TestedRepoOwner;
        protected string TestedRepoName;
        protected string PullRequestCreatorUser;
        protected string PullRequestCreatorGit;
        protected string GitHubBaseAddress = "https://github.com";
        protected int PullRequestNumber;
        protected GitHelper GitHelper;
        protected GitHubHelper GitHubHelper;
        readonly string relativeTempOriginalVB = "tempOriginalVB";
        readonly string relativeTempTestingVB = "tempTestingVB";
        readonly string relativeTempTestingCS = "tempOriginalCS";
        string rootTempDirectoryPath;
        string HumanVBTag = "[Human VB]";
        string HumanVBTagStringFormat = "**<sub>{0}</sub>**";
        CredentialsHandler gitCredentialsHandler;
        protected string FormattedHumanVBTag {
            get { return string.Format(HumanVBTagStringFormat, HumanVBTag); }
        }
        protected DefaultExampleTesterConfigurationEx TesterConfig { get { return (DefaultExampleTesterConfigurationEx)BaseConfiguration; } }
        protected CredentialsHandler GitCredentialsHandler {
            get {
                if (gitCredentialsHandler == null)
                    gitCredentialsHandler = CreateGitCredentialsHandler();
                return gitCredentialsHandler;
            }
        }
        protected bool AutoGenerateVB {
            get { return !IsHumanVBTagInPRHeader && !IsHumanVBTagInReadme; }
        }
        bool IsHumanVBTagInPRHeader {
            get { return PRHeader.Contains(HumanVBTag); }
        }
        bool IsHumanVBTagInReadme {
            get {
                string readmeText = System.IO.File.ReadAllText(Path.Combine(baseWorkingFolder, "README.md"));
                return readmeText.Contains(HumanVBTag);
            }
        }
        protected bool ShouldTestVB {
            get { return AutoGenerateVB || Directory.Exists(Path.Combine(baseWorkingFolder, "VB")); }
        }
        public ExampleRangeTester(string workingFolder, string builds, string prHeader, string repoFullName, int pullRequestNumber, string pullRequestCreator, string pullRequestCreatorGit)
            : base(new DefaultExampleTesterConfigurationEx() { }, new DefaultDataRepositoryService(), new DefaultFileSystemService()) {
            baseWorkingFolder = workingFolder;
            Builds = builds;
            verifiedBuilds = SampleBuild.ParseBuilds(builds, GetPlatformSpecificRemoteDXDependenciesDirectoryPath());
            PRHeader = prHeader;
            string[] repoNameParts = repoFullName.Split('/');
            TestedRepoOwner = repoNameParts[0];
            TestedRepoName = repoNameParts[1];
            PullRequestNumber = pullRequestNumber;
            PullRequestCreatorUser = pullRequestCreator;
            PullRequestCreatorGit = pullRequestCreatorGit;
            GitHelper = new GitHelper(baseWorkingFolder);
            GitHubHelper = new GitHubHelper(TesterConfig.GitHubToken);
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
            Logger.Info("CS testing");
            PrepareCSForTesting();
            if (!TestExampleBuilds(Path.Combine(baseWorkingFolder, "CS"))) {
                return false;
            }
            if (!ShouldTestVB) {
                GitHubHelper.MergePullRequest(TestedRepoOwner, TestedRepoName, PullRequestNumber);
                return true;
            }
            Logger.Info("VB testing");
            PrepareVBForTesting();
            if (!TestExampleBuilds(rootTempDirectoryPath)) {
                if (AutoGenerateVB) {
                    HandleIncorrectlyGeneratedVB();
                }
                return false;
            }
            UpdateResultExampleRepo();
            return true;
        }
        protected void PrepareCSForTesting() {
            string sourceCSProjectPath = Path.Combine(baseWorkingFolder, "CS");
            string absoluteTempTestingCS = Path.Combine(rootTempDirectoryPath, relativeTempTestingCS);
            Directory.CreateDirectory(absoluteTempTestingCS);
            FileSystemHelperEx.CopyFolderContent(sourceCSProjectPath, absoluteTempTestingCS);
            TesterConfig.WorkingSolutionDirectoryPath = absoluteTempTestingCS;
        }
        protected void PrepareVBForTesting() {
            string absoluteSourceCS = Path.Combine(baseWorkingFolder, "CS");
            string absoluteTempOriginalVB = Path.Combine(rootTempDirectoryPath, relativeTempOriginalVB);
            string absoluteTempTestingVB = Path.Combine(rootTempDirectoryPath, relativeTempTestingVB);
            Directory.CreateDirectory(absoluteTempOriginalVB);
            Directory.CreateDirectory(absoluteTempTestingVB);
            if (AutoGenerateVB) {
                GenerateVB(absoluteSourceCS, absoluteTempOriginalVB);
                FileSystemHelperEx.CopyFolderContent(absoluteTempOriginalVB, absoluteTempTestingVB);
            }
            else {
                string absoluteExistingVB = Path.Combine(baseWorkingFolder, "VB");
                FileSystemHelperEx.CopyFolderContent(absoluteExistingVB, absoluteTempOriginalVB);
                FileSystemHelperEx.CopyFolderContent(absoluteExistingVB, absoluteTempTestingVB);
            }
            TesterConfig.WorkingSolutionDirectoryPath = absoluteTempTestingVB;
        }
        void UpdateResultExampleRepo() {
            GitHubHelper.MergePullRequest(TestedRepoOwner, TestedRepoName, PullRequestNumber);
            GitHelper.CheckoutBranch(Builds);
            GitHelper.SetBranchUpstream(Builds, GitHelper.GetRemote("origin"));
            GitHelper.Pull(Builds, GitCredentialsHandler);
            MoveVBToGitRootFolder();
            UpdateHumanVBTag();
            AcceptLocalModifications(Builds);
        }
        void HandleIncorrectlyGeneratedVB() {
            GitHubHelper.CreateFork(PullRequestCreatorUser, TesterConfig.GitUserName, TestedRepoName);
            string botForkPushBranchName = PrepareBotForkBranch();
            MoveVBToGitRootFolder();
            InsertFormattedHumanVBTag();
            AcceptLocalModifications(botForkPushBranchName, " (generated VB)");
            Octokit.PullRequest botForkPullRequest = GitHubHelper.CreatePullRequest(TestedRepoName, PRHeader, TesterConfig.GitUserName, PullRequestCreatorUser, Builds);
            GitHubHelper.AddPullRequestComment(TestedRepoOwner, TestedRepoName, PullRequestNumber, string.Format("VB hasn't been properly generated. Merge the following Pull Request and correct VB: {0}/{1}/{2}/pull/{3}", GitHubBaseAddress, PullRequestCreatorUser, TestedRepoName, botForkPullRequest.Number));
            GitHubHelper.ClosePullRequest(TestedRepoOwner, TestedRepoName, PullRequestNumber);
        }
        void UpdateHumanVBTag() {
            if (!IsHumanVBTagInReadme && IsHumanVBTagInPRHeader)
                InsertFormattedHumanVBTag();
            if (IsHumanVBTagInPRHeader)
                EnsureHumanVBTagIsFormatted();
        }
        void MoveVBToGitRootFolder() {
            FileSystemHelper.SafeClearDirectory(Path.Combine(baseWorkingFolder, "VB"));
            FileSystemHelperEx.CopyFolderContent(Path.Combine(rootTempDirectoryPath, relativeTempOriginalVB), Path.Combine(baseWorkingFolder, "VB"));
        }
        void InsertFormattedHumanVBTag() {
            string readePath = Path.Combine(baseWorkingFolder, "README.md");
            string readmeText = File.ReadAllText(readePath);
            readmeText = readmeText + FormattedHumanVBTag;
            File.WriteAllText(readePath, readmeText);
        }
        void EnsureHumanVBTagIsFormatted() {
            string readePath = Path.Combine(baseWorkingFolder, "README.md");
            string readmeText = System.IO.File.ReadAllText(readePath);
            if (!readmeText.Contains(FormattedHumanVBTag)) {
                readmeText.Replace(HumanVBTag, string.Empty);
                readmeText = readmeText + FormattedHumanVBTag;
                File.WriteAllText(readePath, readmeText);
            }
        }
        void AcceptLocalModifications(string brancToPush, string commitSuffix = null) {
            GitHelper.CommitChanges(PRHeader + commitSuffix);
            GitHelper.PushToRemote(brancToPush, GitCredentialsHandler);
        }
        string PrepareBotForkBranch() {
            string botForkPushBranchName = Builds + "_forPushingToBotFork";
            GitHelper.CheckoutNewBranch(botForkPushBranchName);
            string botForkUrl = string.Format("{0}/{1}/{2}.git", GitHubBaseAddress, TesterConfig.GitUserName, TestedRepoName);
            GitHelper.SetBranchUpstream(botForkPushBranchName, GitHelper.CreateRemote("createdFork", botForkUrl), Builds);
            return botForkPushBranchName;
        }
        //protected void RevertGitToInitialState() {
        //    GitHelper.ForcedRevertBranchState("refs/remotes/origin/" + Builds);
        //    GitHelper.CheckoutBranch(Builds);
        //}
        protected bool TestExampleBuilds(string projectFolder) {
            foreach (SampleBuild build in verifiedBuilds)
                try {
                    TestFileSet(build.StringValue);
                }
                catch (OperationFailedException ex) {
                    Logger.Error("Testing result: {1}{0}{2}{0}{3}.", Environment.NewLine, ex.GetType().Name, ex.Message, ex.StackTrace);
                    GitHubHelper.AddPullRequestComment(TestedRepoOwner, TestedRepoName, PullRequestNumber, ex.Message);
                    GitHubHelper.ClosePullRequest(TestedRepoOwner, TestedRepoName, PullRequestNumber);
                    return false;
                }
            return true;
        }
        protected void GenerateVB(string source, string destination) {
            Logger.Info(string.Format("Generating VB. Source ={0}. Destination={1}", source, destination));
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
