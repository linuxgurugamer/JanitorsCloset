using System.Linq;
using UnityEngine;

using static JanitorsCloset.JanitorsClosetLoader;

namespace JanitorsCloset
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    class GetInternalSpaceConfigUrl : MonoBehaviour
    {
        private void Start()
        {
            foreach (var p in PartLoader.LoadedPartsList.Where(ap => ap.internalConfig != null && ap.internalConfig.HasData))
            {
                UrlDir.UrlConfig config;

                if (!FindInternalSpaceConfig(p.partPrefab, out config))
                    Log.Error("Couldn't find internal space config for " + p.name);
                else
                {
                    Log.Warning("Internal space config for " + p.name + " is at URL " + config.url + ", filename " + config.parent.fullPath);
                }
            }
        }

        public static bool FindInternalSpaceConfigNode(string name, out ConfigNode cfgNode)
        {
            foreach (var c in GameDatabase.Instance.GetConfigs("INTERNAL"))
            {
                //Log.Info("url: " + name + "   c.name: " + c.name);
                if (string.Equals(c.name, name))
                {
                    cfgNode = c.config;
                    return true;
                }
            }
            cfgNode = new ConfigNode();
            return false;
        }

        public static bool FindInternalSpaceConfig(Part part, out UrlDir.UrlConfig config)
        {
            config = null;

            if (part.partInfo.internalConfig == null || !part.partInfo.internalConfig.HasData)
                return false;

            var internalSpaceName = part.partInfo.internalConfig.GetValue("name");

            if (string.IsNullOrEmpty(internalSpaceName))
                return false;

            if (FindInternalSpaceConfigByName(internalSpaceName, out config)) return true;

            config = null;
            return false;
        }

        public static bool FindInternalSpaceConfigByName(string internalModelName, out UrlDir.UrlConfig config)
        {
            config = null;

            foreach (var c in GameDatabase.Instance.GetConfigs("INTERNAL"))
                if (string.Equals(c.name, internalModelName))
                {
                    config = c;
                    return true;
                }

            return false;
        }
    }
}
