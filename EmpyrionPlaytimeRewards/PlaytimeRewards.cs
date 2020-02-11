using Eleon.Modding;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPIDefinitions;
using EmpyrionNetAPITools;
using EmpyrionNetAPITools.Extensions;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace EmpyrionPlaytimeRewards
{
    public class PlaytimeRewards : EmpyrionModBase
    {
        public ModGameAPI GameAPI { get; set; }

        public ConfigurationManager<Configuration> Configuration { get; set; }

        public ConcurrentDictionary<int, Timer> RewardTimers { get; set; } = new ConcurrentDictionary<int, Timer>();

        public PlaytimeRewards()
        {
            EmpyrionConfiguration.ModName = "EmpyrionPlaytimeRewards";
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;

            try
            {
                Log($"**EmpyrionPlaytimeRewards loaded: {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Message);

                LoadConfiguration();
                LogLevel = Configuration.Current.LogLevel;
                ChatCommandManager.CommandPrefix = Configuration.Current.ChatCommandPrefix;

                ChatCommands.Add(new ChatCommand(@"nav help", (I, A) => DisplayHelp(I.playerId), "display help"));

                TaskTools.Delay(1, () => Request_Player_List()
                    .GetAwaiter().GetResult()
                    .list
                    .ForEach(AddTimerForPlayer));

                Event_Player_Connected    += P => AddTimerForPlayer(P.id);
                Event_Player_Disconnected += P => { if (RewardTimers.TryRemove(P.id, out var timer)) timer.Stop(); };
            }
            catch (Exception Error)
            {
                Log($"**EmpyrionPlaytimeRewards Error: {Error} {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Error);
            }
        }

        private void AddTimerForPlayer(int playerId)
        {
            Timer timer;
            if(RewardTimers.TryAdd(playerId, timer = new Timer(Configuration.Current.XpRewardPeriodInMinutes * 60000)))
            {
                Log($"PlayerPlaytimeReward: register {playerId}", LogLevel.Debug);
                timer.Elapsed += (S,E) => AddPlayerReward(playerId).GetAwaiter().GetResult();
                timer.Start();
            }
        }

        private async Task AddPlayerReward(int playerId)
        {
            var P = await Request_Player_Info(playerId.ToId());
            if (P.exp >= Configuration.Current.PlayerMaxXp) return;

            Log($"PlayerGetPlaytimeReward: {P.playerName}/{P.steamId} -> {Configuration.Current.XpPerPeriod} XP");
            await Request_Player_SetPlayerInfo(new PlayerInfoSet() { entityId = playerId, experiencePoints = P.exp + Configuration.Current.XpPerPeriod });
        }

        private async Task DisplayHelp(int playerId)
        {
            await DisplayHelp(playerId, $"Every {Configuration.Current.XpRewardPeriodInMinutes} minutes you get {Configuration.Current.XpPerPeriod} XP");
        }

        private void LoadConfiguration()
        {
            ConfigurationManager<Configuration>.Log = Log;
            Configuration = new ConfigurationManager<Configuration>() { ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Configuration.json") };

            Configuration.Load();
            Configuration.Save();
        }
    }
}
