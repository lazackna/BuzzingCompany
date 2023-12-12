using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using ButtplugManaged;
using HarmonyLib;
using LethalMod.Patches;
using UnityEngine;

namespace LethalMod
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class ButtplugManager : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.lazackna.buzzingcompany";
        public const string PLUGIN_NAME = "BuzzingCompany";
        public const string PLUGIN_VERSION = "0.1";

        internal static ButtplugManager Instance;
        private static Harmony harmony;
        
        #region ButtplugIO
        
        public bool emergencyStop = false;
        private float currentSpeed = 0;
        private ButtplugClient client = null;
        
        private List<ButtplugClientDevice> devices = new List<ButtplugClientDevice>();
        private float timer = 0f;

        private Mutex deviceMutex = new Mutex();

        #endregion

        private void Awake()
        {
            //Hide the plugin from unity so lethal company doesn't remove it
            HideFromGame();

            Logger.LogInfo("Initializing BuzzingCompany");
            Instance = this;
            this.name = "BuzzingCompanyManager";


            InitPatches();
            ApplyPatches();

            Logger.LogInfo($"Loaded {PLUGIN_NAME} {PLUGIN_VERSION}");
            Logger.LogInfo("Starting up buttplug client");
            Task.Run(RestartClient);
        }

        private async void RestartClient()
        {
            if (client != null)
            {
                await client.DisconnectAsync();
                client = null;
            }
        
            client = new ButtplugClient(PLUGIN_NAME);
            client.DeviceAdded += ClientOnDeviceAdded;
            client.DeviceRemoved += ClientOnDeviceRemoved;
            client.ScanningFinished += ClientOnScanningFinished;
        
            Logger.LogInfo("Connecting to buttplug server");
            await client.ConnectAsync(new ButtplugWebsocketConnectorOptions(new Uri(LethalMod.Config.ServerUri.Value)));
        
            try
            {
                await Task.Run(client.StartScanningAsync);
            }
            catch (ButtplugException e)
            {
                Logger.LogError($"Scanning failed: {e.InnerException}");
            }
        }
        
        public static void Vibrate(float strength)
        {
            if (!Instance || Instance.emergencyStop)
            {
                return;
            }

            if (Instance.client == null || !Instance.client.Connected)
            {
                return;
            }

            Instance.deviceMutex.WaitOne();
        
            foreach (var device in Instance.devices)
            {
                device.SendVibrateCmd(Mathf.Min(strength, 1f));
            }
            
            Instance.deviceMutex.ReleaseMutex();
        }

        private void Update()
        {
            if (client == null) return;
            if (emergencyStop) currentSpeed = 0;
        }

        private void ClientOnDeviceAdded(object sender, DeviceAddedEventArgs e)
        {
            Logger.LogInfo("Adding device: " + e.Device.Name);
            devices.Add(e.Device);
        }
        
        private void ClientOnDeviceRemoved(object sender, DeviceRemovedEventArgs e)
        {
            Logger.LogInfo("Removing device: " + e.Device.Name);
            devices.Remove(e.Device);
        }
        
        private void ClientOnScanningFinished(object sender, EventArgs e)
        {
            Logger.LogInfo("Scanning finished");
        }
        
        private void OnDestroy()
        {
            client?.DisconnectAsync().Wait();
            harmony.UnpatchSelf();
        }
        
        private void InitPatches()
        {
            PlayerControllerBPatch.Init(Logger);
        }
        
        private void ApplyPatches()
        {
            harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            //harmony.PatchAll(typeof(QuickMenuManagerPatch));
        }
        
        private void HideFromGame()
        {
            string path = Path.Combine(Paths.BepInExConfigPath);
            try
            {
                List<string> list = File.ReadAllLines(path).ToList();
                int num = list.FindIndex((string line) => line.StartsWith("HideManagerGameObject"));
                if (num != -1 && list[num] != "HideManagerGameObject = true")
                {
                    Logger.LogInfo("\"hideManagerGameObject\" value not correctly set. Fixing it now.");
                    list[num] = "HideManagerGameObject = true";
                }
        
                File.WriteAllLines(path, list);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error modifying config file: " + ex.Message);
            }
        }
    }
}