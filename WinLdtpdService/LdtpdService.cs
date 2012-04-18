/*
WinLDTP 1.0

@author: Nagappan Alagappan <nalagappan@vmware.com>
@copyright: Copyright (c) 2011-12 VMware Inc.,
@license: MIT license

http://ldtp.freedesktop.org

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
using System;
//using System.Threading;
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
        [STAThread]
        static void Main()
        {
            bool debug = false;
            string ldtpDebugEnv = Environment.GetEnvironmentVariable("LDTP_DEBUG");
            string listenAllInterface = Environment.GetEnvironmentVariable(
                "LDTP_LISTEN_ALL_INTERFACE");
            if (ldtpDebugEnv != null && ldtpDebugEnv.Length > 0)
                debug = true;
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
            listener.Prefixes.Add("http://localhost:4118/");
            listener.Prefixes.Add("http://+:4118/");
            // Listen on all possible IP address
            if (listenAllInterface != null && listenAllInterface.Length > 0)
            {
                if (debug)
                    Console.WriteLine("Listening on all interface");
                listener.Prefixes.Add("http://*:4118/");
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
                        context = null;
                        /*
                        // FIXME: If trying to do parallel process
                        // memory usage goes high and never comes back
                        // This is required for startprocessmonitor API
                        Thread parallelProcess = new Thread(delegate()
                        {
                            svc.ProcessRequest(context);
                            context = null;
                        });
                        parallelProcess.Start();
                        /* */
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
    }
}
