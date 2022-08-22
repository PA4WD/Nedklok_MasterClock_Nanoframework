using nanoFramework.Networking;
using nanoFramework.WebServer;
using System;
using System.Device.Gpio;
using System.Device.Wifi;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Net.WebSockets.Server;
using System.Net.WebSockets.WebSocketFrame;
using System.Threading;

namespace Nedklok_MasterClock_Nanoframework
{
    public class Program
    {
        private static GpioPin L9110S_1A, L9110S_1B;

        private static WebSocketServer _webSocketServer;
        private static WebServer _webServer;

        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            var gpioController = new GpioController();
            GpioPin ledPin = gpioController.OpenPin(2, PinMode.Output);
            L9110S_1A = gpioController.OpenPin(26, PinMode.Output);
            L9110S_1B = gpioController.OpenPin(25, PinMode.Output);

            try
            {
                ConnectWifi();

                _webSocketServer = new WebSocketServer(new WebSocketServerOptions()
                {
                    //MaxClients = 5,
                    //IsStandAlone = true,
                    Prefix = "/ws",
                    //Port = 80
                });
                _webSocketServer.MessageReceived += WsServer_MessageReceived;
                _webSocketServer.Start();
                Debug.WriteLine($"WebSocket server is up and running, connect on: ws://{IPAddress.GetDefaultLocalAddress()}:{_webSocketServer.Port}{_webSocketServer.Prefix}");

                _webServer = new WebServer(80, HttpProtocol.Http);
                _webServer.CommandReceived += ServerCommandReceived;
                _webServer.Start();

                // The webapp url
                Debug.WriteLine($"http://{IPAddress.GetDefaultLocalAddress()}/");

                int lastsecond = 0;
                while (true)
                {
                    //Thread.Sleep(TimeSpan.FromSeconds(1));
                    if (DateTime.UtcNow.Second != lastsecond)
                    {
                        lastsecond = DateTime.UtcNow.Second;
                        TickSecond();
                        ledPin.Toggle();
                        Debug.WriteLine($"Second past {lastsecond}");
                        //Debug.WriteLine("system time is: " + DateTime.UtcNow);
                        //if (_wsServer.ClientsCount > 0)
                        //{
                        //    _wsServer.BroadCast(DateTime.UtcNow.ToString());
                        //}
                    }

                    //SSDP.SSDP.HandleSSDP();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex}");
            }

        }

        private static void ServerCommandReceived(object source, WebServerEventArgs e)
        {
            try
            {
                var url = e.Context.Request.RawUrl;
                Debug.WriteLine($"Command received: {url}, Method: {e.Context.Request.HttpMethod}");

                if (url.ToLower() == "/")
                {
                    //check if this is a websocket request or a page request 
                    if (e.Context.Request.Headers["Upgrade"] == "websocket")
                    {
                        Debug.WriteLine("Add websocket");
                        //Upgrade to a websocket
                        _webSocketServer.AddWebSocket(e.Context);
                    }
                    else
                    {
                        e.Context.Response.ContentType = "text/html";
                        WebServer.OutPutStream(e.Context.Response, Resources.GetString(Resources.StringResources.Index));
                    }
                }
                else if (url.ToLower() == "/ws")
                {
                    //check if this is a websocket request or a page request 
                    if (e.Context.Request.Headers["Upgrade"] == "websocket")
                    {
                        Debug.WriteLine("Add websocket");
                        //Upgrade to a websocket
                        _webSocketServer.AddWebSocket(e.Context);
                    }
                    else
                    {
                        e.Context.Response.ContentType = "text/html";
                        WebServer.OutPutStream(e.Context.Response, Resources.GetString(Resources.StringResources.Index));
                    }
                }

                //else if (url.ToLower() == "stopclock")
                //{
                //    Debug.WriteLine("Stop Clock");
                //    WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.OK);
                //}
                //else if (url.ToLower() == "startclock")
                //{
                //    Debug.WriteLine("Start Clock");
                //    WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.OK);
                //}
                else
                {
                    e.Context.Response.ContentType = "text/plain";
                    WebServer.OutPutStream(e.Context.Response, "Don't know what you want");
                }
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.InternalServerError);
            }
        }


        private static void WsServer_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Debug.WriteLine($"ws data: {e.Frame.MessageType} - {e.Frame.Buffer}");

            var wsServer = (WebSocketServer)sender;
            if (e.Frame.MessageType == WebSocketMessageType.Binary && e.Frame.MessageLength == 3)
            {
                //AtomLite.NeoPixel.Image.SetPixel(0, 0, Color.FromArgb(e.Frame.Buffer[0], e.Frame.Buffer[1], e.Frame.Buffer[2]));
                wsServer.BroadCast(e.Frame.Buffer);
            }

            //check the path of the request
            //if (e.Context.Request.RawUrl == "/ws")
            //{
            //    //check if this is a websocket request or a page request 
            //    if (e.Context.Request.Headers["Upgrade"] == "websocket")
            //    {
            //        //Upgrade to a websocket
            //        _wsServer.AddWebSocket(e.Context);
            //    }
            //}
        }


        public static bool ConnectWifi()
        {
            Debug.WriteLine("Waiting for network up and IP address...");

            CancellationTokenSource cs = new(60000);
            bool success = WifiNetworkHelper.ConnectDhcp(WifiCredentials.SSID, WifiCredentials.Password, WifiReconnectionKind.Automatic, requiresDateTime: true, token: cs.Token);
            if (!success)
            {
                Debug.WriteLine($"Can't get a proper IP address and DateTime, error: {NetworkHelper.Status}.");
                if (NetworkHelper.HelperException != null)
                {
                    Debug.WriteLine($"Exception: {NetworkHelper.HelperException}");
                }
                return false;
            }
            Debug.WriteLine("WIFI connected");

            Debug.WriteLine("Waiting for IP...");
            while (true)
            {
                NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
                if (ni.IPv4Address != null && ni.IPv4Address.Length > 0)
                {
                    if (ni.IPv4Address[0] != '0')
                    {
                        Debug.WriteLine($"We have an IP: {ni.IPv4Address}");
                        break;
                    }
                }
                Thread.Sleep(500);
            }
            DateTime now = DateTime.UtcNow;
            Debug.WriteLine($"Current Time Post-Connect: {now.Day}-{now.Month}-{now.Year} {now.Hour}:{now.Minute}:{now.Second}");
            return true;
        }


        private static bool positive;
        private static void TickSecond()
        {
            //new Thread(() =>
            //           {
            if (positive)
            {
                L9110S_1A.Write(1);
                positive = false;
            }
            else
            {
                L9110S_1B.Write(1);
                positive = true;
            }
            Thread.Sleep(200);

            L9110S_1A.Write(0);
            L9110S_1B.Write(0);
            //         }).Start();
        }
    }
}
