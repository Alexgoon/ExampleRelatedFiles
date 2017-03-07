using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitExampleHelper {
    public class GitHelper : IDisposable {
        Repository repository;
        string gitPath;
        public GitHelper(string gitPath) {
            this.repository = new Repository(gitPath);
            this.gitPath = gitPath;
        }
        public void CheckoutBranch(string branchName) {
            string canonicalName = "refs/heads/" + branchName;
            Branch branch = repository.Branches[canonicalName];
            if (branch == null) {
                //LibGit2Sharp.Commands.Checkout(repo, repo.Branches["refs/remotes/origin/" + branchName]);
                branch = repository.CreateBranch(branchName);
            }
            LibGit2Sharp.Commands.Checkout(repository, repository.Branches[canonicalName]);
        }
        public void CheckoutNewBranch(string branchName) {
            string canonicalName = "refs/heads/" + branchName;
            Branch branch = repository.Branches[canonicalName];
            if (branch != null) {
                repository.Branches.Remove(branch);
            }
            branch = repository.CreateBranch(branchName);
            LibGit2Sharp.Commands.Checkout(repository, repository.Branches[canonicalName]);
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
            LibGit2Sharp.Commands.Pull(repository, new LibGit2Sharp.Signature("DevExpressExampleBot", "devexpressexamplebot@gmail.com", new DateTimeOffset(DateTime.Now)), pullOptions);
        }
        public void PushToRemote(string branchName, CredentialsHandler GitCredentialsHandler) {
            Branch branch = repository.Branches["refs/heads/" + branchName];
            var pushOptions = new PushOptions() { };
            pushOptions.CredentialsProvider = GitCredentialsHandler;
            repository.Network.Push(branch, pushOptions);
        }
        public void CommitChanges(string commitMessage) {
            Signature author = new Signature("DevExpressExampleBot", "devexpressexamplebot@gmail.com", DateTime.Now);
            Signature committer = author;
            try {
                LibGit2Sharp.Commands.Stage(repository, "*");
                repository.Commit(commitMessage, author, committer);
            }
            catch (Exception exception) {
                if (exception is EmptyCommitException)
                    Console.WriteLine("Nothing to commit into VB project");
                else
                    throw exception;
            }
        }
        public void ForcedRevertBranchState(string branch) {
            RemoveAllButGit();
            repository.Reset(ResetMode.Hard);
            LibGit2Sharp.Commands.Checkout(repository, repository.Branches[branch]);
        }
        public string GetCurrentBranchName() {
            return repository.Head.CanonicalName;
        }
        public void RemoveAllButGit() {
            foreach (string f in Directory.GetFiles(gitPath))
                File.Delete(f);
            foreach (string dirFullPath in Directory.GetDirectories(gitPath)) {
                string dirName = System.IO.Path.GetFileName(dirFullPath);
                if (dirName != ".git")
                    Directory.Delete(dirFullPath, true);
            }
        }
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
