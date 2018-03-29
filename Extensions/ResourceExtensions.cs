﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Routing;
using BlackBarLabs.Extensions;
using System.Web;
using BlackBarLabs.Api.Resources;
using System.Threading.Tasks;
using System.Net;
using EastFive.Linq;
using EastFive;
using EastFive.Extensions;
using EastFive.Api;

namespace BlackBarLabs.Api
{
    public static class ResourceExtensions
    {
        public static IEnumerable<Guid> ParseGuidString(this string guidString)
        {
            if (String.IsNullOrWhiteSpace(guidString))
                return new Guid[] { };

            var guids = guidString.Split(new char[','])
                .Where(guidStringCandidate => { Guid g; return Guid.TryParse(guidStringCandidate, out g); })
                .Select(guidStringCandidate => { Guid g; Guid.TryParse(guidStringCandidate, out g); return g; });
            return guids;
        }

        public static TResult ParseGuidString<TResult>(this string guidString,
            Func<IEnumerable<Guid>, TResult> multiple,
            Func<TResult> none)
        {
            if (String.IsNullOrWhiteSpace(guidString))
                return none();

            var guids = guidString.Split(new char[] { ',' })
                .Where(guidStringCandidate =>
                {
                    Guid g;
                    var validGuid = Guid.TryParse(guidStringCandidate, out g);
                    return validGuid;
                })
                .Select(guidStringCandidate => { Guid g; Guid.TryParse(guidStringCandidate, out g); return g; })
                .ToArray();
            return multiple(guids);
        }

        public static Resources.WebId GetWebId<TController>(this UrlHelper url,
            Guid id,
            string routeName = "DefaultApi")
        {
            var controllerName =
                typeof(TController).Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var location = url.Link(routeName, new { Controller = controllerName, Id = id });
            return new Resources.WebId
            {
                Key = id.ToString(),
                UUID = id,
                URN = id.ToWebUrn(controllerName, ""),
                Source = new Uri(location),
            };
        }

        public static Resources.WebId GetWebId(this UrlHelper url,
            Guid id, string ns)
        {
            return new Resources.WebId
            {
                Key = id.ToString(),
                UUID = id,
                URN = id.ToWebUrn(ns, ""),
            };
        }

        public static Resources.WebId[] GetWebIds<TController>(this UrlHelper url,
            Guid [] ids,
            string routeName = "DefaultApi")
        {
            return ids.Select(id => url.GetWebId<TController>(id)).ToArray();
        }

        public static Resources.WebIdQuery GetWebIdQuery<TController>(this UrlHelper url,
            Guid [] ids,
            string routeName = "DefaultApi")
        {
            var controllerName =
                typeof(TController).Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var keys = ids.Select(id => id.ToString("N")).Join(",");
            var uuid = keys;
            var urns = ids.Select(id => id.ToWebUrn(controllerName, "").AbsoluteUri).Join(",");
            var location = url.Link(routeName,
                new Dictionary<string, object>
                {
                    { "Controller", controllerName },
                    { $"{controllerName}Id" , uuid },
                });
            return new Resources.WebIdQuery
            {
                UUIDs = keys,
                URN = urns,
                Source = location,
            };
        }

        public static Resources.WebId GetWebId<TController>(this UrlHelper url,
            Guid? idMaybe,
            string routeName = "DefaultApi")
        {
            if (!idMaybe.HasValue)
                return default(WebId);
            return url.GetWebId<TController>(idMaybe.Value, routeName);
        }

        public static Resources.WebId GetWebId<TController>(this UrlHelper url,
            string urn,
            string routeName = "DefaultApi")
        {
            var controllerName =
                typeof(TController).Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var location = url.Link(routeName, new { Controller = controllerName, Id = default(Guid) });
            return new Resources.WebId
            {
                Key = default(Guid).ToString(),
                UUID = default(Guid),
                URN = new Uri(urn),
                Source = new Uri(location)
            };
        }

        public static Resources.WebId GetWebId(this UrlHelper url,
            Type controllerType,
            string urnNamespace,
            string routeName = "DefaultApi")
        {
            var controllerName =
                controllerType.Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var location = url.Link(routeName, new { Controller = controllerName });

            return new Resources.WebId
            {
                Key = string.Empty,
                UUID = Guid.Empty,
                URN = controllerType.GetUrn(urnNamespace),
                Source = new Uri(location),
            };
        }

        public static Uri GetUrn(this Type controllerType,
            string urnNamespace)
        {
            var controllerName =
                controllerType.Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);

            var urn = new Uri("urn:" + urnNamespace + ":" + controllerName);
            var resourceAttributeTypes = controllerType.GetCustomAttributes<Api.ResourceTypeAttribute>();
            if (resourceAttributeTypes.Length > 0)
            {
                var urnModelType = resourceAttributeTypes[0].Urn;
                var modelAttributeTypes = controllerType.GetCustomAttributes<Web.ResourceTypeAttribute>();
                if (modelAttributeTypes.Length > 0)
                {
                    urn = new Uri(modelAttributeTypes[0].Urn);
                }
            }
            return urn;
        }

        public static Uri GetLocation(this UrlHelper url, Type controllerType,
            string routeName = "DefaultApi")
        {
            var controllerName =
                controllerType.Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var location = url.Link(routeName, new { Controller = controllerName });
            return new Uri(location);
        }

        public static Uri GetLocation<TController>(this UrlHelper url,
            string routeName = "DefaultApi")
        {
            if (String.IsNullOrWhiteSpace(routeName))
            {
                var routePrefixes = typeof(TController)
                            .GetCustomAttributes<System.Web.Http.RoutePrefixAttribute>()
                            .Select(routePrefix => routePrefix.Prefix)
                            .ToArray();
                if (routePrefixes.Any())
                    routeName = routePrefixes[0];
                else
                    routeName = "DefaultApi";
            }

            var controllerName =
                typeof(TController).Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var location = url.Link(routeName, new { Controller = controllerName });
            return new Uri(location);
        }

        public static Uri GetLocation<TController>(this UrlHelper url,
            Guid? idMaybe,
            string routeName = default(string))
        {
            if (idMaybe.HasValue)
                return url.GetLocation<TController>(idMaybe.Value, routeName);
            return default(Uri);
        }

        public static Uri GetLocation(this UrlHelper url, Type controllerType, Guid id,
            string routeName = "DefaultApi")
        {
            if (String.IsNullOrWhiteSpace(routeName))
            {
                var routePrefixes = controllerType
                            .GetCustomAttributes<System.Web.Http.RoutePrefixAttribute>()
                            .Select(routePrefix => routePrefix.Prefix)
                            .ToArray();
                if (routePrefixes.Any())
                    routeName = routePrefixes[0];
                else
                    routeName = "DefaultApi";
            }

            var controllerName =
                controllerType.Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var location = url.Link(routeName, new { Controller = controllerName, Id = id });
            return new Uri(location);
        }

        public static Uri GetLocation<TController>(this UrlHelper url,
            Guid id,
            string routeName = default(string))
        {
            return url.GetLocation(typeof(TController), id, routeName);
        }
        
        public static Uri GetLocation<TController>(this UrlHelper url,
            string action,
            string routeName = "DefaultApi")
        {
            var controllerName =
                typeof(TController).Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var location = url.Link(routeName, new { Controller = controllerName, Action = action });
            return new Uri(location);
        }

        public static Uri GetLocationWithQuery(this UrlHelper url, Type controllerType, string query,
            string routeName = "DefaultApi")
        {
            var controllerName =
                controllerType.Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var location = url.Link(routeName, new { Controller = controllerName });
            query = query.StartsWith($"?") ? query.Substring(1) : query;
            var uri = new UriBuilder(location) {Query = query};
            return uri.Uri;
        }

        public static Uri GetLocationWithId(this UrlHelper url, Type controllerType, Guid id,
            string routeName = "DefaultApi")
        {
            var controllerName =
                controllerType.Name.TrimEnd("Controller",
                    (trimmedName) => trimmedName, (originalName) => originalName);
            var location = url.Link(routeName, new { Controller = controllerName });
            location = location + "/" + id;
            return new Uri(location);
        }

        public static Uri AddParameter(this Uri url, string paramName, string paramValue)
        {
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query[paramName] = paramValue;
            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri;
        }

        public static string ToStringOneCharacter(this DayOfWeek dayOfWeek)
        {
            var dtInfo = new System.Globalization.DateTimeFormatInfo();
            dtInfo.AbbreviatedDayNames = new string[] { "U", "M", "T", "W", "R", "F", "S" }; // MTWRFSU
            var dayOfWeekString = dtInfo.GetDayName(dayOfWeek);
            return dayOfWeekString;
        }

        public static TResult ToDayOfWeek<TResult>(this string oneCharacterDayOfWeekAsString,
            Func<DayOfWeek, TResult> success,
            Func<TResult> noMatch)
        {
            var mapping = new Dictionary<string, DayOfWeek>()
            {
                { "U", DayOfWeek.Sunday },
                { "M", DayOfWeek.Monday },
                { "T", DayOfWeek.Tuesday },
                { "W", DayOfWeek.Wednesday },
                { "R", DayOfWeek.Thursday },
                { "F", DayOfWeek.Friday },
                { "S", DayOfWeek.Saturday },
            };
            if (mapping.ContainsKey(oneCharacterDayOfWeekAsString.ToUpper()))
                return success(mapping[oneCharacterDayOfWeekAsString.ToUpper()]);
            DayOfWeek dayOfWeek;
            if (Enum.TryParse(oneCharacterDayOfWeekAsString, out dayOfWeek))
                return success(dayOfWeek);
            return noMatch();
        }
        
        public static bool IsEmpty(this Resources.WebId webId)
        {
            return
                webId.IsDefaultOrNull() ||
                (
                    String.IsNullOrWhiteSpace(webId.Key) &&
                    webId.UUID.IsDefaultOrEmpty() &&
                    webId.URN.IsDefault() &&
                    webId.Source.IsDefault()
                );
        }

        public static TResult GetUUID<TResult>(this Resources.WebId webId,
            Func<Guid, TResult> success,
            Func<TResult> isEmpty)
        {
            if (webId.IsEmpty())
                return isEmpty();
            if (webId.UUID.IsDefaultOrEmpty())
                return isEmpty();
            return success(webId.UUID);
        }

        public static Guid? ToGuid(this Resources.WebId webId)
        {
            if (default(WebId) == webId)
                return default(Guid?);
            if (webId.IsEmpty())
                return default(Guid);
            if (webId.UUID.IsDefaultOrEmpty())
                return default(Guid);
            return webId.UUID;
        }
        
        public static Guid? ToGuid(this Resources.WebIdQuery webIdQuery)
        {
            if (default(WebIdQuery) == webIdQuery)
                return default(Guid?);
            return webIdQuery.Parse(
                (id) => (Guid?)id,
                (ids) => default(Guid?),
                () => default(Guid?),
                () => default(Guid?),
                () => default(Guid?),
                () => default(Guid?));
        }

        public static Guid[] ToGuids(this Resources.WebIdQuery webIdQuery)
        {
            if (default(WebIdQuery) == webIdQuery)
                return default(Guid []);
            return webIdQuery.Parse(
                (id) => new Guid[] { id },
                (ids) => ids.ToArray(),
                () => new Guid[] { },
                () => new Guid[] { },
                () => new Guid[] { },
                () => new Guid[] { });
        }

        public static Resources.WebId GetWebIdUUID(this Guid uuId)
        {
            return new Resources.WebId() { UUID = uuId };
        }
        
        public static Guid[] ToGuids(this WebId[] webIds)
        {
            var guids = webIds
                .NullToEmpty()
                .Select(wId => wId.UUID)
                .ToArray();
            return guids;
        }

        public static WebId[] ToWebIds<TController>(this Guid[] guids, UrlHelper url)
        {
            var webIds = guids
                .NullToEmpty()
                .Select(guid => url.GetWebId<TController>(guid))
                .ToArray();
            return webIds;
        }

        public static DateTime? AsZulu(this DateTime? datetime)
        {
            return datetime.HasValue && (!datetime.Value.IsDefault()) ?
                    datetime.Value.ToUniversalTime()
                    :
                    default(DateTime?);
        }
        
    }
}
