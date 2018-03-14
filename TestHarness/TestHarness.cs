/////////////////////////////////////////////////////////////////////
// TestHarness.cs - TestHarness to tackle with request             //
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
 * - TestharnessEnvironment   : server-side path and address
 * - IPluggableComm           : interface used for message passing and file transfer
 * - CommMessage              : class representing serializable messages
 * - BlockingQueue            : manage ready child process and request
 * 
 * Required Files:
 * ---------------
 * - IMPCommService.cs         : Service interface and Message definition
 * - MPCommService.cs          : Sender and Receiver function
 * - BlockingQueue.cs          : message send and receive
 * 
 * Model Operation:
 * ---------------
 * - Try to connect to Child builder process, and receive dll file that built success
 * - and return the test result.
 * 
 * 
 * Public Interface
 * ----------------
 * - initializeDispather()      : tackle different message
 * - Comm comm                  : Testharness cummunication channel
 * - CommMessage Msgsend        : the message mother builder will send
 * - CommMessage Msgreceive     : the message mother builder will receive
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
using System.Reflection;
using System.IO;

namespace CSE681_Project4
{
    public class TestHarness
    {
        IFileMgr localFileMgr { get; set; } = null;
        Comm comm { get; set; } = null;
        Comm comm1 { get; set; } = null;
        string comm1addr { get; set; } = null;
        string[] dllfile { get; set; } = { };

        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher =
          new Dictionary<string, Func<CommMessage, CommMessage>>();

        /*----< initialize server processing >-------------------------*/

        public TestHarness()
        {
            initializeEnvironment();
            localFileMgr = FileMgrFactory.create(FileMgrType.Local);
        }
        /*----< set Environment properties needed by server >----------*/

        void initializeEnvironment()
        {
            Environment.root = TestHarnessEnvironment.root;
            Environment.address = TestHarnessEnvironment.address;
            Environment.port = TestHarnessEnvironment.port;
            Environment.endPoint = TestHarnessEnvironment.endPoint;
        }
        /*----< define how each message will be processed >------------*/

        void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> testrequest = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                Loaddll();
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = ClientEnvironment.endPoint;
                reply.from = TestHarnessEnvironment.endPoint;
                reply.command = "testrequest";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                foreach(string dllfile in reply.arguments)
                {
                    comm1.postFile(dllfile, TestHarnessEnvironment.root, RepoEnvironment.root);
                }
                return reply;
            };
            messageDispatcher["testrequest"] = testrequest;
        }
        void Loaddll()
        {
            try
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromComponentLibFolder);
                // load all dll files in the test dircetory
                dllfile = Directory.GetFiles(TestHarnessEnvironment.root, "*.dll");
                for (int i = 0; i < dllfile.Length; i++)
                {
                    //asm load the dll file
                    Assembly asm = Assembly.LoadFile(Path.GetFullPath(dllfile[i]));
                    string fileName = Path.GetFileName(dllfile[i]);
                    Console.Write("\n  loaded {0}", fileName);

                    // exercise each tester found in assembly

                    Type[] types = asm.GetTypes();
                    foreach (Type t in types)
                    {
                        //run test on every method included in the dll
                        if (!runSimulatedTest(t, asm))
                            Console.Write("\n  test {0} failed to run", t.ToString());
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        bool runSimulatedTest(Type t, Assembly asm)
        {
            try
            {
                // announce test, get the method in every type from dll file,
                // as all the test files method name "test", and all the methods are
                //public and static, set the bindingflag static and public
                MethodInfo method = t.GetMethod("test", BindingFlags.Static | BindingFlags.Public);

                //the parameter of invoke is null because it is static method
                if (method != null) method.Invoke(null, null);

                // run test
                // to check whether the method of test files will return the value 2
                int Res = 0; ;
                method = t.GetMethod("test", BindingFlags.Static | BindingFlags.Public);
                if (method != null) Res = (int)method.Invoke(null, null);

                //if the method returns 2 indicates success
                //otherwise it fails to test
                Func<int, string> act = (int a) =>
                {
                    if (a == 2) return "passed";
                    return "failed";
                };
                Console.WriteLine("\n  test {0}", act(Res));
                CommMessage testres = new CommMessage(CommMessage.MessageType.request);
                testres.to = ClientEnvironment.endPoint;
                testres.from = comm1addr;
                testres.command = "testresult";
                testres.arguments.Add(act(Res));
                comm1.postMessage(testres);
            }
            catch (Exception ex)
            {
                Console.Write("\n  test failed with message \"{0}\"", ex.Message);
                return false;
            }
            return true;
        }

        Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args)
        {
                Console.Write("\n  called binding error event handler");
                string folderPath = TestHarnessEnvironment.root;
                string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
                if (!File.Exists(assemblyPath)) return null;
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;        
        }

        /*----< Server processing >------------------------------------*/
        /*
         * - all server processing is implemented with the simple loop, below,
         *   and the message dispatcher lambdas defined above.
         */
        static void Main(string[] args)
        {
            TestUtilities.title("Starting Test Harness", '=');
            try
            {
                TestHarness server = new TestHarness();
                server.initializeDispatcher();
                server.comm = new Comm(TestHarnessEnvironment.address, TestHarnessEnvironment.port);
                server.comm1 = new Comm(TestHarnessEnvironment.address,TestHarnessEnvironment.port-500);
                server.comm1addr = "http://localhost:7579/IMessagePassingComm";
                while (true)
                {
                    CommMessage msg = server.comm.getMessage();
                    if (msg.type == CommMessage.MessageType.closeReceiver)
                        break;
                    msg.show();
                    if (msg.command == null)
                        continue;
                    CommMessage reply = server.messageDispatcher[msg.command](msg);
                    reply.show();
                    server.comm.postMessage(reply);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
            }
        }
    }
}

