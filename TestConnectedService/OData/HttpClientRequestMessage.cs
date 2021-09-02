using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData;
using Microsoft.OData.Client;
using RNC.Tools.CertificateManager;

namespace TestConnectedService
{
    public class HttpClientRequestMessage : DataServiceClientRequestMessage
    {
        private readonly HttpRequestMessage requestMessage;
        private readonly HttpClient client;
        private readonly MemoryStream messageStream;
        private readonly Dictionary<string, string> contentHeaderValueCache;

        public HttpClientRequestMessage(string actualMethod)
            : base(actualMethod)
        {
            this.requestMessage = new HttpRequestMessage();
            this.messageStream = new MemoryStream();


            var cert = CertificateManager.GetCertificateByThumbprint("df74ee9108fd3a258511ed2d287e195ce6a50b0d");
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(cert);
            this.client = new HttpClient(handler);

            this.contentHeaderValueCache = new Dictionary<string, string>();
        }

        public override IEnumerable<KeyValuePair<string, string>> Headers
        {
            get
            {
                if (this.requestMessage.Content != null)
                {
                    return HttpHeadersToStringDictionary(this.requestMessage.Headers).Concat(HttpHeadersToStringDictionary(this.requestMessage.Content.Headers));
                }

                return HttpHeadersToStringDictionary(this.requestMessage.Headers).Concat(this.contentHeaderValueCache);
            }
        }

        public override Uri Url
        {
            get { return requestMessage.RequestUri; }
            set { requestMessage.RequestUri = value; }
        }

        public override string Method
        {
            get { return this.requestMessage.Method.ToString(); }
            set { this.requestMessage.Method = new HttpMethod(value); }
        }

        public override ICredentials Credentials
        {
            get { return null; }
            set {  }
        }

        public override int Timeout
        {
            get { return (int)this.client.Timeout.TotalSeconds; }
            set { this.client.Timeout = new TimeSpan(0, 0, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to send data in segments to the Internet resource. 
        /// </summary>
        public override bool SendChunked
        {
            get
            {
                bool? transferEncodingChunked = this.requestMessage.Headers.TransferEncodingChunked;
                return transferEncodingChunked.HasValue && transferEncodingChunked.Value;
            }
            set { this.requestMessage.Headers.TransferEncodingChunked = value; }
        }

        public override int ReadWriteTimeout 
        { 
            get => (int)client.Timeout.TotalSeconds; 
            set { } 
        }

        public override string GetHeader(string headerName)
        {
            //Returns the value of the header with the given name.
            if (requestMessage.Headers.TryGetValues(headerName, out IEnumerable<string> res)) return res.First();
            return string.Empty;
        }

        public override void SetHeader(string headerName, string headerValue)
        {
            // Sets the value of the header with the given name
            requestMessage.Headers.TryAddWithoutValidation(headerName, headerValue);
        }

        public override Stream GetStream()
        {
            return this.messageStream;
        }

        /// <summary>
        /// Abort the current request.
        /// </summary>
        public override void Abort()
        {
            this.client.CancelPendingRequests();
        }

        public override IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state)
        {
            var taskCompletionSource = new TaskCompletionSource<Stream>();
            taskCompletionSource.TrySetResult(this.messageStream);
            return taskCompletionSource.Task.ToApm(callback, state);
        }

        public override Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            return ((Task<Stream>)asyncResult).Result;
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            var send = CreateSendTask();
            return send.ToApm(callback, state);
        }

        public override IODataResponseMessage EndGetResponse(IAsyncResult asyncResult)
        {
            var result = ((Task<HttpResponseMessage>)asyncResult).Result;
            return ConvertHttpClientResponse(result);
        }

        public override IODataResponseMessage GetResponse()
        {
            var send = CreateSendTask();
            send.Wait();
            return ConvertHttpClientResponse(send.Result);
        }

        private Task<HttpResponseMessage> CreateSendTask()
        {
            // Only set the message content if the stream has been written to, otherwise
            // HttpClient will complain if it's a GET request.
            var messageContent = this.messageStream.ToArray();
            if (messageContent.Length > 0)
            {
                this.requestMessage.Content = new ByteArrayContent(messageContent);

                // Apply cached "Content" header values
                foreach (var contentHeader in this.contentHeaderValueCache)
                {
                    this.requestMessage.Content.Headers.Add(contentHeader.Key, contentHeader.Value);
                }
            }

            this.requestMessage.Method = new HttpMethod(this.ActualMethod);

            var send = this.client.SendAsync(this.requestMessage);
            return send;
        }

        private static IDictionary<string, string> HttpHeadersToStringDictionary(HttpHeaders headers)
        {
            return headers.ToDictionary((h1) => h1.Key, (h2) => string.Join(",", h2.Value));
        }

        private static HttpClientResponseMessage ConvertHttpClientResponse(HttpResponseMessage response)
        {
            return new HttpClientResponseMessage(response);
        }
    }

    public static class TaskExtensionMethods
    {
        public static Task<TResult> ToApm<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<TResult>(state);

            task.ContinueWith(
                delegate
                {
                    if (task.IsFaulted)
                    {
                        tcs.TrySetException(task.Exception.InnerExceptions);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(task.Result);
                    }

                    if (callback != null)
                    {
                        callback(tcs.Task);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);

            return tcs.Task;
        }
    }
}
