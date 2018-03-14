/////////////////////////////////////////////////////////////////////
// Childbuilder.cs - Childbuilder to tackle with request           //
// ver 1.0                                                         //
// Tanming Cui                                                     //
/////////////////////////////////////////////////////////////////////
/*
 * Added references to:
 * - System.Collections.Generic;
 * - System.IO;
 * - Microsoft.Build.BuildEngine;
 * - using Microsoft.Build.Framework;
 * - using Microsoft.Build.Execution;
 */
/*
 * This package provides:
 * ----------------------
 * 
 * - MotherbuilderEnvironment : server-side path and address
 * - CommMessage              : class representing serializable messages
 * - BlockingQueue            : manage ready child process and request
 * 
 * Required Files:
 * ---------------
 * - IMPCommService.cs        : Service interface and Message definition
 * - MPCommService.cs         : Sender and Receiver function
 * - Environment.cs           : The server environment
 * - BlockingQueue.cs         : The blocking queue function
 * 
 * Public Interface:
 * ---------------
 * Comm Childproc             : child process communication channel
 * CommMessage Msgreceive     : message child process receive
 * int ChildID                : Child id
 * string Childaddr           : combine child id and base address
 * CommMessage Msgsend        : message child process send
 * string childpath           : child directory path
 * Csprojbuild()              : build the .csproj file 
 * initialnizeDispather()     : initialnize the thread dispather
 *
 * Maintenance History:
 * --------------------
 * ver 1.0 : 06 Dec 2017
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using SWTools;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Execution;
using System.IO;

namespace CSE681_Project4
{
    public class Childbuilder
    {
        IFileMgr localFileMgr { get; set; } = null;
        Comm comm { get; set; } = null;
        Comm comm1 { get; set; } = null;
        string Childipaddr { get; set; } = "";
        string Childipaddr1 { get; set; } = "";
        string ChildID { get; set; } = "";
        string buildrespath { get; set; } = "";
        string buildres { get; set; } = "";
        string childpath { get; set; } = "";
        string outputpath { get; set; } = "obj/Any CPU/Debug/";

        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher =
          new Dictionary<string, Func<CommMessage, CommMessage>>();

        /*----< initialize server processing >-------------------------*/

        public Childbuilder() { }
        void sndtstreq()
        {
            CommMessage testreq = new CommMessage(CommMessage.MessageType.request);
            testreq.from = Childipaddr1;
            testreq.to = TestHarnessEnvironment.endPoint;
            testreq.command = "testrequest";
            testreq.arguments.Add(ChildID);
            testreq.show();
            comm1.postMessage(testreq);
        }
       
        /*----< define how each message will be processed >------------*/

        void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> filetobuild = (CommMessage msg) =>
            {
                string filefullpath = Path.GetFullPath(Path.Combine(childpath, msg.arguments[0]));
                CommMessage reply = new CommMessage(CommMessage.MessageType.request);
                if (Csprojbuild(filefullpath))
                {
                    reply.to = MotherbuilderEnvironment.endPoint;
                    reply.from = Childipaddr;
                    reply.command = "buildfinished";
                    reply.arguments.Add("Child " + ChildID + " finished building" + msg.arguments[0]);
                    reply.arguments.Add("Child " + ChildID +" "+buildres +" "+msg.arguments[0]);
                    reply.arguments.Add(childpath);
                    if (buildres== "Successfully Build")
                    {
                        try
                        {
                            sndtstreq();
                            string[] dllfiles = { };
                            string togetdll = Path.GetFullPath(outputpath); 
                            dllfiles = Directory.GetFiles(togetdll, "*.dll");
                            List<string> dllfilelist = new List<string>();
                            Console.WriteLine(outputpath);
                            for (int i = 0; i < dllfiles.Length; i++) dllfilelist.Add(Path.GetFileName(dllfiles[i]));
                            if (dllfilelist.Count > 0)
                            {
                                foreach (string dll in dllfilelist)comm1.postFile(dll,outputpath,TestHarnessEnvironment.root);  
                            }
                        }
                        catch (Exception ex) { Console.WriteLine(ex.Message); }
                    }
                }
                return reply;
            };
            messageDispatcher["filetobuild"] = filetobuild;
        }
        bool Csprojbuild(string csproj)
        {
            ConsoleLogger logger = new ConsoleLogger();
            Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
            GlobalProperty.Add("Configuration", "Debug");
            GlobalProperty.Add("OutputPath",childpath);
            GlobalProperty.Add("OutputType", "Library");
            GlobalProperty.Add("Platform", "Any CPU");
            try
            {
                //build c# program using msbuild
                BuildRequestData BuildRequest = new BuildRequestData(csproj, GlobalProperty, null, new string[] { "Build" }, null);
                BuildParameters bp = new BuildParameters();
                bp.Loggers = new List<ILogger> { logger };
                BuildResult buildResult = BuildManager.DefaultBuildManager.Build(bp, BuildRequest);
                if (buildResult.OverallResult == BuildResultCode.Success)
                {
                    buildres = "Successfully Build";
                    Console.WriteLine("Build successfully!");
                }
                else
                {
                    buildres = "Fail to build";
                    Console.WriteLine("Fail to build");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return false;
            }
        }
        /*----< Server processing >------------------------------------*/
        /*
         * - all server processing is implemented with the simple loop, below,
         *   and the message dispatcher lambdas defined above.
         */
        static void Main(string[] args)
        {
            TestUtilities.title("Starting Childproc" + args[0], '=');
            try
            {
                int childport = MotherbuilderEnvironment.port + Int32.Parse(args[0]);
                int childport1 = childport + 100;
                Childbuilder server = new Childbuilder();
                server.ChildID = args[0];
                server.childpath = "../../../Child" + args[0] + "/";
                server.outputpath = server.childpath + server.outputpath;
                if (!Directory.Exists(server.childpath)) Directory.CreateDirectory(server.childpath);
                server.initializeDispatcher();
                server.comm = new Comm(MotherbuilderEnvironment.address, childport);
                server.comm1 = new Comm(MotherbuilderEnvironment.address, childport+100);
                server.Childipaddr =  MotherbuilderEnvironment.address +":" +childport + "/IMessagePassingComm";
                server.Childipaddr1 = MotherbuilderEnvironment.address + ":" + childport1 + "/IMessagePassingComm";
                CommMessage greeting = new CommMessage(CommMessage.MessageType.reply);
                greeting.to = MotherbuilderEnvironment.endPoint;
                greeting.from = server.Childipaddr;
                greeting.command = "childready";
                greeting.arguments.Add(args[0]);
                greeting.arguments.Add(server.childpath);
                greeting.show();
                server.comm.postMessage(greeting);
                while (true)
                {
                    CommMessage msg = server.comm.getMessage();
                    if (msg.type == CommMessage.MessageType.close)
                        break;
                    msg.show();
                    if (msg.command == null)
                        continue;
                    CommMessage reply = server.messageDispatcher[msg.command](msg);
                    reply.show();
                    server.comm.postMessage(reply);
                }
                server.comm.close();
            }
            catch (Exception ex)
            {
                Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
            }
        }
    }
}