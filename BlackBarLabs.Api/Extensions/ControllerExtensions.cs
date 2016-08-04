﻿using BlackBarLabs.Api.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace BlackBarLabs.Api
{
    public static class ControllerExtensions
    {
        public static async Task<TResult> ParseMultipartAsync<TResult, TMethod>(this HttpContent content,
            Expression<TMethod> callback)
        {
            if (!content.IsMimeMultipartContent())
            {
                throw new ArgumentException("Content is not multipart", "content");
            }

            var streamProvider = new MultipartMemoryStreamProvider();
            await content.ReadAsMultipartAsync(streamProvider);

            var paramTasks = callback.Parameters.Select(
                async (param) =>
                {
                    var paramContent = streamProvider.Contents.FirstOrDefault(file => file.Headers.ContentDisposition.Name.Contains(param.Name));
                    if (default(HttpContent) == paramContent)
                        return param.Type.IsValueType ? Activator.CreateInstance(param.Type) : null;

                    if (param.Type.GUID == typeof(string).GUID)
                    {
                        var stringValue = await paramContent.ReadAsStringAsync();
                        return (object)stringValue;
                    }
                    if (param.Type.GUID == typeof(Guid).GUID)
                    {
                        var guidStringValue = await paramContent.ReadAsStringAsync();
                        var guidValue = Guid.Parse(guidStringValue);
                        return (object)guidValue;
                    }
                    if (param.Type.GUID == typeof(System.IO.Stream).GUID)
                    {
                        var streamValue = await paramContent.ReadAsStreamAsync();
                        return (object)streamValue;
                    }
                    if (param.Type.GUID == typeof(byte []).GUID)
                    {
                        var byteArrayValue = await paramContent.ReadAsByteArrayAsync();
                        return (object)byteArrayValue;
                    }
                    var value = await paramContent.ReadAsAsync(param.Type);
                    return value;
                });

            var paramsForCallback = await Task.WhenAll(paramTasks);
            var result = ((LambdaExpression)callback).Compile().DynamicInvoke(paramsForCallback);
            return (TResult)result;
        }

        public static IHttpActionResult ToActionResult(this HttpActionDelegate action)
        {
            return new BlackBarLabs.Api.HttpActionResult(action);
        }
        public static IHttpActionResult ToActionResult(this HttpResponseMessage response)
        {
            return new BlackBarLabs.Api.HttpActionResult(() => Task.FromResult(response));
        }

        public static async Task<IHttpActionResult> CreateMultipartResponseAsync(this HttpRequestMessage request,
            IEnumerable<HttpResponseMessage> contents)
        {
            var contentTasks = contents.Select(
                async (content) =>
                {
                    var response = new Response
                    {
                        StatusCode = content.StatusCode,
                        ContentType = content.Content.Headers.ContentType,
                        ContentLocation = content.Content.Headers.ContentLocation,
                        Content = await content.Content.ReadAsStringAsync(),
                    };
                    return response;
                });

            var multipartResponseContent = new MultipartResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = (await Task.WhenAll(contentTasks)).ToList(),
                Location = request.RequestUri,
            };
            
            var multipartResponse = request.CreateResponse(HttpStatusCode.OK, multipartResponseContent);
            multipartResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-multipart+json");
            return multipartResponse.ToActionResult();
        }
    }
}
