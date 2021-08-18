// This source code is a part of project violet-server.
// Copyright (C) 2021. violet-team. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace ex_comment
{
    // https://gist.github.com/define-private-public/d05bc52dd0bed1c4699d49e2737e80e7
    class Program
    {
        public static HttpListener listener;
        public static string url = "http://localhost:6974/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static List<KeyValuePair<int, List<Tuple<DateTime, string, string>>>> comments;

        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                {
                    Console.WriteLine("Shutdown requested");
                    runServer = false;
                }

                var result = string.Join("\r\n---------------------------------------\r\n",
                    comments.Select(x => string.Join("\r\n", x.Value.Where(x =>
                        x.Item3.Contains(HttpUtility.UrlDecode(req.Url.AbsolutePath).Substring(1))).Select(y => $"({x.Key}) [{y.Item2}] {y.Item3}"))).Where(x => x.Length > 0));

                byte[] data = Encoding.UTF8.GetBytes(result);
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }

        static void Main(string[] args)
        {
            var articles =
                JsonConvert.DeserializeObject<Dictionary<int, List<Tuple<DateTime, string, string>>>>(
                    File.ReadAllText("excomment-zip.json"));
            comments = articles.ToList();
            comments.Sort((x, y) => x.Key.CompareTo(y.Key));

            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            listener.Close();
        }
    }
}
