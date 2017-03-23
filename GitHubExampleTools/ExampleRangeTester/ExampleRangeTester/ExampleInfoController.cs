using SingleBuildExampleTester;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ExampleRangeTester {
    public class ExampleInfoController : ExampleInfoControllerBase {
        string HumanVBTag = "[Human VB]";
        string HumanVBTagStringFormat = "**<sub>{0}</sub>**";
        string PRHeader;
        string UnstructuredStringData;
        protected string FormattedHumanVBTag {
            get { return string.Format(HumanVBTagStringFormat, HumanVBTag); }
        }
        public bool IsHumanVBTagInPRHeader {
            get { return PRHeader.Contains(HumanVBTag); }
        }
        public bool IsHumanVBTagInReadme {
            get {
                string readmeText = System.IO.File.ReadAllText(Path.Combine(rootFolder, "README.md"));
                return readmeText.Contains(HumanVBTag);
            }
        }
        public ExampleInfoController(string rootFolderPath, string metadataFileName, string prHeader, string unstructuredStringData)
            : base(rootFolderPath, metadataFileName) {
            PRHeader = prHeader;
            UnstructuredStringData = unstructuredStringData == null ? string.Empty : unstructuredStringData.ToLower();
        }
        public void UpdateHumanVBTagIfNeeded() {
            if (!IsHumanVBTagInReadme && IsHumanVBTagInPRHeader)
                InsertFormattedHumanVBTag();
            else if (IsHumanVBTagInReadme)
                EnsureHumanVBTagIsFormatted();
        }
        public void InsertFormattedHumanVBTag() {
            string readePath = Path.Combine(rootFolder, "README.md");
            string readmeText = File.ReadAllText(readePath);
            readmeText = readmeText + FormattedHumanVBTag;
            File.WriteAllText(readePath, readmeText);
        }
        void EnsureHumanVBTagIsFormatted() {
            string readePath = Path.Combine(rootFolder, "README.md");
            string readmeText = System.IO.File.ReadAllText(readePath);
            if (!readmeText.Contains(FormattedHumanVBTag)) {
                readmeText = readmeText.Replace(HumanVBTag, string.Empty);
                readmeText = readmeText + FormattedHumanVBTag;
                File.WriteAllText(readePath, readmeText);
            }
        }
        protected override ExampleMetadata GetMetadataStorage() {
            ExampleMetadata exampleMetadata = new ExampleMetadata();
            exampleMetadata.Platform = GetExamplePlatform();
            SerializeMetadata(exampleMetadata);
            return exampleMetadata;
        }
        protected string GetExamplePlatform() {
            string platformTag = "platform:";
            if (UnstructuredStringData.Contains(platformTag)) {
                int firstPlatformValueLetterIndex = UnstructuredStringData.IndexOf(platformTag) + platformTag.Length;
                char ch = UnstructuredStringData[firstPlatformValueLetterIndex];
                while (ch == ' ') {
                    ch = UnstructuredStringData[++firstPlatformValueLetterIndex];
                }
                int lastPlatformValueLetterIndex = UnstructuredStringData.IndexOf(' ', firstPlatformValueLetterIndex);
                if (lastPlatformValueLetterIndex == -1)
                    lastPlatformValueLetterIndex = UnstructuredStringData.Length;
                return UnstructuredStringData.Substring(firstPlatformValueLetterIndex, lastPlatformValueLetterIndex - firstPlatformValueLetterIndex).ToLower();
            }
            return string.Empty;
        }
        protected void SerializeMetadata(ExampleMetadata exampleMetadata) {
            using (Stream metadataFileStream = new FileStream(MetadataFileFullPath, FileMode.OpenOrCreate)) {
                XmlSerializer serializer = new XmlSerializer(typeof(ExampleMetadata));
                serializer.Serialize(metadataFileStream, exampleMetadata);
            }
        }
    }
}
