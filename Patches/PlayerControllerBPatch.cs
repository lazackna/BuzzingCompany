using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace LethalMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch : Patch
    {
        private static Action<float, int> heartbeat = (beat, duration) =>
        {
            ButtplugManager.Vibrate(beat);
            //Wait a tiny bit to give a vibration
            Thread.Sleep(duration);
            //Turn vibration off
            ButtplugManager.Vibrate(0);
        };

        private static Thread heartbeatTask = null;

        [HarmonyPatch("JumpToFearLevel")]
        [HarmonyPostfix]
        static void fearPatch(ref StartOfRound ___playersManager)
        {
            float fear = ___playersManager.fearLevel;
            log.LogInfo("New fear level: " + ___playersManager.fearLevel);
            if (fear <= 0.6f)
            {
                log.LogInfo("Player calmed down. Stopping heartbeat mode");
                //Fear stopped. stop heartbeat
                heartbeatTask?.Abort();
                heartbeatTask = null;
                return;
            }

            if (heartbeatTask != null)
            {
                heartbeatTask?.Abort();
                heartbeatTask = null;
            }
            
            float strenght = 0;
            int waitTime = 0;
            int interval = 0;
            if (fear >= 0.7f && fear < 0.8f)
            {
                strenght = 0.3f;
                waitTime = 900;
                interval = 200;
            } else if (fear >= 0.8f && fear < 0.9f)
            {
                strenght = 0.5f;
                waitTime = 550;
                interval = 100;
            } else if (fear >= 0.9f && fear < 1f)
            {
                strenght = 0.7f;
                waitTime = 400;
                interval = 40;
            } else if (fear >= 1f)
            {
                strenght = 1f;
                waitTime = 200;
                interval = 20;
            }

            heartbeatTask = new Thread(() =>
            {
                while (true)
                {
                    heartbeat(strenght - 0.2f, 200);
                    Thread.Sleep(interval);
                    heartbeat(strenght, 200);
                    Thread.Sleep(waitTime);
                }
            });

            heartbeatTask.Start();
        }

        //Hook into the update loop
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void update(ref float ___sprintMeter, ref StartOfRound ___playersManager)
        {
            ___sprintMeter = 1;
            ___playersManager.fearLevel = 0.7f;
            fearPatch(ref ___playersManager);
            Thread.Sleep(5000);
            ___playersManager.fearLevel = 0.8f;
            fearPatch(ref ___playersManager);
            Thread.Sleep(5000);
            ___playersManager.fearLevel = 0.9f;
            fearPatch(ref ___playersManager);
            Thread.Sleep(5000);
            ___playersManager.fearLevel = 1f;
            fearPatch(ref ___playersManager);
            Thread.Sleep(5000);
        }
    }
}