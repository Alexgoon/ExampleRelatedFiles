using CodeCentral.Helpers;
using CodeCentral.Infrastructure;
using CodeCentral.Services;
using CodeCentral.Tester;
using CodeCentral.Tools;
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
            catch (Exception e) {
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

    public class ExampleRangeTester : ExampleTool {
        protected List<SampleBuild> verifiedBuilds;
        protected string Builds;
        protected bool IsOriginalProjectCS;
        protected string CommitMessage = string.Empty;
        CredentialsHandler gitCredentialsHandler;

        protected CredentialsHandler GitCredentialsHandler {
            get {
                if (gitCredentialsHandler == null)
                    gitCredentialsHandler = CreateGitCredentialsHandler();
                return gitCredentialsHandler;
            }
        }

        public ExampleRangeTester(string workingFolder, string builds, string commitMessage, bool isOriginalProjectCS)
            : base(new DefaultExampleTesterConfigurationEx() { LocalDXAssemblyDirectoryPath = @"D:\ExampleTestingDXDlls\" }, new DefaultDataRepositoryService(), new DefaultFileSystemService()) {
            IsOriginalProjectCS = isOriginalProjectCS;
            ((DefaultExampleTesterConfigurationEx)BaseConfiguration).WorkingSolutionDirectoryPath = workingFolder;
            Builds = builds;
            CommitMessage = commitMessage;
            verifiedBuilds = ParseBuilds(builds);

        }

        protected virtual CredentialsHandler CreateGitCredentialsHandler() {
            return new CredentialsHandler((url, usernameFromUrl, types) => new UsernamePasswordCredentials() { Username = "Alexgoon", Password = "777perec777" });
        }

        public bool TestExample() {
            string csTempDirectoryPath = FileSystemHelperEx.CopyWorkingFolderIntoTemp(BaseConfiguration.WorkingSolutionDirectoryPath, "$tempCSFolder");

            //CS testing
            if (!TestExampleBuilds()) {
                EndTesting(csTempDirectoryPath);
                return false;
            }

            //VB testing
            UpdateVBFromCS(csTempDirectoryPath);
            if (!TestExampleBuilds()) {
                EndTesting(csTempDirectoryPath);
                return false;
            }
            EndTesting(csTempDirectoryPath);
            return true;
        }

        protected bool TestExampleBuilds() {
            foreach (SampleBuild build in verifiedBuilds)
                try {
                    TestFileSet(build.StringValue);

                }
                catch (OperationFailedException ex) {
                    Console.WriteLine("Testing result: {1}{0}{2}{0}{3}.", Environment.NewLine, ex.GetType().Name, ex.Message, ex.StackTrace);
                    return false;
                }
            return true;
        }

        protected void EndTesting(string csTempDirectoryPath) {
            FileSystemHelper.SafeDeleteDirectory(csTempDirectoryPath);
            GitHelper.ForcedRevertBranchState(BaseConfiguration.WorkingSolutionDirectoryPath, "refs/remotes/origin/CS_" + Builds);
        }

        protected void UpdateVBFromCS(string csFolderWithInitialData) {

            FileSystemHelper.SafeClearDirectory(BaseConfiguration.WorkingSolutionDirectoryPath, "*");
            GenerateVB(csFolderWithInitialData, BaseConfiguration.WorkingSolutionDirectoryPath);
            GitHelper.CheckoutBranch(csFolderWithInitialData, "VB_" + Builds);

            string gitWorkingFolderPath = Path.Combine(BaseConfiguration.WorkingSolutionDirectoryPath, ".git");
            string gitTempSCFolderPath = Path.Combine(csFolderWithInitialData, ".git");

            FileSystemHelper.SafeDeleteDirectory(gitWorkingFolderPath);
            Directory.Move(gitTempSCFolderPath, gitWorkingFolderPath);

            GitHelper.CommitChanges(BaseConfiguration.WorkingSolutionDirectoryPath, CommitMessage + "(VB)");

            GitHelper.PushToRemote(BaseConfiguration.WorkingSolutionDirectoryPath, "VB_" + Builds, GitCredentialsHandler);
        }

        protected void CommitChanges(Repository repo) {
            Signature author = new Signature("Alexgoon", "alex.russkovdx@gmail.com", DateTime.Now);
            Signature committer = author;
            try {
                LibGit2Sharp.Commands.Stage(repo, "*");
                repo.Commit("VB generation" + Guid.NewGuid(), author, committer);
            }
            catch (Exception e) {
                if (e is EmptyCommitException)
                    Console.WriteLine("Nothing to commit into VB project");
                else
                    throw e;
            }
        }

        void UpdateRemoute(Repository repo, Branch branch) {
            var pushOptions = new PushOptions() { };
            pushOptions.CredentialsProvider = GitCredentialsHandler;
            repo.Network.Push(branch, pushOptions);
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

        List<SampleBuild> ParseBuilds(string builds) {
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

            foreach (string dir in Directory.EnumerateDirectories(GetPlatformSpecificRemoteDXDependenciesDirectoryPath())) {
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
            get {
                return ToString();
            }
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
            return Comparer<int>.Default.Compare(this.FirstDigit * 1000 + this.SecondDigit * 100 + this.ThirdDigit, other.FirstDigit * 1000 + other.SecondDigit * 100 + other.ThirdDigit);
        }
    }

    public class DefaultExampleTesterConfigurationEx : DefaultExampleTesterConfiguration, IExampleToolConfiguration {
        public new string WorkingSolutionDirectoryPath { get; set; }
        public new string LocalDXAssemblyDirectoryPath { get; set; }
        public string GitHubUserName { get; set; }
    }

    public static class FileSystemHelperEx {
        public static string CreateTempFolder(string folderName) {
            return Directory.CreateDirectory(Path.Combine(@"D:\", folderName + Guid.NewGuid().ToString())).FullName;
        }

        public static void RemoveAllButGit(string source) {
            foreach (string f in Directory.GetFiles(source))
                File.Delete(f);
            foreach (string dirFullPath in Directory.GetDirectories(source)) {
                string dirName = System.IO.Path.GetFileName(dirFullPath);
                if (dirName != ".git")
                    Directory.Delete(dirFullPath, true);
            }
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

    public static class GitHelper {
        public static void CheckoutBranch(string gitPath, string branchName) {
            using (var repo = new Repository(gitPath)) {
                Branch branch = repo.Branches["refs/heads/" + branchName];
                if (branch == null) {
                    LibGit2Sharp.Commands.Checkout(repo, repo.Branches["refs/remotes/origin/" + branchName]);
                    branch = repo.CreateBranch(branchName);
                    Remote remote = repo.Network.Remotes["origin"];
                    repo.Branches.Update(branch, b => b.Remote = remote.Name, b => b.UpstreamBranch = branch.CanonicalName);
                }
                LibGit2Sharp.Commands.Checkout(repo, repo.Branches["refs/heads/" + branchName]);
            }
        }

        //public static void Pull(string gitPath, Branch branch, CredentialsHandler GitCredentialsHandler) {
        //    using (var repo = new Repository(gitPath)) {
        //        Remote remote = repo.Network.Remotes["origin"];
        //        repo.Branches.Update(branch, b => b.Remote = remote.Name, b => b.UpstreamBranch = branch.CanonicalName);

        //        LibGit2Sharp.PullOptions pullOptions = new LibGit2Sharp.PullOptions();
        //        pullOptions.FetchOptions = new FetchOptions();
        //        pullOptions.FetchOptions.CredentialsProvider = GitCredentialsHandler;


        //        //FileSystemHelperEx.DeleteProjectFiles(BaseConfiguration.WorkingSolutionDirectoryPath);

        //        LibGit2Sharp.Commands.Pull(repo, new LibGit2Sharp.Signature("Alexgoon", "mail", new DateTimeOffset(DateTime.Now)), pullOptions);
        //    }
        //}

        public static void PushToRemote(string gitPath, string branchName, CredentialsHandler GitCredentialsHandler) {
            using (var repo = new Repository(gitPath)) {
                Branch branch = repo.Branches["refs/heads/" + branchName];
                var pushOptions = new PushOptions() { };
                pushOptions.CredentialsProvider = GitCredentialsHandler;
                repo.Network.Push(branch, pushOptions);
            }
        }

        public static void CommitChanges(string gitPath, string commitMessage) {
            using (var repo = new Repository(gitPath)) {
                Signature author = new Signature("Alexgoon", "alex.russkovdx@gmail.com", DateTime.Now);
                Signature committer = author;
                try {
                    LibGit2Sharp.Commands.Stage(repo, "*");
                    repo.Commit(commitMessage, author, committer);
                }
                catch (Exception e) {
                    if (e is EmptyCommitException)
                        Console.WriteLine("Nothing to commit into VB project");
                    else
                        throw e;
                }
            }
        }

        public static void ForcedRevertBranchState(string gitPath, string branch) {
            using (var repo = new Repository(gitPath)) {
                FileSystemHelperEx.RemoveAllButGit(gitPath);
                repo.Reset(ResetMode.Hard);
                LibGit2Sharp.Commands.Checkout(repo, repo.Branches[branch]);
            }
        }

        public static string GetCurrentBranchName(string gitPath) {
            using (var repo = new Repository(gitPath)) {
                return repo.Head.CanonicalName;
            }
        }
    }



}
