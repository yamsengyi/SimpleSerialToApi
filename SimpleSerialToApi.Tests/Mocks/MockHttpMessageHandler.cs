using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSerialToApi.Tests.Mocks
{
    /// <summary>
    /// Mock HTTP message handler for testing HTTP client behavior
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new Queue<HttpResponseMessage>();
        private readonly List<HttpRequestMessage> _requests = new List<HttpRequestMessage>();
        private Func<HttpRequestMessage, HttpResponseMessage>? _responseFunction;

        /// <summary>
        /// Set a fixed response to return for all requests
        /// </summary>
        public void SetResponse(HttpResponseMessage response)
        {
            _responses.Clear();
            _responses.Enqueue(response);
        }

        /// <summary>
        /// Queue multiple responses to return in order
        /// </summary>
        public void QueueResponse(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        /// <summary>
        /// Set a function to generate responses based on requests
        /// </summary>
        public void SetResponseFunction(Func<HttpRequestMessage, HttpResponseMessage> responseFunction)
        {
            _responseFunction = responseFunction;
        }

        /// <summary>
        /// Get all requests that were sent
        /// </summary>
        public IReadOnlyList<HttpRequestMessage> GetRequests()
        {
            return _requests.AsReadOnly();
        }

        /// <summary>
        /// Clear the request history
        /// </summary>
        public void ClearRequests()
        {
            _requests.Clear();
        }

        /// <summary>
        /// Clear all queued responses
        /// </summary>
        public void ClearResponses()
        {
            _responses.Clear();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // Store the request
            _requests.Add(request);

            // Use response function if available
            if (_responseFunction != null)
            {
                return await Task.FromResult(_responseFunction(request));
            }

            // Return queued response if available
            if (_responses.Count > 0)
            {
                return await Task.FromResult(_responses.Dequeue());
            }

            // Default success response
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\": true}")
            });
        }
    }

    /// <summary>
    /// Helper class for creating common HTTP responses
    /// </summary>
    public static class MockHttpResponses
    {
        public static HttpResponseMessage Success(string content = "{\"success\": true}")
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        }

        public static HttpResponseMessage BadRequest(string content = "{\"error\": \"Bad Request\"}")
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(content)
            };
        }

        public static HttpResponseMessage InternalServerError(string content = "{\"error\": \"Internal Server Error\"}")
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(content)
            };
        }

        public static HttpResponseMessage Timeout()
        {
            return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
            {
                Content = new StringContent("{\"error\": \"Timeout\"}")
            };
        }

        public static HttpResponseMessage Unauthorized(string content = "{\"error\": \"Unauthorized\"}")
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(content)
            };
        }

        public static HttpResponseMessage NotFound(string content = "{\"error\": \"Not Found\"}")
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(content)
            };
        }
    }
}