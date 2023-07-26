using BepInEx;
using System;
using Utilla;
using InfoWatch.Scripts;
using System.Diagnostics;
using DevGorillaLib.Utils;
using DevGorillaLib.Objects;
using Photon.Pun;
using UnityEngine;
using Photon.Voice.Unity;
using GorillaNetworking;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;

namespace InfoWatch
{
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        TimeSpan playTime;
        bool WatchActive;
        DummyWatch watch;
        string TempText;

        Recorder VoiceRecorder;
        Texture2D SpeakerTex;
        Sprite SpeakerSprite;

        ConfigEntry<bool> TwentyFourHr;

        async void Start()
        {
            // sprite and texture
            Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream("InfoWatch.Resources.speaker.png");
            byte[] bytes = new byte[str.Length];
            await str.ReadAsync(bytes, 0, bytes.Length);
            SpeakerTex = new Texture2D(512, 512, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
                name = "speaker"
            };
            SpeakerTex.LoadImage(bytes);
            SpeakerTex.Apply();
            SpeakerSprite = Sprite.Create(SpeakerTex, new Rect(0, 0, 512, 512), Vector2.zero);

            // config
            ConfigFile customFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "InfoWatch.cfg"), true);
            TwentyFourHr = customFile.Bind("Time", "24-Hour Time", false, "Use 24-hour time instead of 12.");

            Utilla.Events.RoomJoined += RoomJoined;
            Utilla.Events.GameInitialized += Init;
        }

        void RoomJoined(object sender, Events.RoomJoinedArgs e)
        {
            VoiceRecorder = PhotonNetworkController.Instance.GetComponent<Recorder>();
            if (e.Gamemode.Contains("HUNT") && WatchActive) { WatchDestroy(); }
            else if (!WatchActive) { WatchCreate(); }
        }

        void Init(object sender, EventArgs e) { WatchCreate(); }

        void Update()
        {
            if (WatchActive)
            {
                playTime = DateTime.Now - Process.GetCurrentProcess().StartTime;
                if (TwentyFourHr.Value)
                {
                    TempText =
                        $"{DateTime.Now:H:mm}\n" +
                        $"SESSION:{new TimeSpanRounder.RoundedTimeSpan(playTime.Ticks, 0).ToString().Substring(0, 5)}";
                }
                else
                {
                    TempText =
                        $"{DateTime.Now:h:mmtt}\n" +
                        $"SESSION:{new TimeSpanRounder.RoundedTimeSpan(playTime.Ticks, 0).ToString().Substring(0, 5)}";
                }

                
                if (PhotonNetwork.InRoom)
                {
                    if (GorillaGameManager.instance is GorillaTagManager tag && tag.currentInfected.Count > 0)
                    {
                        TempText += $"\n{tag.currentInfected.Count}/{PhotonNetwork.PlayerList.Length} TAGGED";
                    }
                    else
                    {
                        TempText += $"\n{PhotonNetwork.PlayerList.Length} PLAYERS";
                    }
                    // transparency doesnt work on the material slot because its handled differently
                    // hat is close enough so whatever lol
                    if (VoiceRecorder != null && VoiceRecorder.IsCurrentlyTransmitting) { watch.SetImage(SpeakerSprite, ref watch.hat); }
                    else { watch.SetImage(null, ref watch.hat); }

                }
                watch.SetWatchText(TempText);
            }
        }

        async void WatchCreate()
        {
            watch = await WatchUtils.CreateDummyWatch(GorillaTagger.Instance.offlineVRRig);
            // there isn't anything to turn the objects off independently
            // and we need the hat image later
            watch.SetImage(null, ref watch.material);
            watch.SetImage(null, ref watch.badge);
            watch.SetImage(null, ref watch.face);
            watch.SetImage(null, ref watch.leftHand);
            watch.SetImage(null, ref watch.rightHand);
            watch.SetImage(null, ref watch.hat);
            WatchActive = true;
        }

        void WatchDestroy()
        {
            WatchUtils.RemoveDummyWatch(GorillaTagger.Instance.offlineVRRig);
            WatchActive = false;
        }
    }
}