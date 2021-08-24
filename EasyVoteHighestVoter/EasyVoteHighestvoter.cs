using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EasyVote-HighestVoter", "Exel80 and tankbusta", "1.1.1", ResourceId = 2671)]
    class EasyVoteHighestvoter : RustPlugin
    {
        // EasyVote is life and <3
        [PluginReference] private Plugin EasyVote;

        // Just make sure im no using other name...
        private const string StoredDataName = "HighestVoter";

        #region Initializing
        void Init()
        {
            // Load data
            _storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(StoredDataName);
            // Start timer
            timer.Repeat(config.checkTime, 0, nextMonth);
        }

        // Now?!
        private void nextMonth()
        {
            // When new month arrives, then this protect spamming.
            bool triggered = false;

            // Clear all permissions from the group (if topvoter gets a group)
            if (config.group != "")
            {   
                string[] groupPermissions = permission.GetUsersInGroup(config.group);
                foreach (var user in groupPermissions)
                {
                    // Sigh, this is needed because the list contains the steam ID and last player name
                    // https://github.com/OxideMod/Oxide.Core/blob/develop/src/Libraries/Permission.cs#L653-L661
                    // GetUsersInGroup returns something like "1337 (tankbusta)"
                    string[] userParts = user.Split(' ');
                    bool hasGroup = permission.UserHasGroup(userParts[0], config.group);

                    permission.RemoveUserGroup(userParts[0], config.group);
                    Puts($"Removing user {userParts[0]} from {config.group} removed? {hasGroup}");
                }
            }

            // If month doesnt match
            if (_storedData.Month != DateTime.Now.Month)
            {
                string HighestPlayer = EasyVote?.Call("getHighestvoter").ToString();
                List<string> steamIds = new List<string>();

                Puts(HighestPlayer);
                // Detect multiple IDs
                if (HighestPlayer.Contains(","))
                {
                    Puts("Detected multiple winners (more then one player has same amount of votes)");
                    foreach (var item in HighestPlayer.Split(','))
                    {
                        steamIds.Add(item);
                    }

                    if (config.allowMultiple) {
                        // Gave reward + Hook
                        foreach (var winningSteamID in steamIds)
                        {
                            GaveRewards(winningSteamID);
                            Puts($"Awarded topvote to => {winningSteamID}");
                        }
                    } else {
                        // We only want one winner, so pick a random number based on the number of potential winners
                        System.Random rnd = new System.Random();
                        HighestPlayer = steamIds[rnd.Next(0, steamIds.Count)];
                        Puts($"Randomly picked lucky winner and the winner is => {HighestPlayer}");
                    }
                } else {
                    // Only a single player gets highest voter
                    GaveRewards(HighestPlayer);
                }

                if (string.IsNullOrEmpty(HighestPlayer))
                {
                    PrintWarning("HighestPlayer is NULL !!! No one have voted your server past month, updated month number.");

                    _storedData.Month = DateTime.Now.Month;
                    Interface.GetMod().DataFileSystem.WriteObject(StoredDataName, _storedData);

                    // Clear all permissions from the group (if topvoter gets a group)
                    if (config.group != "")
                    {   
                        string[] groupPermissions = permission.GetUsersInGroup(config.group);
                        foreach (var user in groupPermissions)
                        {
                            string[] userParts = user.Split(' ');
                            permission.RemoveUserGroup(userParts[0], config.group);
                            Puts($"Removing user {userParts[0]} from {config.group} as no one has voted in the past month");
                        }
                    }
                }

                // TRIGGERED!
                triggered = true;

                // Reset
                EasyVote?.Call("resetData");
            }

            // Triggered?
            if (!triggered)
                Announce();
        }
        #endregion

        #region Reward handlers
        private void GaveRewards(string HighestPlayer)
        {
            _storedData.Month = DateTime.Now.Month;
            Interface.GetMod().DataFileSystem.WriteObject(StoredDataName, _storedData);

            // Try found player
            BasePlayer player = FindPlayer(HighestPlayer).FirstOrDefault();

            // If make sure that player isnt null <3
            if (player != null)
            {
                // Gave reward
                if (config.rewardIs.ToLower() == "group")
                {
                    GaveGroup(HighestPlayer);
                }
                else
                    PrintWarning($"{config.rewardIs.ToLower()} can not be detected. Please, use \"group\" only!");

                // Congrats msg <3
                Congrats(player.displayName, player.UserIDString);
            }
        }

        private void GaveGroup(string HighestPlayer)
        {
            // Add user to group
            permission.AddUserGroup(HighestPlayer, config.group);

            if (config.logEnabled)
            {
                LogToFile("Highestvoter",
                            $"[{DateTime.Now.ToString()}] [HighestPlayer: {HighestPlayer} " +
                            $"Voter has been added to his reward group => {config.group}", this);
            }
        }
        #endregion
        
        #region Localization
        string _lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["HighestGroup"] = "<color=cyan>The player with the highest number of votes per month gets a free</color> <color=yellow>{0}</color> " +
                "<color=cyan>rank for 1 month.</color> <color=yellow>/vote</color> <color=cyan>Vote now to get free rank!</color>",
                ["HighestGroupCongrats"] = "<color=yellow>{0}</color> <color=cyan>was highest voter past month.</color> <color=cyan>He earned free</color> " +
                "<color=yellow>{1}</color> <color=cyan>rank for 1 month. Vote now to earn it next month!</color>",
            }, this);
        }
        #endregion

        #region Storing
        class StoredData
        {
            public int Month = DateTime.Now.Month;
            public StoredData() { }
        }
        StoredData _storedData;
        #endregion

        #region Configuration
        private Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Enable logging, save to oxide/logs/EasyVoteHighestvoter (true / false)")]
            public bool logEnabled;

            [JsonProperty(PropertyName = "Interval timer (seconds)")]
            public int checkTime;

            [JsonProperty(PropertyName = "Allow Multiple Top-Voters (true / false)")]
            public bool allowMultiple;

            [JsonProperty(PropertyName = "Highest voter reward (group)")]
            public string rewardIs;

            [JsonProperty(PropertyName = "Highest voter reward group (group name)")]
            public string group;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    logEnabled = true,
                    checkTime = 1800,
                    rewardIs = "group",
                    group = "hero",
                    allowMultiple = true
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintWarning($"Could not read oxide/config/{Name}.json, creating new config file");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Helper 
        private void Announce()
        {
            PrintToChat(_lang("HighestGroup", null, config.group));
        }

        private void Congrats(string name, string id = null)
        {
            PrintToChat(_lang("HighestGroupCongrats", id, name, config.group));
        }

        private static HashSet<BasePlayer> FindPlayer(string nameOrIdOrIp)
        {
            var players = new HashSet<BasePlayer>();
            if (string.IsNullOrEmpty(nameOrIdOrIp)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(sleepingPlayer);
                else if (!string.IsNullOrEmpty(sleepingPlayer.displayName) && sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(sleepingPlayer);
            }
            return players;
        }
        #endregion
    }
}