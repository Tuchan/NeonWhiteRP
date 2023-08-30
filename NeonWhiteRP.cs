using Harmony;
using I2.Loc;
using MelonLoader;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Discord;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

namespace NeonWhiteRPNew
{

    public class NeonWhiteRP : MelonMod
    {
        public static Discord.Discord discord;
        public static new HarmonyLib.Harmony Harmony { get; private set; }
        public static bool checkedForUpdates = false;
        public static Game game;
        public static int resets = 0;
        public static string lastLevel = "";
        public static long globalTimestamp = 0;
        public static MenuScreenLevelRush menuScreenLevelRushInstance;
        public static ActivityManager activityManager;
        public static string modVersion;
        public static LevelRush.LevelRushType globalLevelRushType = LevelRush.LevelRushType.None;
        public static bool globalHeavenMode = true;
        public static string globalLevelRushTime = "-1";
        public static string playerName = "Neon White";
        public static int globalUserRanking = 0;

        public override void OnInitializeMelon()
        {
            MelonInfoAttribute melonInfo = Assembly.GetExecutingAssembly().GetCustomAttribute<MelonInfoAttribute>();
            if (melonInfo != null)
            {
                modVersion = melonInfo.Version;
            }
        }

        [Obsolete]
        public override void OnApplicationLateStart()
        {
            discord = new Discord.Discord(1138868754688770148, (ulong)CreateFlags.Default);
            UpdateRP(new Activity { Details = "Launching the game", Assets = { LargeImage = "neonwhite", LargeText = "Rich Presence Created by Tuchan ver. " + modVersion, }, });

            game = Singleton<Game>.Instance;

            SceneManager.activeSceneChanged += OnActiveSceneChange;
            
            Harmony = new HarmonyLib.Harmony("NeonWhiteRP");

            MethodInfo method = typeof(MainMenu).GetMethod("EnterLocation");
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(NeonWhiteRP).GetMethod("PostEnterLocation"));
            Harmony.Patch(method, null, harmonyMethod);

            method = typeof(MainMenu).GetMethod("SetState");
            harmonyMethod = new HarmonyMethod(typeof(NeonWhiteRP).GetMethod("PostSetState"));
            Harmony.Patch(method, null, harmonyMethod);

            method = typeof(LevelRush).GetMethod("SetLevelRush");
            harmonyMethod = new HarmonyMethod(typeof(NeonWhiteRP).GetMethod("PostSetLevelRush"));
            Harmony.Patch(method, null, harmonyMethod);

            method = typeof(Leaderboards).GetMethod("SetUsername");
            harmonyMethod = new HarmonyMethod(typeof(NeonWhiteRP).GetMethod("PostSetUsername"));
            Harmony.Patch(method, null, harmonyMethod);

            method = typeof(Leaderboards).GetMethod("SetUserRanking");
            harmonyMethod = new HarmonyMethod(typeof(NeonWhiteRP).GetMethod("PostSetUserRanking"));
            Harmony.Patch(method, null, harmonyMethod);

            method = typeof(MenuScreenGlobalNeonScore).GetMethod("LeaderboardPlayerStatsCallback");
            harmonyMethod = new HarmonyMethod(typeof(NeonWhiteRP).GetMethod("PostLeaderboardPlayerStatsCallback"));
            Harmony.Patch(method, null, harmonyMethod);

            method = typeof(LeaderboardScore).GetMethod("SetScore");
            harmonyMethod = new HarmonyMethod(typeof(NeonWhiteRP).GetMethod("PostSetScore"));
            Harmony.Patch(method, harmonyMethod);
        }
        
        private void OnActiveSceneChange(Scene previousScene, Scene newScene)
        {
            Activity activity = new Activity();
            switch (newScene.name) 
            {
                case "Menu":
                    lastLevel = "";
                    resets = 0;
                    MelonCoroutines.Start(CheckForUpdates());
                    activity.Details = MenuFirst.Value;
                    activity.State = MenuSecond.Value;
                    activity.Assets.LargeImage = "neonwhite";
                    activity.Assets.LargeText = "Rich Presence Created by Tuchan";
                    activity.Timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    break;
                case "CustomLevelMenu":
                    activity.Details = CustomMenuFirst.Value;
                    activity.State = CustomMenuSecond.Value;
                    activity.Assets.LargeImage = "neonwhite";
                    activity.Assets.LargeText = "Rich Presence Created by Tuchan";
                    activity.Assets.SmallImage = "rmmby";
                    activity.Assets.SmallText = "Custom Levels Powered by RMMBY";
                    activity.Timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    break;
                default: //Level names
                    if(lastLevel == game.GetCurrentLevel().levelID)
                    {
                        if (game.GetCurrentLevelTimerCentiseconds() == 0) resets++;
                    } else
                    {
                        resets = 0;
                    }
                    //resets = (lastLevel == game.GetCurrentLevel().levelID) ? resets + 1 : 0; //todo fix that
                    String levelName = LocalizationManager.GetTranslation(game.GetCurrentLevel().GetLevelDisplayName(), true, 0, true, false, null, null, true);
                    String bestTime = Game.GetTimerFormattedMillisecond(GameDataManager.GetLevelStats(game.GetCurrentLevel().levelID).GetTimeBestMicroseconds());
                    activity.Details = CustomString(SpeedrunFirst.Value, ((SceneManager.GetActiveScene().name == "CustomLevel") ? game.GetCurrentLevel().levelDisplayName : levelName), (bestTime == "16666:39.999" || bestTime == "0:00.000" ? "N/A" : bestTime), globalUserRanking.ToString(), resets.ToString(), null);
                    activity.State = CustomString(SpeedrunSecond.Value, ((SceneManager.GetActiveScene().name == "CustomLevel") ? game.GetCurrentLevel().levelDisplayName : levelName), (bestTime == "16666:39.999" || bestTime == "0:00.000" ? "N/A" : bestTime), globalUserRanking.ToString(), resets.ToString(), null);
                    if (SceneManager.GetActiveScene().name == "CustomLevel")
                    {
                        activity.Assets.LargeImage = "rmmby";
                        activity.Assets.LargeText = "Custom Levels Powered by RMMBY";
                    } else
                    {
                        activity.Assets.LargeImage = "location" + game.GetCurrentLevel().environmentLocationData.locationID.ToLower();
                        activity.Assets.LargeText = "District: " + LocalizationManager.GetTranslation(game.GetCurrentLevel().GetLevelEnvironmentDisplayName(), true, 0, true, false, null, null, true);
                    }
                    if (globalLevelRushType == LevelRush.LevelRushType.None)
                    {
                        activity.Assets.SmallImage = "neonwhite";
                        activity.Assets.SmallText = "Normal Mode";
                    }
                    else
                    {
                        activity.State = CustomString(SpeedrunSecond.Value, ((SceneManager.GetActiveScene().name == "CustomLevel") ? game.GetCurrentLevel().levelDisplayName : levelName), (globalLevelRushTime == "16666:39.999" || globalLevelRushTime == "0:00.000" ? "N/A" : globalLevelRushTime), globalUserRanking.ToString(), resets.ToString(), null);
                        activity.Assets.SmallImage = globalLevelRushType.ToString().ToLower().Substring(0, globalLevelRushType.ToString().Length - 4) + (globalHeavenMode == true ? "heaven" : "hell");
                        activity.Assets.SmallText = globalLevelRushType.ToString().Substring(0, globalLevelRushType.ToString().Length - 4) + "'s " + (globalHeavenMode == true ? "Heaven" : "Hell") + " Rush";
                    }

                    if (!SessionVsLevel.Value)
                    {
                        if (game.GetCurrentLevelTimerCentiseconds() == 0)
                        {
                            activity.Timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            globalTimestamp = activity.Timestamps.Start;
                        }
                        else
                        {
                            activity.Timestamps.Start = globalTimestamp;
                        }
                    }
                    else if (SessionVsLevel.Value)
                    {
                        if(lastLevel != game.GetCurrentLevel().levelID)
                        {
                            activity.Timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            globalTimestamp = activity.Timestamps.Start;
                        }
                        activity.Timestamps.Start = globalTimestamp;
                    }

                    lastLevel = game.GetCurrentLevel().levelID;
                    break;
            };
            UpdateRP(activity);
        }
        public static void PostEnterLocation(ref LocationData data)
        {
            Activity activity = new Activity();
            switch (data.locationID)
            {
                case "PORTAL":
                    activity.Details = PORTALFirst.Value;
                    activity.State = PORTALSecond.Value;
                    activity.Assets.LargeImage = "locationheavensgate";
                    activity.Assets.LargeText = "Heaven's Gate";
                    break;
                case "BEACH":
                    activity.Details = BEACHFirst.Value;
                    activity.State = BEACHSecond.Value;
                    activity.Assets.LargeImage = "locationbeach";
                    activity.Assets.LargeText = "Beach";
                    break;
                case "BAR":
                    activity.Details = BARFirst.Value;
                    activity.State = BARSecond.Value;
                    activity.Assets.LargeImage = "locationbar";
                    activity.Assets.LargeText = "Neon Bar";
                    break;
                case "SQUARE":
                    activity.Details = SQUAREFirst.Value;
                    activity.State = SQUARESecond.Value;
                    activity.Assets.LargeImage = "locationpark";
                    activity.Assets.LargeText = "Believer's Park";
                    break;
                case "CHURCH":
                    activity.Details = CHURCHFirst.Value;
                    activity.State = CHURCHSecond.Value;
                    activity.Assets.LargeImage = "locationchurch";
                    activity.Assets.LargeText = "Cathedral";
                    break;
                case "SHRINE":
                    activity.Details = SHRINEFirst.Value;
                    activity.State = SHRINESecond.Value;
                    activity.Assets.LargeImage = "locationshrine";
                    activity.Assets.LargeText = "Neon Mask Shrine";
                    break;
                case "CITYHALLLOBBY":
                    activity.Details = CITYHALLLOBBYFirst.Value;
                    activity.State = CITYHALLLOBBYSecond.Value;
                    activity.Assets.LargeImage = "locationlobby";
                    activity.Assets.LargeText = "Heaven Central Authority";
                    break;
                case "WHITESROOM":
                    activity.Details = WHITESROOMFirst.Value;
                    activity.State = WHITESROOMSecond.Value;
                    activity.Assets.LargeImage = "locationwhitesroom";
                    activity.Assets.LargeText = "White's Room";
                    break;
            };
            activity.Timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if(activity.Details != null)
            {
                UpdateRP(activity);
            }
        }
        public static void PostSetState(ref MainMenu.State newState)
        {
            Activity activity = new Activity();
            switch (newState)
            {
                case MainMenu.State.Map:
                    lastLevel = "";
                    resets = 0;
                    activity.Details = HUBFirst.Value;
                    activity.State = HUBSecond.Value;
                    activity.Assets.LargeImage = "locationmenu";
                    activity.Assets.LargeText = "Central Heaven";
                    activity.Assets.SmallImage = "neonrank";
                    activity.Assets.SmallText = "Neon Rank: " + game.GetGameData().GetNeonRankForDisplay();
                    activity.Timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    break;
                case MainMenu.State.Dialogue:
                    lastLevel = "";
                    resets = 0;
                    if (DIALOGUEEnable.Value)
                    {
                        activity.Details = DIALOGUEFirst.Value;
                        activity.State = DIALOGUESecond.Value;
                        activity.Assets.LargeImage = "neonwhite";
                        activity.Assets.LargeText = "Viewing a cutscene";
                    }
                    break;
            }
            activity.Timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (activity.Details != null)
            {
                UpdateRP(activity);
            }
        }
        public static void PostSetLevelRush(ref LevelRush.LevelRushType levelRushType, ref bool heavenRush)
        {
            globalLevelRushType = levelRushType;
            globalHeavenMode = heavenRush;
        }
        public static void PostSetUsername(ref string newName)
        {
            playerName = newName;
        }
        public static void PostLeaderboardPlayerStatsCallback(ref int rankingValue)
        {
            UpdateRP(new Activity { Details = CustomString(GlobalRankFirst.Value, null, null, null, null, rankingValue.ToString()), State = CustomString(GlobalRankSecond.Value, null, null, null, null, rankingValue.ToString()), Assets = { LargeImage = "globalneonrank", LargeText = "Viewing Global Neon Score", }, Timestamps = { Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()} });
        }
        public static void PostSetUserRanking(ref int newRanking)
        {
            globalUserRanking = newRanking;
        }
        public static void PostSetScore(LeaderboardScore __instance, ref ScoreData newData)
        {
            if (newData._userScore) globalLevelRushTime = Game.GetTimerFormattedMillisecond(Utils.ConvertMillisecondsToMicroseconds(newData._scoreValueMilliseconds));
        }
        public static string CustomString(string input, string levelName, string bestTime, string levelRank, string resets, string rankingValue)
        {
            StringBuilder output = new StringBuilder();
            bool isToken = false;
            StringBuilder token = new StringBuilder();

            foreach (char symbol in input)
            {
                if (symbol == '%')
                {
                    if (isToken)
                    {
                        switch (token.ToString())
                        {
                            case "levelname":
                                output.Append(levelName);
                                break;
                            case "besttime":
                                output.Append(bestTime);
                                break;
                            case "levelrank":
                                output.Append(levelRank);
                                break;
                            case "resets":
                                output.Append(resets);
                                break;
                            case "playername":
                                output.Append(playerName);
                                break;
                            case "globalrank":
                                output.Append(rankingValue);
                                break;
                            default:
                                output.Append($"%{token}%");
                                break;
                        }
                        token.Clear();
                    }
                    isToken = !isToken;
                    continue;
                }

                if (isToken)
                {
                    token.Append(symbol);
                }
                else
                {
                    output.Append(symbol);
                }
            }

            if (isToken) output.Append($"%{token}");
            return output.ToString();
        }
        private static void UpdateRP(Activity activity)
        {
            if(!RPEnable.Value)
            {
                activity = new Activity();
            }
            activityManager = discord.GetActivityManager();
            activityManager.UpdateActivity(activity, (res) => { Debug.Log("Result: " + res); });
        }
        public override void OnPreferencesSaved()
        {
            UpdateRP(new Activity());
        }
        public override void OnUpdate()
        {
            discord.RunCallbacks();
        }
        public IEnumerator CheckForUpdates()
        {
            if (checkedForUpdates) yield break;
            yield return new WaitForSecondsRealtime(1f);

            yield return CheckForUpdatesAsync();
        }
        private async Task CheckForUpdatesAsync()
        {
            Version currentVersion = new Version(modVersion);
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "NeonWhiteRichPresence");
            HttpResponseMessage response = await client.GetAsync("https://api.github.com/repos/Tuchan/NeonWhiteRP/releases/latest");
            if (!response.IsSuccessStatusCode)
            {
                LoggerInstance.Error("Failed to check for updates");
                LoggerInstance.Error($"Response: {response.StatusCode} {response.ReasonPhrase}");
            }
            String responseString = await response.Content.ReadAsStringAsync();
            int startIndex = responseString.IndexOf("tag_name") + 11;
            int endIndex = responseString.IndexOf("\"", startIndex);
            String latestVersionString = responseString.Substring(startIndex, endIndex - startIndex);
            Version latestVersion = new Version(latestVersionString);
            if (currentVersion < latestVersion)
            {
                String githubLink = "https://github.com/Tuchan/NeonWhiteRP/releases/latest";
                String popupPrefix = "Main Menu/Canvas/Popup/Popup Window/Window Holder/Popup Scaler/Popup Content Holder/";
                LoggerInstance.Warning("A new update is avaliable!");
                LoggerInstance.Warning($"Current Version: {modVersion}");
                LoggerInstance.Warning($"Latest Version: {latestVersionString}");
                LoggerInstance.Warning("Download it here: " + githubLink);
                GameObject.Find("Main Menu/Canvas/Main Menu/Panel/Title Panel/Title Buttons/Quit Button").GetComponent<MenuButtonHolder>().ButtonRef.onClick.Invoke();
                GameObject.Find(popupPrefix + "Popup Text").GetComponent<TextMeshProUGUI>().text = "A new update is avaliable for NeonWhiteRP, do you want to visit the download page?";
                Button button = GameObject.Find(popupPrefix + "Popup Buttons/Button Yes/Button").GetComponent<Button>();
                Button.ButtonClickedEvent oldEvent = button.onClick;
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() =>
                {
                    Application.OpenURL(githubLink);
                    button.onClick = oldEvent;
                    GameObject.Find(popupPrefix + "Popup Buttons/Button No").GetComponent<MenuButtonHolder>().ButtonRef.onClick.Invoke();
                });
            }
            else
            {
                LoggerInstance.Msg("You're up to date! :D");
            }
            checkedForUpdates = true;
        }

        //Melon Preferences
        public static MelonPreferences_Category config;
        public static MelonPreferences_Entry<bool> RPEnable;

        public static MelonPreferences_Entry<string> MenuFirst;
        public static MelonPreferences_Entry<string> MenuSecond;
        public static MelonPreferences_Entry<string> CustomMenuFirst;
        public static MelonPreferences_Entry<string> CustomMenuSecond;

        public static MelonPreferences_Entry<bool> SessionVsLevel;
        public static MelonPreferences_Entry<string> SpeedrunFirst;
        public static MelonPreferences_Entry<string> SpeedrunSecond;

        public static MelonPreferences_Entry<string> HUBFirst;
        public static MelonPreferences_Entry<string> HUBSecond;
        public static MelonPreferences_Entry<string> PORTALFirst;
        public static MelonPreferences_Entry<string> PORTALSecond;
        public static MelonPreferences_Entry<string> BEACHFirst;
        public static MelonPreferences_Entry<string> BEACHSecond;
        public static MelonPreferences_Entry<string> BARFirst;
        public static MelonPreferences_Entry<string> BARSecond;
        public static MelonPreferences_Entry<string> SQUAREFirst;
        public static MelonPreferences_Entry<string> SQUARESecond;
        public static MelonPreferences_Entry<string> CHURCHFirst;
        public static MelonPreferences_Entry<string> CHURCHSecond;
        public static MelonPreferences_Entry<string> SHRINEFirst;
        public static MelonPreferences_Entry<string> SHRINESecond;
        public static MelonPreferences_Entry<string> CITYHALLLOBBYFirst;
        public static MelonPreferences_Entry<string> CITYHALLLOBBYSecond;
        public static MelonPreferences_Entry<string> WHITESROOMFirst;
        public static MelonPreferences_Entry<string> WHITESROOMSecond;
        public static MelonPreferences_Entry<string> GlobalRankFirst;
        public static MelonPreferences_Entry<string> GlobalRankSecond;

        public static MelonPreferences_Entry<bool> DIALOGUEEnable;
        public static MelonPreferences_Entry<string> DIALOGUEFirst;
        public static MelonPreferences_Entry<string> DIALOGUESecond;


        [Obsolete]
        public override void OnApplicationStart()
        {
            config = MelonPreferences.CreateCategory("Discord Rich Presence Settings");
            RPEnable = config.CreateEntry("Enable Rich Presence", true, description: "Enables Rich Presence, duh. (To fully remove \"RP\" effect, uninstall)");

            MenuFirst = config.CreateEntry("Main Menu 1st Line", "In Main Menu", description: "Customize the first line that shows up when going into the Main Menu.");
            MenuSecond = config.CreateEntry("Main Menu 2nd Line", "", description: "Customize the second line that shows up when going into the Main Menu. To remove, leave blank.");
            CustomMenuFirst = config.CreateEntry("Custom Menu 1st Line", "In Custom Menu", description: "Customize the first line that shows up when going into the Custom Menu.");
            CustomMenuSecond = config.CreateEntry("Custom Menu 2nd Line", "", description: "Customize the second line that shows up when going into the Custom Menu. To remove, leave blank.");

            SessionVsLevel = config.CreateEntry("Enable Session Times", true, description: "If set to TRUE, the RP 'time elapsed' will stay across restarts. If FALSE, time elapsed will reset after restarting the level.");
            SpeedrunFirst = config.CreateEntry("Speedrun 1st Line", "Speedrunning - %levelname%", description: "Customize the first line that shows up when playing a level. \nAllowed placeholders: %levelname% %besttime% %levelrank% %resets%");
            SpeedrunSecond = config.CreateEntry("Speedrun 2nd Line", "PB: %besttime% (#%levelrank%) | Resets: %resets%", description: "Customize the second line that shows up when playing a level. To remove, leave blank. \nAllowed placeholders: %levelname% %besttime% %levelrank% %resets%");

            HUBFirst = config.CreateEntry("Central Heaven 1st Line", "Wandering around Central Heaven", description: "Customize the first line that shows up when going into the Central Heaven.");
            HUBSecond = config.CreateEntry("Central Heaven 2nd Line", "", description: "Customize the second line that shows up when going into the Central Heaven. To remove, leave blank.");
            PORTALFirst = config.CreateEntry("Heavens Gate 1st Line", "Selecting a Job at Heaven's Gate", description: "Customize the first line that shows up when going into Heaven's Gate.");
            PORTALSecond = config.CreateEntry("Heavens Gate 2nd Line", "", description: "Customize the second line that shows up when going into Heaven's Gate. To remove, leave blank.");
            BEACHFirst = config.CreateEntry("Beach 1st Line", "Enjoying the view of a Beach", description: "Customize the first line that shows up when going into the Beach.");
            BEACHSecond = config.CreateEntry("Beach 2nd Line", "", description: "Customize the second line that shows up when going into the Beach. To remove, leave blank.");
            BARFirst = config.CreateEntry("Neon Bar 1st Line", "Talking with other Neons", description: "Customize the first line that shows up when going into the Bar.");
            BARSecond = config.CreateEntry("Neon Bar 2nd Line", "at a Neon Bar", description: "Customize the second line that shows up when going into the Bar. To remove, leave blank.");
            SQUAREFirst = config.CreateEntry("Believers Park 1st Line", "Talking with residents", description: "Customize the first line that shows up when going into Believer's Park.");
            SQUARESecond = config.CreateEntry("Believers Park 2nd Line", "at Believer's Park", description: "Customize the second line that shows up when going into Believer's Park. To remove, leave blank.");
            CHURCHFirst = config.CreateEntry("Cathedral 1st Line", "Attending the daily sermon", description: "Customize the first line that shows up when going into The Cathedral.");
            CHURCHSecond = config.CreateEntry("Cathedral 2nd Line", "at The Cathedral", description: "Customize the second line that shows up when going into The Cathedral. To remove, leave blank.");
            SHRINEFirst = config.CreateEntry("Neon Mask Shrine 1st Line", "Visiting the Neon Mask Shrine", description: "Customize the first line that shows up when going into Neon Mask Shrine.");
            SHRINESecond = config.CreateEntry("Neon Mask Shrine 2nd Line", "", description: "Customize the second line that shows up when going into Neon Mask Shrine. To remove, leave blank.");
            CITYHALLLOBBYFirst = config.CreateEntry("Heaven Central Authority 1st Line", "Getting new assignments from Mikey", description: "Customize the first line that shows up when going into Heaven Central Authority.");
            CITYHALLLOBBYSecond = config.CreateEntry("Heaven Central Authority 2nd Line", "at Heaven Central Authority", description: "Customize the second line that shows up when going into Heaven Central Authority. To remove, leave blank.");
            WHITESROOMFirst = config.CreateEntry("Whites Room 1st Line", "Resting in White's Room", description: "Customize the first line that shows up when going into White's Room.");
            WHITESROOMSecond = config.CreateEntry("Whites Room 2nd Line", "", description: "Customize the second line that shows up when going into White's Room. To remove, leave blank.");
            GlobalRankFirst = config.CreateEntry("Global Neon Ranking 1st Line", "%playername%'s Global Neon Rank:", description: "Customize the first line that shows up when going into Global Ranking. \nAllowed placeholders: %playername% %globalrank%");
            GlobalRankSecond = config.CreateEntry("Global Neon Ranking 2nd Line", "#%globalrank%", description: "Customize the second line that shows up when going into Global Ranking. To remove, leave blank. \nAllowed placeholders: %playername% %globalrank%");

            DIALOGUEEnable = config.CreateEntry("Enable Dialogue Rich Presence", true, description: "Enables Rich Presence for cutscenes/dialogue.");
            DIALOGUEFirst = config.CreateEntry("Dialogue 1st Line", "Viewing a cutscene", description: "Customize the first line that shows up when watching a cutscene/dialogue.");
            DIALOGUESecond = config.CreateEntry("Dialogue 2nd Line", "", description: "Customize the second line that shows up when watching a cutscene/dialogue. To remove, leave blank.");
        }
    }
}
