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
        public static string playerName = "Neon White";

        public override void OnInitializeMelon()
        {
            discord = new Discord.Discord(1138868754688770148, (ulong)CreateFlags.Default);
            MelonInfoAttribute melonInfo = Assembly.GetExecutingAssembly().GetCustomAttribute<MelonInfoAttribute>();
            if (melonInfo != null)
            {
                modVersion = melonInfo.Version;
            }
            UpdateRP(new Activity{Details = "Launching the game",Assets ={LargeImage = "neonwhite",LargeText = "Rich Presence Created by Tuchan ver."+modVersion,},});
        }

        [Obsolete]
        public override void OnApplicationLateStart()
        {
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

            method = typeof(MenuScreenGlobalNeonScore).GetMethod("LeaderboardPlayerStatsCallback");
            harmonyMethod = new HarmonyMethod(typeof(NeonWhiteRP).GetMethod("PostLeaderboardPlayerStatsCallback"));
            Harmony.Patch(method, null, harmonyMethod);
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
                    activity.Details = "In Main Menu";
                    activity.Assets.LargeImage = "neonwhite";
                    activity.Assets.LargeText = "Rich Presence Created by Tuchan";
                    activity.Timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    break;
                default: //Level names
                    resets = (lastLevel == game.GetCurrentLevel().levelID) ? resets + 1 : 0;
                    activity.Details = "Speedrunning - " + LocalizationManager.GetTranslation(game.GetCurrentLevel().GetLevelDisplayName(), true, 0, true, false, null, null, true);
                    activity.State = "Best: " + Game.GetTimerFormattedMillisecond(GameDataManager.GetLevelStats(game.GetCurrentLevel().levelID).GetTimeLastMicroseconds()) + " | Resets: " + resets;
                    if (SceneManager.GetActiveScene().name == "CustomLevel")
                    {
                        activity.Assets.LargeImage = "rmmby";
                        activity.Assets.LargeText = "Custom Level Powered by RMMBY"; //also add custom level menu
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
                        activity.Assets.SmallImage = globalLevelRushType.ToString().ToLower().Substring(0, globalLevelRushType.ToString().Length - 4) + (globalHeavenMode == true ? "heaven" : "hell");
                        activity.Assets.SmallText = globalLevelRushType.ToString().Substring(0, globalLevelRushType.ToString().Length - 4) + "'s " + (globalHeavenMode == true ? "Heaven" : "Hell") + " Rush";
                    }

                    if (!SessionVsLevel.Value || lastLevel != game.GetCurrentLevel().levelID)
                    {
                        activity.Timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        if (SessionVsLevel.Value)
                        {
                            globalTimestamp = activity.Timestamps.Start;
                        }
                    }
                    else if (SessionVsLevel.Value)
                    {
                        activity.Timestamps.Start = globalTimestamp;
                    }

                    lastLevel = game.GetCurrentLevel().levelID;
                    break;
            };
            UpdateRP(activity);
        }

        public static void PostEnterLocation(ref LocationData data) //was static
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
            UpdateRP(new Activity { Details = playerName + "'s Global Neon Rank: #" + rankingValue, State = "", Assets = { LargeImage = "globalneonrank", LargeText = "Viewing Global Neon Score", }, Timestamps = { Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()} });
        }
        

        private static void UpdateRP(Activity activity)
        {
            if (RPEnable.Value)
            {
                activityManager = discord.GetActivityManager();
                activityManager.UpdateActivity(activity, (res) => { Debug.Log("Result: " + res); });
            }
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
            if (currentVersion < latestVersion) //this is a mess ik :((((
            {
                LoggerInstance.Warning("A new update is avaliable!");
                LoggerInstance.Warning($"Current Version: {modVersion}");
                LoggerInstance.Warning($"Latest Version: {latestVersionString}");
                LoggerInstance.Warning("Download it here: https://github.com/Tuchan/NeonWhiteRP/releases/latest");

                MenuButtonHolder menuButtonHolder = GameObject.Find("Main Menu/Canvas/Main Menu/Panel/Title Panel/Title Buttons/Quit Button").GetComponent<MenuButtonHolder>();
                menuButtonHolder.ButtonRef.onClick.Invoke();

                TextMeshProUGUI popupText = GameObject.Find("Main Menu/Canvas/Popup/Popup Window/Window Holder/Popup Scaler/Popup Content Holder/Popup Text").GetComponent<TextMeshProUGUI>();
                popupText.text = "A new update is avaliable for NeonWhiteRP, do you want to visit the download page?";

                GameObject yesButton = GameObject.Find("Main Menu/Canvas/Popup/Popup Window/Window Holder/Popup Scaler/Popup Content Holder/Popup Buttons/Button Yes");
                yesButton.SetActive(false);

                GameObject moddedButton = GameObject.Find("Main Menu/Canvas/Popup/Popup Window/Window Holder/Popup Scaler/Popup Content Holder/Popup Buttons/Button Cancel");
                moddedButton.SetActive(true);
                moddedButton.GetComponentInChildren<TextMeshProUGUI>().text = "yes";
                moddedButton.transform.SetSiblingIndex(1);
                Button moddedButtonClass = GameObject.Find("Main Menu/Canvas/Popup/Popup Window/Window Holder/Popup Scaler/Popup Content Holder/Popup Buttons/Button Cancel/Button").GetComponent<Button>();
                moddedButtonClass.onClick = new Button.ButtonClickedEvent();
                moddedButtonClass.onClick.AddListener(() => Application.OpenURL("https://github.com/Tuchan/NeonWhiteRP/releases/latest"));

                GameObject noButton = GameObject.Find("Main Menu/Canvas/Popup/Popup Window/Window Holder/Popup Scaler/Popup Content Holder/Popup Buttons/Button No");
                noButton.transform.SetSiblingIndex(2);
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

        public static MelonPreferences_Entry<bool> SessionVsLevel;

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

        public static MelonPreferences_Entry<bool> DIALOGUEEnable;
        public static MelonPreferences_Entry<string> DIALOGUEFirst;
        public static MelonPreferences_Entry<string> DIALOGUESecond;


        [Obsolete]
        public override void OnApplicationStart()
        {
            config = MelonPreferences.CreateCategory("Discord Rich Presence Settings");
            RPEnable = config.CreateEntry("Enable Rich Presence", true, description: "Enables Rich Presence, duh. (Requires Restart)");

            SessionVsLevel = config.CreateEntry("Enable Session Times", true, description: "If set to TRUE, the RP 'time elapsed' will stay across restarts. If FALSE, time elapsed will reset after restarting the level.");

            HUBFirst = config.CreateEntry("Central Heaven 1st Line", "Wandering around Central Heaven", description: "Customize the first line that shows up when going into the Central Heaven. To remove, leave blank.");
            HUBSecond = config.CreateEntry("Central Heaven 2nd Line", "", description: "Customize the second line that shows up when going into the Central Heaven. To remove, leave blank.");
            PORTALFirst = config.CreateEntry("Heaven's Gate 1st Line", "Selecting a Job at Heaven's Gate", description: "Customize the first line that shows up when going into Heaven's Gate. To remove, leave blank.");
            PORTALSecond = config.CreateEntry("Heaven's Gate 2nd Line", "", description: "Customize the second line that shows up when going into Heaven's Gate. To remove, leave blank.");
            BEACHFirst = config.CreateEntry("Beach 1st Line", "Enjoying the view of a Beach", description: "Customize the first line that shows up when going into the Beach. To remove, leave blank.");
            BEACHSecond = config.CreateEntry("Beach 2nd Line", "", description: "Customize the second line that shows up when going into the Beach. To remove, leave blank.");
            BARFirst = config.CreateEntry("Neon Bar 1st Line", "Talking with other Neons", description: "Customize the first line that shows up when going into the Bar. To remove, leave blank.");
            BARSecond = config.CreateEntry("Neon Bar 2nd Line", "at a Neon Bar", description: "Customize the second line that shows up when going into the Bar. To remove, leave blank.");
            SQUAREFirst = config.CreateEntry("Believer's Park 1st Line", "Talking with residents", description: "Customize the first line that shows up when going into Believer's Park. To remove, leave blank.");
            SQUARESecond = config.CreateEntry("Believer's Park 2nd Line", "at Believer's Park", description: "Customize the second line that shows up when going into Believer's Park. To remove, leave blank.");
            CHURCHFirst = config.CreateEntry("Cathedral 1st Line", "Attending the daily sermon", description: "Customize the first line that shows up when going into The Cathedral. To remove, leave blank.");
            CHURCHSecond = config.CreateEntry("Cathedral 2nd Line", "at The Cathedral", description: "Customize the second line that shows up when going into The Cathedral. To remove, leave blank.");
            SHRINEFirst = config.CreateEntry("Neon Mask Shrine 1st Line", "Visiting the Neon Mask Shrine", description: "Customize the first line that shows up when going into Neon Mask Shrine. To remove, leave blank.");
            SHRINESecond = config.CreateEntry("Neon Mask Shrine 2nd Line", "", description: "Customize the second line that shows up when going into Neon Mask Shrine. To remove, leave blank.");
            CITYHALLLOBBYFirst = config.CreateEntry("Heaven Central Authority 1st Line", "Getting new assignments from Mikey", description: "Customize the first line that shows up when going into Heaven Central Authority. To remove, leave blank.");
            CITYHALLLOBBYSecond = config.CreateEntry("Heaven Central Authority 2nd Line", "at Heaven Central Authority", description: "Customize the second line that shows up when going into Heaven Central Authority. To remove, leave blank.");
            WHITESROOMFirst = config.CreateEntry("White's Room 1st Line", "Resting in White's Room", description: "Customize the first line that shows up when going into White's Room. To remove, leave blank.");
            WHITESROOMSecond = config.CreateEntry("White's Room 2nd Line", "", description: "Customize the second line that shows up when going into White's Room. To remove, leave blank.");

            DIALOGUEEnable = config.CreateEntry("Enable Dialogue Rich Presence", true, description: "Enables Rich Presence for cutscenes/dialogue.");
            DIALOGUEFirst = config.CreateEntry("Dialogue 1st Line", "Viewing a cutscene", description: "Customize the first line that shows up when watching a cutscene/dialogue. To remove, leave blank.");
            DIALOGUESecond = config.CreateEntry("Dialogue 2nd Line", "", description: "Customize the second line that shows up when watching a cutscene/dialogue. To remove, leave blank.");
        }
    }
}
