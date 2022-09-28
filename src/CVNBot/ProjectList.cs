using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Xml;
using log4net;

namespace CVNBot
{
    class ProjectList : SortedList
    {
        ILog logger = LogManager.GetLogger("CVNBot.ProjectList");

        public string fnProjectsXML;
        public string currentBatchReloadChannel = "";

        /// <summary>
        /// Dumps all Projects to an XML file (Projects.xml)
        /// </summary>
        void DumpToFile()
        {
            logger.Info("Saving configuration to " + fnProjectsXML);

            using (StreamWriter sw = new StreamWriter(fnProjectsXML))
            {
                sw.WriteLine("<projects>");
                foreach (DictionaryEntry dicent in this)
                {
                    Project prj = (Project)dicent.Value;
                    // Get each Project's details and append it to the XML file
                    sw.WriteLine(prj.DumpProjectDetails());
                }
                sw.WriteLine("</projects>");
            }
        }

        /// <summary>
        /// Loads and initializes Projects from an XML file (Projects.xml)
        /// </summary>
        public void LoadFromFile()
        {
            logger.Info("Reading projects from " + fnProjectsXML);
            XmlDocument doc = new XmlDocument();
            doc.Load(fnProjectsXML);
            XmlNode parentnode = doc.FirstChild;
            for (int i = 0; i < parentnode.ChildNodes.Count; i++)
            {
                string prjDefinition = "<project>" + parentnode.ChildNodes[i].InnerXml + "</project>";
                Project prj = new Project();
                prj.ReadProjectDetails(prjDefinition);
                this.Add(prj.projectName, prj);
            }
        }

        /// <summary>
        /// Adds a new Project to the ProjectList. Remember to dump the configuration afterwards by calling dumpToFile()
        /// </summary>
        /// <param name="projectName">Name of the project (e.g., loginwiki) to add</param>
        public void AddNewProject(string projectName)
        {
            if (this.ContainsKey(projectName))
                throw new Exception(Program.GetFormatMessage(16400, projectName));

            logger.InfoFormat("Registering new project {0}", projectName);
            Project prj = new Project();
            prj.projectName = projectName;

            string subdomain = projectName.Substring(0, projectName.Length - Program.config.projectSuffix.Length);
            string domain = Program.config.projectDomain;

            prj.rooturl = "https://" + subdomain + "." + domain + "/";

            prj.RetrieveWikiDetails();
            this.Add(projectName, prj);

            // Dump new settings
            DumpToFile();
        }

        /// <summary>
        /// Removes a project from the ProjectList
        /// </summary>
        /// <param name="projectName">Name of the project to remove</param>
        public void DeleteProject(string projectName)
        {
            if (!this.ContainsKey(projectName))
            {
                throw new Exception(Program.GetFormatMessage(16401, projectName));
            }

            logger.Info("Deleting existing project " + projectName);

            // Leave monitoring channel:
            logger.Info("Leaving #" + projectName);
            Program.rcirc.rcirc.RfcPart("#" + projectName, "No longer monitored");

            // Wait for existing RCEvents in separate thread to go through:
            Thread.Sleep(4000);

            // Finally, remove from list:
            this.Remove(projectName);

            // Dump new settings:
            DumpToFile();
        }

        public void ReloadAllWikis()
        {
            Thread.CurrentThread.Name = "ReloadAll";

            Program.SendMessageF(Meebey.SmartIrc4net.SendType.Message, currentBatchReloadChannel,
                                 "Request to reload all " + this.Count.ToString() + " wikis accepted.",
                                 Meebey.SmartIrc4net.Priority.High);

            ProjectList original = new ProjectList();
            original.fnProjectsXML = Program.config.projectsFile;
            original.LoadFromFile();

            foreach (DictionaryEntry dicent in original)
            {
                Project prj = (Project)dicent.Value;
                prj.RetrieveWikiDetails();
                Thread.Sleep(600);
            }

            // Dump new settings:
            DumpToFile();

            Program.SendMessageF(Meebey.SmartIrc4net.SendType.Message, currentBatchReloadChannel,
                                 "Reloaded all wikis. Phew, give the servers a break :(",
                                 Meebey.SmartIrc4net.Priority.High);
        }
    }
}
