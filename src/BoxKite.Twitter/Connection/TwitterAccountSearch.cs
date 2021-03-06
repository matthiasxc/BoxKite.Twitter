﻿// (c) 2012-2013 Nick Hodge mailto:hodgenick@gmail.com & Brendan Forster
// License: MS-PL

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using BoxKite.Twitter.Helpers;
using BoxKite.Twitter.Models;

namespace BoxKite.Twitter
{
    public partial class TwitterAccount : BindableBase
    {
        // largestSeenIds
        // TBD: these should/could be properties on Account so they can be persisted across launches
        private long searchLargestSeenId;
        //

        // status bits
        private readonly Subject<bool> searchBackFillCompleted = new Subject<bool>();
        private readonly Subject<bool> searchStreamConnected = new Subject<bool>();
        //

        readonly Subject<Tweet> _searchtimeline = new Subject<Tweet>();
        public IObservable<Tweet> SearchTimeLine { get { return _searchtimeline; } }
        private string _currentSearchText = "";

        private CancellationTokenSource TwitterSearchCommunication;

        public void StartSearch(string textToSearch)
        {
            TwitterSearchCommunication = new CancellationTokenSource();
            _eventAggregator.GetEvent<TwitterSearchStreamDisconnectEvent>().Subscribe(ManageSearchStreamDisconnect);
            //
            _currentSearchText = textToSearch;
            SearchStream = Session.StartSearchStream(_eventAggregator, track: textToSearch);
            SearchStream.FoundTweets.Subscribe(_searchtimeline.OnNext);
            SearchStream.Start();
            //
            Task.Factory.StartNew(ProcessSearchBackFill_Pump);
            searchStreamConnected.Where(status => status.IsFalse()).Subscribe(StartPollingSearch);
            searchStreamConnected.OnNext(true);
        }

        // subscriber to the userstream disconnecting
        private void ManageSearchStreamDisconnect(TwitterSearchStreamDisconnectEvent disconnectEvent)
        {
            searchStreamConnected.OnNext(false); // push message saying userStream is no longer connected
        }

        public void StopSearch()
        {
            TwitterSearchCommunication.Cancel();
            searchStreamConnected.OnNext(false);
            SearchStream.Stop();
        }

        private async void ProcessSearchBackFill_Pump()
        {   
            searchLargestSeenId = await GetSearchTimeLine_Backfill();
            searchBackFillCompleted.OnNext(true);
        }

        private async Task<long> GetSearchTimeLine_Backfill()
        {
            int backofftimer = 30;
            long smallestid = 0;
            long largestid = 0;
            int backfillQuota = 50;

            do
            {
                //TODO: need to unhardcode SearchResultType
                var searchtl = await Session.SearchFor(searchtext:_currentSearchText, searchResponseType:SearchResultType.Recent, count: pagingSize, max_id: smallestid);
                if (searchtl.OK)
                {
                    smallestid = long.MaxValue;
                    foreach (var tweet in searchtl.Tweets)
                    {
                        if (tweet.Id < smallestid) smallestid = tweet.Id;
                        if (tweet.Id > largestid) largestid = tweet.Id;
                        _searchtimeline.OnNext(tweet);
                        backfillQuota--;
                    }
                }
                else
                {
                    // The Backoff will trigger 7 times before just giving up
                    // once at 30s, 60s, 1m, 2m, 4m, 8m and then 16m
                    // note that the last call into this will be 1m above the 15 "rate limit reset" window 
                    Task.Delay(TimeSpan.FromSeconds(backofftimer));
                    if (backofftimer > maxbackoff)
                    backofftimer = backofftimer * 2;
                }
            } while (backfillQuota > 0);
            return largestid;
        }

        private void StartPollingSearch(bool status)
        {
            // firstly wait on the backfills to complete before firing off these
            searchBackFillCompleted.Where(st => st == true).Subscribe(s =>
            {
                // this will fire once per minute for 24 hours from init
                var observable = Observable.Interval(TimeSpan.FromSeconds(20));
                observable.Subscribe(async t =>
                {
                    searchLargestSeenId = await GetSearchTimeLine_Failover(searchLargestSeenId);
                });
            });
        }

        private async Task<long> GetSearchTimeLine_Failover(long sinceid)
        {
            var largestseenid = sinceid;

            var searchtl = await Session.SearchFor(searchtext: _currentSearchText, searchResponseType: SearchResultType.Recent, count: pagingSize, since_id: sinceid);
            if (!searchtl.OK) return largestseenid;

            foreach (var tweet in searchtl.Tweets.Where(tweet => tweet.Id > sinceid))
            {
                if (tweet.Id > sinceid) largestseenid = tweet.Id;
                _searchtimeline.OnNext(tweet);
            }
            return largestseenid;
        }

    }
}
