﻿using BlackBarLabs.Extensions;
using EastFive.Collections.Generic;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using EastFive.Serialization;
using System.Net.NetworkInformation;
using EastFive.Extensions;
using BlackBarLabs.Web;
using System.Reflection;
using System.Net.Http;
using EastFive.Linq;
using System.Net;
using BlackBarLabs.Api;
using BlackBarLabs;
using System.Threading;

namespace EastFive.Api.Modules
{
    public abstract class ApplicationHandler : System.Net.Http.DelegatingHandler
    {
        protected System.Web.Http.HttpConfiguration config;
        private string applicationProperty = Guid.NewGuid().ToString("N");

        public ApplicationHandler(System.Web.Http.HttpConfiguration config)
        {
            this.config = config;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // In the event that SendAsync(HttpApplication ...) calls base.SendAsync(request, cancellationToken) then this method
            // would be called. This method would then in turn call back to SendAsync(HttpApplication...) which would cause 
            // recursion to stack overflow. Therefore, a property (.applicationProperty) is added to the request to identify if this method has
            // already been called.
            // This situation can be avoided by using the contiuation callback instead of calling base, this serves a defensive programming.

            // Check if this method has already been called
            if (request.Properties.ContainsKey(applicationProperty))
                // TODO: Log event here.
                return base.SendAsync(request, cancellationToken);

            return request.GetApplication(
                httpApp =>
                {
                    // add applicationProperty as a property to identify this method has already been called.
                    request.Properties.Add(applicationProperty, httpApp);
                    return SendAsync(httpApp, request, cancellationToken, (requestBase, cancellationTokenBase) => base.SendAsync(requestBase, cancellationTokenBase));
                },
                () => base.SendAsync(request, cancellationToken));
        }

        protected abstract Task<HttpResponseMessage> SendAsync(HttpApplication httpApp, HttpRequestMessage request, CancellationToken cancellationToken, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> continuation);
    }
}
