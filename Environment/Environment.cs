///////////////////////////////////////////////////////////////////////////
// Environment.cs - defines environment properties for Client, Repo,     //
//builder, test harness                                                  // 
//                                                                       //
//                                                                       //
// ver 1.0                                                               //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017       //
///////////////////////////////////////////////////////////////////////////
/*
 * Maintenance History:
 * --------------------
 * ver 1.0 : 23 Oct 2017
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSE681_Project4
{
    public struct Environment
    {
        public static string root { get; set; }
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; }
        public static string address { get; set; }
        public static int port { get; set; }
        public static bool verbose { get; set; }
    }
    public struct ClientEnvironment
    {
        public static string root { get; set; } = "../../../ClientFiles/";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8080/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8080;
        public static bool verbose { get; set; } = false;
    }

    public struct RepoEnvironment
    {
        public static string root { get; set; } = "../../../RepoFiles/";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8090/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8090;
        public static bool verbose { get; set; } = false;
    }

    public struct MotherbuilderEnvironment
    {
        public static string root { get; set; } = "../../../BuilderFiles/";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:9090/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 9090;
        public static bool verbose { get; set; } = false;
    }
    public struct TestHarnessEnvironment
    {
        public static string root { get; set; } = "../../../TestHarnessFiles/";
        public static long blockSize { get; set; } = 1024;
        public static string endPoint { get; set; } = "http://localhost:8079/IMessagePassingComm";
        public static string address { get; set; } = "http://localhost";
        public static int port { get; set; } = 8079;
        public static bool verbose { get; set; } = false;
    }
}
