﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

using BlackBarLabs.Extensions;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using EastFive;

namespace BlackBarLabs.Api.Resources
{
    [TypeConverter(typeof(WebIdQueryConverter))]
    public class WebIdQuery : IQueryParameter
    {
        public string UUIDs { get; set; }
        
        public string URN { get; set; }
        
        public string Source { get; set; }

        private string query;

        public bool IsSpecified()
        {
            return true;
        }

        public static implicit operator WebIdQuery(string query)
        {
            return new WebIdQuery() { query = query };
        }

        public static implicit operator WebIdQuery(Guid value)
        {
            return new WebIdQuery() { query = value.ToString() };
        }

        public static implicit operator WebIdQuery(WebId value)
        {
            return (default(WebId) != value) ?
                new WebIdQuery()
                {
                    query = value.UUID.ToString()
                }
                :
                default(WebIdQuery);
        }

        public static implicit operator WebIdQuery(Guid [] values)
        {
            var query = String.Join(",", values.Select(value => value.ToString()));
            return new WebIdQuery() { query = query };
        }

        public TResult Parse<TResult>(
            Func<Guid[], TResult> multiple,
            Func<TResult> empty,
            Func<TResult> unparsable)
        {
            if (String.IsNullOrWhiteSpace(this.query))
                return empty();

            Guid singleGuid;
            if(Guid.TryParse(this.query, out singleGuid))
                return multiple(singleGuid.AsEnumerable().ToArray());

            // Catch case of empty array
            if (this.query == "[]")
                return multiple(new Guid[] { });

            var guidRegex = @"([a-f0-9A-F]{32}|([a-f0-9A-F]{8}-[a-f0-9A-F]{4}-[a-f0-9A-F]{4}-[a-f0-9A-F]{4}-[a-f0-9A-F]{12}))";
            if(!Regex.IsMatch(this.query, guidRegex))
                return unparsable();

            var matches = Regex.Matches(this.query, guidRegex);
            var ids = RegexToEnumerable(matches).ToArray();
            return multiple(ids);
        }

        private static IEnumerable<Guid> RegexToEnumerable(MatchCollection matches)
        {
            foreach (Match match in matches)
            {
                yield return Guid.Parse(match.Value);
            }
        }

        public TResult Parse<TResult>(
            HttpRequestMessage request,
            Func<Guid, TResult> single,
            Func<IEnumerable<Guid>, TResult> multiple,
            Func<TResult> unspecified,
            Func<TResult> unparsable)
        {
            if (String.IsNullOrWhiteSpace(request.RequestUri.Query))
            {
                if (String.IsNullOrWhiteSpace(this.query))
                    return unspecified();
                Guid singleGuid;
                if (Guid.TryParse(this.query, out singleGuid))
                {
                    return single(singleGuid);
                }
            }
            return Parse(multiple, unspecified, unparsable);
        }

        public TResult Parse2<TResult>(
            HttpRequestMessage request,
            Func<Guid, TResult> single,
            Func<IEnumerable<Guid>, TResult> multiple,
            Func<TResult> unspecified,
            Func<TResult> unparsable)
        {
            if (String.IsNullOrWhiteSpace(request.RequestUri.Query))
            {
                if (String.IsNullOrWhiteSpace(this.query))
                    return unspecified();
                Guid singleGuid;
                if (Guid.TryParse(this.query, out singleGuid))
                {
                    return single(singleGuid);
                }
            }
            return Parse(
                (guids) =>
                {
                    if (guids.Length == 1 && (!query.Contains(",") && !query.Contains("[")))
                        return single(guids.First());
                    return multiple(guids);
                },
                unspecified, unparsable);
        }

        public TResult Parse<TResult>(
            Func<Guid, TResult> single,
            Func<IEnumerable<Guid>, TResult> multiple,
            Func<TResult> unspecified,
            Func<TResult> empty,
            Func<TResult> any,
            Func<TResult> unparsable)
        {
            if (String.IsNullOrWhiteSpace(this.query))
                return unspecified();
            if (String.Compare("empty", this.query.ToLower()) == 0)
                return empty();
            if (String.Compare("null", this.query.ToLower()) == 0)
                return empty();
            if (String.Compare("any", this.query.ToLower()) == 0)
                return any();

            if (this.query.First() != '[' && this.query.Last() != ']')
            {
                if (String.IsNullOrWhiteSpace(this.query))
                    return unspecified();
                Guid singleGuid;
                if (Guid.TryParse(this.query, out singleGuid))
                {
                    return single(singleGuid);
                }
            }
            return Parse(multiple, unspecified, unparsable);
        }

        public TResult Parse<TResult>(
            Func<QueryMatchAttribute, TResult> parsed,
            Func<string, TResult> unparsable)
        {
            return this.ParseInternal(parsed, unparsable);
        }

        public static string Compile(Guid[] guids)
        {
            return $"[{guids.Select(guid => guid.ToString("N")).Join(",")}]";
        }
    }

    class WebIdQueryConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context,
            CultureInfo culture, object value)
        {
            if (value is string)
            {
                var valueString = value as string;
                WebIdQuery query = valueString;
                return query;
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
