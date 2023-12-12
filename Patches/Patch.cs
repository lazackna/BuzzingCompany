using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace LethalMod.Patches
{
    public class Patch
    {
        protected static ManualLogSource log;

        public static void Init(ManualLogSource _log)
        {
            log = _log;
        }
    }
}