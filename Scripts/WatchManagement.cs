using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace InfoWatch.Scripts
{
    public static class WatchManagement
    {
        public static GameObject tempWatch;
        public static GameObject watch;
        public static bool WatchActive;

        // https://github.com/pixel773/Gorilla-s-Doom/blob/main/Gorilla'sDoom/Scripts/DoomManager.cs
        public static void ManageWatch(bool link)
        {
            if (tempWatch == null) tempWatch = new GameObject("WatchTemp");
            if (link && watch != null)
            {
                GorillaTagger.Instance.offlineVRRig.huntComputer = watch;
                watch.GetComponentInChildren<GorillaHuntComputer>().enabled = true;
                GorillaHuntComputer localComputer = watch.GetComponentInChildren<GorillaHuntComputer>();
                localComputer.material.gameObject.SetActive(true);
                return;
            }

            watch = GorillaTagger.Instance.offlineVRRig.huntComputer;
            GorillaHuntComputer computer = watch.GetComponentInChildren<GorillaHuntComputer>();
            computer.enabled = false; // Disabling the component so the Update method can't do anything
            computer.hat.gameObject.SetActive(false);
            computer.face.gameObject.SetActive(false);
            computer.badge.gameObject.SetActive(false);
            computer.leftHand.gameObject.SetActive(false);
            computer.rightHand.gameObject.SetActive(false);
            computer.material.gameObject.SetActive(false);
        }

        public static void InitWatch()
        {
            WatchManagement.ManageWatch(false);
            WatchManagement.watch.SetActive(true);
            WatchManagement.SetWatchText("HEY ALL!");
            WatchActive = true;
        }

        public static void RemoveWatch()
        {
            WatchManagement.ManageWatch(true);
            WatchActive = false;
        }

        public static void SetWatchText(string text)
        {
            GorillaHuntComputer computer = watch?.GetComponentInChildren<GorillaHuntComputer>();
            if (computer != null) computer.text.text = text; // So much "text" it's making my brain hurt
        }

    }
}
