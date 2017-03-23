using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SingleBuildExampleTester {
    public class ExampleInfoControllerBase {
        string metadataFile;
        protected string rootFolder;
        ExampleMetadata metadataStorage;
        public ExampleMetadata MetadataStorage {
            get {
                if (metadataStorage == null) {
                    metadataStorage = GetMetadataStorage();
                }
                return metadataStorage;
            }
        }
        public bool IsWindows10AppsPlatformExample {
            get { return string.Compare(MetadataStorage.Platform, "uwp", true) == 0; }
        }
        public bool IsDevExtremePlatformExample {
            get { return string.Compare(MetadataStorage.Platform, "devextreme", true) == 0; }
        }
        public bool IsPlatformEmpty {
            get { return string.IsNullOrEmpty(MetadataStorage.Platform); }
        }
        protected string MetadataFileFullPath {
            get { return string.Format(@"{0}\{1}", rootFolder, metadataFile); }
        }

        public ExampleInfoControllerBase(string rootFolderPath, string metadataFileName) {
            this.rootFolder = rootFolderPath;
            this.metadataFile = metadataFileName;
        }
        protected virtual ExampleMetadata GetMetadataStorage() {
            if (!File.Exists(MetadataFileFullPath))
                throw new FileNotFoundException("Cannot find metadata file " + MetadataFileFullPath);
            using (Stream metadataFileStream = new FileStream(MetadataFileFullPath, FileMode.Open)) {
                XmlSerializer serializer = new XmlSerializer(typeof(ExampleMetadata));
                return (ExampleMetadata)serializer.Deserialize(metadataFileStream);
            }
        }
    }
}
