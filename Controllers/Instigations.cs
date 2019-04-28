﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EastFive.Api.Controllers
{
    public struct Security
    {
        public Guid performingAsActorId;
        public System.Security.Claims.Claim[] claims;
    }

    public struct SessionToken
    {
        public Guid sessionId;
        public Claim[] claims;
        public Guid? accountIdMaybe;
    }

    public struct ApiSecurity
    {

    }

    public struct ContentBytes
    {
        public byte [] content;
        public MediaTypeHeaderValue contentType;
    }

    public struct ContentStream
    {
        public System.IO.Stream content;
        public MediaTypeHeaderValue contentType;
    }
    
    public struct DateTimeEmpty
    {

    }
    
    public struct DateTimeAny
    {

    }

    public struct DateTimeQuery
    {
        public DateTime start;
        public DateTime end;

        public DateTimeQuery(DateTime start, DateTime end)
        {
            this.start = start;
            this.end = end;
        }
    }

    public struct WebIdAny
    {

    }

    public struct WebIdNone
    {

    }

    public struct WebIdNot
    {
        public Guid notUUID;
    }
}
