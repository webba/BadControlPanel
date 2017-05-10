using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;


namespace GameControlPanel
{
    class LocalHttpListener
    {
        public const string UriAddress = "http://localhost:8888/";
        HttpListener _httpListener;

        public LocalHttpListener()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(LocalHttpListener.UriAddress);
        }

        public void Start()
        {
            _httpListener.Start();

            while (_httpListener.IsListening)
                ProcessRequest();
        }

        public void Stop()
        {
            _httpListener.Stop();
        }

        void ProcessRequest()
        {
            var result = _httpListener.BeginGetContext(ListenerCallback, _httpListener);
            result.AsyncWaitHandle.WaitOne();
        }

        void ListenerCallback(IAsyncResult result)
        {
            try
            {
                var context = _httpListener.EndGetContext(result);
                var info = Read(context.Request);

                List<string> uriparts = new List<string>(info.Url.ToString().Split('?'));
                if (uriparts[0] == "http://localhost:8888/servers")
                {
                    List<string> parameters = new List<string>(uriparts[1].Split('&'));
                    Dictionary<string, string> Parameters = new Dictionary<string,string>();
                    foreach (string s in parameters)
                    {
                        string[] parts = s.Split('=');
                        Parameters[parts[0]] = parts[1];
                    }
                    switch (Parameters["action"])
                    {
                        case "stop":
                            Console.WriteLine("Stopping: {0}", Parameters["uniqueid"]);
                            ProcessManager.StopGame(Convert.ToInt32(Parameters["uniqueid"]));
                            break;
                        case "start":
                            Console.WriteLine("Starting: {0}", Parameters["uniqueid"]);
                            ProcessManager.StartGame(Convert.ToInt32(Parameters["uniqueid"]));
                            break;
                        case "update":
                            Console.WriteLine("Updating: {0}", Parameters["uniqueid"]);
                            ProcessManager.UpdatePathParams(Convert.ToInt32(Parameters["uniqueid"]));
                            break;
                        default:
                            break;
                    }
                }

                CreateResponse(context.Response, info.ToString());
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.ToString());
            }
        }

        public static WebRequestInfo Read(HttpListenerRequest request)
        {
            var info = new WebRequestInfo();
            info.HttpMethod = request.HttpMethod;
            info.Url = request.Url;

            if (request.HasEntityBody)
            {
                Encoding encoding = request.ContentEncoding;
                using (var bodyStream = request.InputStream)
                using (var streamReader = new StreamReader(bodyStream, encoding))
                {
                    if (request.ContentType != null)
                        info.ContentType = request.ContentType;

                    info.ContentLength = request.ContentLength64;
                    info.Body = streamReader.ReadToEnd();
                }
            }

            return info;
        }

        public static WebResponseInfo Read(HttpWebResponse response)
        {
            var info = new WebResponseInfo();
            info.StatusCode = response.StatusCode;
            info.StatusDescription = response.StatusDescription;
            info.ContentEncoding = response.ContentEncoding;
            info.ContentLength = response.ContentLength;
            info.ContentType = response.ContentType;

            using (var bodyStream = response.GetResponseStream())
            using (var streamReader = new StreamReader(bodyStream, Encoding.UTF8))
            {
                info.Body = streamReader.ReadToEnd();
            }

            return info;
        }

        private static void CreateResponse(HttpListenerResponse response, string body)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = HttpStatusCode.OK.ToString();
            byte[] buffer = Encoding.UTF8.GetBytes(body);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }

    public class WebRequestInfo
    {
        public string Body { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public string HttpMethod { get; set; }
        public Uri Url { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("HttpMethod {0}", HttpMethod));
            sb.AppendLine(string.Format("Url {0}", Url));
            sb.AppendLine(string.Format("ContentType {0}", ContentType));
            sb.AppendLine(string.Format("ContentLength {0}", ContentLength));
            sb.AppendLine(string.Format("Body {0}", Body));
            return sb.ToString();
        }
    }

    public class WebResponseInfo
    {
        public string Body { get; set; }
        public string ContentEncoding { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("StatusCode {0} StatusDescripton {1}", StatusCode, StatusDescription));
            sb.AppendLine(string.Format("ContentType {0} ContentEncoding {1} ContentLength {2}", ContentType, ContentEncoding, ContentLength));
            sb.AppendLine(string.Format("Body {0}", Body));
            return sb.ToString();
        }
    }
}
