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
using System.Net.NetworkInformation;

namespace InfoWatch
{
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        // core
        TimeSpan playTime;
        bool WatchActive;
        DummyWatch watch;
        string TempText;
        // network stuff
        Recorder VoiceRecorder;
        // icons
        Sprite SpeakerSprite;
        Sprite OneBarSprite;
        Sprite TwoBarSprite;
        Sprite ThreeBarSprite;
        Sprite FourBarSprite;

        ConfigEntry<bool> TwentyFourHr;

        async void Start()
        {
            // streams
            Stream speakerstr = Assembly.GetExecutingAssembly().GetManifestResourceStream("InfoWatch.Resources.speaker.png");
            Stream pingbar1 = Assembly.GetExecutingAssembly().GetManifestResourceStream("InfoWatch.Resources.pingbar1.png");
            Stream pingbar2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("InfoWatch.Resources.pingbar2.png");
            Stream pingbar3 = Assembly.GetExecutingAssembly().GetManifestResourceStream("InfoWatch.Resources.pingbar3.png");
            Stream pingbar4 = Assembly.GetExecutingAssembly().GetManifestResourceStream("InfoWatch.Resources.pingbar4.png");
            // byte arrays
            byte[] speakerBytes = new byte[speakerstr.Length];
            byte[] OneBarBytes = new byte[pingbar1.Length];
            byte[] TwoBarBytes = new byte[pingbar2.Length];
            byte[] ThreeBarBytes = new byte[pingbar3.Length];
            byte[] FourBarBytes = new byte[pingbar4.Length];
            // reading
            await speakerstr.ReadAsync(speakerBytes, 0, speakerBytes.Length);
            await pingbar1.ReadAsync(OneBarBytes, 0, OneBarBytes.Length);
            await pingbar2.ReadAsync(TwoBarBytes, 0, TwoBarBytes.Length);
            await pingbar3.ReadAsync(ThreeBarBytes, 0, ThreeBarBytes.Length);
            await pingbar4.ReadAsync(FourBarBytes, 0, FourBarBytes.Length);
            // texture and sprite creation
            var SpeakerTex = new Texture2D(512, 512, TextureFormat.RGBA32, true) {wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, name = "speaker"};
            SpeakerTex.LoadImage(speakerBytes);
            SpeakerTex.Apply();
            SpeakerSprite = Sprite.Create(SpeakerTex, new Rect(0, 0, 512, 512), Vector2.zero);

            var OneBarTex = new Texture2D(8, 8, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, name = "onebar" };
            OneBarTex.LoadImage(OneBarBytes);
            OneBarTex.Apply();
            OneBarSprite = Sprite.Create(OneBarTex, new Rect(0, 0, 8, 8), Vector2.zero);

            var TwoBarTex = new Texture2D(8, 8, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, name = "twobar" };
            TwoBarTex.LoadImage(TwoBarBytes);
            TwoBarTex.Apply();
            TwoBarSprite = Sprite.Create(TwoBarTex, new Rect(0, 0, 8, 8), Vector2.zero);

            var ThreeBarTex = new Texture2D(8, 8, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, name = "threebar" };
            ThreeBarTex.LoadImage(ThreeBarBytes);
            ThreeBarTex.Apply();
            ThreeBarSprite = Sprite.Create(ThreeBarTex, new Rect(0, 0, 8, 8), Vector2.zero);

            var FourBarTex = new Texture2D(8, 8, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, name = "fourbar" };
            FourBarTex.LoadImage(FourBarBytes);
            FourBarTex.Apply();
            FourBarSprite = Sprite.Create(FourBarTex, new Rect(0, 0, 8, 8), Vector2.zero);

            // config
            ConfigFile customFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "InfoWatch.cfg"), true);
            TwentyFourHr = customFile.Bind("Time", "24-Hour Time", false, "Use 24-hour time instead of 12.");

            Utilla.Events.RoomJoined += RoomJoined;
            Utilla.Events.RoomLeft += RoomLeft;
            Utilla.Events.GameInitialized += Init;
        }

        void RoomJoined(object sender, Events.RoomJoinedArgs e)
        {
            VoiceRecorder = PhotonNetworkController.Instance.GetComponent<Recorder>();
            if (e.Gamemode.Contains("HUNT") && WatchActive) { WatchDestroy(); }
            else if (!WatchActive) { WatchCreate(); }
        }

        void RoomLeft(object sender, EventArgs e)
        {
            watch.SetImage(null, DummyWatch.ImageType.LeftHand);
            watch.SetImage(null, DummyWatch.ImageType.RightHand);
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
                    
                    if (VoiceRecorder != null && VoiceRecorder.IsCurrentlyTransmitting) { watch.SetImage(SpeakerSprite, DummyWatch.ImageType.RightHand); }
                    else { watch.SetImage(null, DummyWatch.ImageType.RightHand); }

                    int ping = PhotonNetwork.GetPing();
                    if (0 <= ping && ping <= 30)
                    {
                        watch.SetImage(FourBarSprite, DummyWatch.ImageType.LeftHand);
                    }
                    else if (30 <= ping && ping <= 60)
                    {
                        watch.SetImage(ThreeBarSprite, DummyWatch.ImageType.LeftHand);
                    }
                    else if (60 <= ping && ping <= 90)
                    {
                        watch.SetImage(TwoBarSprite, DummyWatch.ImageType.LeftHand);
                    }
                    else if (90 <= ping && ping <= 120)
                    {
                        watch.SetImage(OneBarSprite, DummyWatch.ImageType.LeftHand);
                    }

                }
                watch.SetWatchText(TempText);
            }
        }

        async void WatchCreate()
        {
            watch = await DummyWatch.CreateDummyWatch(Assembly.GetExecutingAssembly(), GorillaTagger.Instance.offlineVRRig);
            // there isn't anything to turn the objects off independently
            // and we only need some of the images later
            watch.SetImage(null, DummyWatch.ImageType.Hat);
            // what could we use these for
            watch.SetImage(null, DummyWatch.ImageType.Badge);
            watch.SetImage(null, DummyWatch.ImageType.Face);
            watch.SetImage(null, DummyWatch.ImageType.LeftHand);
            watch.SetImage(null, DummyWatch.ImageType.RightHand);
            // material swatches are handled differently now
            watch.SetColourSwatch(Color.clear);
            WatchActive = true;
        }

        void WatchDestroy()
        {
            DummyWatch.RemoveDummyWatch(Assembly.GetExecutingAssembly(), GorillaTagger.Instance.offlineVRRig);
            WatchActive = false;
        }
    }
}