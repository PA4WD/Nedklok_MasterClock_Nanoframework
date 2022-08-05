using nanoFramework.Networking;
using nanoFramework.WebServer;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
//using System.Net.WebSockets;
//using System.Net.WebSockets.Server;
using System.Threading;

namespace Nedklok_MasterClock_Nanoframework
{
    public class Program
    {
        private static GpioPin L9110S_1A, L9110S_1B;

        //private static WebSocketServer _wsServer;

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

                //Debug.WriteLine("system time is: " + DateTime.UtcNow);
                // set RTC
                //Rtc.SetSystemTime(new DateTime(2018, 2, 28, 10, 20, 30));

                Debug.WriteLine($"UTC system time is: {DateTime.UtcNow}");


                //DateTime(DateTime.UtcNow.Ticks, DateTimeKind.Local);

                //TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                //Debug.WriteLine(tzi.DisplayName);


                // Conversion from IANA to Windows
                //string ianaId1 = "America/Los_Angeles";
                //if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(ianaId1, out string winId1))
                //    throw new TimeZoneNotFoundException($"No Windows time zone found for "{ ianaId1 }".");
                //Console.WriteLine($"{ianaId1} => {winId1}");  // "America/Los_Angeles => Pacific Standard Time"


                Debug.WriteLine($"SNTP server 1 {Sntp.Server1}");
                Debug.WriteLine($"SNTP server 1 {Sntp.Server2}");
                Sntp.UpdateNow();
                Sntp.Start();

                using (WebServer server = new WebServer(80, HttpProtocol.Http, new Type[] { typeof(ControllerWeb) }))
                {
                    server.CommandReceived += ServerCommandReceived;
                    server.Start();
                }
                //WebSocketServer _wsServer = new WebSocketServer(new WebSocketServerOptions()
                //_wsServer = new WebSocketServer(new WebSocketServerOptions()
                //{
                //    MaxClients = 2,
                //    IsStandAlone = false
                //});
                //_wsServer.MessageReceived += WsServer_MessageReceived;
                //_wsServer.Start();


                //SetupSSDP();

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
                //check the path of the request
                if (e.Context.Request.RawUrl == "/ws")
                {
                    //check if this is a websocket request or a page request 
                    if (e.Context.Request.Headers["Upgrade"] == "websocket")
                    {
                        //Upgrade to a websocket
                        //_wsServer.AddWebSocket(e.Context);
                    }
                }
                else
                {
                    var url = e.Context.Request.RawUrl;
                    Debug.WriteLine($"Command received: {url}, Method: {e.Context.Request.HttpMethod}");

                    e.Context.Response.ContentType = "text/plain";
                    WebServer.OutPutStream(e.Context.Response, "Don't know what you want");
                }
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.InternalServerError);
            }
        }


        //private static void WsServer_MessageReceived(object sender, MessageReceivedEventArgs e)
        //{
        //    Debug.WriteLine($"ws data: {e.Frame.MessageType} - {e.Frame.Buffer}");

        //    //check the path of the request
        //    //if (e.Context.Request.RawUrl == "/ws")
        //    //{
        //    //    //check if this is a websocket request or a page request 
        //    //    if (e.Context.Request.Headers["Upgrade"] == "websocket")
        //    //    {
        //    //        //Upgrade to a websocket
        //    //        _wsServer.AddWebSocket(e.Context);
        //    //    }
        //    //}
        //}


        private static void ConnectWifi()
        {
            Debug.WriteLine("Waiting for network up and IP address...");

            CancellationTokenSource cs = new(60000);
            bool success = WifiNetworkHelper.ConnectDhcp(WifiCredentials.SSID, WifiCredentials.Password, requiresDateTime: true, token: cs.Token);
            if (!success)
            {
                Debug.WriteLine($"Can't get a proper IP address and DateTime, error: {WifiNetworkHelper.Status}.");
                if (WifiNetworkHelper.HelperException != null)
                {
                    Debug.WriteLine($"Exception: {WifiNetworkHelper.HelperException}");
                }
                return;
            }
            Debug.WriteLine("WIFI connected");
            CheckIP();
        }

        private static void CheckIP()
        {
            var myAddress = IPGlobalProperties.GetIPAddress();
            if (myAddress != IPAddress.Any && myAddress.ToString() != "1")
            {
                Debug.WriteLine($"We have and IP: {myAddress}");
            }
            else
            {
                Debug.WriteLine("No IP...");
            }
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
