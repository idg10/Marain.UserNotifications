﻿// <copyright file="ClientBase.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.UserNotifications.Client
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Base class for the clients.
    /// </summary>
    public class ClientBase
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ClientBase"/> class.
        /// </summary>
        /// <param name="baseUrl">The base Url for the service.</param>
        /// <param name="serializerOptions">The Json serializer options.</param>
        protected ClientBase(string baseUrl, JsonSerializerOptions serializerOptions)
        {
            this.BaseUrl = new Uri(baseUrl, UriKind.Absolute);
            this.SerializerOptions = serializerOptions;
        }

        /// <summary>
        /// Gets or sets the base Uri for the service.
        /// </summary>
        public Uri BaseUrl { get; set; }

        /// <summary>
        /// Gets the HTTP client that will be used for underlying requests.
        /// </summary>
        protected HttpClient HttpClient { get; } = new HttpClient();

        /// <summary>
        /// Gets the serialization options that will be used to serialize and deserialize data.
        /// </summary>
        protected JsonSerializerOptions SerializerOptions { get; }

        /// <summary>
        /// Builds an HTTP request with the supplied data.
        /// </summary>
        /// <typeparam name="T">The object type to send as the request content.</typeparam>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="requestUri">The URI of the request.</param>
        /// <param name="body">The data to send as the request content.</param>
        /// <returns>The constructed message.</returns>
        protected HttpRequestMessage BuildRequest<T>(HttpMethod method, Uri requestUri, T body)
        {
            var request = new HttpRequestMessage(method, requestUri);

            string json = JsonSerializer.Serialize(body, this.SerializerOptions);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return request;
        }

        /// <summary>
        /// Builds an HTTP request with the supplied data.
        /// </summary>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="requestUri">The URI of the request.</param>
        /// <returns>The constructed message.</returns>
        protected HttpRequestMessage BuildRequest(HttpMethod method, Uri requestUri)
        {
            return new HttpRequestMessage(method, requestUri);
        }

        /// <summary>
        /// Builds a URL from the supplied path and query params.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="queryParameters">The query parameters.</param>
        /// <returns>The Uri.</returns>
        protected Uri ConstructUri(string path, params (string, string)[] queryParameters)
        {
            var builder = new UriBuilder(this.BaseUrl);
            builder.Path = path;
            foreach ((string key, string value) in queryParameters)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    builder.Query += $"{key}={Uri.EscapeUriString(value)}";
                }
            }

            return builder.Uri;
        }

        /// <summary>
        /// Gets the response body as a JsonDocument.
        /// </summary>
        /// <param name="responseMessage">The response.</param>
        /// <returns>The resulting JsonDocument.</returns>
        protected async Task<JsonDocument> GetResponseJsonDocumentAsync(HttpResponseMessage responseMessage)
        {
            // TODO: Make this right
            string json = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonDocument.Parse(json);
        }
    }
}