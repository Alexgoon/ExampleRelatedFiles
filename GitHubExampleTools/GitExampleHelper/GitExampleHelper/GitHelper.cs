using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using SingleBuildExampleTester;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitExampleHelper {
    public class GitHelper : IDisposable {
        Repository repository;
        string GitPath;
        string UserName;
        string Email;
        public GitHelper(string gitPath, string userName, string email) {
            this.repository = new Repository(gitPath);
            GitPath = gitPath;
            UserName = userName;
            Email = email;
        }
        public void CheckoutBranch(string branchName) {
            string canonicalName = "refs/heads/" + branchName;
            Branch branch = repository.Branches[canonicalName];
            if (branch == null) {
                branch = repository.CreateBranch(branchName);
            }
            LibGit2Sharp.Commands.Checkout(repository, repository.Branches[canonicalName]);
        }
        public void SetRepositoryHeadInCurrentWorkingTree(string branchName) {
            string canonicalName = "refs/heads/" + branchName;
            Branch branch = repository.Branches[canonicalName];
            if (branch == null) {
                branch = repository.CreateBranch(branchName);
                LibGit2Sharp.Commands.Checkout(repository, repository.Branches[canonicalName]);
            }
            else {
                Commit currentLatestCommit = repository.Head.Tip;
                repository.Refs.UpdateTarget("HEAD", canonicalName);
                repository.Reset(ResetMode.Mixed, currentLatestCommit);
            }
        }
        public Remote CreateRemote(string name, string url) {
            Remote remote = repository.Network.Remotes[name];
            if (remote == null) {
                remote = repository.Network.Remotes.Add(name, url);
            }
            return remote;
        }
        public Remote GetRemote(string name) {
            return repository.Network.Remotes[name];
        }
        public void SetBranchUpstream(string branchName, Remote remote, string upstreamBranchName = null) {
            Branch branch = repository.Branches["refs/heads/" + branchName];
            if (string.IsNullOrEmpty(upstreamBranchName))
                upstreamBranchName = branch.CanonicalName;
            else
                upstreamBranchName = "refs/heads/" + upstreamBranchName;
            repository.Branches.Update(branch, b => b.Remote = remote.Name, b => b.UpstreamBranch = upstreamBranchName);
        }
        public void Pull(string branchName, CredentialsHandler GitCredentialsHandler) {
            LibGit2Sharp.PullOptions pullOptions = new LibGit2Sharp.PullOptions();
            pullOptions.FetchOptions = new FetchOptions();
            pullOptions.FetchOptions.CredentialsProvider = GitCredentialsHandler;
            LibGit2Sharp.Commands.Pull(repository, new LibGit2Sharp.Signature(UserName, Email, new DateTimeOffset(DateTime.Now)), pullOptions);
        }
        public void PushToRemote(string branchName, CredentialsHandler GitCredentialsHandler) {
            Branch branch = repository.Branches["refs/heads/" + branchName];
            var pushOptions = new PushOptions() { };
            pushOptions.CredentialsProvider = GitCredentialsHandler;
            repository.Network.Push(branch, pushOptions);
        }
        public void CommitChanges(string commitMessage, string exclude = null) {
            Signature author = new Signature(UserName, Email, DateTime.Now);
            Signature committer = author;
            try {
                LibGit2Sharp.Commands.Stage(repository, "*");
                if (exclude != null)
                    LibGit2Sharp.Commands.Unstage(repository, exclude);
                repository.Commit(commitMessage, author, committer);
            }
            catch (Exception exception) {
                if (exception is EmptyCommitException)
                    Console.WriteLine("Nothing to commit");
                else
                    throw exception;
            }
        }
        public void RemoveUnstagedFiles() {
            FileSystemHelperEx.SafeClearDirectory(GitPath, new string[] { ".git" });
            repository.Reset(ResetMode.Hard);
            LibGit2Sharp.Commands.Checkout(repository, repository.Head.Tip);
        }
        //public string GetCurrentBranchName() {
        //    return repository.Head.CanonicalName;
        //}
        //public void RemoveAllButGit() {
        //    foreach (string f in Directory.GetFiles(GitPath))
        //        File.Delete(f);
        //    foreach (string dirFullPath in Directory.GetDirectories(GitPath)) {
        //        string dirName = System.IO.Path.GetFileName(dirFullPath);
        //        if (dirName != ".git")
        //            Directory.Delete(dirFullPath, true);
        //    }
        //}
        public void Dispose() {
            repository.Dispose();
        }
    }

    public class GitHubHelper {
        protected Octokit.GitHubClient client;
        public GitHubHelper(string gitHubToken) {
            client = CreateGitHubClient(gitHubToken);
        }
        public async Task<Octokit.IssueComment> AddPullRequestCommentAsync(string owner, string repoName, int pullRequestNumber, string message) {
            return await client.Issue.Comment.Create(owner, repoName, pullRequestNumber, message);
        }
        public Octokit.IssueComment AddPullRequestComment(string owner, string repoName, int pullRequestNumber, string message) {
            return RunSync<Octokit.IssueComment>(new Func<Task<Octokit.IssueComment>>(() => AddPullRequestCommentAsync(owner, repoName, pullRequestNumber, message)));
        }
        public async Task<Octokit.PullRequestMerge> MergePullRequestAsync(string owner, string repoName, int pullRequestNumber) {
            return await client.PullRequest.Merge(owner, repoName, pullRequestNumber, new Octokit.MergePullRequest());
        }
        public Octokit.PullRequestMerge MergePullRequest(string repoName, string owner, int pullRequestNumber) {
            return RunSync<Octokit.PullRequestMerge>(new Func<Task<Octokit.PullRequestMerge>>(() => MergePullRequestAsync(repoName, owner, pullRequestNumber)));
        }
        public async Task<Octokit.Repository> CreateForkAsync(string forkedRepoOwner, string destinationRepoOwner, string repoName) {
            Exception caughtException = null;
            try {
                await client.Repository.Delete(destinationRepoOwner, repoName);
            }
            catch (Exception ex) {
                caughtException = ex;
            }
            if (caughtException is Octokit.NotFoundException || caughtException == null)
                return await client.Repository.Forks.Create(forkedRepoOwner, repoName, new Octokit.NewRepositoryFork());
            return null;
        }
        public Octokit.Repository CreateFork(string forkedRepoOwner, string destinationRepoOwner, string repoName) {
            return RunSync<Octokit.Repository>(new Func<Task<Octokit.Repository>>(() => CreateForkAsync(forkedRepoOwner, destinationRepoOwner, repoName)));
        }
        public async Task<Octokit.PullRequest> CreatePullRequestAsync(string repoName, string prHeader, string prCreator, string targetRepoOwner, string branchName) {
            string canonicalBranchName = string.Format("refs/heads/{0}", branchName);
            return await client.PullRequest.Create(targetRepoOwner, repoName, new Octokit.NewPullRequest(prHeader, string.Format("{0}:{1}", prCreator, canonicalBranchName), canonicalBranchName));
        }
        public async Task<Octokit.PullRequest> ClosePullRequestAsync(string repoOwner, string repoName, int prNumber) {
            return await client.PullRequest.Update(repoOwner, repoName, prNumber, new Octokit.PullRequestUpdate() { State = Octokit.ItemState.Closed });
        }
        public Octokit.PullRequest ClosePullRequest(string repoOwner, string repoName, int prNumber) {
            return RunSync<Octokit.PullRequest>(new Func<Task<Octokit.PullRequest>>(() => ClosePullRequestAsync(repoOwner, repoName, prNumber)));
        }
        public Octokit.PullRequest CreatePullRequest(string repoName, string prHeader, string prCreator, string targetRepoOwner, string branchName) {
            return RunSync<Octokit.PullRequest>(new Func<Task<Octokit.PullRequest>>(() => CreatePullRequestAsync(repoName, prHeader, prCreator, targetRepoOwner, branchName)));
        }
        public async Task<string> GetRepositoryDescriptionAsync(string repoOwner, string repoName) {
            Octokit.Repository repo = await client.Repository.Get(repoOwner, repoName);
            return repo.Description;
        }
        public string GetRepositoryDescription(string repoOwner, string repoName) {
            return RunSync<string>(new Func<Task<string>>(() => GetRepositoryDescriptionAsync(repoOwner, repoName)));
        }
        Octokit.GitHubClient CreateGitHubClient(string gitHubToken) {
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("code-central"));
            var basicAuth = new Octokit.Credentials(gitHubToken);
            client.Credentials = basicAuth;
            return client;
        }
        RType RunSync<RType>(Func<Task<RType>> asyncFunction) {
            var task = Task<RType>.Run(new Func<Task<RType>>(async () => {
                Task<RType> asyncFunctionTask = asyncFunction();
                return await asyncFunctionTask;
            }));
            task.Wait();
            return task.Result;
        }
    }
}
