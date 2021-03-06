﻿// (c) 2012-2013 Nick Hodge mailto:hodgenick@gmail.com & Brendan Forster
// License: MS-PL

using System;
using System.Net;
using BoxKite.Twitter.Extensions;
using BoxKite.Twitter.Models;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace BoxKite.Twitter
{
    public class TwitterAuthenticator
    {
        private readonly string clientID = ""; // twitter API calls these Consumers, or from their perspective consumers of their API
        private readonly string clientSecret = ""; // twitter API calls these Consumers, or from their perspective consumers of their API
        private readonly string callbackuri = ""; // twitter API uses this in the case of xauth (I think)
        private string oAuthToken = "";
        private string accessToken = "";
        private string accessTokenSecret = "";
        private string userID = "";
        private string screenName = "";
        private readonly IPlatformAdaptor _platformAdaptor; // platform specific HMACSHA1

#if (PORTABLE)
        public TwitterAuthenticator(string clientID, string clientSecret, IPlatformAdaptor platformAdaptor)
        {
            this.clientID = clientID;
            this.clientSecret = clientSecret;
            this._platformAdaptor = platformAdaptor;
        }
#elif (WINDOWS)
        public TwitterAuthenticator(string clientID, string clientSecret)
        {
            this.clientID = clientID;
            this.clientSecret = clientSecret;
            this._platformAdaptor = new DesktopPlatformAdaptor();
        }
#elif (WIN8RT)
        public TwitterAuthenticator(string clientID, string clientSecret, string callbackuri)
        {
            this.clientID = clientID;
            this.clientSecret = clientSecret;
            this.callbackuri = callbackuri;
            this._platformAdaptor = new Win8RTPlatformAdaptor();
        }
#endif
#if (PORTABLE || WINDOWS)
        public async Task<bool> StartAuthentication()
        {
            if (string.IsNullOrWhiteSpace(clientID))
                throw new ArgumentException("ClientID must be specified", clientID);

            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException("ClientSecret must be specified", clientSecret);

            var sinceEpoch = GenerateTimeStamp();
            var nonce = GenerateNonce();

            var sigBaseStringParams =
                string.Format(
                    "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method=HMAC-SHA1&oauth_timestamp={2}&oauth_version=1.0",
                    clientID,
                    nonce,
                    sinceEpoch);

            var sigBaseString = string.Format("POST&{0}&{1}", RequestTokenUrl.UrlEncode(), sigBaseStringParams.UrlEncode());
            var signature = GenerateSignature(clientSecret, sigBaseString, null);
            var dataToPost = string.Format(
                    "OAuth realm=\"\", oauth_nonce=\"{0}\", oauth_timestamp=\"{1}\", oauth_consumer_key=\"{2}\", oauth_signature_method=\"HMAC-SHA1\", oauth_version=\"1.0\", oauth_signature=\"{3}\"",
                    nonce,
                    sinceEpoch,
                    clientID,
                    signature.UrlEncode());

            var response = await PostData(RequestTokenUrl, dataToPost);

            if (string.IsNullOrWhiteSpace(response))
                return false;

            var oauthCallbackConfirmed = false;

            foreach (var splits in response.Split('&').Select(t => t.Split('=')))
            {
                switch (splits[0])
                {
                    case "oauth_token": //these tokens are request tokens, first step before getting access tokens
                        oAuthToken = splits[1];
                        break;
                    case "oauth_token_secret":
                        var OAuthTokenSecret = splits[1];
                        break;
                    case "oauth_callback_confirmed":
                        if (splits[1].ToLower() == "true") oauthCallbackConfirmed = true;
                        break;
                }
            }

            if (oauthCallbackConfirmed)
                _platformAdaptor.DisplayAuthInBrowser(AuthenticateUrl + oAuthToken);

            return oauthCallbackConfirmed;
        }

        public async Task<TwitterCredentials> ConfirmPin(string pinAuthorizationCode)
        {
            if (string.IsNullOrWhiteSpace(pinAuthorizationCode))
                throw new ArgumentException("pinAuthorizationCode must be specified", pinAuthorizationCode);

            var sinceEpoch = GenerateTimeStamp();
            var nonce = GenerateNonce();

            var dataToPost = string.Format(
                    "OAuth realm=\"\", oauth_nonce=\"{0}\", oauth_timestamp=\"{1}\", oauth_consumer_key=\"{2}\", oauth_signature_method=\"HMAC-SHA1\", oauth_version=\"1.0\", oauth_verifier=\"{3}\", oauth_token=\"{4}\"",
                    nonce,
                    sinceEpoch,
                    clientID,
                    pinAuthorizationCode,
                    oAuthToken);

            var response = await PostData(AuthorizeTokenUrl, dataToPost);

            if (string.IsNullOrWhiteSpace(response))
                return TwitterCredentials.Null; //oops something wrong here

            var useraccessConfirmed = false;

            foreach (var splits in response.Split('&').Select(t => t.Split('=')))
            {
                switch (splits[0])
                {
                    case "oauth_token": //these tokens are request tokens, first step before getting access tokens
                        accessToken = splits[1];
                        break;
                    case "oauth_token_secret":
                        accessTokenSecret = splits[1];
                        break;
                    case "user_id":
                        userID = splits[1];
                        useraccessConfirmed = true;
                        break;
                    case "screen_name":
                        screenName = splits[1];
                        break;
                }
            }
            return useraccessConfirmed ? GetUserCredentials() : TwitterCredentials.Null;
        }

#elif (WIN8RT)
        public async Task<TwitterCredentials> Authentication()
        {
            if (string.IsNullOrWhiteSpace(clientID))
                throw new ArgumentException("ClientID must be specified", clientID);

            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException("ClientSecret must be specified", clientSecret);

            var sinceEpoch = GenerateTimeStamp();
            var nonce = GenerateNonce();

            var sigBaseStringParams =
                string.Format(
                    "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method=HMAC-SHA1&oauth_timestamp={2}&oauth_version=1.0",
                    clientID,
                    nonce,
                    sinceEpoch);

            var sigBaseString = string.Format("POST&{0}&{1}", RequestTokenUrl.UrlEncode(), sigBaseStringParams.UrlEncode());
            var signature = GenerateSignature(clientSecret, sigBaseString, null);
            var dataToPost = string.Format(
                    "OAuth realm=\"\", oauth_nonce=\"{0}\", oauth_timestamp=\"{1}\", oauth_consumer_key=\"{2}\", oauth_signature_method=\"HMAC-SHA1\", oauth_version=\"1.0\", oauth_signature=\"{3}\"",
                    nonce,
                    sinceEpoch,
                    clientID,
                    signature.UrlEncode());

            var response = await PostData(RequestTokenUrl, dataToPost);

            if (string.IsNullOrWhiteSpace(response))
                return TwitterCredentials.Null;

            var oauthCallbackConfirmed = false;

            foreach (var splits in response.Split('&').Select(t => t.Split('=')))
            {
                switch (splits[0])
                {
                    case "oauth_token": //these tokens are request tokens, first step before getting access tokens
                        oAuthToken = splits[1];
                        break;
                    case "oauth_token_secret":
                        var OAuthTokenSecret = splits[1];
                        break;
                    case "oauth_callback_confirmed":
                        if (splits[1].ToLower() == "true") oauthCallbackConfirmed = true;
                        break;
                }
            }

            if (oauthCallbackConfirmed)
            {
                var authresponse = await _platformAdaptor.AuthWithBroker(AuthenticateUrl + oAuthToken, callbackuri);
                if (!string.IsNullOrWhiteSpace(authresponse))
                {
                    var responseData = authresponse.Substring(authresponse.IndexOf("oauth_token"));
                    string request_token = null;
                    string oauth_verifier = null;
                    String[] keyValPairs = responseData.Split('&');

                    foreach (var t in keyValPairs)
                    {
                        var splits = t.Split('=');
                        switch (splits[0])
                        {
                            case "oauth_token":
                                request_token = splits[1];
                                break;
                            case "oauth_verifier":
                                oauth_verifier = splits[1];
                                break;
                        }
                    }

                    sinceEpoch = GenerateTimeStamp();
                    nonce = GenerateNonce();

                    sigBaseStringParams = string.Format(
                        "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method=HMAC-SHA1&oauth_timestamp={2}&oauth_token={3}&oauth_version=1.0",
                        clientID,
                        nonce,
                        sinceEpoch,
                        request_token);

                    sigBaseString = string.Format("POST&{0}&{1}", AuthorizeTokenUrl.UrlEncode(), sigBaseStringParams.UrlEncode());
                    signature = GenerateSignature(clientSecret, sigBaseString, null);

                    var httpContent = String.Format("oauth_verifier={0}", oauth_verifier);

                    dataToPost = string.Format(
                            "OAuth realm=\"\", oauth_nonce=\"{0}\", oauth_timestamp=\"{1}\", oauth_consumer_key=\"{2}\", oauth_signature_method=\"HMAC-SHA1\", oauth_version=\"1.0\", oauth_token=\"{3}\", oauth_signature=\"{4}\"",
                            nonce,
                            sinceEpoch,
                            clientID,
                            request_token,
                            signature.UrlEncode());


                    response = await PostData(AuthorizeTokenUrl, dataToPost, httpContent);

                    if (string.IsNullOrWhiteSpace(response))
                        return TwitterCredentials.Null; //oops something wrong here

                    var useraccessConfirmed = false;

                    foreach (var splits in response.Split('&').Select(t => t.Split('=')))
                    {
                        switch (splits[0])
                        {
                            case "oauth_token": //these tokens are request tokens, first step before getting access tokens
                                accessToken = splits[1];
                                break;
                            case "oauth_token_secret":
                                accessTokenSecret = splits[1];
                                break;
                            case "user_id":
                                userID = splits[1];
                                useraccessConfirmed = true;
                                break;
                            case "screen_name":
                                screenName = splits[1];
                                break;
                        }
                    }
                    return useraccessConfirmed ? GetUserCredentials() : TwitterCredentials.Null;
                }
            }
            return TwitterCredentials.Null;
        }
#endif
        private TwitterCredentials GetUserCredentials()
        {
            var credentials = new TwitterCredentials
            {
                ConsumerKey = clientID,
                ConsumerSecret = clientSecret,
                Token = accessToken,
                TokenSecret = accessTokenSecret,
                ScreenName = screenName,
                UserID = Int32.Parse(userID),
                Valid = true
            };
            return credentials;
        }

        /* Utilities */
        const string SafeURLEncodeChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        const string RequestTokenUrl = "http://api.twitter.com/oauth/request_token";
        const string AuthenticateUrl = "https://api.twitter.com/oauth/authorize?oauth_token=";
        const string AuthorizeTokenUrl = "https://api.twitter.com/oauth/access_token";

        private string GenerateSignature(string signingKey, string baseString, string tokenSecret)
        {
            _platformAdaptor.AssignKey(Encoding.UTF8.GetBytes(string.Format("{0}&{1}", OAuthUrlEncode(signingKey),
                string.IsNullOrEmpty(tokenSecret)
                    ? ""
                    : OAuthUrlEncode(tokenSecret))));
            var dataBuffer = Encoding.UTF8.GetBytes(baseString);
            var hashBytes = _platformAdaptor.ComputeHash(dataBuffer);
            var signatureString = Convert.ToBase64String(hashBytes);
            return signatureString;
        }

        private static string OAuthUrlEncode(string value)
        {
            var result = new StringBuilder();

            foreach (var symbol in value)
            {
                if (SafeURLEncodeChars.IndexOf((char) symbol) != -1)
                {
                    result.Append(symbol);
                }
                else
                {
                    result.Append('%' + String.Format("{0:X2}", (int)symbol));
                }
            }

            return result.ToString();
        }

        private static string GenerateNonce()
        {
            var random = new Random();
            return random.Next(1234000, 99999999).ToString(CultureInfo.InvariantCulture);
        }

        private static string GenerateTimeStamp()
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        private static async Task<string> PostData(string url, string data, string content = null)
        {
            try
            {
                var handler = new HttpClientHandler();
                if (handler.SupportsAutomaticDecompression)
                {
                    handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                }
                var client = new HttpClient(handler);
                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
                request.Headers.Add("Accept-Encoding", "identity");
                request.Headers.Add("User-Agent", "BoxKite.Twitter/1.0");
                request.Headers.Add("Authorization", data);
                if (content != null)
                {
                    request.Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded");
                }
                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                return "";
            }
        }

    }
}