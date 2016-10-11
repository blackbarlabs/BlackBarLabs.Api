﻿using BlackBarLabs.Api.Resources;
using BlackBarLabs.Collections.Generic;
using BlackBarLabs.Core.Collections;
using BlackBarLabs.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;

namespace BlackBarLabs.Api
{
    public static partial class QueryExtensions
    {
        private class WebIdGuid : WebIdQuery
        {
            public Guid Guid { get; private set; }

            public WebIdGuid(Guid guid)
            {
                this.Guid = guid;
            }
        }

        private class WebIdGuids : WebIdQuery
        {
            public Guid [] Guids { get; private set; }

            public WebIdGuids(Guid [] guids)
            {
                this.Guids = guids;
            }
        }


        private class WebIdEmpty : WebIdQuery
        {
        }

        private class WebIdBadRequest : WebIdQuery
        {
        }

        private class WebIdUnspecified : WebIdQuery
        {
        }

        [AttributeUsage(AttributeTargets.Method)]
        private class QueryParameterTypeAttribute : System.Attribute
        {
            public QueryParameterTypeAttribute()
            {
            }

            private Type webIdQueryType;
            public Type WebIdQueryType
            {
                get
                {
                    return this.webIdQueryType;
                }
                set
                {
                    webIdQueryType = value;
                }
            }
        }

        [QueryParameterType(WebIdQueryType = typeof(WebIdGuid))]
        public static Guid ParamSingle(this WebIdQuery query)
        {
            if (!(query is WebIdGuid))
                throw new InvalidOperationException("Do not use ParamSingle outside of ParseAsync");

            var wiqo = query as WebIdGuid;
            return wiqo.Guid;
        }

        [QueryParameterType(WebIdQueryType = typeof(WebIdEmpty))]
        public static Guid? ParamEmpty(this WebIdQuery query)
        {
            if (!(query is WebIdEmpty))
                throw new InvalidOperationException("Do not use ParamEmpty outside of ParseAsync");
            return default(Guid?);
        }

        [QueryParameterType(WebIdQueryType = typeof(WebIdGuids))]
        public static Guid[] ParamOr(this WebIdQuery query)
        {
            if (!(query is WebIdGuids))
                throw new InvalidOperationException("Do not use ParamOr outside of ParseAsync");

            var wiqo = query as WebIdGuids;
            return wiqo.Guids;
        }
    }
}