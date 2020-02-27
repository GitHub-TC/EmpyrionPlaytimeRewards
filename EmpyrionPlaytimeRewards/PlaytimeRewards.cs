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
    public enum ExpLevel
    {
        L1 = 0,
        L2 = 799,
        L3 = 3199,
        L4 = 7199,
        L5 = 12799,
        L6 = 20000,
        L7 = 28799,
        L8 = 39200,
        L9 = 51199,
        L10 = 64800,
        L11 = 80000,
        L12 = 96799,
        L13 = 115199,
        L14 = 135199,
        L15 = 156800,
        L16 = 180000,
        L17 = 204799,
        L18 = 231200,
        L19 = 259200,
        L20 = 288800,
        L21 = 320000,
        L22 = 352799,
        L23 = 387199,
        L24 = 423200,
        L25 = 500000,
    }

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

            var nextExp = P.exp + Configuration.Current.XpPerPeriod;
            if (GetExperienceLevel(P.exp) == GetExperienceLevel(nextExp)) {
                Log($"PlayerGetPlaytimeReward: {P.playerName}/{P.steamId} -> {Configuration.Current.XpPerPeriod} XP");
                await Request_Player_SetPlayerInfo(new PlayerInfoSet() { entityId = playerId, experiencePoints = nextExp });
            }
            else {
                Log($"PlayerGetPlaytimeReward (hold for level switch to {GetExperienceLevel(nextExp)}): {P.playerName}/{P.steamId} -> {Configuration.Current.XpPerPeriod} XP");
            }
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

        public ExpLevel GetExperienceLevel(int expPoints)
        {
            ExpLevel result = ExpLevel.L1;

            foreach (ExpLevel level in System.Enum.GetValues(typeof(ExpLevel)))
            {
                if (expPoints < (int)level) break;
                else                        result = level;
            }

            return result;
        }
    }
}
