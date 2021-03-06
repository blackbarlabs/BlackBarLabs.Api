﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using EastFive.Linq;
using EastFive.Collections.Generic;
using EastFive;
using EastFive.Reflection;
using EastFive.Extensions;
using EastFive.Api.Controllers;
using EastFive.Linq.Expressions;
using BlackBarLabs.Extensions;
using BlackBarLabs.Api;
using EastFive.Linq.Async;

namespace EastFive.Api
{
    public static class InvokeApplicationExtensions
    {
        #region GET

        [HttpMethodRequestBuilder(Method = "Get")]
        public static IQueryable<TResource> HttpGet<TResource>(this IQueryable<TResource> requestQuery)
        {
            return requestQuery;
        }
        public class HttpMethodRequestBuilderAttribute : Attribute, IBuildHttpRequests
        {
            public string Method { get; set; }

            public HttpRequestMessage MutateRequest(HttpRequestMessage request, MethodInfo method, Expression[] arguments)
            {
                request.Method =new HttpMethod(Method);
                return request;
            }
        }

        #endregion

        #region POST

        [HttpPostRequestBuilder]
        public static IQueryable<TResource> HttpPost<TResource>(this IQueryable<TResource> requestQuery,
            TResource resource,
            System.Net.Http.Headers.HttpRequestHeaders headers = default)
        {
            if (!typeof(RequestMessage<TResource>).IsAssignableFrom(requestQuery.GetType()))
                throw new ArgumentException($"query must be of type `{typeof(RequestMessage<TResource>).FullName}` not `{requestQuery.GetType().FullName}`", "query");
            var requestMessageQuery = requestQuery as RequestMessage<TResource>;

            var methodInfo = typeof(InvokeApplicationExtensions)
                .GetMethod("HttpPost", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(typeof(TResource));
            var resourceExpr = Expression.Constant(resource, typeof(TResource));
            var headersExpr = Expression.Constant(headers, typeof(System.Net.Http.Headers.HttpRequestHeaders));
            var condition = Expression.Call(methodInfo, requestQuery.Expression, resourceExpr, headersExpr);

            var requestMessageNewQuery = requestMessageQuery.FromExpression(condition);
            return requestMessageNewQuery;
        }
        public class HttpPostRequestBuilderAttribute : Attribute, IBuildHttpRequests
        {
            public HttpRequestMessage MutateRequest(HttpRequestMessage request, MethodInfo method, Expression[] arguments)
            {
                request.Method = HttpMethod.Post;
                var resource = arguments[0].Resolve();
                var headers = (System.Net.Http.Headers.HttpRequestHeaders)arguments[1].Resolve();

                var contentJsonString = JsonConvert.SerializeObject(resource, new Serialization.Converter());
                request.Content = new StreamContent(contentJsonString.ToStream());
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                foreach (var header in headers.NullToEmpty())
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                return request;
            }
        }

        #endregion

        #region PATCH

        [HttpPatchRequestBuilder]
        public static IQueryable<TResource> HttpPatch<TResource>(this IQueryable<TResource> requestQuery,
            TResource resource,
            System.Net.Http.Headers.HttpRequestHeaders headers = default)
        {
            if (!typeof(RequestMessage<TResource>).IsAssignableFrom(requestQuery.GetType()))
                throw new ArgumentException($"query must be of type `{typeof(RequestMessage<TResource>).FullName}` not `{requestQuery.GetType().FullName}`", "query");
            var requestMessageQuery = requestQuery as RequestMessage<TResource>;

            var methodInfo = typeof(InvokeApplicationExtensions)
                .GetMethod("HttpPatch", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(typeof(TResource));
            var resourceExpr = Expression.Constant(resource, typeof(TResource));
            var headersExpr = Expression.Constant(headers, typeof(System.Net.Http.Headers.HttpRequestHeaders));
            var condition = Expression.Call(methodInfo, requestQuery.Expression, resourceExpr, headersExpr);

            var requestMessageNewQuery = requestMessageQuery.FromExpression(condition);
            return requestMessageNewQuery;
        }

        public class HttpPatchRequestBuilderAttribute : Attribute, IBuildHttpRequests
        {
            public HttpRequestMessage MutateRequest(HttpRequestMessage request, MethodInfo method, Expression[] arguments)
            {
                request.Method = new HttpMethod("Patch");
                var resource = arguments[0].Resolve();
                var headers = (System.Net.Http.Headers.HttpRequestHeaders)arguments[1].Resolve();

                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new Serialization.Converter());
                settings.DefaultValueHandling = DefaultValueHandling.Ignore;
                var contentJsonString = JsonConvert.SerializeObject(resource, settings);
                request.Content = new StreamContent(contentJsonString.ToStream());
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                foreach (var header in headers.NullToEmpty())
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                return request;
            }
        }

        #endregion

        #region DELETE

        [HttpMethodRequestBuilder(Method = "Delete")]
        public static IQueryable<TResource> HttpDelete<TResource>(this IQueryable<TResource> requestQuery)
        {
            return requestQuery;
        }

        #endregion

        #region Action

        [HttpActionRequestBuilder]
        public static IQueryable<TResource> HttpAction<TResource>(this IQueryable<TResource> requestQuery,
            string actionName,
            System.Net.Http.Headers.HttpRequestHeaders headers = default)
        {
            if (!typeof(RequestMessage<TResource>).IsAssignableFrom(requestQuery.GetType()))
                throw new ArgumentException($"query must be of type `{typeof(RequestMessage<TResource>).FullName}` not `{requestQuery.GetType().FullName}`", "query");
            var requestMessageQuery = requestQuery as RequestMessage<TResource>;

            var condition = Expression.Call(
                typeof(ResourceQueryExtensions), "HttpPost", new Type[] { typeof(TResource) },
                requestQuery.Expression, Expression.Constant(actionName), Expression.Constant(headers));

            var requestMessageNewQuery = requestMessageQuery.FromExpression(condition);
            return requestMessageNewQuery;
        }

        public class HttpActionRequestBuilder : Attribute, IBuildHttpRequests
        {
            public HttpRequestMessage MutateRequest(HttpRequestMessage request, MethodInfo method, Expression[] arguments)
            {
                var actionName = (string)arguments[0].Resolve();
                var headers = (System.Net.Http.Headers.HttpRequestHeaders)arguments[1].Resolve();
                request.RequestUri = request.RequestUri.AppendToPath(actionName);

                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                return request;
            }
        }

        #endregion

        public static async Task<TResult> MethodAsync<TResource, TResult>(this IQueryable<TResource> requestQuery,
            Func<TResource, TResult> onContent = default,
            Func<TResource[], TResult> onContents = default,
            Func<object[], TResult> onContentObjects = default,
            Func<string, TResult> onHtml = default,
            Func<byte[], string, TResult> onXls = default,
            Func<TResult> onCreated = default,
            Func<TResource, string, TResult> onCreatedBody = default,
            Func<TResult> onUpdated = default,

            Func<Uri, TResult> onRedirect = default,

            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onUnauthorized = default(Func<TResult>),
            Func<TResult> onExists = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default,
            Func<string, TResult> onFailure = default,

            Func<TResult> onNotImplemented = default,
            Func<IExecuteAsync, Task<TResult>> onExecuteBackground = default,
            Func<HttpResponseMessage, TResult> onResponse = default,
            Func<HttpStatusCode, TResult> onNotOverriddenResponse = default)
        {
            var request = (requestQuery as RequestMessage<TResource>);
            var application = request.InvokeApplication.Application;
            application.CreatedResponse<TResource, TResult>(onCreated);
            application.CreatedBodyResponse<TResource, TResult>(onCreatedBody);
            application.BadRequestResponse<TResource, TResult>(onBadRequest);
            application.AlreadyExistsResponse<TResource, TResult>(onExists);
            application.RefNotFoundTypeResponse(onRefDoesNotExistsType);
            application.RedirectResponse<TResource, TResult>(onRedirect);
            application.NotImplementedResponse<TResource, TResult>(onNotImplemented);

            if (!onContent.IsDefaultOrNull())
            {
                application.ContentResponse(onContent);
                application.ContentTypeResponse<TResource, TResult>((body, contentType) => onContent(body));
            }
            application.MultipartContentResponse(onContents);
            if(!onContentObjects.IsDefaultOrNull())
                application.MultipartContentObjectResponse<TResource, TResult>(onContentObjects);
            application.NotFoundResponse<TResource, TResult>(onNotFound);
            application.HtmlResponse<TResource, TResult>(onHtml);
            application.XlsResponse<TResource, TResult>(onXls);

            application.NoContentResponse<TResource, TResult>(onUpdated);
            application.UnauthorizedResponse<TResource, TResult>(onUnauthorized);
            application.GeneralConflictResponse<TResource, TResult>(onFailure);
            application.ExecuteBackgroundResponse<TResource, TResult>(onExecuteBackground);

            var httpRequest = request.CompileRequest();
            var response = await request.InvokeApplication.SendAsync(httpRequest);

            if (!onResponse.IsDefaultOrNull())
                return onResponse(response);

            if (response is IDidNotOverride)
                (response as IDidNotOverride).OnFailure();

            if (response is IReturnResult)
            {
                var attachedResponse = response as IReturnResult;
                var result = attachedResponse.GetResultCasted<TResult>();
                return result;
            }

            if (!onNotOverriddenResponse.IsDefaultOrNull())
            {
                return onNotOverriddenResponse(response.StatusCode);
            }
            var msg = $"Failed to override response with status code `{response.StatusCode}` for {typeof(TResource).FullName}" +
                $"\nResponse:{response.ReasonPhrase}";
            throw new Exception(msg);
        }

        public static Task<TResult> GetAsync<TResource, TResult>(this IQueryable<TResource> requestQuery,
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<TResult> onUnauthorized = default,
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, TResult> onRedirect = default(Func<Uri, TResult>),
            Func<TResult> onCreated = default(Func<TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>),
            Func<byte[], string, TResult> onXls = default,
            Func<IExecuteAsync, Task<TResult>> onExecuteBackground = default,
            Func<HttpStatusCode, TResult> onNotOverriddenResponse = default)
        {
            return requestQuery
                .HttpGet()
                .MethodAsync<TResource, TResult>(
                    onContent: onContent,
                    onContents: onContents,
                    onContentObjects: onContentObjects,
                    onBadRequest: onBadRequest,
                    onNotFound: onNotFound,
                    onUnauthorized: onUnauthorized,
                    onRefDoesNotExistsType: onRefDoesNotExistsType,
                    onRedirect: onRedirect,
                    onCreated: onCreated,
                    onHtml: onHtml,
                    onXls: onXls,
                    onExecuteBackground: onExecuteBackground,
                    onNotOverriddenResponse: onNotOverriddenResponse);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="application"></param>
        /// <param name="resource"></param>
        /// <param name="onCreated"></param>
        /// <param name="onBadRequest"></param>
        /// <param name="onExists"></param>
        /// <param name="onRefDoesNotExistsType"></param>
        /// <param name="onNotImplemented"></param>
        /// <returns></returns>
        /// <remarks>Response hooks are only called if the method is actually invoked. Responses from the framework are not trapped.</remarks>
        public static Task<TResult> PostAsync<TResource, TResult>(this IQueryable<TResource> requestQuery,
                TResource resource,
            Func<TResult> onCreated = default,
            Func<TResource, string, TResult> onCreatedBody = default,
            Func<TResult> onBadRequest = default,
            Func<TResult> onExists = default,
            Func<Type, TResult> onRefDoesNotExistsType = default,
            Func<Uri, TResult> onRedirect = default,
            Func<TResult> onNotImplemented = default,
            Func<IExecuteAsync, Task<TResult>> onExecuteBackground = default,
            Func<HttpStatusCode, TResult> onNotOverriddenResponse = default)
        {
            return requestQuery
                .HttpPost(resource)
                .MethodAsync<TResource, TResult>(
                    onCreated: onCreated,
                    onCreatedBody: onCreatedBody,
                    onBadRequest: onBadRequest,
                    onExists: onExists,
                    onRefDoesNotExistsType: onRefDoesNotExistsType,
                    onRedirect: onRedirect,
                    onNotImplemented: onNotImplemented,
                    onExecuteBackground: onExecuteBackground,
                    onNotOverriddenResponse: onNotOverriddenResponse);
        }

        public static Task<TResult> PatchAsync<TResource, TResult>(this IQueryable<TResource> requestQuery,
                TResource resource,
            Func<TResult> onUpdated = default,
            Func<TResource, TResult> onUpdatedBody = default,
            Func<TResult> onNotFound = default,
            Func<TResult> onUnauthorized = default,
            Func<string, TResult> onFailure = default,
            Func<HttpStatusCode, TResult> onNotOverriddenResponse = default,
            Func<HttpResponseMessage, TResult> onResponse = default)
        {
            return requestQuery
                .HttpPatch(resource)
                .MethodAsync<TResource, TResult>(
                    onUpdated: onUpdated,
                    onContent: onUpdatedBody,
                    onNotFound: onNotFound,
                    onUnauthorized: onUnauthorized,
                    onFailure: onFailure,
                    onNotOverriddenResponse: onNotOverriddenResponse,
                    onResponse: onResponse);
        }

        public static Task<TResult> DeleteAsync<TResource, TResult>(this IQueryable<TResource> request,
            Func<TResult> onNoContent = default(Func<TResult>),
            Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<TResult> onBadRequest = default(Func<TResult>),
            Func<TResult> onNotFound = default(Func<TResult>),
            Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
            Func<Uri, TResult> onRedirect = default(Func<Uri, TResult>),
            Func<string, TResult> onHtml = default(Func<string, TResult>))
        {
            return request
                .HttpDelete()
                .MethodAsync<TResource, TResult>(
                    onUpdated: onNoContent,
                    onContent: onContent,
                    onContents: onContents,
                    onBadRequest: onBadRequest,
                    onNotFound: onNotFound,
                    onRefDoesNotExistsType: onRefDoesNotExistsType,
                    onRedirect: onRedirect,
                    onHtml: onHtml);
        }

        public static Task<TResult> ActionAsync<TResource, TResult>(this IQueryable<TResource> requestQuery,
                string actionName,
            Func<TResult> onUpdated = default,
            Func<TResource, TResult> onUpdatedBody = default,
            Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
            Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
            Func<TResult> onNotFound = default,
            Func<TResult> onUnauthorized = default,
            Func<string, TResult> onFailure = default,
            Func<HttpStatusCode, TResult> onNotOverriddenResponse = default,
            Func<HttpResponseMessage, TResult> onResponse = default)
        {
            return requestQuery
                .HttpAction(actionName)
                .MethodAsync<TResource, TResult>(
                    onUpdated: onUpdated,
                    onContent: onUpdatedBody,
                    onContents: onContents,
                    onContentObjects: onContentObjects,
                    onNotFound: onNotFound,
                    onUnauthorized: onUnauthorized,
                    onFailure: onFailure,
                    onNotOverriddenResponse: onNotOverriddenResponse,
                    onResponse: onResponse);
        }

        #region Response types

        public interface IReturnResult
        {
            TResult GetResultCasted<TResult>();
        }

        private class AttachedHttpResponseMessage<TResult> : HttpResponseMessage, IReturnResult
        {
            public TResult Result { get; }

            public AttachedHttpResponseMessage(TResult result)
            {
                this.Result = result;
            }

            public HttpResponseMessage Inner { get; }
            public AttachedHttpResponseMessage(TResult result, HttpResponseMessage inner)
            {
                this.Result = result;
                this.Inner = inner;
            }

            public TResult1 GetResultCasted<TResult1>()
            {
                return (TResult1)(this.Result as object);
            }
        }

        private interface IDidNotOverride
        {
            void OnFailure();
        }

        private class NoOverrideHttpResponseMessage<TResource> : HttpResponseMessage, IDidNotOverride
        {
            private Type typeOfResponse;
            private HttpRequestMessage request;
            public NoOverrideHttpResponseMessage(Type typeOfResponse, HttpRequestMessage request)
            {
                this.typeOfResponse = typeOfResponse;
                this.request = request;
            }

            public void OnFailure()
            {
                var message = $"Failed to override response for: [{request.Method.Method}] `{typeof(TResource).FullName}`.`{typeOfResponse.Name}`";
                throw new Exception(message);
            }
        }

        private static void ContentResponse<TResource, TResult>(this IApplication application,
            Func<TResource, TResult> onContent)
        {
            application.SetInstigator(
                typeof(ContentResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    ContentResponse created =
                        (content, mimeType) =>
                        {
                            if (onContent.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(ContentResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            if (!(content is TResource))
                                throw new Exception($"Could not cast {content.GetType().FullName} to {typeof(TResource).FullName}.");
                            var resource = (TResource)content;
                            var result = onContent(resource);
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                },
                onContent.IsDefaultOrNull());
        }

        private static void HtmlResponse<TResource, TResult>(this IApplication application,
            Func<string, TResult> onHtml)
        {
            application.SetInstigator(
                typeof(HtmlResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    HtmlResponse created =
                        (content) =>
                        {
                            if (onHtml.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(EastFive.Api.HtmlResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onHtml(content);
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                });
        }

        private static void XlsResponse<TResource, TResult>(this IApplication application,
            Func<byte[], string, TResult> onXls)
        {
            application.SetInstigator(
                typeof(XlsxResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    XlsxResponse created =
                        (content, name) =>
                        {
                            if (onXls.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(EastFive.Api.XlsxResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onXls(content, name);
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                });
        }

        private static void BadRequestResponse<TResource, TResult>(this IApplication application,
            Func<TResult> onBadRequest)
        {
            application.SetInstigator(
                typeof(BadRequestResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    BadRequestResponse badRequest =
                        () =>
                        {
                            if (onBadRequest.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(BadRequestResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onBadRequest();
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(badRequest);
                });
        }
        
        private static void RefNotFoundTypeResponse<TResult>(this IApplication application,
            Func<Type, TResult> referencedDocDoesNotExists)
        {
            application.SetInstigatorGeneric(
                typeof(ReferencedDocumentDoesNotExistsResponse<>),
                (type, thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    var scope = new CallbackWrapperReferencedDocumentDoesNotExistsResponse<TResult>(referencedDocDoesNotExists,
                        thisAgain, requestAgain, paramInfo, onSuccess);
                    var multipartResponseMethodInfoGeneric = typeof(CallbackWrapperReferencedDocumentDoesNotExistsResponse<TResult>)
                        .GetMethod("RefNotFoundTypeResponseGeneric", BindingFlags.Public | BindingFlags.Instance);
                    var multipartResponseMethodInfoBound = multipartResponseMethodInfoGeneric
                        .MakeGenericMethod(type.GenericTypeArguments);
                    var dele = Delegate.CreateDelegate(type, scope, multipartResponseMethodInfoBound);
                    return onSuccess((object)dele);
                },
                referencedDocDoesNotExists.IsDefaultOrNull());
        }

        public class CallbackWrapperReferencedDocumentDoesNotExistsResponse<TResult>
        {
            private Func<Type, TResult> referencedDocDoesNotExists;
            private HttpApplication thisAgain;
            private HttpRequestMessage requestAgain;
            private ParameterInfo paramInfo;
            private Func<object, Task<HttpResponseMessage>> onSuccess;
            
            public CallbackWrapperReferencedDocumentDoesNotExistsResponse(Func<Type, TResult> referencedDocDoesNotExists,
                HttpApplication thisAgain, HttpRequestMessage requestAgain, ParameterInfo paramInfo, Func<object, Task<HttpResponseMessage>> onSuccess)
            {
                this.referencedDocDoesNotExists = referencedDocDoesNotExists;
                this.thisAgain = thisAgain;
                this.requestAgain = requestAgain;
                this.paramInfo = paramInfo;
                this.onSuccess = onSuccess;
            }

            public HttpResponseMessage RefNotFoundTypeResponseGeneric<TResource>()
            {
                if (referencedDocDoesNotExists.IsDefaultOrNull())
                    return FailureToOverride<TResource>(typeof(ReferencedDocumentDoesNotExistsResponse<>), thisAgain, requestAgain, paramInfo, onSuccess);

                var result = referencedDocDoesNotExists(typeof(TResource));
                return new AttachedHttpResponseMessage<TResult>(result);
            }
        }

        private class InstigatorGenericWrapper1<TCallback, TResult, TResource>
        {
            private Type type;
            private HttpApplication httpApp;
            private HttpRequestMessage request;
            private ParameterInfo paramInfo;
            private TCallback callback;
            private Func<object, Task<HttpResponseMessage>> onSuccess;

            public InstigatorGenericWrapper1(Type type,
                HttpApplication httpApp, HttpRequestMessage request, ParameterInfo paramInfo,
                TCallback callback, Func<object, Task<HttpResponseMessage>> onSuccess)
            {
                this.type = type;
                this.httpApp = httpApp;
                this.request = request;
                this.paramInfo = paramInfo;
                this.callback = callback;
                this.onSuccess = onSuccess;
            }

            HttpResponseMessage ContentTypeResponse(object content, string mediaType = default(string))
            {
                if (callback.IsDefault())
                    return FailureToOverride<TResource>(
                        type, this.httpApp, this.request, this.paramInfo, onSuccess);
                var contentObj = (object)content;
                var contentType = (TResource)contentObj;
                var callbackObj = (object)callback;
                var callbackDelegate = (Delegate)callbackObj;
                var resultObj = callbackDelegate.DynamicInvoke(contentType, mediaType);
                var result = (TResult)resultObj;
                return new AttachedHttpResponseMessage<TResult>(result);
            }

            HttpResponseMessage CreatedBodyResponse(object content, string mediaType = default(string))
            {
                if (callback.IsDefault())
                    return FailureToOverride<TResource>(
                        type, this.httpApp, this.request, this.paramInfo, onSuccess);
                var contentObj = (object)content;
                var contentType = (TResource)contentObj;
                var callbackObj = (object)callback;
                var callbackDelegate = (Delegate)callbackObj;
                var resultObj = callbackDelegate.DynamicInvoke(contentType, mediaType);
                var result = (TResult)resultObj;
                return new AttachedHttpResponseMessage<TResult>(result);
            }
        }

        private static void CreatedBodyResponse<TResource, TResult>(this IApplication application,
            Func<TResource, string, TResult> onCreated)
        {
            application.SetInstigatorGeneric(
                typeof(CreatedBodyResponse<>),
                (type, httpApp, request, paramInfo, onSuccess) =>
                {
                    type = typeof(CreatedBodyResponse<>).MakeGenericType(typeof(TResource));
                    var wrapperConcreteType = typeof(InstigatorGenericWrapper1<,,>).MakeGenericType(
                        //type.GenericTypeArguments
                        //    .Append(typeof(Func<TResource, string, TResult>))
                        typeof(Func<TResource, string, TResult>)
                            .AsArray()
                            .Append(typeof(TResult))
                            .Append(typeof(TResource))
                            .ToArray());
                    var wrapperInstance = Activator.CreateInstance(wrapperConcreteType,
                        new object[] { type, httpApp, request, paramInfo, onCreated, onSuccess });
                    var dele = Delegate.CreateDelegate(type, wrapperInstance, "CreatedBodyResponse", false);
                    return onSuccess(dele);
                },
                onCreated.IsDefaultOrNull());
        }

        private static void ContentTypeResponse<TResource, TResult>(this IApplication application,
            Func<TResource, string, TResult> onCreated)
        {
            application.SetInstigatorGeneric(
                typeof(EastFive.Api.ContentTypeResponse<>),
                (type, httpApp, request, paramInfo, onSuccess) =>
                {
                    //type = typeof(ContentTypeResponse<>).MakeGenericType(typeof(TResource));
                    //var wrapperConcreteType = typeof(InstigatorGenericWrapper1<,,>).MakeGenericType(
                    //    typeof(Func<TResource, string, TResult>)
                    //        .AsArray()
                    //        .Append(typeof(TResult))
                    //        .Append(typeof(TResource))
                    //        .ToArray());
                    //var wrapperInstance = Activator.CreateInstance(wrapperConcreteType,
                    //    new object[] { type, httpApp, request, paramInfo, onCreated, onSuccess });

                    var resourceType = type.GenericTypeArguments.First();
                    var funcOfRes = typeof(Func<,,>).MakeGenericType(new[] { resourceType, typeof(string), typeof(TResult) });
                    var wrapperConcreteType = typeof(InstigatorGenericWrapper1<,,>).MakeGenericType(
                        funcOfRes
                            .AsArray()
                            .Append(typeof(TResult))
                            .Append(resourceType)
                            .ToArray());
                    var wrapperInstance = Activator.CreateInstance(wrapperConcreteType,
                        new object[] { type, httpApp, request, paramInfo, onCreated, onSuccess });

                    var dele = Delegate.CreateDelegate(type, wrapperInstance, "ContentTypeResponse", false);
                    return onSuccess(dele);
                },
                onCreated.IsDefaultOrNull());
        }

        private static void CreatedResponse<TResource, TResult>(this IApplication application,
            Func<TResult> onCreated)
        {
            application.SetInstigator(
                typeof(CreatedResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    CreatedResponse created =
                        () =>
                        {
                            if (onCreated.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(CreatedResponse),
                                    thisAgain, requestAgain, paramInfo, onSuccess);
                            return new AttachedHttpResponseMessage<TResult>(onCreated());
                        };
                    return onSuccess(created);
                },
                onCreated.IsDefaultOrNull());
        }

        private static void RedirectResponse<TResource, TResult>(this IApplication application,
            Func<Uri, TResult> onRedirect)
        {
            application.SetInstigator(
                typeof(RedirectResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    RedirectResponse redirect =
                        (where) =>
                        {
                            if (onRedirect.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(RedirectResponse),
                                    thisAgain, requestAgain, paramInfo, onSuccess);
                            return new AttachedHttpResponseMessage<TResult>(onRedirect(where));
                        };
                    return onSuccess(redirect);
                });
        }

        private static void AlreadyExistsResponse<TResource, TResult>(this IApplication application,
            Func<TResult> onAlreadyExists)
        {
            if (!onAlreadyExists.IsDefaultOrNull())
                application.SetInstigator(
                    typeof(AlreadyExistsResponse),
                    (thisAgain, requestAgain, paramInfo, onSuccess) =>
                    {
                        AlreadyExistsResponse exists =
                            () =>
                            {
                                if (onAlreadyExists.IsDefaultOrNull())
                                    return FailureToOverride<TResource>(
                                        typeof(AlreadyExistsResponse),
                                        thisAgain, requestAgain, paramInfo, onSuccess);
                                return new AttachedHttpResponseMessage<TResult>(onAlreadyExists());
                            };
                        return onSuccess(exists);
                    });
        }


        private static void NotImplementedResponse<TResource, TResult>(this IApplication application,
            Func<TResult> onNotImplemented)
        {
            application.SetInstigator(
                typeof(NotImplementedResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    NotImplementedResponse notImplemented =
                        () =>
                        {
                            if (onNotImplemented.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(NotImplementedResponse),
                                    thisAgain, requestAgain, paramInfo, onSuccess);
                            return new AttachedHttpResponseMessage<TResult>(onNotImplemented());
                        };
                    return onSuccess(notImplemented);
                });
        }

        private static void MultipartContentResponse<TResource, TResult>(this IApplication application,
            Func<TResource[], TResult> onContents)
        {
            application.SetInstigator(
                typeof(MultipartAcceptArrayResponseAsync),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    MultipartAcceptArrayResponseAsync created =
                        (contents) =>
                        {
                            var resources = contents.Cast<TResource>().ToArray();
                            // TODO: try catch
                            //if (!(content is TResource))
                            //    Assert.Fail($"Could not cast {content.GetType().FullName} to {typeof(TResource).FullName}.");

                            if (onContents.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(MultipartAcceptArrayResponseAsync), 
                                    thisAgain, requestAgain, paramInfo, onSuccess).AsTask();
                            var result = onContents(resources);
                            return new AttachedHttpResponseMessage<TResult>(result).ToTask<HttpResponseMessage>();
                        };
                    return onSuccess(created);
                });

            application.SetInstigatorGeneric(
                typeof(MultipartResponseAsync<>),
                (type, thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    var callbackWrapperType = typeof(CallbackWrapper<,>).MakeGenericType(
                        paramInfo.ParameterType.GenericTypeArguments.Append(typeof(TResult)).ToArray());

                    //  new CallbackWrapper<TResource, TResult>(onContents, null, thisAgain, requestAgain, paramInfo, onSuccess);
                    var instantiationParams = new object[]
                        {
                            onContents,
                            null,
                            thisAgain,
                            requestAgain,
                            paramInfo,
                            onSuccess,
                        };
                    var scope = Activator.CreateInstance(callbackWrapperType, instantiationParams);

                    var multipartResponseMethodInfoGeneric = callbackWrapperType.GetMethod("MultipartResponseAsyncGeneric", BindingFlags.Public | BindingFlags.Instance);
                    var multipartResponseMethodInfoBound = multipartResponseMethodInfoGeneric;
                    var dele = Delegate.CreateDelegate(type, scope, multipartResponseMethodInfoBound);
                    return onSuccess((object)dele);
                });
        }

        private static void MultipartContentObjectResponse<TResource, TResult>(this IApplication application,
            Func<object[], TResult> onContents)
        {
            application.SetInstigator(
                typeof(EastFive.Api.MultipartAcceptArrayResponseAsync),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    EastFive.Api.MultipartAcceptArrayResponseAsync created =
                        (contents) =>
                        {
                            var resources = contents.ToArray();
                            // TODO: try catch
                            //if (!(content is TResource))
                            //    Assert.Fail($"Could not cast {content.GetType().FullName} to {typeof(TResource).FullName}.");

                            if (onContents.IsDefaultOrNull())
                                return FailureToOverride<TResource>(
                                    typeof(MultipartAcceptArrayResponseAsync),
                                    thisAgain, requestAgain, paramInfo, onSuccess).AsTask();
                            var result = onContents(resources);
                            return new AttachedHttpResponseMessage<TResult>(result).ToTask<HttpResponseMessage>();
                        };
                    return onSuccess(created);
                });

            application.SetInstigatorGeneric(
                typeof(MultipartResponseAsync<>),
                (type, thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    var callbackWrapperInstance = typeof(CallbackWrapper<,>).MakeGenericType(
                        new Type[] { type.GenericTypeArguments.First(), typeof(TResult) });
                    //var scope = new CallbackWrapper<TResource, TResult>(null, onContents, thisAgain, requestAgain, paramInfo, onSuccess);
                    var scope = Activator.CreateInstance(callbackWrapperInstance, 
                        new object[] { null, onContents, thisAgain, requestAgain, paramInfo, onSuccess });
                    var multipartResponseMethodInfoGeneric = callbackWrapperInstance.GetMethod("MultipartResponseAsyncGeneric", BindingFlags.Public | BindingFlags.Instance);
                    var multipartResponseMethodInfoBound = multipartResponseMethodInfoGeneric; // multipartResponseMethodInfoGeneric.MakeGenericMethod(type.GenericTypeArguments);
                    var dele = Delegate.CreateDelegate(type, scope, multipartResponseMethodInfoBound);
                    return onSuccess((object)dele);
                });
        }

        public class CallbackWrapper<TResource, TResult>
        {
            private Func<TResource[], TResult> callback;
            private Func<object[], TResult> callbackObjs;
            private HttpApplication thisAgain;
            private HttpRequestMessage requestAgain;
            private ParameterInfo paramInfo;
            private Func<object, Task<HttpResponseMessage>> onSuccess;
            
            public CallbackWrapper(Func<TResource[], TResult> onContents, Func<object[], TResult> callbackObjs,
                HttpApplication thisAgain, HttpRequestMessage requestAgain, ParameterInfo paramInfo,
                Func<object, Task<HttpResponseMessage>> onSuccess)
            {
                this.callback = onContents;
                this.callbackObjs = callbackObjs;
                this.thisAgain = thisAgain;
                this.requestAgain = requestAgain;
                this.paramInfo = paramInfo;
                this.onSuccess = onSuccess;
            }

            public async Task<HttpResponseMessage> MultipartResponseAsyncGeneric(IEnumerableAsync<TResource> resources)
            {
                if (!callback.IsDefaultOrNull())
                {
                    var resourcesArray = await resources.ToArrayAsync();
                    var result = callback(resourcesArray);
                    return new AttachedHttpResponseMessage<TResult>(result);
                }
                if (!callbackObjs.IsDefaultOrNull())
                {
                    var resourcesArray = await resources.ToArrayAsync();
                    var result = callbackObjs(resourcesArray.Cast<object>().ToArray());
                    return new AttachedHttpResponseMessage<TResult>(result);
                }
                return FailureToOverride<TResource>(typeof(EastFive.Api.MultipartResponseAsync<>), 
                    thisAgain, requestAgain, paramInfo, onSuccess);
            }
            
        }

        private static void NoContentResponse<TResource, TResult>(this IApplication application,
            Func<TResult> onNoContent)
        {
            application.SetInstigator(
                typeof(NoContentResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    NoContentResponse created =
                        () =>
                        {
                            if (onNoContent.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(NoContentResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onNoContent();
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                });
        }

        private static void NotFoundResponse<TResource, TResult>(this IApplication application,
            Func<TResult> onNotFound)
        {
            application.SetInstigator(
                typeof(NotFoundResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    NotFoundResponse notFound =
                        () =>
                        {
                            if (onNotFound.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(NotFoundResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onNotFound();
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(notFound);
                });
        }

        private static void UnauthorizedResponse<TResource, TResult>(this IApplication application,
            Func<TResult> onUnauthorized)
        {
            application.SetInstigator(
                typeof(UnauthorizedResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    UnauthorizedResponse created =
                        () =>
                        {
                            if (onUnauthorized.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(UnauthorizedResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onUnauthorized();
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                });
        }

        private static void GeneralConflictResponse<TResource, TResult>(this IApplication application,
            Func<string, TResult> onGeneralConflictResponse)
        {
            application.SetInstigator(
                typeof(GeneralConflictResponse),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    GeneralConflictResponse created =
                        (reason) =>
                        {
                            if (onGeneralConflictResponse.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(GeneralConflictResponse), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = onGeneralConflictResponse(reason);
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
               });
        }

        private static void ExecuteBackgroundResponse<TResource, TResult>(this IApplication application,
            Func<IExecuteAsync, Task<TResult>> onExecuteBackgroundResponse)
        {
            application.SetInstigator(
                typeof(ExecuteBackgroundResponseAsync),
                (thisAgain, requestAgain, paramInfo, onSuccess) =>
                {
                    ExecuteBackgroundResponseAsync created =
                        async (executionContent) =>
                        {
                            if (onExecuteBackgroundResponse.IsDefaultOrNull())
                                return FailureToOverride<TResource>(typeof(ExecuteBackgroundResponseAsync), thisAgain, requestAgain, paramInfo, onSuccess);
                            var result = await onExecuteBackgroundResponse(executionContent);
                            return new AttachedHttpResponseMessage<TResult>(result);
                        };
                    return onSuccess(created);
                });
        }


        private static HttpResponseMessage FailureToOverride<TResource>(Type typeOfResponse,
            HttpApplication application,
            HttpRequestMessage request, ParameterInfo paramInfo,
            Func<object, Task<HttpResponseMessage>> onSuccess)
        {
            return new NoOverrideHttpResponseMessage<TResource>(paramInfo.ParameterType, request);
        }

        #endregion


        //public static async Task<TResult> UrlAsync<TResource, TResultInner, TResult>(this IApplication application,
        //        IInvokeApplication invokeApplication,
        //        HttpMethod method, Uri location,
        //    Func<TResultInner, TResult> onExecuted,

        //    Func<TResource, TResult> onContent = default(Func<TResource, TResult>),
        //    Func<TResource[], TResult> onContents = default(Func<TResource[], TResult>),
        //    Func<object[], TResult> onContentObjects = default(Func<object[], TResult>),
        //    Func<string, TResult> onHtml = default(Func<string, TResult>),
        //    Func<TResult> onCreated = default(Func<TResult>),
        //    Func<TResource, string, TResult> onCreatedBody = default(Func<TResource, string, TResult>),
        //    Func<TResult> onUpdated = default(Func<TResult>),

        //    Func<Uri, TResult> onRedirect = default(Func<Uri, TResult>),

        //    Func<TResult> onBadRequest = default(Func<TResult>),
        //    Func<TResult> onUnauthorized = default(Func<TResult>),
        //    Func<TResult> onExists = default(Func<TResult>),
        //    Func<TResult> onNotFound = default(Func<TResult>),
        //    Func<Type, TResult> onRefDoesNotExistsType = default(Func<Type, TResult>),
        //    Func<string, TResult> onFailure = default(Func<string, TResult>),

        //    Func<TResult> onNotImplemented = default(Func<TResult>),
        //    Func<IExecuteAsync, Task<TResult>> onExecuteBackground = default(Func<IExecuteAsync, Task<TResult>>))
        //{
        //    throw new NotImplementedException();
        //    //var request = invokeApplication.GetRequest<TResource>();
        //    ////request.Method = method;
        //    ////request.RequestUri = location;

        //    //application.CreatedResponse<TResource, TResult>(onCreated);
        //    //application.CreatedBodyResponse<TResource, TResult>(onCreatedBody);
        //    //application.BadRequestResponse<TResource, TResult>(onBadRequest);
        //    //application.AlreadyExistsResponse<TResource, TResult>(onExists);
        //    //application.RefNotFoundTypeResponse(onRefDoesNotExistsType);
        //    //application.RedirectResponse<TResource, TResult>(onRedirect);
        //    //application.NotImplementedResponse<TResource, TResult>(onNotImplemented);

        //    //application.ContentResponse(onContent);
        //    //application.ContentTypeResponse<TResource, TResult>((body, contentType) => onContent(body));
        //    //application.MultipartContentResponse(onContents);
        //    //if (!onContentObjects.IsDefaultOrNull())
        //    //    application.MultipartContentObjectResponse<TResource, TResult>(onContentObjects);
        //    //application.NotFoundResponse<TResource, TResult>(onNotFound);
        //    //application.HtmlResponse<TResource, TResult>(onHtml);

        //    //application.NoContentResponse<TResource, TResult>(onUpdated);
        //    //application.UnauthorizedResponse<TResource, TResult>(onUnauthorized);
        //    //application.GeneralConflictResponse<TResource, TResult>(onFailure);
        //    //application.ExecuteBackgroundResponse<TResource, TResult>(onExecuteBackground);

        //    //var response = await application.SendAsync(request.Request);

        //    //if (response is IDidNotOverride)
        //    //    (response as IDidNotOverride).OnFailure();

        //    //if (!(response is IReturnResult))
        //    //    throw new Exception($"Failed to override response with status code `{response.StatusCode}` for {typeof(TResource).FullName}\nResponse:{response.ReasonPhrase}");

        //    //var attachedResponse = response as IReturnResult;
        //    //var result = attachedResponse.GetResultCasted<TResultInner>();
        //    //return onExecuted(result);
        //}
    }
}
