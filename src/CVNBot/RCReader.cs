using System;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using Meebey.SmartIrc4net;

namespace CVNBot
{
    struct RCEvent
    {
        public enum EventType
        {
            delete, restore, upload, block, unblock, edit, protect, unprotect,
            move, rollback, newuser, import, unknown, newuser2, autocreate,
            modifyprotect
        }

        public string interwikiLink;
        public string project;
        public string title;
        public string url;
        public string user;
        public bool minor;
        public bool newpage;
        public bool botflag;
        public int szdiff;
        public string comment;
        public EventType eventtype;
        public string blockLength;
        public string movedTo;

        public override string ToString()
        {
            return "[" + project + "] " + user + " edited [[" + title + "]] (" + szdiff.ToString() + ") " + url + " " + comment;
        }
    }

    class RCReader
    {
        public IrcClient rcirc = new IrcClient();
        public DateTime lastMessage = DateTime.Now;

        // RC parsing regexen
        static readonly Regex stripColours = new Regex(@"\x04\d{0,2}\*?");
        static readonly Regex stripColours2 = new Regex(@"\x03\d{0,2}");
        static readonly Regex stripBold = new Regex(@"\x02");
        static readonly Regex rszDiff = new Regex(@"\(([\+\-])([0-9]+)\)");

        static readonly ILog logger = LogManager.GetLogger("CVNBot.RCReader");

        public void InitiateConnection()
        {
            Thread.CurrentThread.Name = "RCReader";

            logger.Info("Thread started");

            // Set up RCReader
            rcirc.Encoding = System.Text.Encoding.UTF8;
            rcirc.AutoReconnect = true;
            rcirc.AutoRejoin = true;

            rcirc.OnChannelMessage += Rcirc_OnChannelMessage;
            rcirc.OnConnected += Rcirc_OnConnected;

            try
            {
                rcirc.Connect(Program.config.ircReaderServerName, 6667);
            }
            catch (ConnectionException e)
            {
                logger.Warn("Could not connect", e);
                return;
            }

            try
            {
                rcirc.Login(Program.config.readerBotNick, "CVNBotReader", 4, "CVNBotReader");

                logger.InfoFormat("Joining {0}", Program.config.readerFeedChannel);
                rcirc.RfcJoin(Program.config.readerFeedChannel);

                // Enter loop
                rcirc.Listen();
                // When Listen() returns the IRC session is over
                rcirc.Disconnect();
            }
            catch (ConnectionException)
            {
                // Final disconnect may throw, ignore.
                return;
            }
        }

        void Rcirc_OnConnected(object sender, EventArgs e)
        {
            logger.InfoFormat("Connected to {0}", Program.config.ircReaderServerName);
        }

        void Rcirc_OnChannelMessage(object sender, IrcEventArgs e)
        {
            lastMessage = DateTime.Now;

            // Based on RCParser.py->parseRCmsg()
            string strippedmsg = stripBold.Replace(stripColours.Replace(CVNBotUtils.ReplaceStrMax(e.Data.Message, '\x03', '\x04', 16), "\x03"), "");
            string[] fields = strippedmsg.Split(new char[] { '\x03' }, 17);
            if (fields.Length == 17)
            {
                if (fields[16].EndsWith("\x03"))
                    fields[16] = fields[16].Substring(0, fields[16].Length - 1);
            }
            else
            {
                // Probably really long article title or something that got cut off; we can't handle these
                return;
            }

            try
            {
                RCEvent rce;
                rce.eventtype = RCEvent.EventType.unknown;
                rce.blockLength = "";
                rce.movedTo = "";
                rce.project = fields[0].Trim() ?? Program.config.defaultProject;

                if (!Program.prjlist.ContainsKey(rce.project))
                {
                    Program.prjlist.AddNewProject(rce.project);
                    Program.listman.ConfigGetAdmins(rce.project);
                    Program.listman.ConfigGetBots(rce.project);
                }

                string subdomain = rce.project.Substring(0, rce.project.Length - Program.config.projectSuffix.Length);

                rce.interwikiLink = Program.config.interwikiPrefix + subdomain + ":";
                rce.title = Project.TranslateNamespace(rce.project, fields[4]);
                rce.url = fields[8];
                rce.user = fields[12];
                Project project = ((Project)Program.prjlist[rce.project]);
                // At the moment, fields[14] contains IRC colour codes. For plain edits, remove just the \x03's. For logs, remove using the regex.
                Match titlemo = project.rSpecialLogRegex.Match(fields[4]);
                if (!titlemo.Success)
                {
                    // This is a regular edit
                    rce.minor = fields[6].Contains("M");
                    rce.newpage = fields[6].Contains("N");
                    rce.botflag = fields[6].Contains("B");
                    rce.eventtype = RCEvent.EventType.edit;
                    rce.comment = fields[16].Replace("\x03", "");
                }
                else
                {
                    // This is a log edit; check for type
                    string logType = titlemo.Groups[1].Captures[0].Value;
                    // Fix comments
                    rce.comment = stripColours2.Replace(fields[16], "");
                    switch (logType)
                    {
                        case "newusers":
                            // Could be a user creating their own account, or a user creating a sockpuppet

                            if (fields[4].Contains("create2"))
                            {
                                Match mc2 = project.rCreate2Regex.Match(rce.comment);
                                if (mc2.Success)
                                {
                                    rce.title = mc2.Groups[1].Captures[0].Value;
                                    rce.eventtype = RCEvent.EventType.newuser2;
                                }
                                else
                                {
                                    logger.Warn("Unmatched create2 event in " + rce.project + ": " + e.Data.Message);
                                }
                            }
                            else
                            {
                                if (fields[4].Contains("autocreate"))
                                {
                                    rce.eventtype = RCEvent.EventType.autocreate;
                                }
                                else
                                {
                                    rce.eventtype = RCEvent.EventType.newuser;
                                }
                            }
                            break;
                        case "block":
                            if (fields[4].Contains("unblock"))
                            {
                                Match ubm = project.runblockRegex.Match(rce.comment);
                                if (ubm.Success)
                                {
                                    rce.eventtype = RCEvent.EventType.unblock;
                                    rce.title = ubm.Groups["item1"].Captures[0].Value;
                                    try
                                    {
                                        rce.comment = ubm.Groups["comment"].Captures[0].Value;
                                    }
                                    catch (ArgumentOutOfRangeException) { }
                                }
                                else
                                {
                                    logger.Warn("Unmatched block/unblock type in " + rce.project + ": " + e.Data.Message);
                                    return;
                                }
                            }
                            else if (fields[4].Contains("reblock"))
                            {
                                Match rbm = project.rreblockRegex.Match(rce.comment);
                                if (rbm.Success)
                                {
                                    // Treat reblock the same as a new block for simplicity
                                    rce.eventtype = RCEvent.EventType.block;
                                    rce.title = rbm.Groups["item1"].Captures[0].Value;
                                }
                                else
                                {
                                    logger.Warn("Unmatched block/reblock type in " + rce.project + ": " + e.Data.Message);
                                    return;
                                }
                            }
                            else
                            {
                                Match bm = project.rblockRegex.Match(rce.comment);
                                if (bm.Success)
                                {
                                    rce.eventtype = RCEvent.EventType.block;
                                    rce.title = bm.Groups["item1"].Captures[0].Value;
                                    // Assume default value of 24 hours in case the on-wiki message override
                                    // is missing expiry ($2) from its interface messag
                                    rce.blockLength = "24 hours";
                                    try
                                    {
                                        rce.blockLength = bm.Groups["item2"].Captures[0].Value;
                                    }
                                    catch (ArgumentOutOfRangeException) { }
                                    try
                                    {
                                        rce.comment = bm.Groups["comment"].Captures[0].Value;
                                    }
                                    catch (ArgumentOutOfRangeException) { }
                                }
                                else
                                {
                                    logger.Warn("Unmatched block type in " + rce.project + ": " + e.Data.Message);
                                    return;
                                }
                            }
                            break;
                        case "protect":
                            // Could be a protect, modifyprotect or unprotect; need to parse regex
                            Match pm = project.rprotectRegex.Match(rce.comment);
                            Match modpm = project.rmodifyprotectRegex.Match(rce.comment);
                            Match upm = project.runprotectRegex.Match(rce.comment);
                            if (pm.Success)
                            {
                                rce.eventtype = RCEvent.EventType.protect;
                                rce.title = Project.TranslateNamespace(rce.project, pm.Groups["item1"].Captures[0].Value);
                                try
                                {
                                    rce.comment = pm.Groups["comment"].Captures[0].Value;
                                }
                                catch (ArgumentOutOfRangeException) { }
                            }
                            else if (modpm.Success)
                            {
                                rce.eventtype = RCEvent.EventType.modifyprotect;
                                rce.title = Project.TranslateNamespace(rce.project, modpm.Groups["item1"].Captures[0].Value);
                                try
                                {
                                    rce.comment = modpm.Groups["comment"].Captures[0].Value;
                                }
                                catch (ArgumentOutOfRangeException) { }
                            }
                            else
                            {
                                if (upm.Success)
                                {
                                    rce.eventtype = RCEvent.EventType.unprotect;
                                    rce.title = Project.TranslateNamespace(rce.project, upm.Groups["item1"].Captures[0].Value);
                                    try
                                    {
                                        rce.comment = upm.Groups["comment"].Captures[0].Value;
                                    }
                                    catch (ArgumentOutOfRangeException) { }
                                }
                                else
                                {
                                    logger.Warn("Unmatched protect type in " + rce.project + ": " + e.Data.Message);
                                    return;
                                }
                            }
                            break;
                        case "rights":
                            // Ignore event
                            return;
                        //break;
                        case "delete":
                            // Could be a delete or restore; need to parse regex
                            Match dm = project.rdeleteRegex.Match(rce.comment);
                            if (dm.Success)
                            {
                                rce.eventtype = RCEvent.EventType.delete;
                                rce.title = Project.TranslateNamespace(rce.project, dm.Groups["item1"].Captures[0].Value);
                                try
                                {
                                    rce.comment = dm.Groups["comment"].Captures[0].Value;
                                }
                                catch (ArgumentOutOfRangeException) { }
                            }
                            else
                            {
                                Match udm = project.rrestoreRegex.Match(rce.comment);
                                if (udm.Success)
                                {
                                    rce.eventtype = RCEvent.EventType.restore;
                                    rce.title = Project.TranslateNamespace(rce.project, udm.Groups["item1"].Captures[0].Value);
                                    try
                                    {
                                        rce.comment = udm.Groups["comment"].Captures[0].Value;
                                    }
                                    catch (ArgumentOutOfRangeException) { }
                                }
                                else
                                {
                                    // Could be 'revision' (change visibility of revision) or something else
                                    // Ignore event
                                    return;
                                }
                            }
                            break;
                        case "upload":
                            Match um = project.ruploadRegex.Match(rce.comment);
                            if (um.Success)
                            {
                                rce.eventtype = RCEvent.EventType.upload;
                                rce.title = Project.TranslateNamespace(rce.project, um.Groups["item1"].Captures[0].Value);
                                try
                                {
                                    rce.comment = um.Groups["comment"].Captures[0].Value;
                                }
                                catch (ArgumentOutOfRangeException) { }
                            }
                            else
                            {
                                // Could be 'overwrite' (upload new version) or something else
                                // Ignore event
                                return;
                            }
                            break;
                        case "move":
                            //Is a move
                            rce.eventtype = RCEvent.EventType.move;
                            //Check "move over redirect" first: it's longer, and plain "move" may match both (e.g., en-default)
                            Match mrm = project.rmoveredirRegex.Match(rce.comment);
                            if (mrm.Success)
                            {
                                rce.title = Project.TranslateNamespace(rce.project, mrm.Groups["item1"].Captures[0].Value);
                                rce.movedTo = Project.TranslateNamespace(rce.project, mrm.Groups["item2"].Captures[0].Value);
                                //We use the unused blockLength field to store our "moved from" URL
                                rce.blockLength = project.rooturl + "wiki/" + CVNBotUtils.WikiEncode(mrm.Groups["item1"].Captures[0].Value);
                                try
                                {
                                    rce.comment = mrm.Groups["comment"].Captures[0].Value;
                                }
                                catch (ArgumentOutOfRangeException) { }
                            }
                            else
                            {
                                Match mm = project.rmoveRegex.Match(rce.comment);
                                if (mm.Success)
                                {
                                    rce.title = Project.TranslateNamespace(rce.project, mm.Groups["item1"].Captures[0].Value);
                                    rce.movedTo = Project.TranslateNamespace(rce.project, mm.Groups["item2"].Captures[0].Value);
                                    //We use the unused blockLength field to store our "moved from" URL
                                    rce.blockLength = project.rooturl + "wiki/" + CVNBotUtils.WikiEncode(mm.Groups["item1"].Captures[0].Value);
                                    try
                                    {
                                        rce.comment = mm.Groups["comment"].Captures[0].Value;
                                    }
                                    catch (ArgumentOutOfRangeException) { }
                                }
                                else
                                {
                                    logger.Warn("Unmatched move type in " + rce.project + ": " + e.Data.Message);
                                    return;
                                }
                            }
                            break;
                        case "import":
                            //rce.eventtype = RCEvent.EventType.import;
                            // Ignore event
                            return;
                        //break;
                        default:
                            // Ignore event
                            return;
                    }
                    // These flags don't apply to log events, but must be initialized
                    rce.minor = false;
                    rce.newpage = false;
                    rce.botflag = false;
                }

                // Deal with the diff size
                Match n = rszDiff.Match(fields[15]);
                if (n.Success)
                {
                    if (n.Groups[1].Captures[0].Value == "+")
                        rce.szdiff = Convert.ToInt32(n.Groups[2].Captures[0].Value);
                    else
                        rce.szdiff = 0 - Convert.ToInt32(n.Groups[2].Captures[0].Value);
                }
                else
                    rce.szdiff = 0;

                try
                {
                    Program.ReactToRCEvent(rce);
                }
                catch (Exception exce)
                {
                    logger.Error("Failed to handle RCEvent", exce);
                    Program.BroadcastDD("ERROR", "ReactorException", exce.Message, e.Data.Channel + " " + e.Data.Message);
                }
            }
            catch (ArgumentOutOfRangeException eor)
            {
                // Broadcast this for Distributed Debugging
                logger.Error("Failed to process incoming message", eor);
                Program.BroadcastDD("ERROR", "RCR_AOORE", eor.Message, e.Data.Channel + "/" + e.Data.Message
                    + "Fields: " + fields);
            }
        }

    }
}
