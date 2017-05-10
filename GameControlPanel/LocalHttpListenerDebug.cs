using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web;

namespace GameControlPanel
{
        class LocalHttpListenerDebug
        {
            public const string UriAddress = "http://localhost:8888/";
            HttpListener _httpListener;

            public LocalHttpListenerDebug()
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(LocalHttpListenerDebug.UriAddress);
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
                var context = _httpListener.EndGetContext(result);
                var info = Read(context.Request);

                Console.WriteLine("Server received: {0}{1}",
                  Environment.NewLine,
                  info.ToString());

                CreateResponse(context.Response, info.ToString());
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
}
