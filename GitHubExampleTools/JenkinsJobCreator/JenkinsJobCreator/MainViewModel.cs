using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace JenkinsJobCreator {

    public class MainViewModel {
        GitHubClient ghClient;
        string OrgName {
            get {
                return System.Configuration.ConfigurationManager.AppSettings["OrgName"];
            }
        }
        string GitHubToken {
            get {
                return System.Configuration.ConfigurationManager.AppSettings["GitHubToken"];
            }
        }
        string JenkinsToken {
            get {
                return System.Configuration.ConfigurationManager.AppSettings["JenkinsToken"];
            }
        }
        public virtual IReadOnlyList<Repository> Repos {
            get;
            set;
        }
        public virtual bool IsLoading {
            get;
            set;
        }
        public ObservableCollection<string> ErrorLog {
            get;
            set;
        }
        public GitHubClient GHClient {
            get {
                if (ghClient == null) {
                    ghClient = new GitHubClient(new Octokit.ProductHeaderValue("my-cool-app"));
                    var basicAuth = new Credentials(this.GitHubToken);
                    ghClient.Credentials = basicAuth;
                }
                return ghClient;
            }
        }
        public MainViewModel() {
            ErrorLog = new ObservableCollection<string>();
        }
        public async void PopulateRepos(string organizationName) {
            IsLoading = true;
            Repos = await GHClient.Repository.GetAllForOrg(organizationName);
            IsLoading = false;
        }

        public bool CanPopulateRepos(string organizationName) {
            return !IsLoading;
        }
        public async void CreateJobs(string jenkinsHost) {
            XmlDocument doc = new XmlDocument();
            doc.Load("config.xml");
            XmlNode projectUrlNode = doc.DocumentElement.SelectSingleNode(@"/project/properties/com.coravy.hudson.plugins.github.GithubProjectProperty/projectUrl");
            XmlNode gitRepoUrlNode = doc.DocumentElement.SelectSingleNode(@"/project/scm/userRemoteConfigs/hudson.plugins.git.UserRemoteConfig/url");
            XmlNode whiteListedBranches = doc.DocumentElement.SelectSingleNode(@"/project/triggers/org.jenkinsci.plugins.ghprb.GhprbTrigger/whiteListTargetBranches/org.jenkinsci.plugins.ghprb.GhprbBranch/branch");
            ErrorLog.Clear();
            foreach (Repository rep in Repos) {
                IReadOnlyList<Branch> branches = await GHClient.Repository.Branch.GetAll(rep.Id);
                foreach (Branch branch in branches) {

                    string[] branchParts = branch.Name.Split('_');

                    if (branchParts[0] == "VB")
                        continue;
                    string pureBranchName;
                    if (branchParts.Length == 2)
                        pureBranchName = branchParts[1];
                    else
                        pureBranchName = branchParts[0];

                    string repoUrl = string.Format(@"https://github.com/{0}/{1}", OrgName, rep.Name);

                    projectUrlNode.InnerText = repoUrl;
                    gitRepoUrlNode.InnerText = repoUrl + ".git";
                    whiteListedBranches.InnerText = string.Format("^.*{0}.*", pureBranchName);

                    pureBranchName = pureBranchName.Replace("+", "%2b");
                    string reqUrl = jenkinsHost + "/createItem?name=" + string.Format("{0}_{1}", rep.Name, pureBranchName);

                    try {
                        Post(reqUrl, doc.OuterXml);
                    }
                    catch (Exception e) {
                        ErrorLog.Add(e.Message + ": " + System.Environment.NewLine);
                    }
                }

            }
            MessageBox.Show("Ready");
        }
        public bool CanCreateJobs(string jenkinsHost) {
            return Repos != null;
        }
        public async void RemoveJobs(string jenkinsHost) {
            ErrorLog.Clear();
            foreach (Repository rep in Repos) {
                IReadOnlyList<Branch> branches = await GHClient.Repository.Branch.GetAll(rep.Id);
                foreach (Branch branch in branches) {
                    string[] branchParts = branch.Name.Split('_');
                    if (branchParts[0] == "VB")
                        continue;
                    string pureBranchName;
                    if (branchParts.Length == 2)
                        pureBranchName = branchParts[1];
                    else
                        pureBranchName = branchParts[0];
                    string reqUrl = string.Format("{0}/job/{1}_{2}/doDelete", jenkinsHost, rep.Name, pureBranchName);
                    try {
                        Post(reqUrl, "");
                    }
                    catch (Exception e) {
                        ErrorLog.Add(reqUrl + ": " + e.Message + System.Environment.NewLine);
                    }
                }
            }
            MessageBox.Show("Ready");
        }

        public bool CanRemoveJobs(string jenkinsHost) {
            return Repos != null;
        }

        public string Post(string url, string postData) {
            WebRequest request = WebRequest.Create(url);

            byte[] credentialBuffer = new UTF8Encoding().GetBytes(string.Format("admin:{0}", this.JenkinsToken));
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(credentialBuffer);
            request.PreAuthenticate = true;

            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "text/xml";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            dataStream = response.GetResponseStream();
            var reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }
    }
}
