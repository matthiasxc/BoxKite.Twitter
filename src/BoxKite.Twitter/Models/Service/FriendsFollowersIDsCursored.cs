﻿// (c) 2012-2013 Nick Hodge mailto:hodgenick@gmail.com & Brendan Forster
// License: MS-PL

using System.Collections.Generic;
using Newtonsoft.Json;

namespace BoxKite.Twitter.Models
{
    public class FriendsFollowersIDsCursored : TwitterControlBase
    {
        [JsonProperty(PropertyName = "ids")]
        public IEnumerable<int> IDs { get; set; }
        public long previous_cursor { get; set; }
        public long next_cursor { get; set; }
    }
}
