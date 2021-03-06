// (c) 2012-2013 Nick Hodge mailto:hodgenick@gmail.com & Brendan Forster
// License: MS-PL

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using BoxKite.Twitter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reactive.EventAggregator;

namespace BoxKite.Twitter
{
    public class UserStream : IUserStream
    {
        readonly Subject<Tweet> tweets = new Subject<Tweet>();
        readonly Subject<DirectMessage> directmessages = new Subject<DirectMessage>();
        readonly Subject<StreamEvent> events = new Subject<StreamEvent>();
        readonly Subject<DeleteEvent> deleteevents = new Subject<DeleteEvent>();
        readonly Subject<StreamScrubGeo> scrubgeorequests = new Subject<StreamScrubGeo>();
        readonly Subject<StreamLimitNotice> limitnotices = new Subject<StreamLimitNotice>();
        readonly Subject<StreamStatusWithheld> statuswithheld = new Subject<StreamStatusWithheld>();
        readonly Subject<StreamUserWithheld> userwithheld = new Subject<StreamUserWithheld>();
        readonly Subject<IEnumerable<long>> friends = new Subject<IEnumerable<long>>();

        readonly TimeSpan initialDelay = TimeSpan.FromSeconds(20);
        readonly Func<Task<HttpResponseMessage>> createOpenConnection;

        public IObservable<Tweet> Tweets { get { return tweets; } }
        public IObservable<DirectMessage> DirectMessages { get { return directmessages; } }
        public IObservable<StreamEvent> Events { get { return events; } }
        public IObservable<DeleteEvent> DeleteEvents { get { return deleteevents; } }
        public IObservable<StreamScrubGeo> ScrubGeoRequests { get { return scrubgeorequests; } }
        public IObservable<StreamLimitNotice> LimitNotices { get { return limitnotices; } }
        public IObservable<StreamStatusWithheld> StatusWithheld { get { return statuswithheld; } }
        public IObservable<StreamUserWithheld> UserWithheld { get { return userwithheld; } }
        public IObservable<IEnumerable<long>> Friends { get { return friends; } }

        public bool IsActive { get; private set; }
        public CancellationTokenSource CancelUserStream { get; set; }
        public TimeSpan delay = TimeSpan.FromSeconds(5);

        private IEventAggregator _eventAggregator;

        public UserStream(Func<Task<HttpResponseMessage>> createOpenConnection)
        {
            this.createOpenConnection = createOpenConnection;
            CancelUserStream = new CancellationTokenSource();
            IsActive = true;
        }

        public UserStream(Func<Task<HttpResponseMessage>> createOpenConnection, IEventAggregator eventAggregator)
        {
            this.createOpenConnection = createOpenConnection;
            CancelUserStream = new CancellationTokenSource();
            _eventAggregator = eventAggregator;
            IsActive = true;
        }

        public void Start()
        {
            CancelUserStream = new CancellationTokenSource();
            Task.Factory.StartNew(ProcessMessages, CancelUserStream.Token);
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            friends.Dispose();
            tweets.Dispose();
            directmessages.Dispose();
            events.Dispose();
            directmessages.Dispose();
            scrubgeorequests.Dispose();

            CancelUserStream.Cancel();
            IsActive = false;

            if (_eventAggregator != null)
                _eventAggregator.Publish(new TwitterUserStreamDisconnectEvent());
        }

        private async void ProcessMessages()
        {
            try
            {
                var responseStream = await GetStream();
                while (!CancelUserStream.IsCancellationRequested)
                {
                    // reconnect if the stream was closed previously
                    if (responseStream == null)
                    {
                        await Task.Delay(delay);
                        responseStream = await GetStream();
                    }

                    //get the line of JSON from the stream
                    string line;
                    try
                    {
                        line = responseStream.ReadLine();
                    }
                    catch (IOException)
                    {
                        delay += initialDelay;
                        responseStream.Dispose();
                        responseStream = null;
                        line = "";
                    }

                    // special, non JSON and therefore highly unlikely to be sent from the live service
                    // this is the token string used by the testing harness.
                    if (line == "ENDBOXKITEUSERSTREAMTEST")
                    {
                        Stop();
                        continue;
                    }

                    // we have a valid connection - clear delay
                    delay = TimeSpan.Zero;

                    // fall through
                    if (string.IsNullOrWhiteSpace(line.Trim())) continue;

                    var obj = JsonConvert.DeserializeObject<JObject>(line);

                    //https://dev.twitter.com/docs/streaming-apis/messages

                    if (obj["direct_message"] != null)
                    {
                        directmessages.OnNext(MapFromStreamTo<DirectMessage>(obj["direct_message"].ToString()));
                        continue;
                    }

                    if (obj["in_reply_to_user_id"] != null)
                    {
                        tweets.OnNext(MapFromStreamTo<Tweet>(obj.ToString()));
                        continue;
                    }

                    if (obj["friends"] != null)
                    {
                        SendFriendsMessage(obj["friends"].Values<long>());
                        continue;
                    }

                    // source: https://dev.twitter.com/docs/streaming-apis/messages#Events_event
                    if (obj["event"] != null)
                    {
                        events.OnNext(MapFromEventInStream(obj));
                        continue;
                    }

                    if (obj["scrub_geo"] != null)
                    {
                        scrubgeorequests.OnNext(MapFromStreamTo<StreamScrubGeo>(obj["scrub_geo"].ToString()));
                        continue;
                    }

                    if (obj["limit"] != null)
                    {
                        limitnotices.OnNext(MapFromStreamTo<StreamLimitNotice>(obj["limit"].ToString()));
                        continue;
                    }

                    if (obj["delete"] != null)
                    {
                        deleteevents.OnNext(MapFromStreamTo<DeleteEvent>(obj["delete"].ToString()));
                        continue;
                    }

                    if (obj["status_withheld"] != null)
                    {
                        statuswithheld.OnNext(
                            MapFromStreamTo<StreamStatusWithheld>(obj["status_withheld"].ToString()));
                        continue;
                    }

                    if (obj["user_withheld"] != null)
                    {
                        userwithheld.OnNext(MapFromStreamTo<StreamUserWithheld>(obj["user_withheld"].ToString()));
                        continue;
                    }

                    if (obj["disconnect"] != null)
                    {
                        var disconnect = MapFromStreamTo<StreamDisconnect>(obj["disconnect"].ToString());
                        // check for non-hard disconnects & attempt reconnect
                        continue;
                    }

                    if (obj["warning"] == null) continue; // no warnings, so start loop from beginning again

                    if (obj["warning"]["percent_full"] != null)
                    {
                        var stallwarning = MapFromStreamTo<StreamStallWarning>(obj["warning"].ToString());
                        // do something something stall warning.
                    }
                    if (obj["warning"]["user_id"] != null)
                    {
                        var userfollowswarning =
                            MapFromStreamTo<StreamToManyFollowsWarning>(obj["warning"].ToString());
                        // do something something user follows warning this is pretty final, actually.
                        Stop();
                    }
                }
                Stop();
            }
            catch (Exception ex)
            {
                Stop();
            }
        }

        private async Task<StreamReader> GetStream()
        {
            var response = await createOpenConnection();
            var stream = await response.Content.ReadAsStreamAsync();
            var responseStream = new StreamReader(stream);
            return responseStream;
        }

        private void SendFriendsMessage(IEnumerable<long> obj)
        {
            friends.OnNext(obj);
        }

        private static StreamEvent MapFromEventInStream(dynamic e)
        {
            string eventName = e["event"].Value.ToString();
            if (eventName.Substring(0, 5) == "list_") // as list_*
            {
                return MapFromStreamTo<ListStreamEvent>(e.ToString());
            }

            switch (eventName)
            {
                case "favorite":
                case "unfavorite":
                    return MapFromStreamTo<TweetStreamEvent>(e.ToString());
                default:
                    return MapFromStreamTo<StreamEvent>(e.ToString());
            }
        }

        private static T MapFromStreamTo<T>(string t)
        {
            return JsonConvert.DeserializeObject<T>(t);
        }
    }
}
