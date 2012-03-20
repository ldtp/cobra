/*
WinLDTP 1.0

@author: Nagappan Alagappan <nalagappan@vmware.com>
@copyright: Copyright (c) 2011-12 VMware Inc.,
@license: LGPLv2

http://ldtp.freedesktop.org

This file may be distributed and/or modified under the terms of the GNU General
Public License version 2 as published by the Free Software Foundation. This file
is distributed without any warranty; without even the implied warranty of
merchantability or fitness for a particular purpose.

See 'README.txt' in the source distribution for more information.

Headers in this file shall remain intact.
*/
using System;
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
            string listenAllInterface = Environment.GetEnvironmentVariable("LDTP_LISTEN_ALL_INTERFACE");
            if (ldtpDebugEnv != null && ldtpDebugEnv.Length > 0)
                debug = true;
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
            ///*
            XmlRpcListenerService svc = new Core(debug);
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
            try
            {
                while (true)
                {
                    try
                    {
                        if (debug)
                            Console.WriteLine("Waiting for clients");
                        HttpListenerContext context = listener.GetContext();
                        // Don't create LDTP instance here, this creates
                        // new object for every request !
                        // Moved before creating HttpListener
                        //XmlRpcListenerService svc = new LdtpdMain();
                        if (debug)
                            Console.WriteLine("Processing request");
                        svc.ProcessRequest(context);
                        context = null;
                        GC.Collect();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                listener.Stop();
            }
            /**/
        }
    }
}
