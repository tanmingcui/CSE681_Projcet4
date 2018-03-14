/////////////////////////////////////////////////////////////////////
// MockRepo.cs - Repocitory         to tackle with request         //
// ver 1.0                                                         //
// Tanming Cui                                                     //
/////////////////////////////////////////////////////////////////////
/*
 * Added references to:
 * - System.Collections.Generic;
 */
/*
 * This package provides:
 * ----------------------
 * - RepoEnvironment          : the repo and mother builder address
 * - ServiceEnvironment       : server-side path and address
 * - IPluggableComm           : interface used for message passing and file transfer
 * - CommMessage              : class representing serializable messages
 * 
 * Required Files:
 * ---------------
 * - IMPCommService.cs        : Service interface and Message definition
 * - Comm.cs                  : Sender and Receiver function
 * 
 * Public Interface:
 * ---------------
 * Comm comm                  : repo communication channel
 * CommMessage Msgsend        : message repo send
 * CommMessage Msgreceive     : message repo receive
 * List<string> allfiles      : all files need to be built
 * initialnizeDispather()     : tackle with different message command
 * Maintenance History:
 * --------------------
 * ver 1.0 : 06 Dec 2017
 * - first release
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using System.Diagnostics;
using SWTools;

namespace CSE681_Project4
{
    public class MockRepo
    {
        IFileMgr localFileMgr { get; set; } = null;
        Comm comm { get; set; } = null;
        Comm comm1 { get; set; } = null;
        string comm1addr { get; set; } = "";
        static int Reqid { get; set; } = 0;
        BlockingQueue<string> task { get; set; } = null;
        Dictionary<string, List<string>> filerelation = new Dictionary<string, List<string>>();

        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher { get; set; } =
          new Dictionary<string, Func<CommMessage, CommMessage>>();

        /*----< initialize server processing >-------------------------*/

        public MockRepo()
        {
            initializeEnvironment();
            localFileMgr = FileMgrFactory.create(FileMgrType.Local);
        }
        void initializefilerelation()
        {
            List<string> csfile = new List<string>();
            csfile.Add("Allright.cs");
            filerelation.Add("Allright.csproj", csfile);
            List<string> csfile1 = new List<string>();
            csfile1.Add("Program1.cs");
            csfile1.Add("Program.cs");
            filerelation.Add("ConsoleApp1.csproj", csfile1);
            List<string> csfile2 = new List<string>();
            csfile2.Add("TanmingClass1.cs");
            csfile2.Add("TanmingClass2.cs");
            csfile2.Add("TanmingClass3.cs");
            csfile2.Add("Tanmingtd.cs");
            filerelation.Add("TanmingTest.csproj", csfile2);
            List<string> csfile3 = new List<string>();
            csfile3.Add("tctest1.cs");
            csfile3.Add("tctest2.cs");
            csfile3.Add("tctest3.cs");
            filerelation.Add("tctest.csproj", csfile3);
            List<string> csfile4 = new List<string>();
            csfile4.Add("test1.cs");
            filerelation.Add("test1.csproj", csfile4);
        }
        /*----< set Environment properties needed by server >----------*/

        void initializeEnvironment()
        {
            Environment.root = RepoEnvironment.root;
            Environment.address = RepoEnvironment.address;
            Environment.port = RepoEnvironment.port;
            Environment.endPoint = RepoEnvironment.endPoint;
        }
        /*----< define how each message will be processed >------------*/
        void tryconnect1()
        {
            CommMessage tryconnect = new CommMessage(CommMessage.MessageType.connect);
            tryconnect.command = "tryconnect";
            tryconnect.from = comm1addr;
            tryconnect.to = ClientEnvironment.endPoint;
            tryconnect.arguments.Add("Sending files");
            comm1.postMessage(tryconnect);
        }
        void sendfileandreq(string files,CommMessage msg1)
        {
            CommMessage buildrequest = new CommMessage(CommMessage.MessageType.request);
            buildrequest.from = comm1addr;
            buildrequest.to = MotherbuilderEnvironment.endPoint;
            buildrequest.command = "buildrequest";
            List<string> testcsfile = new List<string>();
            testcsfile = filerelation[files];
            buildrequest.arguments.Add(files);
            foreach (string csfile in msg1.arguments) if (testcsfile.Contains(csfile)) buildrequest.arguments.Add(csfile);
            buildrequest.show();
            comm1.postMessage(buildrequest);
        }
        CommMessage replygetfilelist(CommMessage msg1)
        {
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = msg1.from; reply.from = msg1.to; reply.command = "getfilelist";
            reply.arguments = localFileMgr.getFiles().ToList<string>();
            return reply;
        }
        CommMessage replybuildrequest()
        {
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = ClientEnvironment.endPoint;
            reply.from = RepoEnvironment.endPoint;
            reply.command = "buildrequest";
            reply.arguments.Add("Request are received");
            return reply;
        }
        CommMessage replymakexml(CommMessage msg1)
        {
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = msg1.from;
            reply.from = msg1.to;
            reply.command = "makeXML";
            reply.arguments = localFileMgr.getFiles().ToList<string>();
            return reply;
        }
        void buildaxml(string testdriver1,CommMessage msg1)
        {
            TestReq testrequest = new TestReq();
            Reqid++;
            string xmlname = "BuildRequest" + Reqid + ".xml";
            xmlname = Path.Combine(RepoEnvironment.root, xmlname);
            testrequest.author = msg1.author;
            testrequest.testDriver.Add(testdriver1);
            List<string> testfiles = filerelation[testdriver1];
            foreach (string testfile in msg1.arguments) if (testfiles.Contains(testfile)) testrequest.testedFiles.Add(testfile);
            testrequest.makeRequest();
            testrequest.saveXml(xmlname);
        }
        void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> getfilelist = (CommMessage msg) =>
            {
                return replygetfilelist(msg);
            };
            messageDispatcher["getfilelist"] = getfilelist;
            Func<CommMessage, CommMessage> sendfile = (CommMessage msg) =>
            {
                try
                {
                    tryconnect1();
                    foreach (string file in msg.arguments)
                    {
                        string filepath = Path.Combine(MotherbuilderEnvironment.root, file);
                        if (!File.Exists(filepath)) comm1.postFile(file, RepoEnvironment.root, MotherbuilderEnvironment.root);
                    }
                    foreach (string file in msg.arguments) if (file.Contains(".csproj"))sendfileandreq(file,msg);
                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
                return replybuildrequest();
            };
            messageDispatcher["sendfile"] = sendfile;
            Func<CommMessage, CommMessage> makeXML = (CommMessage msg) =>
            {
                try{ foreach (string testdriver in msg.arguments)if (testdriver.Contains(".csproj"))buildaxml(testdriver, msg);}
                catch (Exception ex) { Console.WriteLine(ex.Message);}
                return replymakexml(msg);
            };
            messageDispatcher["makeXML"] = makeXML;
            Func<CommMessage, CommMessage> parsexml = (CommMessage msg) =>
            {
                parsexmlhelper(msg);
                CommMessage reply1 = new CommMessage(CommMessage.MessageType.request);
                reply1.from = RepoEnvironment.endPoint;
                reply1.to = ClientEnvironment.endPoint;
                reply1.command = "finishparsexml";
                reply1.arguments.Add("finish parse xml file");
                return reply1;
            };
            messageDispatcher["parsexml"] = parsexml;

        }
        void parsexmlhelper(CommMessage msg1)
        {
            try
            {
                foreach (string xmlfile in msg1.arguments) {
                    string filepath = Path.Combine(MotherbuilderEnvironment.root, xmlfile);
                    if (!File.Exists(filepath)) comm1.postFile(xmlfile, RepoEnvironment.root, MotherbuilderEnvironment.root);
                }
                foreach (string xmlfile in msg1.arguments)
                {
                    string xmlpath = Path.Combine(RepoEnvironment.root, xmlfile);
                    TestReq readxml = new TestReq();
                    readxml.loadXml(xmlpath);
                    List<string> tstdriver = new List<string>();
                    List<string> tstfiles = new List<string>();
                    tstfiles = readxml.parseList("tested");
                    tstdriver = readxml.parseDriverList("testDriver");
                    CommMessage reply = new CommMessage(CommMessage.MessageType.request);
                    reply.from = RepoEnvironment.endPoint;
                    reply.to = MotherbuilderEnvironment.endPoint;
                    reply.command = "buildrequest";
                    foreach (string file in tstfiles)
                    {
                        string filepath = Path.Combine(MotherbuilderEnvironment.root,file);
                        if(!File.Exists(filepath))comm1.postFile(file, RepoEnvironment.root, MotherbuilderEnvironment.root);
                        reply.arguments.Add(file);
                    }
                    foreach (string file in tstdriver)
                    {
                        string filepath = Path.Combine(MotherbuilderEnvironment.root, file);
                        if (!File.Exists(filepath)) comm1.postFile(file, RepoEnvironment.root, MotherbuilderEnvironment.root);
                        reply.arguments.Add(file);
                        
                    }
                    reply.show();
                    comm1.postMessage(reply);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        /*----< Server processing >------------------------------------*/
        /*
         * - all server processing is implemented with the simple loop, below,
         *   and the message dispatcher lambdas defined above.
         */

        static void Main(string[] args)
        {
            TestUtilities.title("Starting Mock Repo Server", '=');
            try
            {
                MockRepo server = new MockRepo();
                server.initializefilerelation();
                server.initializeDispatcher();
                server.comm = new Comm(RepoEnvironment.address, RepoEnvironment.port);
                server.comm1 = new Comm(RepoEnvironment.address, RepoEnvironment.port - 2000);
                server.comm1addr = "http://localhost:6090/IMessagePassingComm";
                server.task = new BlockingQueue<string>();
                while (true)
                {
                    CommMessage msg = server.comm.getMessage();
                    if (msg.type == CommMessage.MessageType.close) break;
                    msg.show();
                    if (msg.command == null) continue;
                    CommMessage reply = server.messageDispatcher[msg.command](msg);
                    reply.show();
                    server.comm.postMessage(reply);
                }
                server.comm.close();
                server.comm1.close();
            }
            catch (Exception ex) { Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
            }
        }
    }
}
