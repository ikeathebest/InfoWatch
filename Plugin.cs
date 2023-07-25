using BepInEx;
using System;
using UnityEngine;
using Utilla;
using InfoWatch.Scripts;
using System.Diagnostics;
using System.Threading.Tasks;

namespace InfoWatch
{
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        TimeSpan playTime;
        bool InHunt;

        void Start()
        {
            Utilla.Events.GameInitialized += OnGameInitialized;
            Utilla.Events.RoomJoined += RoomJoined;
            Utilla.Events.RoomLeft += RoomLeft;
        }

        async void OnGameInitialized(object sender, EventArgs e)
        {
            await Task.Delay(10000); // wild
            WatchManagement.InitWatch();
        }

        void RoomJoined(object sender, Events.RoomJoinedArgs e)
        {
            if (e.Gamemode.Contains("HUNT") && WatchManagement.WatchActive)
            {
                InHunt = true;
                WatchManagement.RemoveWatch();
            }
            else
            {
                InHunt = false;
                WatchManagement.InitWatch();
            }
        }

        void RoomLeft(object sender, Events.RoomJoinedArgs e)
        {
            InHunt = false;
        }

        void Update()
        {
            if (WatchManagement.WatchActive)
            {
                playTime = DateTime.Now - Process.GetCurrentProcess().StartTime;

                WatchManagement.SetWatchText
                    ($"{DateTime.Now:h:mm tt}\n\n" + // make the text readable for now while i figure out the stupid ass secret thing
                    $"PLAYTIME:\n" +
                    $"{new TimeSpanRounder.RoundedTimeSpan(playTime.Ticks, 0):hh:mm:ss}");
            }
        }
    }
}
