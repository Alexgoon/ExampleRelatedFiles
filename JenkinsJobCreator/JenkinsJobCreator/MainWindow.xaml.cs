using DevExpress.Mvvm;
using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace JenkinsJobCreator {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }
    }

    public class ViewModel {

        public ViewModel() {
            OrgName = "codecentral-examples";
            ErrorLog = new ObservableCollection<string>();
        }

        public virtual IReadOnlyList<Repository> Repos {
            get;
            set;
        }

        public virtual bool IsLoading {
            get;
            set;
        }

        public string OrgName {
            get;
            set;
        }

        public ObservableCollection<string> ErrorLog {
            get;
            set;
        }

        public async void PopulateRepos(string organizationName) {
            IsLoading = true;
            var client = new GitHubClient(new Octokit.ProductHeaderValue("my-cool-app"));
            var basicAuth = new Credentials("Alexploide", "ayo1Nij");
            client.Credentials = basicAuth;
            Repos = await client.Repository.GetAllForOrg(organizationName);
            IsLoading = false;
        }

        public bool CanPopulateRepos(string organizationName) {
            return !IsLoading;
        }

        public void CreateJobs(string jenkinsHost) {
            XmlDocument doc = new XmlDocument();
            doc.Load("config.xml");
            XmlNode projectUrlNode = doc.DocumentElement.SelectSingleNode(@"/project/properties/com.coravy.hudson.plugins.github.GithubProjectProperty/projectUrl");
            XmlNode gitRepoUrlNode = doc.DocumentElement.SelectSingleNode(@"/project/scm/userRemoteConfigs/hudson.plugins.git.UserRemoteConfig/url");
            ErrorLog.Clear();
            foreach (Repository rep in Repos) {
                string repoUrl = string.Format(@"https://github.com/{0}/{1}", OrgName, rep.Name);
                projectUrlNode.InnerText = repoUrl;
                gitRepoUrlNode.InnerText = repoUrl + ".git";
                string reqUrl = jenkinsHost + "/createItem?name=" + rep.Name;
                try {
                    Post(reqUrl, doc.OuterXml);
                }
                catch (Exception e) {
                    ErrorLog.Add(e.Message + ": " + System.Environment.NewLine);
                }
            }
            MessageBox.Show("Ready");
        }

        public bool CanCreateJobs(string jenkinsHost) {
            return Repos != null;
        }

        public void RemoveJobs(string jenkinsHost) {
            ErrorLog.Clear();
            foreach (Repository rep in Repos) {
                string repoUrl = string.Format(@"https://github.com/{0}/{1}", OrgName, rep.Name);
                string reqUrl = string.Format("{0}/job/{1}/doDelete", jenkinsHost, rep.Name);
                try {
                    Post(reqUrl, "");
                }
                catch (Exception e) {
                    ErrorLog.Add(reqUrl + ": " + e.Message + System.Environment.NewLine);
                }
            }
            MessageBox.Show("Ready");
        }

        public bool CanRemoveJobs(string jenkinsHost) {
            return Repos != null;
        }

        public string Post(string url, string postData) {
            WebRequest request = WebRequest.Create(url);

            byte[] credentialBuffer = new UTF8Encoding().GetBytes("Admin" + ":" + "cc3367eb3685ae8c3e0f319aa319c646");
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
