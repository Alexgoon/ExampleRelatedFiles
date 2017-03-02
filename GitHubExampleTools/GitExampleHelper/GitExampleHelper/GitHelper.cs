using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitExampleHelper {


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

        public static void Pull(string gitPath, string branchName, CredentialsHandler GitCredentialsHandler) {
            using (var repo = new Repository(gitPath)) {

                LibGit2Sharp.PullOptions pullOptions = new LibGit2Sharp.PullOptions();
                pullOptions.FetchOptions = new FetchOptions();
                pullOptions.FetchOptions.CredentialsProvider = GitCredentialsHandler;

                LibGit2Sharp.Commands.Pull(repo, new LibGit2Sharp.Signature("DevExpressExampleBot", "devexpressexamplebot@gmail.com", new DateTimeOffset(DateTime.Now)), pullOptions);
            }
        }

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
                Signature author = new Signature("DevExpressExampleBot", "devexpressexamplebot@gmail.com", DateTime.Now);
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
                RemoveAllButGit(gitPath);
                repo.Reset(ResetMode.Hard);
                LibGit2Sharp.Commands.Checkout(repo, repo.Branches[branch]);
            }
        }

        public static string GetCurrentBranchName(string gitPath) {
            using (var repo = new Repository(gitPath)) {
                return repo.Head.CanonicalName;
            }
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

        public static Patch GetDifferencesWithLastCommit(string repoFullPath) {
            using (var repo = new Repository(repoFullPath)) {
                Tree commitTree = repo.Head.Tip.Tree;
                Tree parentCommitTree = repo.Head.Tip.Parents.Single().Tree;
                return repo.Diff.Compare<Patch>(parentCommitTree, commitTree);
            }
        }
    }

    public static class GitHubHelper {
        public static async Task<Octokit.IssueComment> AddPullRequestCommentAsync(string gitHubToken, string repoFullName, int pullRequestNumber, string message) {
            string[] repoNameParts = repoFullName.Split('/');
            string owner = repoNameParts[0];
            string repoName = repoNameParts[1];
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("my-cool-app"));
            var basicAuth = new Octokit.Credentials(gitHubToken);
            client.Credentials = basicAuth;
            return await client.Issue.Comment.Create(owner, repoName, pullRequestNumber, message);
        }

        public static void AddPullRequestComment(string gitHubToken, string repoFullName, int pullRequestNumber, string message) {
            var task = Task.Run(async () => { await AddPullRequestCommentAsync(gitHubToken, repoFullName, pullRequestNumber, message); });
            task.Wait();
        }

        public static async Task<Octokit.PullRequestMerge> MergePullRequestAsync(string gitHubToken, string repoFullName, int pullRequestNumber) {
            string[] repoNameParts = repoFullName.Split('/');
            string owner = repoNameParts[0];
            string repoName = repoNameParts[1];
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("my-cool-app"));
            var basicAuth = new Octokit.Credentials(gitHubToken);
            client.Credentials = basicAuth;
            return await client.PullRequest.Merge(owner, repoName, pullRequestNumber, new Octokit.MergePullRequest());
        }

        public static void MergePullRequest(string gitHubToken, string repoFullName, int pullRequestNumber) {
            var task = Task.Run(async () => { await MergePullRequestAsync(gitHubToken, repoFullName, pullRequestNumber); });
            task.Wait();
        }
    }
}
