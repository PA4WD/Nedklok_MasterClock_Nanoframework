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
using System.Text;
using System.Threading;

namespace Nedklok_MasterClock_Nanoframework
{
    public class Program
    {
        private static GpioPin L9110S_1A, L9110S_1B, LED;

        private static WebSocketServer _webSocketServer;
        private static WebServer _webServer;

        private static int _waitSeconds;
        private static ClockState _clockState;

        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            var gpioController = new GpioController();
            LED = gpioController.OpenPin(2, PinMode.Output);
            L9110S_1A = gpioController.OpenPin(26, PinMode.Output);
            L9110S_1B = gpioController.OpenPin(25, PinMode.Output);

            try
            {
                ConnectWifi();

                _webSocketServer = new WebSocketServer(new WebSocketServerOptions()
                {
                    IsStandAlone = false,
                    Prefix = "/ws",
                    Port = 80
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

                    if (DateTime.UtcNow.Second != lastsecond)
                    {
                        lastsecond = DateTime.UtcNow.Second;
                        switch (_clockState)
                        {
                            case ClockState.Running:
                                TickSecond(1);
                                break;
                            case ClockState.Stopped:
                                break;
                            case ClockState.Adding:
                                break;
                            case ClockState.waiting:
                                _waitSeconds--;
                                //Debug.WriteLine($"wait for {_waitSeconds}");
                                if (_waitSeconds == 0)
                                {
                                    _clockState = ClockState.Running;
                                }
                                break;
                            default:
                                break;
                        }                        
                        //Debug.WriteLine($"Second past {lastsecond}");
                        //Debug.WriteLine("system time is: " + DateTime.UtcNow);
                        if (_webSocketServer.ClientsCount > 0)
                        {
                            _webSocketServer.BroadCast(DateTime.UtcNow.ToString());
                        }
                    }
                    Thread.Sleep(1);
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
                var url = e.Context.Request.RawUrl.ToLower();
                Debug.WriteLine($"Command received: {url}, Method: {e.Context.Request.HttpMethod}");

                if (url == "/ws" && e.Context.Request.Headers["Upgrade"] == "websocket")
                {
                    //Debug.WriteLine("Add websocket");
                    _webSocketServer.AddWebSocket(e.Context);
                }
                else if (url == "/favicon.ico")
                {
                    //WebServer.SendFileOverHTTP(e.Context.Response, "favicon.ico", Resources.GetBytes(Resources.BinaryResources.favicon));
                }
                else
                {
                    e.Context.Response.ContentType = "text/html; charset=utf-8";
                    WebServer.OutPutStream(e.Context.Response, Resources.GetString(Resources.StringResources.Index));
                }
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.InternalServerError);
            }
        }


        private static void WsServer_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //Debug.WriteLine($"ws data: {e.Frame.MessageType} - {e.Frame.Buffer}");
            var wsServer = (WebSocketServer)sender;
            if (e.Frame.MessageType == WebSocketMessageType.Text && e.Frame.MessageLength > 0)
            {
                //Debug.WriteLine($"Text messagetype - {Encoding.UTF8.GetString(e.Frame.Buffer, 0, e.Frame.MessageLength)}");

                string[] cmd = Encoding.UTF8.GetString(e.Frame.Buffer, 0, e.Frame.MessageLength).Split(' ');
                if (cmd[0] == "startclock")
                {
                    _clockState = ClockState.Running;
                }
                else if (cmd[0] == "stopclock")
                {
                    _clockState = ClockState.Stopped;
                }
                else if (cmd[0] == "add")
                {
                    int ticks = int.Parse(cmd[1]);
                    //Debug.WriteLine($"add ticks {ticks}");
                    _clockState = ClockState.Adding;
                    TickSecond(ticks);
                    _clockState = ClockState.Running;
                }
                else if (cmd[0] == "wait")
                {
                    int ticks = int.Parse(cmd[1]);
                    //Debug.WriteLine($"wait ticks {ticks}");
                    _clockState = ClockState.waiting;
                    _waitSeconds = ticks;
                }

                //wsServer.BroadCast(e.Frame.Buffer);
            }
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
        private static void TickSecond(int ticks)
        {
            for (int i = ticks; i > 0; i--)
            {
                //Debug.WriteLine($"Tick = {i}");

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

                LED.Toggle();

                if (i > 1)
                {
                    Thread.Sleep(200);
                }
            }
        }
    }
}
