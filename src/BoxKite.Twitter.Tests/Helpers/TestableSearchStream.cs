﻿// (c) 2012-2013 Nick Hodge mailto:hodgenick@gmail.com & Brendan Forster
// License: MS-PL

using System;
using System.Net.Http;
using System.Threading.Tasks;
using BoxKite.Twitter.Modules.Streaming;

namespace BoxKite.Twitter.Tests.Modules
{
    public static class SearchStreamExtensions
    {
        public static ISearchStream StartSearchStream(this IUserSession session, string track = null, string follow = null, string locations = null)
        {
            var searchStream = new SearchStream(session);
            searchStream.SearchParameters = searchStream.ChangeSearchParameters(track, follow, locations);
            Func<Task<HttpResponseMessage>> startConnection =
                () =>
                {
                    var resp = ((TestableSession)session).MakeResponse();
                    return resp;
                };
           searchStream.CreateOpenConnection = startConnection;
            return searchStream;
        }
    }
}