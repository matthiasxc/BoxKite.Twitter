﻿// (c) 2012-2013 Nick Hodge mailto:hodgenick@gmail.com & Brendan Forster
// License: MS-PL

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace BoxKite.Twitter
{
    public interface IUserSession
    {
        Task<HttpResponseMessage> GetAsync(string relativeUrl, SortedDictionary<string, string> parameters);
        Task<HttpResponseMessage> PostAsync(string relativeUrl, SortedDictionary<string, string> parameters);

        Task<HttpResponseMessage> PostFileAsync(string url, SortedDictionary<string, string> parameters, string fileName, string fileContentsKey, byte[] fileContents = null, Stream srImage = null);
        HttpRequestMessage CreateGet(string fullUrl, SortedDictionary<string, string> parameters);
        HttpRequestMessage CreatePost(string fullUrl, SortedDictionary<string, string> parameters);
    }
}
