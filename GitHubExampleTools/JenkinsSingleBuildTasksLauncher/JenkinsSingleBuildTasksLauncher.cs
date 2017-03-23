using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace JenkinsSingleBuildTasksLauncher {
    public class JenkinsSingleBuildTasksLauncher {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        string BaseJenkinsAddress {
            get {
                return System.Configuration.ConfigurationManager.AppSettings["BaseJenkinsAddress"];
            }
        }
        string JenkinsTaskSignature {
            get {
                return System.Configuration.ConfigurationManager.AppSettings["JenkinsTaskSignature"];
            }
        }
        public JenkinsSingleBuildTasksLauncher() {

        }
        public void TriggerTasks() {
            XmlDocument doc = new XmlDocument();
            string xml = GetJenkinsData(string.Format("{0}/api/xml", BaseJenkinsAddress));
            doc.LoadXml(xml);
            XmlNodeList nodes = doc.SelectNodes(@"hudson/job/name");
            foreach (XmlNode node in nodes) {
                if (node.InnerText.Contains(JenkinsTaskSignature)) {
                    logger.Info("Triggering {0}", node.InnerText);
                    RunJob(string.Format("{0}/job/{1}/build", BaseJenkinsAddress, node.InnerText));
                }
            }
        }

        string RunJob(string url) {
            return Request(url, "POST");
        }

        string GetJenkinsData(string url) {
            return Request(url, "GET");
        }

        string Request(string url, string method) {
            WebRequest request = WebRequest.Create(url);
            byte[] credentialBuffer = new UTF8Encoding().GetBytes("Admin" + ":" + "ce688fd203beafac92afe40d8c745bec");
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(credentialBuffer);
            request.PreAuthenticate = true;
            request.Method = method;
            WebResponse response = request.GetResponse();
            Stream dataStream;
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
