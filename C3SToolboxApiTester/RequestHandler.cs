using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using Maris;

namespace C3SToolboxApiTester
{
    class RequestHandler
    {
        public bool debug = false;
        public static string UserAgent = "Maris API Probe";
        public int TimeoutLong = 10 * 60 * 1000; // = 10 minutes
        public int Timeout = 60 * 1000; // = 60 seconds
        public RequestHandler()
        {
            AppLib.AppendLog("New RequestHandler");
        }


        public HttpRequestReturnValue GetRequest(string url, string requestHash = null)
        {

            if(null == requestHash)
            {
                requestHash = string.Format("{0:X}", url.GetHashCode());
            }

            if (debug) AppLib.AppendLog("GET " + url, requestHash);
            
            // Create a request for the URL. 		
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            
            request.UserAgent = "Maris API Probe";
            request.Timeout = Timeout;

            HttpWebResponse response = null;
            try
            {
                // Get the response.
                response = (HttpWebResponse) request.GetResponse();
               
            } 
            catch (WebException e)
            {
                response = (HttpWebResponse) e.Response;

                AppLib.AppendLog(e, requestHash);
            }
            catch (System.Exception e)
            {
                AppLib.AppendLog(e, requestHash);
            }

            string responseContent = "";
            int statusCode = 0;

            if (response != null)
            {
                statusCode = (int) response.StatusCode;
                Stream dataStream = response.GetResponseStream();
                if (dataStream != null)
                {
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);

                    // Read the content.
                    responseContent = reader.ReadToEnd();

                    // Cleanup the streams and the response.
                    if (reader != null) reader.Close();
                }

                if (dataStream != null) dataStream.Close();
            }

            // Cleanup the streams and the response.
            if (response != null) response.Close();

            if (debug) AppLib.AppendLog(statusCode, requestHash);

            return new HttpRequestReturnValue(statusCode, responseContent, requestHash);
        }


        public HttpRequestReturnValue PostRequest(string url, string content="{}", string ContentType = "application/json", string requestHash = null)
        {
            if (null == requestHash)
            {
                requestHash = string.Format("{0:X}", (url + content + ContentType).GetHashCode());
            }

            if (debug) AppLib.AppendLog("POST " + url + " " + ContentType, requestHash);
            if (debug) AppLib.AppendLog(content, requestHash);

            // Create a request for the URL. 		
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.UserAgent = RequestHandler.UserAgent;

            request.ContentType = ContentType;

            request.Method = "POST";

            HttpWebResponse response = null;
            try
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(content);
                }

                // Get the response.
                response = (HttpWebResponse)request.GetResponse();

            }
            catch (WebException e)
            {
                response = (HttpWebResponse) e.Response;

                AppLib.AppendLog(e, requestHash);
            }
            catch (System.Exception e)
            {
                AppLib.AppendLog(e, requestHash);
            }

            string responseContent = "";
            int statusCode = 0;

            if (response != null)
            {
                statusCode = (int) response.StatusCode;
                Stream dataStream = response.GetResponseStream();
                if (dataStream != null)
                {
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);

                    // Read the content.
                    responseContent = reader.ReadToEnd();

                    // Cleanup the streams and the response.
                    if (reader != null) reader.Close();
                }

                if (dataStream != null) dataStream.Close();
            }

            // Cleanup the streams and the response.
            if (response != null) response.Close();

            if (debug) AppLib.AppendLog(statusCode, requestHash);

            return new HttpRequestReturnValue(statusCode, responseContent, requestHash);
        }
    }
    public class HttpRequestReturnValue
    {
        public HttpRequestReturnValue(int statusCode, string content, string requestHash = "")
        {
            StatusCode = statusCode;
            Content = content;
            RequestHash = requestHash;
        }

        public int StatusCode { get; set; }
        public string Content { get; set; }
        public string RequestHash { get; set; }

    }
}
