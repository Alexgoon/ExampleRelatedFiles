using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace JenkinsSingleBuildTasksLauncher {
    class Program {
        static void Main(string[] args) {
            new JenkinsSingleBuildTasksLauncher().TriggerTasks();
        }
    }
}
