using nanoFramework.WebServer;
using System;

namespace Nedklok_MasterClock_Nanoframework
{
    internal class ControllerWeb
    {
        //[Route("test"), Route("Test2"), Route("tEst42"), Route("TEST")]
        [Route("/")]
        //[CaseSensitive]
        [Method("GET")]
        public void RoutePostTest(WebServerEventArgs e)
        {
            e.Context.Response.ContentType = "text/html";
            WebServer.OutPutStream(e.Context.Response, Resources.GetString(Resources.StringResources.Index));
        }

        [Route("favicon.ico")]
        [Method("GET")]
        public void RouteFavoicon(WebServerEventArgs e)
        {
            WebServer.SendFileOverHTTP(e.Context.Response, "favicon.ico", Resources.GetBytes(Resources.BinaryResources.favicon));
        }

        [Route("time")]
        [Method("GET")]
        public void RouteAnyTest(WebServerEventArgs e)
        {
            e.Context.Response.ContentType = "text/plain";
            WebServer.OutPutStream(e.Context.Response, DateTime.UtcNow.ToString());
        }

        //[Route("description.xml")]
        //[Method("GET")]
        //public void SSDPDescription(WebServerEventArgs e)
        //{
        //    //WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.OK);
        //    e.Context.Response.ContentType = "text/xml";
        //    WebServer.OutPutStream(e.Context.Response, SSDP.SSDPServer.GetSSDPDesciption());
        //}

        //[Route("urlencode")]
        //public void UrlEncode(WebServerEventArgs e)
        //{
        //    var rawUrl = e.Context.Request.RawUrl;
        //    var paramsUrl = WebServer.DecodeParam(rawUrl);
        //    string ret = "Parameters | Encoded | Decoded";
        //    foreach (var param in paramsUrl)
        //    {
        //        ret += $"{param.Name} | ";
        //        ret += $"{param.Value} | ";
        //        // Need to wait for latest version of System.Net
        //        // See https://github.com/nanoframework/lib-nanoFramework.System.Net.Http/blob/develop/nanoFramework.System.Net.Http/Http/System.Net.HttpUtility.cs
        //        ret += $"{System.Web.HttpUtility.UrlDecode(param.Value)}";
        //        ret += "\r\n";
        //    }
        //    WebServer.OutPutStream(e.Context.Response, ret);
        //}
    }
}