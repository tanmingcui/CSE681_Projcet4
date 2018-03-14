/////////////////////////////////////////////////////////////////////
// Motherbuilder.cs - Motherbuilder to tackle with request         //
// ver 1.0                                                         //
// Tanming Cui                                                     //
/////////////////////////////////////////////////////////////////////
/*
 * Added references to:
 * - using System.Diagnostics;
 * - using System.IO;
 */
/*
 * This package provides:
 * ----------------------
 * - MotherbuilderEnvironment : server-side path and address
 * - IPluggableComm           : interface used for message passing and file transfer
 * - CommMessage              : class representing serializable messages
 * - BlockingQueue            : manage ready child process and request
 * 
 * Required Files:
 * ---------------
 * - IMPCommService.cs         : Service interface and Message definition
 * - Comm.cs                   : Sender and Receiver function
 * - BlockingQueue.cs          : record the ready child Ip, testdriver name
 * 
 * Model Operation:
 * ---------------
 * - Try to connect to repo and access to the test files, keep waiting for the request
 * - from client, and use 2 bolcking to manage build request and test driver files, 
 * - the ready child will enqueue, the request from client will enqueue, the child on 
 * - the top of the queue will get the request from top of queue.
 * 
 * Public Interface
 * ----------------
 * - creatProcess(int i)        : generate i child process 
 * - tryconnect()               : try to connect to repo
 * - initial()                  : create mother builder communication channel
 * - int numofchild             : record the amount of child from client
 * - Comm motherbuilder         : mother builder cummunication channel
 * - CommMessage Msgsend        : the message mother builder will send
 * - CommMessage Msgreceive     : the message mother builder will receive
 * - BlockingQueue<string>task           :build request 
 * - BlockingQueue<string>readychild     :ready child ipaddress
 * Maintenance History:
 * --------------------
 * ver 1.0 : 06 Dec 2017
 * - first release
 */

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using SWTools;

namespace CSE681_Project4
{
    public class BuildServer
    {
        IFileMgr localFileMgr { get; set; } = null;
        Comm comm { get; set; } = null;
        Comm comm1 { get; set; } = null;
        BlockingQueue<List<string>> task { get; set; } = null;
        BlockingQueue<List<string>> readychild { get; set; } = null;
        List<string> message { get; set; } = null;
        bool starttobbuild { get; set; } = false;
        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher =
          new Dictionary<string, Func<CommMessage, CommMessage>>();

        /*----< initialize server processing >-------------------------*/

        public BuildServer()
        {
            initializeEnvironment();
            localFileMgr = FileMgrFactory.create(FileMgrType.Local);
        }
        /*----< set Environment properties needed by server >----------*/

        void initializeEnvironment()
        {
            Environment.root = MotherbuilderEnvironment.root;
            Environment.address = MotherbuilderEnvironment.address;
            Environment.port = MotherbuilderEnvironment.port;
            Environment.endPoint = MotherbuilderEnvironment.endPoint;
        }
        /*----< define how each message will be processed >------------*/
        bool createProcess(int i)
        {
            Process proc = new Process();
            string fileName = "..\\..\\..\\Childproc\\bin\\debug\\Childproc.exe";
            string absFileSpec = Path.GetFullPath(fileName);
            Console.Write("\n  attempting to start {0}", absFileSpec);
            try
            {
                Process.Start(fileName, i.ToString());
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }
        void replyclient()
        {
            CommMessage reply1 = new CommMessage(CommMessage.MessageType.reply);
            reply1.to = ClientEnvironment.endPoint;
            reply1.from = MotherbuilderEnvironment.endPoint;
            reply1.command = "startingchildprocess";
            reply1.arguments.Add("Mother builder starts to work");
            comm1.postMessage(reply1);
        }
        CommMessage startchildprochelper(CommMessage msg1)
        {
            message = new List<string>();
            for (int i = 1; i <= Int32.Parse(msg1.arguments[0]); i++)
            {
                if (createProcess(i)) message.Add("Child " + i + " started successfully");
                else message.Add("Child " + i + "Failed to start");
            }
            localFileMgr.currentPath = "";
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = ClientEnvironment.endPoint;
            reply.from = MotherbuilderEnvironment.endPoint;
            reply.command = "startingchildprocess";
            reply.arguments = message;
            replyclient();
            return reply;
        }
        CommMessage childreadyhelper(CommMessage msg1)
        {
            List<string> record = new List<string>();
            record.Add(msg1.from);
            foreach (string childpath in msg1.arguments) if (childpath.Contains("/Child")) record.Add(childpath);
            readychild.enQ(record);
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = ClientEnvironment.endPoint;
            reply.from = MotherbuilderEnvironment.endPoint;
            reply.command = "childprocessready";
            reply.arguments.Add("Child " + msg1.arguments[0] + " is ready");
            return reply;
        }
        void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> startchildproc = (CommMessage msg) =>
            {
                return startchildprochelper(msg);
            };
            messageDispatcher["startchildproc"] = startchildproc;
            Func<CommMessage, CommMessage> childready = (CommMessage msg) =>
            {
                return childreadyhelper(msg);
            };
            messageDispatcher["childready"] = childready;
            Func<CommMessage, CommMessage> buildfinished = (CommMessage msg) =>
            {
                List<string> record = new List<string>();
                record.Add(msg.from);
                foreach (string childpath in msg.arguments)if (childpath.Contains("/Child")) record.Add(childpath);
                readychild.enQ(record);
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = ClientEnvironment.endPoint;
                reply.from = MotherbuilderEnvironment.endPoint;
                reply.command = "childfinishbuild";
                foreach (string news in msg.arguments) reply.arguments.Add(news);
                return reply;
            };
            messageDispatcher["buildfinished"] = buildfinished;
            Func<CommMessage, CommMessage> buildrequest = (CommMessage msg) =>
            {
                task.enQ(msg.arguments);
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = ClientEnvironment.endPoint;
                reply.from = MotherbuilderEnvironment.endPoint;
                reply.command = "buildrequest";
                reply.arguments.Add("Files are received");
                return reply;
            };
            messageDispatcher["buildrequest"] = buildrequest;
            Func<CommMessage, CommMessage> tobuild = (CommMessage msg) =>
             {
                 return tobuildhelper();
             };
            messageDispatcher["tobuild"] = tobuild;
        }
        CommMessage tobuildhelper()
        {
            starttobbuild = true;
            CommMessage buildfinish = new CommMessage(CommMessage.MessageType.reply);
            buildfinish.from = MotherbuilderEnvironment.endPoint;
            buildfinish.to = ClientEnvironment.endPoint;
            buildfinish.command = "tobuild";
            buildfinish.arguments.Add("Build started");
            return buildfinish;
        }
        /*----< Server processing >------------------------------------*/
        /*
         * - all server processing is implemented with the simple loop, below,
         *   and the message dispatcher lambdas defined above.
         */
        static void Main(string[] args)
        {
            TestUtilities.title("Starting Motherbuilder Server", '=');
            try
            {
                BuildServer server = new BuildServer();
                server.starttobbuild = false;
                server.initializeDispatcher();
                server.comm = new Comm(MotherbuilderEnvironment.address, MotherbuilderEnvironment.port);
                server.comm1 = new Comm(MotherbuilderEnvironment.address,6060);
                string comm1addr = "http://localhost:6060/IMessagePassingComm";
                server.task = new BlockingQueue<List<string>>();
                server.readychild = new BlockingQueue<List<string>>();
                server.message = new List<string>();
                while (true)
                {
                    CommMessage msg = server.comm.getMessage();
                    if (msg.command == "closeitself")break;
                    msg.show();
                    if (msg.command == null) continue;
                    CommMessage reply = server.messageDispatcher[msg.command](msg);
                    server.comm.postMessage(reply);
                    if (server.starttobbuild)
                    {
                        if (server.task.size() > 0 && server.readychild.size() > 0)
                        {
                            List<string> childinfo = new List<string>();
                            childinfo = server.readychild.deQ();
                            List<string> filegroup = new List<string>();
                            filegroup = server.task.deQ();
                            foreach (string csfile in filegroup)
                            {
                                string csfilepath = Path.Combine(childinfo.Last(),csfile);
                                if(!File.Exists(csfilepath))server.comm1.postFile(csfile, MotherbuilderEnvironment.root, childinfo.Last());
                            }
                            CommMessage Msgsend = new CommMessage(CommMessage.MessageType.request);
                            Msgsend.from = comm1addr;
                            Msgsend.to = childinfo.First();
                            Msgsend.command = "filetobuild";
                            Msgsend.arguments = filegroup;
                            Msgsend.show();
                            server.comm1.postMessage(Msgsend);  
                        }
                    }
                }
                server.comm.close();
                server.comm1.close();
            }
            catch (Exception ex) { Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message); }      
        }
    }
}
