using System.Collections.Generic;
using System.Linq;
using static JanitorsCloset.JanitorsClosetLoader;

namespace JanitorsCloset
{
    class Utilities
    {
        /// <summary>
        /// Get the partURL for the mesh
        /// </summary>
        /// <param name="pSearch"></param>
        /// <returns>string</returns>
        public static string GetMeshURL(AvailablePart pSearch)
        {
            if (pSearch == null)
            {
                Log.Info("GetMeshURL, pSearch is null");
                return "";
            }
            if (pSearch.partConfig == null)
            {
                Log.Info("GetMeshURL, pSearch.partConfig is null");
                return "";
            }
            string s = pSearch.partConfig.GetValue("mesh");

            if (s != null && s != "")
            {
                string partUrl = pSearch.partUrlConfig.parent.url.Substring(0, pSearch.partUrlConfig.parent.url.LastIndexOf('/'));
                s = partUrl + "/" + s.Substring(0, s.Length - 3);
            }
            return s;
        }

        /// <summary>
        /// Get the partURL for the part
        /// </summary>
        /// <param name="modelNode"></param>
        /// <returns>string</returns>
        public static string GetModelURL(ConfigNode modelNode)
        {
            string model = modelNode.GetValue("model");

            return model;
        }

        public static List<ConfigNode> GetModuleConfigNodes(ConfigNode partConfig, string moduleName)
        {
            return partConfig.GetNodes("MODULE").ToList().FindAll(n => n.GetValue("name") == moduleName);
        }

        public static string FormatFileSize(long fileSize)
        {
            string result;
            if (fileSize < 1024)
            {
                result = fileSize.ToString() + " b";
            }
            else if (fileSize < 1048576)
            {
                result = (fileSize / 1024.0d).ToString("F3") + " Kb";
            }
            else if (fileSize < 1073741824)
            {
                result = (fileSize / 1048576.0d).ToString("F3") + " Mb";
            }
            else
            {
                result = (fileSize / 1073741824.0d).ToString("F3") + " Gb";
            }
            return result;
        }

        public static string Percent(int value, int total)
        {
            int percent;
            if (total == 0)
            {
                return "100%";
            }
            percent = value / total;
            if (percent < 0)
            {
                percent = 0;
            }
            if (percent > 100)
            {
                percent = 100;
            }
            return percent.ToString() + "%";
        }
    }
}
