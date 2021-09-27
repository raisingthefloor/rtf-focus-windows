﻿using CitadelCore.Extensions;
using CitadelCore.IO;
using CitadelCore.Logging;
using CitadelCore.Net.Http;
using CitadelCore.Net.Proxy;
using CitadelCore.Windows.Net.Proxy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsFirewallHelper;


namespace Morphic.BlockWeb
{
    internal class BlockService : ServiceBase
    {
        private static byte[] s_blockPageBytes;

        private static readonly ushort s_standardHttpPortNetworkOrder = (ushort)IPAddress.HostToNetworkOrder((short)80);
        private static readonly ushort s_standardHttpsPortNetworkOrder = (ushort)IPAddress.HostToNetworkOrder((short)443);
        private static readonly ushort s_altHttpPortNetworkOrder = (ushort)IPAddress.HostToNetworkOrder((short)8080);
        private static readonly ushort s_altHttpsPortNetworkOrder = (ushort)IPAddress.HostToNetworkOrder((short)8443);

        /// <summary>
        /// We pass this in to stream copy operations whenever the user has asked us to pull a
        /// payload from the net into memory. We set a hard limit of ~128 megs simply to avoid being
        /// vulnerable to an attack that would balloon memory consumption.
        /// </summary>
        private static readonly long s_maxInMemoryData = 128000000;

        private static HttpClient s_client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = false,
            ClientCertificateOptions = ClientCertificateOption.Automatic,
            AllowAutoRedirect = true,
            Proxy = null
        }, true);

        private static FirewallResponse OnFirewallCheck(FirewallRequest request)
        {
            // Only filter chrome, msedge and firefox
            var filtering = request.BinaryAbsolutePath.IndexOf("chrome", StringComparison.OrdinalIgnoreCase) != -1 ||
                request.BinaryAbsolutePath.IndexOf("msedge", StringComparison.OrdinalIgnoreCase) != -1 ||
                request.BinaryAbsolutePath.IndexOf("firefox", StringComparison.OrdinalIgnoreCase) != -1;
            
            //var filtering = true;

            if (filtering)
            {
                if (
                    request.RemotePort == s_standardHttpPortNetworkOrder || 
                    request.RemotePort == s_standardHttpsPortNetworkOrder ||
                    request.RemotePort == s_altHttpPortNetworkOrder ||
                    request.RemotePort == s_altHttpsPortNetworkOrder
                    )
                {
                    // Let's allow chrome to access TCP 80 and 443, but block all other ports.
                    //Console.WriteLine("Filtering application {0} destined for {1}", request.BinaryAbsolutePath, (ushort)IPAddress.HostToNetworkOrder((short)request.RemotePort));
                    return new FirewallResponse(CitadelCore.Net.Proxy.FirewallAction.FilterApplication);
                }
                else
                {
                    // Let's allow chrome to access TCP 80 and 443, but ignore all other
                    // ports. We want to allow non 80/443 requests to go through because
                    // this example now demonstrates the replay API, which will cause
                    // a bunch of browser tabs to open whenever you visit my website.
                    //
                    // If we filtered the replays back through the proxy, who knows
                    // what would happen! Actually that's not true, you'd invoke an infinite
                    // loopback, spawn a ton of browser tabs and then call me a bad programmer.
                    //Console.WriteLine("Ignoring internet for application {0} destined for {1}", request.BinaryAbsolutePath, (ushort)IPAddress.HostToNetworkOrder((short)request.RemotePort));
                    return new FirewallResponse(CitadelCore.Net.Proxy.FirewallAction.DontFilterApplication);
                }
            }

            // For all other applications, just let them access the internet without filtering.
            //Console.WriteLine("Not filtering application {0} destined for {1}", request.BinaryAbsolutePath, (ushort)IPAddress.HostToNetworkOrder((short)request.RemotePort));
            return new FirewallResponse(CitadelCore.Net.Proxy.FirewallAction.DontFilterApplication);
        }

        

        /// <summary>
        /// Called whenever a new request or response message is intercepted.
        /// </summary>
        /// <param name="messageInfo">
        /// The message info.
        /// </param>
        /// <remarks>
        /// In this callback we can do all kinds of crazy things, including fully modify the HTTP
        /// headers, the request target, etc etc.
        /// </remarks>
        private static void OnNewMessage(HttpMessageInfo messageInfo)
        {
            if (messageInfo.BodyContentType != string.Empty)
            {
                Console.WriteLine("New message with content of type: {0}\n\t{1}\n\t{2}", messageInfo.BodyContentType, messageInfo.Url, messageInfo.MessageProtocol);
            }
            else
            {
                Console.WriteLine("New message: {0}\n\t{1}", messageInfo.Url, messageInfo.MessageProtocol);
            }

            // Block only this casino website.
            if (messageInfo.Url.Host.Contains("facebook.com"))
            {
                messageInfo.MessageType = MessageType.Response;
                messageInfo.ProxyNextAction = ProxyNextAction.DropConnection;
                messageInfo.BodyContentType = "text/html";
                messageInfo.Body = s_blockPageBytes;
                return;
            }

            // Block only this casino website.
            if (messageInfo.Url.Host.Contains("youtube.com"))
            {
                messageInfo.MessageType = MessageType.Response;
                messageInfo.ProxyNextAction = ProxyNextAction.DropConnection;
                messageInfo.BodyContentType = "text/html";
                messageInfo.Body = s_blockPageBytes;
                return;
            }

            // By default, allow and ignore content, but not any responses to this content.
            messageInfo.ProxyNextAction = ProxyNextAction.AllowAndIgnoreContent;
        }

        private static void OnStreamedContentInspection(HttpMessageInfo messageInfo, StreamOperation operation, Memory<byte> buffer, out bool dropConnection)
        {
            dropConnection = false;
        }

        private static void OnWholeBodyContentInspection(HttpMessageInfo messageInfo)
        {

        }

        private static void OnReplayInspection(HttpMessageInfo messageInfo, string replayUrl, HttpReplayTerminationCallback cancellationCallback)
        {

        }

        private static Task OnManualFulfillmentCallback(HttpMessageInfo messageInfo, HttpContext context)
        {
            return Task.CompletedTask;
        }
        private static void GrantSelfFirewallAccess()
        {
            string processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            var hostAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            // We want to delete all rules that match our process name, so we can create new ones
            // that we know will work.
            var myRules = FirewallManager.Instance.Rules.Where(r => r.Name.Equals(processName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (myRules != null)
            {
                foreach (var rule in myRules)
                {
                    FirewallManager.Instance.Rules.Remove(rule);
                }
            }

            // Allow all inbound and outbound communications from our process.
            var inboundRule = FirewallManager.Instance.CreateApplicationRule(
                FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public,
                processName,
                WindowsFirewallHelper.FirewallAction.Allow, hostAssembly.Location
            );
            inboundRule.Direction = FirewallDirection.Inbound;

            FirewallManager.Instance.Rules.Add(inboundRule);

            var outboundRule = FirewallManager.Instance.CreateApplicationRule(
                FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public,
                processName,
                WindowsFirewallHelper.FirewallAction.Allow, hostAssembly.Location
            );
            outboundRule.Direction = FirewallDirection.Outbound;

            // Add the rules to the manager, which will commit them to Windows.
            FirewallManager.Instance.Rules.Add(outboundRule);
        }

        static ManualResetEvent manualResetEvent = new ManualResetEvent(false);

        public void StartBlock()
        {
            GrantSelfFirewallAccess();

            s_blockPageBytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BlockedPage.html"));

            // Let the user decide when to quit with ctrl+c.
            //var manualResetEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                manualResetEvent.Set();
                Console.WriteLine("Shutting Down");
            };

            // Hooking into these properties gives us an abstract interface where we may use
            // informational, warning and error messages generated by the internals of the proxy in
            // whatsoever way we see fit, though the design was to allow users to choose logging mechanisms.
            LoggerProxy.Default.OnInfo += (msg) =>
            {
                Console.WriteLine("INFO: {0}", msg);
            };

            LoggerProxy.Default.OnWarning += (msg) =>
            {
                Console.WriteLine("WARN: {0}", msg);
            };

            LoggerProxy.Default.OnError += (msg) =>
            {
                Console.WriteLine("ERRO: {0}", msg);
            };

            var cfg = new ProxyServerConfiguration
            {
                AuthorityName = "Morphic Focus Authority",
                FirewallCheckCallback = OnFirewallCheck,
                HttpMessageReplayInspectionCallback = OnReplayInspection,
                NewHttpMessageHandler = OnNewMessage,
                HttpMessageWholeBodyInspectionHandler = OnWholeBodyContentInspection,
                HttpMessageStreamedInspectionHandler = OnStreamedContentInspection,
                HttpExternalRequestHandlerCallback = OnManualFulfillmentCallback,
                BlockExternalProxies = true
            };


            // Just create the server.
            var proxyServer = new WindowsProxyServer(cfg);

            // Give it a kick.
            proxyServer.Start(0);

            // And you're up and running.
            Console.WriteLine("Proxy Running");

            Console.WriteLine("Listening for IPv4 HTTP/HTTPS connections on port {0}.", proxyServer.V4HttpEndpoint.Port);
            Console.WriteLine("Listening for IPv6 HTTP/HTTPS connections on port {0}.", proxyServer.V6HttpEndpoint.Port);

            // Don't exit on me yet fam.
            manualResetEvent.WaitOne();

            Console.WriteLine("Exiting.");

            // Stop if you must.
            proxyServer.Stop();
        }

        protected override void OnStart(string[] args)
        {
            Log("Starting");
            StartBlock();
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            manualResetEvent.Set();
            Console.WriteLine("Shutting Down");
            base.OnStop();
        }

        protected override void OnPause()
        {
            base.OnPause();
        }

        private const string _logFileLocation = @"E:\Focus\Focus .NET\rtf-focus-windows\Morphic.BlockWeb\publish\servicelog.txt";

        public BlockService()
        {
            Log("Starting");
            StartBlock();
        }

        public static void Log(string logMessage)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFileLocation));
                File.AppendAllText(_logFileLocation, DateTime.UtcNow.ToString() + " : " + logMessage + Environment.NewLine);
            }
            catch
            {

            }
        }
    }
}