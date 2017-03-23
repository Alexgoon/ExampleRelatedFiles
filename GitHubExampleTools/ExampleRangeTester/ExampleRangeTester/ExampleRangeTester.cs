using CodeCentral.Helpers;
using CodeCentral.Infrastructure;
using CodeCentral.Services;
using CodeCentral.Tester;
using CodeCentral.Tools;
using ExampleRangeTester.Helpers;
using GitExampleHelper;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using NLog.Internal;
using SingleBuildExampleTester;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ExampleRangeTester {
    public class ExampleRangeTester : BaseExampleTester {
        protected List<ExampleBuild> verifiedBuilds;
        protected string BranchName;
        protected string PRHeader = string.Empty;
        protected string TestedRepoOwner;
        protected string TestedRepoName;
        protected string PullRequestCreatorUser;
        protected string PullRequestCreatorGit;
        protected string GitHubBaseAddress = "https://github.com";
        protected int PullRequestNumber;
        protected GitHelper GitHelper;
        protected GitHubHelper GitHubHelper;
        CredentialsHandler gitCredentialsHandler;
        protected RangeExampleTesterConfiguration TesterConfig {
            get { return (RangeExampleTesterConfiguration)BaseConfiguration; }
        }
        protected CredentialsHandler GitCredentialsHandler {
            get {
                if (gitCredentialsHandler == null)
                    gitCredentialsHandler = CreateGitCredentialsHandler();
                return gitCredentialsHandler;
            }
        }
        protected bool AutoGenerateVB {
            get { return !((ExampleInfoController)ExampleInfoController).IsHumanVBTagInPRHeader && !((ExampleInfoController)ExampleInfoController).IsHumanVBTagInReadme; }
        }
        protected bool ShouldTestVB {
            get { return AutoGenerateVB || Directory.Exists(Path.Combine(SourceVBPath)); }
        }
        public ExampleRangeTester(string workingFolder, string branchName, string prHeader, string repoFullName, int pullRequestNumber, string pullRequestCreator, string pullRequestCreatorGit)
            : base(workingFolder, new RangeExampleTesterConfiguration()) {
            BranchName = branchName;
            string[] repoNameParts = repoFullName.Split('/');
            TestedRepoOwner = repoNameParts[0];
            TestedRepoName = repoNameParts[1];
            PRHeader = prHeader;
            PullRequestNumber = pullRequestNumber;
            PullRequestCreatorUser = pullRequestCreator;
            PullRequestCreatorGit = pullRequestCreatorGit;
            GitHelper = new GitHelper(workingFolder, TesterConfig.GitUserName, "fakeEmail");
            GitHubHelper = new GitHubHelper(TesterConfig.GitHubToken);
            verifiedBuilds = BuildsHelper.GetBuilds(branchName, workingFolder, GetPlatformSpecificRemoteDXDependenciesDirectoryPath());
        }
        public override bool ProcessExample() {
            if (ExampleInfoController.IsPlatformEmpty) {
                string invalidPlatformMessage = "Please ensure that the example platform is specified in the repositofy description in the following format: Platform:PlatformName";
                GitHubHelper.AddPullRequestComment(TestedRepoOwner, TestedRepoName, PullRequestNumber, invalidPlatformMessage);
                GitHubHelper.ClosePullRequest(TestedRepoOwner, TestedRepoName, PullRequestNumber);
                Logger.Error(invalidPlatformMessage);
                return false;
            }
            if (ExampleInfoController.IsDevExtremePlatformExample) {
                UpdateResultExampleRepo(false);
                return true;
            }
            bool testResult = false;
            try {
                testResult = ProcessExampleCore();
            }
            finally {
                FileSystemHelper.SafeDeleteDirectory(rootTempDirectoryPath);
            }
            return testResult;
        }
        protected bool ProcessExampleCore() {
            Logger.Info("CS testing");
            PrepareForCSTesting();
            if (!TestExampleBuilds()) {
                return false;
            }
            if (!ShouldTestVB) {
                UpdateResultExampleRepo(false);
                return true;
            }
            Logger.Info("VB testing");
            PrepareForVBTesting();
            if (!TestExampleBuilds()) {
                if (AutoGenerateVB) {
                    HandleIncorrectlyGeneratedVB();
                }
                return false;
            }
            UpdateResultExampleRepo(true);
            return true;
        }
        protected override void PrepareForCSTesting() {
            GitHelper.RemoveUnstagedFiles();
            base.PrepareForCSTesting();
        }
        protected override void PrepareForVBTesting() {
            string absoluteTempOriginalVB = Path.Combine(rootTempDirectoryPath, relativeTempOriginalVB);
            string absoluteTempTestingVB = Path.Combine(rootTempDirectoryPath, relativeTempTestingVB);
            Directory.CreateDirectory(absoluteTempOriginalVB);
            Directory.CreateDirectory(absoluteTempTestingVB);
            if (AutoGenerateVB) {
                GenerateVB(SourceCSPath, absoluteTempOriginalVB);
                FileSystemHelperEx.CopyFolderContent(absoluteTempOriginalVB, absoluteTempTestingVB);
            }
            else {
                FileSystemHelperEx.CopyFolderContent(SourceVBPath, absoluteTempOriginalVB);
                FileSystemHelperEx.CopyFolderContent(SourceVBPath, absoluteTempTestingVB);
            }
            TesterConfig.WorkingSolutionDirectoryPath = absoluteTempTestingVB;
        }
        void ConvertToFirstBuildInRange() {
            TesterConfig.WorkingSolutionDirectoryPath = baseWorkingFolder;
            string concreteBuildLocalDXDependenciesDirectoryPath = GetConcreteBuildLocalDXDependenciesDirectoryPath(BranchName.Replace("+", ""));
            ConvertToAnotherBuild(concreteBuildLocalDXDependenciesDirectoryPath);
        }
        void UpdateResultExampleRepo(bool processVB) {
            GitHubHelper.MergePullRequest(TestedRepoOwner, TestedRepoName, PullRequestNumber);
            PrapareGitBranchChanges();
            TrashCleaner.Clean(baseWorkingFolder);
            ConvertToFirstBuildInRange();
            if (processVB) {
                MoveVBIntoBaseWorkingFolder();
                ((ExampleInfoController)ExampleInfoController).UpdateHumanVBTagIfNeeded();
                MoveToCSFolderIfNeeded();
            }
            AcceptLocalModifications(BranchName);
        }
        void MoveToCSFolderIfNeeded() {
            string csDefaultPath = Path.Combine(baseWorkingFolder, "CS");
            if (!Directory.Exists(csDefaultPath)) {
                Directory.CreateDirectory(csDefaultPath);
                string[] itemsToSkip = new[] { "CS", "VB", "Readme.md", ".gitignore", "LICENSE", ".git", BaseTesterConfig.MetadataFileName };
                FileSystemHelperEx.SafeMoveFiles(baseWorkingFolder, csDefaultPath, itemsToSkip);
            }
        }
        protected override ExampleInfoControllerBase CreateExampleInfoController() {
            string repoDescription = GitHubHelper.GetRepositoryDescription(TestedRepoOwner, TestedRepoName);
            return new ExampleInfoController(baseWorkingFolder, TesterConfig.MetadataFileName, PRHeader, repoDescription);
        }
        void HandleIncorrectlyGeneratedVB() {
            GitHubHelper.CreateFork(PullRequestCreatorUser, TesterConfig.GitUserName, TestedRepoName);
            string botForkPushBranchName = PrepareBotForkBranch();
            MoveVBIntoBaseWorkingFolder();
            ((ExampleInfoController)ExampleInfoController).InsertFormattedHumanVBTag();
            MoveToCSFolderIfNeeded();
            AcceptLocalModifications(botForkPushBranchName, " (generated VB)");
            Octokit.PullRequest botForkPullRequest = GitHubHelper.CreatePullRequest(TestedRepoName, PRHeader, TesterConfig.GitUserName, PullRequestCreatorUser, BranchName);
            GitHubHelper.AddPullRequestComment(TestedRepoOwner, TestedRepoName, PullRequestNumber, string.Format("{0}, VB hasn't been properly generated. Merge the following Pull Request and correct VB: {1}/{2}/{3}/pull/{4}", PullRequestCreatorUser, GitHubBaseAddress, PullRequestCreatorUser, TestedRepoName, botForkPullRequest.Number));
            GitHubHelper.ClosePullRequest(TestedRepoOwner, TestedRepoName, PullRequestNumber);
        }
        protected virtual CredentialsHandler CreateGitCredentialsHandler() {
            return new CredentialsHandler((url, usernameFromUrl, types) => new UsernamePasswordCredentials() { Username = TesterConfig.GitUserName, Password = TesterConfig.GitUserPassword, });
        }
        void PrapareGitBranchChanges() {
            GitHelper.CheckoutBranch(BranchName);
            GitHelper.SetBranchUpstream(BranchName, GitHelper.GetRemote("origin"));
            GitHelper.Pull(BranchName, GitCredentialsHandler);
        }
        void MoveVBIntoBaseWorkingFolder() {
            FileSystemHelper.SafeClearDirectory(SourceVBPath);
            Logger.Info("Coping from {0} to {1}", Path.Combine(rootTempDirectoryPath, relativeTempOriginalVB), SourceVBPath);
            FileSystemHelperEx.CopyFolderContent(Path.Combine(rootTempDirectoryPath, relativeTempOriginalVB), SourceVBPath, new[] { ".git" });
        }
        void AcceptLocalModifications(string branchToPush, string commitSuffix = null) {
            GitHelper.CommitChanges(PRHeader + commitSuffix, TesterConfig.MetadataFileName);
            GitHelper.PushToRemote(branchToPush, GitCredentialsHandler);
        }
        string PrepareBotForkBranch() {
            string botForkPushBranchName = BranchName + "_forPushingToBotFork";
            //GitHelper.CheckoutBranch(botForkPushBranchName);
            GitHelper.SetRepositoryHeadInCurrentWorkingTree(botForkPushBranchName);
            string botForkUrl = string.Format("{0}/{1}/{2}.git", GitHubBaseAddress, TesterConfig.GitUserName, TestedRepoName);
            GitHelper.SetBranchUpstream(botForkPushBranchName, GitHelper.CreateRemote("createdFork", botForkUrl), BranchName);
            return botForkPushBranchName;
        }
        protected bool TestExampleBuilds() {
            foreach (ExampleBuild build in verifiedBuilds)
                try {
                    TestFileSet(build.StringValue);
                }
                catch (OperationFailedException ex) {
                    Logger.Error("Testing result: {1}{0}{2}{0}{3}.", Environment.NewLine, ex.GetType().Name, ex.Message, ex.StackTrace);
                    GitHubHelper.AddPullRequestComment(TestedRepoOwner, TestedRepoName, PullRequestNumber, string.Format("{0}, an error has occurred. Please correct the project in your fork and create a new pull request. \n {1}", PullRequestCreatorUser, ex.Message));
                    GitHubHelper.ClosePullRequest(TestedRepoOwner, TestedRepoName, PullRequestNumber);
                    return false;
                }
            return true;
        }
        protected void GenerateVB(string source, string destination) {
            //Logger.Info(string.Format("Generating VB. Source= {0}. Destination={1}", source, destination));
            var instantVB = new InstantVB(destination, toolPath: "Instant VB.exe", toolSettingsFilePath: "Instant VB.dat");
            ExecuteExternalTool(instantVB, source, "[VB CONVERSION ERROR]");
        }
    }

    public class RangeExampleTesterConfiguration : BaseDefaultExampleTesterConfiguration {
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
