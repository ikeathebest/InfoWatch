using BepInEx;
using System;
using UnityEngine;
using Utilla;
using InfoWatch.Scripts;
using System.Diagnostics;
using DevGorillaLib.Utils;
using DevGorillaLib.Objects;

namespace InfoWatch
{
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        TimeSpan playTime;
        bool WatchActive;
        DummyWatch watch;

        void Start()
        {
            Utilla.Events.RoomJoined += RoomJoined;
        }

        async void RoomJoined(object sender, Events.RoomJoinedArgs e)
        {
            if (e.Gamemode.Contains("HUNT") && WatchActive)
            {
                WatchUtils.RemoveDummyWatch(GorillaTagger.Instance.offlineVRRig);
                WatchActive = false;
            }
            else if (!WatchActive)
            {
                watch = await WatchUtils.CreateDummyWatch(GorillaTagger.Instance.offlineVRRig);
                watch.SetImageVisibility(false);
                WatchActive = true;
            }
        }

        void Update()
        {
            if (WatchActive)
            {
                playTime = DateTime.Now - Process.GetCurrentProcess().StartTime;

                watch.SetWatchText
                    ($"{DateTime.Now:h:mm tt}\n" +
                    $"PLAYTIME:\n" +
                    $"{new TimeSpanRounder.RoundedTimeSpan(playTime.Ticks, 0):hh:mm:ss}");
            }
        }
    }
}
