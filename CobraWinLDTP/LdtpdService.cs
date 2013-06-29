/*
 * Cobra WinLDTP 3.5
 * 
 * Author: Nagappan Alagappan <nalagappan@vmware.com>
 * Copyright: Copyright (c) 2011-13 VMware, Inc. All Rights Reserved.
 * License: MIT license
 * 
 * http://ldtp.freedesktop.org
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
 * of the Software, and to permit persons to whom the Software is furnished to do
 * so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
*/
using System;
using System.Threading;
//using System.Runtime.Remoting;
//using System.Runtime.Remoting.Channels;
//using System.Runtime.Remoting.Channels.Http;

// Additional namespace
using Ldtpd;
using System.Net;
using CookComputing.XmlRpc;

namespace WinLdtpdService
{
    class LdtpdService
    {
        public bool debug = false;
        string ldtpDebugEnv = Environment.GetEnvironmentVariable("LDTP_DEBUG");
        string ldtpPort = Environment.GetEnvironmentVariable("LDTP_SERVER_PORT");
        string listenAllInterface = Environment.GetEnvironmentVariable(
            "LDTP_LISTEN_ALL_INTERFACE");
        public Common common = null;
        public WindowList windowList = null;
        public HttpListener listener = null;
        public XmlRpcListenerService svc = null;
        LdtpdService()
        {
            if (!String.IsNullOrEmpty(ldtpDebugEnv))
                debug = true;
            if (String.IsNullOrEmpty(ldtpPort))
                ldtpPort = "4118";
            common = new Common(debug);
            windowList = new WindowList(common);
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:" + ldtpPort + "/");
            listener.Prefixes.Add("http://+:" + ldtpPort + "/");
            // Listen on all possible IP address
            if (listenAllInterface != null && listenAllInterface.Length > 0)
            {
                if (debug)
                    Console.WriteLine("Listening on all interface");
                listener.Prefixes.Add("http://*:" + ldtpPort + "/");
            }
            else
            {
                // For Windows 8, still you need to add firewall rules
                // Refer: README.txt
                if (debug)
                    Console.WriteLine("Listening only on local interface");
            }
            listener.Start();
            svc = new Core(windowList, common, debug);
        }

        void ListenerCallback(IAsyncResult result)
        {
            HttpListenerContext context;
            try
            {
                if (debug)
                    Console.WriteLine("Processing request");
                HttpListener listener = (HttpListener)result.AsyncState;
                // Call EndGetContext to complete the asynchronous operation.
                context = listener.EndGetContext(result);
                try
                {
                    svc.ProcessRequest(context);
                }
                catch (InvalidOperationException ex)
                {
                    common.LogMessage(ex);
                }
                context.Response.Close();
                listener = null;
            }
            catch (Exception ex)
            {
                common.LogMessage(ex);
            }
            finally
            {
                context = null;
                GC.Collect();
            }
        }

        static void MultiThreadExec()
        {
            IAsyncResult result;
            LdtpdService ldtpService = new LdtpdService();
            try
            {
                while (true)
                {
                    try
                    {
                        GC.Collect();
                        if (ldtpService.debug)
                            Console.WriteLine("Waiting for clients");
                        result = ldtpService.listener.BeginGetContext(
                            new AsyncCallback(ldtpService.ListenerCallback),
                            ldtpService.listener);
                        result.AsyncWaitHandle.WaitOne();
                    }
                    catch (InvalidOperationException ex)
                    {
                        ldtpService.common.LogMessage(ex);
                    }
                    finally
                    {
                        result = null;
                    }
                }
            }
            catch (Exception ex)
            {
                ldtpService.common.LogMessage(ex);
            }
            finally
            {
                ldtpService.svc = null;
                ldtpService.windowList = null;
                ldtpService.listener.Stop();
            }
        }

        static void SingleThreadExec()
        {
            bool debug = false;
            string ldtpDebugEnv = Environment.GetEnvironmentVariable("LDTP_DEBUG");
            string ldtpPort = Environment.GetEnvironmentVariable("LDTP_SERVER_PORT");
            string listenAllInterface = Environment.GetEnvironmentVariable(
                "LDTP_LISTEN_ALL_INTERFACE");
            if (!String.IsNullOrEmpty(ldtpDebugEnv))
                debug = true;
            if (String.IsNullOrEmpty(ldtpPort))
                ldtpPort = "4118";
            Common common = new Common(debug);
            /*
            // If planning to use Remoting instead of HTTP
            // use this commented portion of code
            // NOTE: To have this at work, you need to add your
            // app under Firewall
            IDictionary props = new Hashtable();
            props["name"] = "LdtpdService";
            props["port"] = 4118;
            HttpChannel channel = new HttpChannel(
               props,
               null,
               new XmlRpcServerFormatterSinkProvider()
            );
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(
              typeof(LdtpdMain),
              "service.rem",
              WellKnownObjectMode.Singleton);
            Console.WriteLine("Press <ENTER> to shutdown");
            Console.ReadLine();
            /**/
            WindowList windowList = new WindowList(common);
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:" + ldtpPort + "/");
            listener.Prefixes.Add("http://+:" + ldtpPort + "/");
            // Listen on all possible IP address
            if (String.IsNullOrEmpty(listenAllInterface))
            {
                if (debug)
                    Console.WriteLine("Listening on all interface");
                listener.Prefixes.Add("http://*:" + ldtpPort + "/");
            }
            else
            {
                // For Windows 8, still you need to add firewall rules
                // Refer: README.txt
                if (debug)
                    Console.WriteLine("Listening only on local interface");
            }
            listener.Start();
            XmlRpcListenerService svc = new Core(windowList, common, debug);
            try
            {
                while (true)
                {
                    GC.Collect();
                    try
                    {
                        if (debug)
                            Console.WriteLine("Waiting for clients");
                        HttpListenerContext context = listener.GetContext();
                        // Don't create LDTP instance here, this creates
                        // new object for every request !
                        // Moved before creating HttpListenerContext
                        //XmlRpcListenerService svc = new LdtpdMain();
                        if (debug)
                            Console.WriteLine("Processing request");
                        svc.ProcessRequest(context);
                        /*
                        // FIXME: If trying to do parallel process
                        // memory usage goes high and never comes back
                        // This is required for startprocessmonitor API
                        Thread parallelProcess = new Thread(delegate()
                        {
                            try
                            {
                                svc.ProcessRequest(context);
                            }
                            finally
                            {
                                context.Response.Close();
                                context = null;
                                GC.Collect();
                            }
                        });
                        parallelProcess.Start();
                        /* */
                        context.Response.Close();
                        context = null;
                    }
                    catch (InvalidOperationException ex)
                    {
                        common.LogMessage(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                common.LogMessage(ex);
            }
            finally
            {
                svc = null;
                windowList = null;
                listener.Stop();
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++ )
                {
                    switch (args[i])
                    {
                        case "-v":
                        case "--version":
                            Console.WriteLine("3.5.0");
                            return;
                        case "-d":
                        case "--debug":
                            Environment.SetEnvironmentVariable("LDTP_DEBUG", "2");
                            break;
                        case "-p":
                        case "--port":
                            // To fetch next port argument increment
                            i++;
                            try
                            {
                                // To check int type as argument
                                Convert.ToInt32(args[i]);
                                Environment.SetEnvironmentVariable("LDTP_SERVER_PORT",
                                    args[i]);
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Invalid port: " + args[i]);
                                return;
                            }
                            catch (IndexOutOfRangeException)
                            {
                                Console.WriteLine("Port number expected");
                                return;
                            }
                            break;
                        case "-h":
                        case "--help":
                            Console.WriteLine("[-v/--version] [-d/--debug] [-p/--port <port number>] [-h/--help]");
                            return;
                    }
                }
            }
            string ldtpParallelMemLeak = Environment.GetEnvironmentVariable(
                "LDTP_PARALLEL_MEM_LEAK");
            if (String.IsNullOrEmpty(ldtpParallelMemLeak))
                MultiThreadExec();
            else
                SingleThreadExec();
        }
    }
}
