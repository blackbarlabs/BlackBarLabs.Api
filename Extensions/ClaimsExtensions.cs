﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using BlackBarLabs.Extensions;

namespace BlackBarLabs.Api
{
    public static class ClaimsExtensions
    {
        public static Task<HttpResponseMessage> GetAccountIdAsync(this IEnumerable<System.Security.Claims.Claim> claims,
            HttpRequestMessage request, string accountIdClaimType,
            Func<Guid, Task<HttpResponseMessage>> success)
        {
            var adminClaim = claims
                .FirstOrDefault((claim) => String.Compare(claim.Type, accountIdClaimType) == 0);

            if (default(System.Security.Claims.Claim) == adminClaim)
                return request.CreateResponse(HttpStatusCode.Unauthorized).ToTask();

            var accountId = Guid.Parse(adminClaim.Value);
            return success(accountId);
        }

        public static Task<HttpResponseMessage[]> GetAccountIdAsync(this IEnumerable<System.Security.Claims.Claim> claims, HttpRequestMessage request, string accountIdClaimType,
            Func<Guid, Task<HttpResponseMessage[]>> success)
        {
            var adminClaim = claims
                .FirstOrDefault((claim) => String.Compare(claim.Type, accountIdClaimType) == 0);

            if (default(System.Security.Claims.Claim) == adminClaim)
                return request.CreateResponse(HttpStatusCode.Unauthorized).ToEnumerable().ToArray().ToTask();

            var accountId = Guid.Parse(adminClaim.Value);
            return success(accountId);
        }
    }
}