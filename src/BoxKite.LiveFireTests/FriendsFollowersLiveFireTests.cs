﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoxKite.Twitter;
using BoxKite.Twitter.Console.Helpers;
using BoxKite.Twitter.Models.Service;

namespace BoxKite.LiveFireTests
{
    public class FriendsFollowersLiveFireTests
    {
        public async Task<bool> DoFriendsFollowersTest(IUserSession session, List<int> testSeq)
        {
            var successStatus = true;
            try
            {
                // 1
                if (testSeq.Contains(1))
                {
                    ConsoleOutput.PrintMessage("6.1 FriendsFollowers\\GetFriendsIDs", ConsoleColor.Gray);

                    long nextcursor = -1;
                    var ffListCount = 0;

                    do
                    {
                        var ff1List = await session.GetFriendsIDs(cursor: nextcursor);
                        if (ff1List.twitterFaulted) continue;
                        nextcursor = ff1List.next_cursor;
                        ConsoleOutput.PrintMessage(String.Format("Previous cursor: {0} Next cursor: {1}",
                            ff1List.previous_cursor, ff1List.next_cursor));
                        foreach (var l in ff1List.IDs)
                        {
                            ffListCount++;
                            ConsoleOutput.PrintMessage(String.Format("User ID: {0}", l ));
                        }
                    } while (nextcursor != 0);

                    ConsoleOutput.PrintMessage(String.Format("Total Friends List Count: {0}",
                       ffListCount));
                }

                // 2
                if (testSeq.Contains(2))
                {
                    ConsoleOutput.PrintMessage("6.2 FriendsFollowers\\GetFollowersIDs", ConsoleColor.Gray);

                    long nextcursor = -1;
                    var ff2ListCount = 0;

                    do
                    {
                        var ff2List = await session.GetFollowersIDs(cursor: nextcursor);
                        if (ff2List.twitterFaulted) continue;
                        nextcursor = ff2List.next_cursor;
                        ConsoleOutput.PrintMessage(String.Format("Previous cursor: {0} Next cursor: {1}",
                            ff2List.previous_cursor, ff2List.next_cursor));
                        foreach (var l in ff2List.IDs)
                        {
                            ff2ListCount++;
                            ConsoleOutput.PrintMessage(String.Format("User ID: {0}", l));
                        }
                    } while (nextcursor != 0);

                    ConsoleOutput.PrintMessage(String.Format("Total Followers List Count: {0}",
                       ff2ListCount));
                }

                // 3
                if (testSeq.Contains(3))
                {
                    ConsoleOutput.PrintMessage("6.3 FriendsFollowers\\GetFriendships", ConsoleColor.Gray);
                    var ff3 = await session.GetFriendships(new List<string>{"katyperry","shiftkey"});

                    if (!ff3.ToList()[0].twitterFaulted)
                    {
                        foreach (var t in ff3)
                        {
                            ConsoleOutput.PrintMessage(
                                String.Format("Name: {0} // UserID: {1}", t.ScreenName, t.UserId));
                            foreach (var c in t.Connections)
                            {
                                ConsoleOutput.PrintMessage(
                                String.Format("Connection: {0}", c.ToString()));
                            }
                        }
                    }
                    else
                        successStatus = false;
                }


                // 4
                if (testSeq.Contains(4))
                {
                    ConsoleOutput.PrintMessage("6.4 FriendsFollowers\\GetFriendshipRequestsIncoming", ConsoleColor.Gray);

                    long nextcursor = -1;
                    var ff4ListCount = 0;

                    do
                    {
                        var ff4List = await session.GetFriendshipRequestsIncoming(cursor: nextcursor);
                        if (ff4List.twitterFaulted) continue;
                        nextcursor = ff4List.next_cursor;
                        ConsoleOutput.PrintMessage(String.Format("Previous cursor: {0} Next cursor: {1}",
                            ff4List.previous_cursor, ff4List.next_cursor));
                        foreach (var l in ff4List.IDs)
                        {
                            ff4ListCount++;
                            ConsoleOutput.PrintMessage(String.Format("User ID: {0}", l));
                        }
                    } while (nextcursor != 0);

                    ConsoleOutput.PrintMessage(String.Format("Total Followers List Count: {0}",
                       ff4ListCount));
                }

                // 5
                if (testSeq.Contains(5))
                {
                    ConsoleOutput.PrintMessage("6.5 FriendsFollowers\\GetFriendshipRequestsOutgoing", ConsoleColor.Gray);

                    long nextcursor = -1;
                    var ff5ListCount = 0;

                    do
                    {
                        var ff5List = await session.GetFriendshipRequestsOutgoing(cursor: nextcursor);
                        if (ff5List.twitterFaulted) continue;
                        nextcursor = ff5List.next_cursor;
                        ConsoleOutput.PrintMessage(String.Format("Previous cursor: {0} Next cursor: {1}",
                            ff5List.previous_cursor, ff5List.next_cursor));
                        foreach (var l in ff5List.IDs)
                        {
                            ff5ListCount++;
                            ConsoleOutput.PrintMessage(String.Format("User ID: {0}", l));
                        }
                    } while (nextcursor != 0);

                    ConsoleOutput.PrintMessage(String.Format("Total Followers List Count: {0}",
                       ff5ListCount));
                }

                // 6
                if (testSeq.Contains(6))
                {
                    ConsoleOutput.PrintMessage("6.6 FriendsFollowers\\CreateFriendship", ConsoleColor.Gray);
                    var ff6 = await session.CreateFriendship("katyperry");
                    if (!ff6.twitterFaulted)
                    {
                        ConsoleOutput.PrintMessage(String.Format("Name: {0}", ff6.Name));
                        ConsoleOutput.PrintMessage(String.Format("User ID: {0}", ff6.UserId));
                        ConsoleOutput.PrintMessage(String.Format("Avatar URL: {0}", ff6.Avatar));
                        ConsoleOutput.PrintMessage(String.Format("Followers: {0}", ff6.Followers));
                        ConsoleOutput.PrintMessage(String.Format("Friends: {0}", ff6.Friends));
                        ConsoleOutput.PrintMessage(String.Format("Description: {0}", ff6.Description));
                    }
                    else
                        successStatus = false;
                }

                // 7
                if (testSeq.Contains(7))
                {
                    ConsoleOutput.PrintMessage("6.7 FriendsFollowers\\DeleteFriendship", ConsoleColor.Gray);
                    var ff7 = await session.DeleteFriendship(user_id: 21447363);
                    if (!ff7.twitterFaulted)
                    {
                        ConsoleOutput.PrintMessage(String.Format("Name: {0}", ff7.Name));
                        ConsoleOutput.PrintMessage(String.Format("User ID: {0}", ff7.UserId));
                        ConsoleOutput.PrintMessage(String.Format("Avatar URL: {0}", ff7.Avatar));
                        ConsoleOutput.PrintMessage(String.Format("Followers: {0}", ff7.Followers));
                        ConsoleOutput.PrintMessage(String.Format("Friends: {0}", ff7.Friends));
                        ConsoleOutput.PrintMessage(String.Format("Description: {0}", ff7.Description));
                    }
                    else
                        successStatus = false;
                }
            }
            catch (Exception e)
            {
                ConsoleOutput.PrintError(e.ToString());
                return false;
            }
            return successStatus;
        }
    }
}