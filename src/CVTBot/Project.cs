using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace CVTBot
{
    internal class Project
    {
        private static readonly ILog logger = LogManager.GetLogger("CVTBot.Project");

        public string projectName;
        public string rooturl; // Format: https://meta.miraheze.org/

        public Regex rrestoreRegex;
        public Regex rdeleteRegex;
        public Regex rprotectRegex;
        public Regex runprotectRegex;
        public Regex rmodifyprotectRegex;
        public Regex ruploadRegex;
        public Regex rmoveRegex;
        public Regex rmoveredirRegex;
        public Regex rblockRegex;
        public Regex runblockRegex;
        public Regex rreblockRegex;
        public Regex rautosummBlank;
        public Regex rautosummReplace;
        public Regex rSpecialLogRegex;
        public Regex rCreate2Regex;

        public Hashtable namespaces;
        private readonly Dictionary<string, string> regexDict = new Dictionary<string, string>();
        private static readonly char[] rechars = { '\\', '.', '(', ')', '[', ']', '^', '*', '+', '?', '{', '}', '|' };
        private string snamespaces;
        private string sMwMessages;

        /// <summary>
        /// Generates Regex objects from regex strings in class. Always generate the namespace list before calling this!
        /// </summary>
        private void GenerateRegexen()
        {
            Dictionary<string, string> regexPatterns = regexDict.ContainsKey("restoreRegex") ?
                regexDict : ((Project)Program.prjlist[Program.config.centralProject]).regexDict;

            rrestoreRegex = new Regex(regexPatterns["restoreRegex"]);
            rdeleteRegex = new Regex(regexPatterns["deleteRegex"]);
            rprotectRegex = new Regex(regexPatterns["protectRegex"]);
            runprotectRegex = new Regex(regexPatterns["unprotectRegex"]);
            rmodifyprotectRegex = new Regex(regexPatterns["modifyprotectRegex"]);
            ruploadRegex = new Regex(regexPatterns["uploadRegex"]);
            rmoveRegex = new Regex(regexPatterns["moveRegex"]);
            rmoveredirRegex = new Regex(regexPatterns["moveredirRegex"]);
            rblockRegex = new Regex(regexPatterns["blockRegex"]);
            runblockRegex = new Regex(regexPatterns["unblockRegex"]);
            rreblockRegex = new Regex(regexPatterns["reblockRegex"]);
            rautosummBlank = new Regex(regexPatterns["autosummBlank"]);
            rautosummReplace = new Regex(regexPatterns["autosummReplace"]);

            rSpecialLogRegex = new Regex(regexDict["specialLogRegex"]);
            rCreate2Regex = new Regex(namespaces["2"] + @":([^:]+)");
        }

        public string DumpProjectDetails()
        {
            StringWriter output = new StringWriter();

            using (XmlTextWriter dump = new XmlTextWriter(output))
            {
                dump.WriteStartElement("project");

                dump.WriteElementString("projectName", projectName);
                dump.WriteElementString("rooturl", rooturl);
                dump.WriteElementString("speciallog", rSpecialLogRegex.ToString());
                dump.WriteElementString("namespaces", snamespaces);

                dump.WriteElementString("restoreRegex", rrestoreRegex.ToString());
                dump.WriteElementString("deleteRegex", rdeleteRegex.ToString());
                dump.WriteElementString("protectRegex", rprotectRegex.ToString());
                dump.WriteElementString("unprotectRegex", runprotectRegex.ToString());
                dump.WriteElementString("modifyprotectRegex", rmodifyprotectRegex.ToString());
                dump.WriteElementString("uploadRegex", ruploadRegex.ToString());
                dump.WriteElementString("moveRegex", rmoveRegex.ToString());
                dump.WriteElementString("moveredirRegex", rmoveredirRegex.ToString());
                dump.WriteElementString("blockRegex", rblockRegex.ToString());
                dump.WriteElementString("unblockRegex", runblockRegex.ToString());
                dump.WriteElementString("reblockRegex", rreblockRegex.ToString());
                dump.WriteElementString("autosummBlank", rautosummBlank.ToString());
                dump.WriteElementString("autosummReplace", rautosummReplace.ToString());

                dump.WriteEndElement();
                dump.Flush();
            }

            return output.ToString();
        }

        public void ReadProjectDetails(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode parentnode = doc.FirstChild;
            for (int i = 0; i < parentnode.ChildNodes.Count; i++)
            {
                string key = parentnode.ChildNodes[i].Name;
                string value = parentnode.ChildNodes[i].InnerText;
                switch (key)
                {
                    case "projectName": projectName = value; break;
                    case "rooturl": rooturl = value; break;
                    case "speciallog": regexDict["specialLogRegex"] = value; break;
                    case "namespaces": snamespaces = value; break;
                    case "restoreRegex": regexDict["restoreRegex"] = value; break;
                    case "deleteRegex": regexDict["deleteRegex"] = value; break;
                    case "protectRegex": regexDict["protectRegex"] = value; break;
                    case "unprotectRegex": regexDict["unprotectRegex"] = value; break;
                    case "modifyprotectRegex": regexDict["modifyprotectRegex"] = value; break;
                    case "uploadRegex": regexDict["uploadRegex"] = value; break;
                    case "moveRegex": regexDict["moveRegex"] = value; break;
                    case "moveredirRegex": regexDict["moveredirRegex"] = value; break;
                    case "blockRegex": regexDict["blockRegex"] = value; break;
                    case "unblockRegex": regexDict["unblockRegex"] = value; break;
                    case "reblockRegex": regexDict["reblockRegex"] = value; break;
                    case "autosummBlank": regexDict["autosummBlank"] = value; break;
                    case "autosummReplace": regexDict["autosummReplace"] = value; break;
                }
            }
            // Always get namespaces before generating regexen
            GetNamespaces(true);
            // Regenerate regexen
            GenerateRegexen();
        }

        private void GetNamespaces(bool snamespacesAlreadySet)
        {
            if (!snamespacesAlreadySet)
            {
                logger.InfoFormat("Fetching namespaces from {0}", rooturl);
                try
                {
                    snamespaces = CVTBotUtils.GetRawDocument(rooturl + "w/api.php?format=xml&action=query&meta=siteinfo&siprop=namespaces");
                }
                catch (Exception e)
                {
                    logger.ErrorFormat("Can't load list of namespaces from {0}, returned {1}", rooturl, e.Message);
                    if (e.Message.Contains("404"))
                    {
                        try
                        {
                            Program.prjlist.DeleteProject(projectName);
                            _ = Program.listman.PurgeWikiData(projectName);
                            logger.InfoFormat("Deleted and purged project {0}", projectName);
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Delete/purge project failed", ex);
                        }
                    }

                    return;
                }

                if (snamespaces == "")
                {
                    logger.ErrorFormat("Can't load list of namespaces from {0}", rooturl);

                    return;
                }
            }

            // Load the orignal API response first so that we
            // can use it to rebuild XML later, isolating namespaces
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(snamespaces);

            if (doc.GetElementsByTagName("namespaces").Count == 1)
            {
                namespaces = new Hashtable();
                string namespacesLogline = "";

                snamespaces = doc.GetElementsByTagName("namespaces")[0].OuterXml;
                doc.LoadXml(snamespaces);

                XmlNode namespacesNode = doc.GetElementsByTagName("namespaces")[0];
                for (int i = 0; i < namespacesNode.ChildNodes.Count; i++)
                {
                    namespaces.Add(namespacesNode.ChildNodes[i].Attributes["id"].Value, namespacesNode.ChildNodes[i].InnerText);
                    namespacesLogline += "id[" + namespacesNode.ChildNodes[i].Attributes["id"].Value + "]=" + namespacesNode.ChildNodes[i].InnerText + "; ";
                }
            }
        }

        public struct MessagesOption
        {
            public int NumberOfArgs;
            public string RegexName;
            public bool NonStrictFlag;
            public MessagesOption(int ArgNumberOfArgs, string ArgRegexName, bool ArgNonStrictFlag)
            {
                NumberOfArgs = ArgNumberOfArgs;
                RegexName = ArgRegexName;
                NonStrictFlag = ArgNonStrictFlag;
            }
        }

        public void RetrieveWikiDetails()
        {
            // Find out what the localized Special: (ID -1) namespace is, and create a regex
            GetNamespaces(false);

            if (namespaces == null)
            {
                // Set defaults for namespaces
                namespaces = new Hashtable
                {
                    { "-2", "Media" },
                    { "-1", "Special" },
                    { "0", "" },
                    { "1", "Talk" },
                    { "2", "User" },
                    { "3", "User talk" },
                    { "4", "Project" },
                    { "5", "Project talk" },
                    { "6", "File" },
                    { "7", "File talk" },
                    { "8", "MediaWiki" },
                    { "9", "MediaWiki talk" },
                    { "10", "Template" },
                    { "11", "Template talk" },
                    { "12", "Help" },
                    { "13", "Help talk" },
                    { "14", "Category" },
                    { "15", "Category talk" }
                };
            }

            regexDict["specialLogRegex"] = namespaces["-1"] + @":.+?/(.+)";

            logger.InfoFormat("Fetching interface messages from {0}", rooturl);

            Dictionary<string, MessagesOption> Messages = new Dictionary<string, MessagesOption>
            {
                // Location of message, number of required parameters, reference to regex, allow lazy
                // Retrieve messages for all the required events and generate regexen for them

                { "Undeletedarticle", new MessagesOption(1, "restoreRegex", false) },
                { "Deletedarticle", new MessagesOption(1, "deleteRegex", false) },
                { "Protectedarticle", new MessagesOption(1, "protectRegex", false) },
                { "Unprotectedarticle", new MessagesOption(1, "unprotectRegex", false) },
                { "Modifiedarticleprotection", new MessagesOption(1, "modifyprotectRegex", true) },
                { "Uploadedimage", new MessagesOption(0, "uploadRegex", false) },
                { "1movedto2", new MessagesOption(2, "moveRegex", false) },
                { "1movedto2_redir", new MessagesOption(2, "moveredirRegex", false) },

                // blockRegex is nonStrict because some wikis override the message without including $2 (block length).
                // RCReader will fall back to "24 hours" if this is the case.
                // Some newer messages have a third item,
                // $3 ("anononly,nocreate,autoblock"). This may conflict with $2 detection.
                // Trying (changed 2 -> 3) to see if length of time will be correctly detected using just this method:
                { "Blocklogentry", new MessagesOption(3, "blockRegex", true) },

                { "Unblocklogentry", new MessagesOption(0, "unblockRegex", false) },
                { "Reblock-logentry", new MessagesOption(3, "reblockRegex", false) },
                { "Autosumm-blank", new MessagesOption(0, "autosummBlank", false) },

                // autosummReplace is nonStrict because some wikis use translations overrides without
                // a "$1" parameter for the content.
                { "Autosumm-replace", new MessagesOption(1, "autosummReplace", true) }
            };

            GetInterfaceMessages(Messages);
            GenerateRegexen();
        }

        private void GetInterfaceMessages(Dictionary<string, MessagesOption> Messages)
        {
            string CombinedMessages = string.Join("|", Messages.Keys);

            try
            {
                sMwMessages = CVTBotUtils.GetRawDocument(
                    rooturl +
                    "w/api.php?action=query&meta=allmessages&format=xml" +
                    "&ammessages=" + CombinedMessages
                );
            }
            catch (Exception e)
            {
                logger.ErrorFormat("Can't load list of InterfaceMessages from {0}. {1} Getting from {2}.", rooturl, e.Message, Program.config.centralProject);

                return;
            }

            if (sMwMessages == "")
            {
                logger.ErrorFormat("Can't load list of InterfaceMessages from {0}", rooturl);

                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(sMwMessages);
            string mwMessagesLogline = "";
            XmlNode allmessagesNode = doc.GetElementsByTagName("allmessages")[0];
            if (allmessagesNode == null)
            {
                logger.ErrorFormat("InterfaceMessages returned null from {0}", rooturl);

                return;
            }
            for (int i = 0; i < allmessagesNode.ChildNodes.Count; i++)
            {
                string elmName = allmessagesNode.ChildNodes[i].Attributes["name"].Value;
                GenerateRegex(
                    elmName,
                    allmessagesNode.ChildNodes[i].InnerText,
                    Messages[elmName].NumberOfArgs,
                    Messages[elmName].RegexName,
                    Messages[elmName].NonStrictFlag
                );
                mwMessagesLogline += "name[" + elmName + "]=" + allmessagesNode.ChildNodes[i].InnerText + "; ";
            }
        }

        private void GenerateRegex(string mwMessageTitle, string mwMessage, int reqCount, string destRegex, bool nonStrict)
        {
            // Now gently coax that into a regex
            foreach (char c in rechars)
            {
                mwMessage = mwMessage.Replace(c.ToString(), @"\" + c.ToString());
            }

            mwMessage = mwMessage.Replace("$1", "(?<item1>.+?)");
            mwMessage = mwMessage.Replace("$2", "(?<item2>.+?)");
            mwMessage = mwMessage.Replace("$3", "(?<item3>.+?)");
            mwMessage = mwMessage.Replace("$1", "(?:.+?)");
            mwMessage = mwMessage.Replace("$2", "(?:.+?)");
            mwMessage = mwMessage.Replace("$3", "(?:.+?)");
            mwMessage = mwMessage.Replace("$", @"\$");
            mwMessage = "^" + mwMessage + @"(?:: (?<comment>.*?))?$"; // Special:Log comments are preceded by a colon

            // Dirty code: Block log exceptions!
            if (mwMessageTitle == "Blocklogentry")
            {
                mwMessage = mwMessage.Replace("(?<item3>.+?)", "\\((?<item3>.+?)\\)");
                mwMessage = mwMessage.Replace(@"(?<item2>.+?)(?:: (?<comment>.*?))?$", "(?<item2>.+?)$");
            }

            try
            {
                _ = Regex.Match("", mwMessage);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to test-generate regex " + mwMessage + " for " + mwMessageTitle + "; " + e.Message);
            }

            if (reqCount >= 1)
            {
                if (!mwMessage.Contains(@"(?<item1>.+?)") && !nonStrict)
                {
                    throw new Exception("Regex " + mwMessageTitle + " requires one or more items but item1 not found in " + mwMessage);
                }

                if (reqCount >= 2)
                {
                    if (!mwMessage.Contains(@"(?<item2>.+?)") && !nonStrict)
                    {
                        throw new Exception("Regex " + mwMessageTitle + " requires two or more items but item2 not found in " + mwMessage);
                    }
                }
            }

            regexDict[destRegex] = mwMessage;
        }

        /// <summary>
        /// Gets the namespace code
        /// </summary>
        /// <param name="pageTitle">A page title, such as "Special:Helloworld" and "Helloworld"</param>
        /// <returns></returns>
        public int DetectNamespace(string pageTitle)
        {
            if (pageTitle.Contains(":"))
            {
                string nsLocal = pageTitle.Substring(0, pageTitle.IndexOf(':'));
                // Try to locate value (As fast as ContainsValue())
                foreach (DictionaryEntry de in namespaces)
                {
                    if ((string)de.Value == nsLocal)
                    {
                        return Convert.ToInt32(de.Key);
                    }
                }
            }
            // If no match for the prefix found, or if no colon,
            // assume main namespace
            return 0;
        }

        /// <summary>
        /// Returns a copy of the article title with the namespace translated into English
        /// </summary>
        /// <param name="originalTitle">Title in original (localized) language</param>
        /// <returns></returns>
        public static string TranslateNamespace(string project, string originalTitle)
        {
            if (originalTitle.Contains(":"))
            {
                string nsEnglish;

                // *Don't change these* unless it's a stopping bug. These names are made part of the title
                // in the watchlist and items database. (ie. don't change Image to File unless Image is broken)
                // When they do need to be changed, make sure to make note in the RELEASE-NOTES that databases
                // should be updated manually to keep all regexes and watchlists functional!
                switch (((Project)Program.prjlist[project]).DetectNamespace(originalTitle))
                {
                    case -2:
                        nsEnglish = "Media";
                        break;
                    case -1:
                        nsEnglish = "Special";
                        break;
                    case 1:
                        nsEnglish = "Talk";
                        break;
                    case 2:
                        nsEnglish = "User";
                        break;
                    case 3:
                        nsEnglish = "User talk";
                        break;
                    case 4:
                        nsEnglish = "Project";
                        break;
                    case 5:
                        nsEnglish = "Project talk";
                        break;
                    case 6:
                        nsEnglish = "File";
                        break;
                    case 7:
                        nsEnglish = "File talk";
                        break;
                    case 8:
                        nsEnglish = "MediaWiki";
                        break;
                    case 9:
                        nsEnglish = "MediaWiki talk";
                        break;
                    case 10:
                        nsEnglish = "Template";
                        break;
                    case 11:
                        nsEnglish = "Template talk";
                        break;
                    case 12:
                        nsEnglish = "Help";
                        break;
                    case 13:
                        nsEnglish = "Help talk";
                        break;
                    case 14:
                        nsEnglish = "Category";
                        break;
                    case 15:
                        nsEnglish = "Category talk";
                        break;
                    default:
                        return originalTitle;
                }

                // If we're still here, then nsEnglish has been set
                return nsEnglish + originalTitle.Substring(originalTitle.IndexOf(':'));
            }

            // Mainspace articles do not need translation
            return originalTitle;
        }
    }
}
