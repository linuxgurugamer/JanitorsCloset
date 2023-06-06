using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;

using static JanitorsCloset.JanitorsClosetLoader;

namespace JanitorsCloset
{
    class ModFilterWindow : MonoBehaviour
    {
        internal static ModFilterWindow instance;

        const string INVERSE = "-inverse";

        public class PartSizeDescr
        {
            public string bulkheadProfile;
            public string descr;

            public PartSizeDescr(string size, string descr)
            {
                this.bulkheadProfile = size;
                this.descr = descr;
            }
        }
        static SortedDictionary<string, PartSizeDescr> partSizeDict = null;
        public class PartInfo : IComparable
        {
            string partName;
            //static string[] partSizeDescr = new string[] { "Size 0 (0.625m)", "Size 1 (1.25m)", "Size 1.5 /(1.875m)", "Size 2 (2.5m)", "Size 3 (3.75m)", "Size 4 (5m)", "Size 5 (7.5m)" };

            // Implement IComparable CompareTo method - provide default sort order.
            public int CompareTo(object obj1)
            {
                return String.Compare(this.partName, ((PartInfo)obj1).partName);
            }
            //----------------           ---------------------------------------------------------------------------------------------------------------
            public string RemoveWhiteSpace(string input)
            {
                return new string(input.ToCharArray()
                    .Where(c => !Char.IsWhiteSpace(c))
                    .ToArray());
            }

            public PartInfo(AvailablePart part)
            {
                string key = "";
                this.partName = part.name;
                if (partSizeDict == null)
                    Log.Error("partSizeDict is null in PartInfo");

                Log.Info("Part: " + part.name + ", PartSizeDescrDict.Count: " + partSizeDict.Count);

                //  Log.Info("partSizeDescr.Length: " + partSizeDescr.Length.ToString());

                if (part.bulkheadProfiles != null && part.bulkheadProfiles != ""
                    && part.bulkheadProfiles != "srf")
                {
                    //bool srf = false;

                    List<string> bulkheads = part.bulkheadProfiles.Split(',').ToList();
                    for (int i = bulkheads.Count - 1; i >= 0; i--)
                    {
                        bulkheads[i] = bulkheads[i].Trim().ToLower();
                        if (bulkheads[i].Contains("srf"))
                        {
                            Log.Info("Part: " + part.name + ", i: " + i + ", removing bulkhead: " + bulkheads[i]);
                                bulkheads.RemoveAt(i);
                        }
                    }

                    if (bulkheads.Count > 1)
                    {
                        bulkheads = bulkheads.OrderBy(x => x).ToList();
                    }
                    if (bulkheads.Count > 0)
                    {
                        string smallSizeStr = bulkheads[0];
                        string largeSizeStr = bulkheads[bulkheads.Count - 1];
                        if (smallSizeStr == largeSizeStr)
                        {
                            partSize = BestGuessReadableName(smallSizeStr, capitalize: true);
                            key = smallSizeStr;
                        }
                        else
                        {
                            partSize = "Adapter: " + BestGuessReadableName(smallSizeStr, capitalize: true) + " to " + BestGuessReadableName(largeSizeStr, capitalize: true);
                            key = smallSizeStr + "-" + largeSizeStr;
                        }
                    }
                    //if (srf)
                    //    partSize += ", Srf";

                    //Log.Info("part: " + part.name + ", partSize: " + partSize + ", bulkheads[0]: [" + bulkheads[0] + "], bulkheads.Count: " + bulkheads.Count + ", bulkheads[bulkheads.Count - 1]: [" + bulkheads[bulkheads.Count - 1] + "]");
                }
                else
                {
                    partSize = "Srf";
                    key = "srf";
                }
                if (partSizeDict.ContainsKey(key))
                    partSize = partSizeDict[key].descr;
                else
                {
                    if (!part.name.Contains("kerbalEVA") && part.name != "flag")
                        Log.Error("Unknown BulkheadProfiles: part: " + part.name + ", bulkheadProfiles: " + part.bulkheadProfiles);
                }
#if false
                try
                { Log.Info("PartInfo, Part: " + part.name + ", partSize: " + key + " = " + partSize); }
                catch (Exception ex) { Log.Error("Error: " + ex.Message); }
#endif
            }

            //-------------------------------------------------------------------------------------------------------------------------------
            public string partSize;
            //public double sortSize;


            public int defaultPos;

        };


        static Rect modWindowRect = new Rect(0, 0, 0, 0);

        private const int MOD_WINDOW_ID = 94;
        private const int SIZE_WINDOW_ID = 95;

        private struct ToggleState
        {
            public bool enabledState;
            public bool latched;
            public bool inverse;
        }

        private static SortedDictionary<string, ToggleState> modButtons = new SortedDictionary<string, ToggleState>();
        private static SortedDictionary<string, ToggleState> sizeButtons = new SortedDictionary<string, ToggleState>();
        private static SortedDictionary<string, ToggleState> resourceButtons = new SortedDictionary<string, ToggleState>();
        private static SortedDictionary<string, ToggleState> partModuleButtons = new SortedDictionary<string, ToggleState>();

        private static SortedDictionary<string, PartInfo> partInfos = new SortedDictionary<string, PartInfo>();

        private static SortedDictionary<string, HashSet<AvailablePart>> modHash = new SortedDictionary<string, HashSet<AvailablePart>>();
        private static SortedDictionary<string, HashSet<AvailablePart>> sizeHash = new SortedDictionary<string, HashSet<AvailablePart>>();
        private static SortedDictionary<string, HashSet<AvailablePart>> resourceHash = new SortedDictionary<string, HashSet<AvailablePart>>();
        private static SortedDictionary<string, HashSet<AvailablePart>> partModuleHash = new SortedDictionary<string, HashSet<AvailablePart>>();

        public int ModFilteredCount = 0;
        public int ModInverseCount = 0;
        public int SizeFilteredCount = 0;
        public int ResourceFilteredCount = 0;
        public int ModuleFilteredCount = 0;
        public int ModuleInverseCount = 0;
        bool hideUnpurchased = false;

        private static UrlDir.UrlConfig[] configs = null;

        int selectedFilterList = 1;

        //-------------------------------------------------------------------------------------------------------------------------------------------
        bool PartIsPurchased(AvailablePart info)
        {
            if (PartLoader.Instance == null) return false;
            return HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX || ResearchAndDevelopment.PartModelPurchased(info);
        }

        static public string FindPartMod(AvailablePart part)
        {
            if (configs == null)
                configs = GameDatabase.Instance.GetConfigs("PART");

            Log.Info("ModFilterWindow.FindPartMod, part.name: " + part.name);
            UrlDir.UrlConfig config = Array.Find<UrlDir.UrlConfig>(configs, (c => (part.name == c.name.Replace('_', '.').Replace(' ', '.'))));
            if (config == null)
            {
                config = Array.Find<UrlDir.UrlConfig>(configs, (c => (part.name == c.name)));
                if (config == null)
                    return "";
            }
            var id = new UrlDir.UrlIdentifier(config.url);
            return id[0];
        }

        public void Show()
        {
            this.enabled = true;

            modWindowRect = FilterWindowRect("Mods", Math.Max(modButtons.Count, Math.Max(sizeButtons.Count, Math.Max(resourceButtons.Count, partModuleButtons.Count))));

            if (JanitorsCloset.GetModFilterWin != null)
            {
                var r = (Rect)JanitorsCloset.GetModFilterWin;
                modWindowRect.x = r.x;
                modWindowRect.y = r.y;

            }
        }

        public void Hide() { this.enabled = false; }

        static string UsefulModuleName(string longName)
        {
            Log.Info("ModFilterWindow.UsefulModuleName");

            if (longName.StartsWith("Module"))
                return longName.Substring(6);
            if (longName.StartsWith("FXModule"))
                return "FX" + longName.Substring(8);
            return longName;
        }


        void InitialPartsScan(List<AvailablePart> loadedParts)
        {
            Log.Info("ModFilterWindow.InitialPartsScan");

            int index = 1;
            foreach (var part in loadedParts)
            {
                if (part == null)
                    continue;
                Log.Info(string.Format("PROCESS {0}", part.name));

                PartInfo partInfo = new PartInfo(part);
                partInfos[part.name]= partInfo;
                //if (!partInfos.ContainsKey(part.name))
                //    partInfos.Add(part.name, partInfo);
                //else
                //    Log.Error("Part already loaded: " + part.name);

                partInfo.defaultPos = index++;

                // add the size to the list of all sizes known if it's the first time we've seen this part size

                try
                {
                    if (!sizeButtons.ContainsKey(partInfo.partSize))
                    {
                        Log.Info(string.Format("define new size filter key {0}", partInfo.partSize));
                        sizeButtons.Add(partInfo.partSize, new ToggleState() { enabledState = true, latched = false, inverse = false });
                        sizeHash.Add(partInfo.partSize, new HashSet<AvailablePart>());
                    }
                    Log.Info(string.Format("add {0} to sizeHash for {1}", part.name, partInfo.partSize));
                    sizeHash[partInfo.partSize].Add(part);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught (1), message: " + ex.Message);
                }

                // Add any resources the part has listed
                if (part.resourceInfos.Count > 0)
                {
                    foreach (var res in part.resourceInfos)
                    {

                        try
                        {
                            if (!resourceButtons.ContainsKey(res.resourceName))
                            {
                                Log.Info(string.Format("define new resource filter key {0}", res.resourceName));
                                resourceButtons.Add(res.resourceName, new ToggleState() { enabledState = true, latched = false, inverse = false });
                                resourceHash.Add(res.resourceName, new HashSet<AvailablePart>());
                            }
                            Log.Info(string.Format("add {0} to resourceHash for {1}", part.name, res.resourceName));
                            resourceHash[res.resourceName].Add(part);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Exception caught (2), message: " + ex.Message);
                        }
                    }
                }
                else
                {
                    string resname = "None";

                    try
                    {
                        if (!resourceButtons.ContainsKey(resname))
                        {
                            resourceButtons.Add(resname, new ToggleState() { enabledState = true, latched = false, inverse = false });
                            resourceHash.Add(resname, new HashSet<AvailablePart>());
                        }
                        Log.Info(string.Format("add {0} to resourceHash for {1}", part.name, resname));
                        resourceHash[resname].Add(part);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception caught (3), message: " + ex.Message);
                    }
                }

                if (part.partPrefab.Modules.Count == 0)
                {
                    string moduleName = "None";
                    try
                    {
                        if (!partModuleButtons.ContainsKey(moduleName))
                        {
                            Log.Info(string.Format("define new module.moduleName filter key {0}", moduleName));
                            partModuleButtons.Add(moduleName, new ToggleState() { enabledState = true, latched = false, inverse = false });
                            partModuleHash.Add(moduleName, new HashSet<AvailablePart>());
                        }
                        Log.Info(string.Format("add {0} to partModuleHash for moduleName: {1}", part.name, moduleName));
                        partModuleHash[moduleName].Add(part);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception caught (6), message: " + ex.Message);
                    }
                }
                else
                {
                    foreach (var module in part.partPrefab.Modules)
                    {
                        // First get all the part modules here

                        Log.Info("module: part name: " + part.name + ", moduleName: " + module.moduleName);

                        try
                        {
                            if (!partModuleButtons.ContainsKey(module.moduleName))
                            {

                                Log.Info(string.Format("define new module.moduleName filter key {0}", module.moduleName));
                                partModuleButtons.Add(module.moduleName, new ToggleState() { enabledState = true, latched = false, inverse = false });
                                partModuleHash.Add(module.moduleName, new HashSet<AvailablePart>());
                            }
                            Log.Info(string.Format("add {0} to partModuleHash for moduleName: {1}", part.name, module.moduleName));
                            partModuleHash[module.moduleName].Add(part);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Exception caught (4), message: " + ex.Message);
                        }

                        // Now get the resources used by the modules

                        foreach (var res in module.resHandler.inputResources)
                        {

                            try
                            {
                                if (!resourceButtons.ContainsKey(res.name))
                                {
                                    Log.Info(string.Format("define new res.inputResource filter key {0}", res.name));
                                    resourceButtons.Add(res.name, new ToggleState() { enabledState = true, latched = false, inverse = false });
                                    resourceHash.Add(res.name, new HashSet<AvailablePart>());
                                }
                                Log.Info(string.Format("add {0} to resourceHash for inputResource: {1}", part.name, res.name));
                                resourceHash[res.name].Add(part);
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Exception caught (5), message: " + ex.Message);
                            }

                        }
                        for (int i = 0; i < module.resHandler.outputResources.Count; i++)
                        {
                            var res = module.resHandler.outputResources[i];
                        //}
                        //foreach (var res in module.resHandler.outputResources)
                        //{
                            try
                            {
                                if (!resourceButtons.ContainsKey(res.name))
                                {
                                    Log.Info(string.Format("define new res.outputResources filter key {0}", res.name));
                                    resourceButtons.Add(res.name, new ToggleState() { enabledState = true, latched = false, inverse = false });
                                    resourceHash.Add(res.name, new HashSet<AvailablePart>());
                                }
                                Log.Info(string.Format("add {0} to resourceHash for outputResources: {1}", part.name, res.name));
                                resourceHash[res.name].Add(part);
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Exception caught (6), message: " + ex.Message);
                            }

                        }
                        switch (module.moduleName)
                        {
                            case "ModuleEngines":
                                {
                                    Log.Info("ModuleEngines");
                                    ModuleEngines me = module as ModuleEngines;
                                    for (int i = me.propellants.Count - 1; i >= 0; i--)
                                    {
                                        Propellant propellant = me.propellants[i];

                                        try
                                        {
                                            if (!resourceButtons.ContainsKey(propellant.name))
                                            {
                                                Log.Info(string.Format("define new propellant filter key {0}", propellant.name));
                                                resourceButtons.Add(propellant.name, new ToggleState() { enabledState = true, latched = false, inverse = false });
                                                resourceHash.Add(propellant.name, new HashSet<AvailablePart>());
                                            }
                                            Log.Info(string.Format("add {0} to resourceHash for outputResources: {1}", part.name, propellant.name));
                                            resourceHash[propellant.name].Add(part);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error("Exception caught (7), message: " + ex.Message);
                                        }

                                    }
                                    break;
                                }
                            case "ModuleEnginesFX":
                                {
                                    Log.Info("ModuleEnginesFX");

                                    ModuleEngines me = module as ModuleEnginesFX;
                                    for (int i = me.propellants.Count - 1; i >= 0; i--)
                                    {
                                        Propellant propellant = me.propellants[i];

                                        try
                                        {
                                            if (!resourceButtons.ContainsKey(propellant.name))
                                            {
                                                Log.Info(string.Format("define new propellant filter key {0}", propellant.name));
                                                resourceButtons.Add(propellant.name, new ToggleState() { enabledState = true, latched = false, inverse = false });
                                                resourceHash.Add(propellant.name, new HashSet<AvailablePart>());
                                            }
                                            Log.Info(string.Format("add {0} to resourceHash for outputResources: {1}", part.name, propellant.name));
                                            resourceHash[propellant.name].Add(part);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error("Exception caught (8), message: " + ex.Message);
                                        }

                                    }
                                    break;
                                }
                            case "ModuleRCS":
                                {
                                    Log.Info("ModuleRCS");

                                    ModuleRCS me = module as ModuleRCS;
                                    for (int i = me.propellants.Count - 1; i >= 0; i--)
                                    {
                                        Propellant propellant = me.propellants[i];

                                        try
                                        {
                                            if (!resourceButtons.ContainsKey(propellant.name))
                                            {
                                                Log.Info(string.Format("define new propellant filter key {0}", propellant.name));
                                                resourceButtons.Add(propellant.name, new ToggleState() { enabledState = true, latched = false, inverse = false });
                                                resourceHash.Add(propellant.name, new HashSet<AvailablePart>());
                                            }
                                            Log.Info(string.Format("add {0} to resourceHash for propellant: {1}", part.name, propellant.name));
                                            if (!resourceHash[propellant.name].Contains(part))
                                                resourceHash[propellant.name].Add(part);
                                            else
                                                Log.Error("Part: " + part.name + "  already added to hash: " + propellant.name);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error("Exception caught (9), message: " + ex.Message);
                                        }

                                    }
                                    break;
                                }
                            case "ModuleRCSFX":
                                {
                                    Log.Info("ModuleRCSFX");

                                    ModuleRCSFX me = module as ModuleRCSFX;

                                    for (int i = me.propellants.Count - 1; i >= 0; i--)
                                    {
                                        Propellant propellant = me.propellants[i];

                                        try
                                        {
                                            if (!resourceButtons.ContainsKey(propellant.name))
                                            {
                                                Log.Info(string.Format("define new propellant filter key {0}", propellant.name));
                                                resourceButtons.Add(propellant.name, new ToggleState() { enabledState = true, latched = false, inverse = false });
                                                resourceHash.Add(propellant.name, new HashSet<AvailablePart>());
                                            }
                                            Log.Info(string.Format("add {0} to resourceHash for propellant: {1}", part.name, propellant.name));
                                            if (!resourceHash[propellant.name].Contains(part))
                                                resourceHash[propellant.name].Add(part);
                                            else
                                                Log.Error("Part: " + part.name + "  already added to hash: " + propellant.name);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error("Exception caught (10), message: " + ex.Message);
                                        }


                                    }

                                    break;
                                }
                        }
                    }
                }


                // the part's base directory name is used to filter entire mods in and out
                string partModName = FindPartMod(part);
                Log.Info("partModName: " + partModName);
                if (partModName != "")
                {

                    try
                    {
                        if (!modButtons.ContainsKey(partModName))
                        {
                            Log.Info(string.Format("define new mod filter key {0}", partModName));
                            modButtons.Add(partModName, new ToggleState() { enabledState = true, latched = false, inverse = false });
                            modHash.Add(partModName, new HashSet<AvailablePart>());
                        }
                        Log.Info(string.Format("add {0} to modHash for {1}", part.name, partModName));
                        if (!modHash[partModName].Contains(part))
                            modHash[partModName].Add(part);
                        else
                            Log.Error("Part: " + part.name + "  already added to hash: " + partModName);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception caught (11), message: " + ex.Message);
                    }


                    // save all the module names that are anywhere in this part
                    if (part.partPrefab == null)
                        continue;
                    if (part.partPrefab.Modules == null)
                        continue;

                    foreach (PartModule module in part.partPrefab.Modules)
                    {
                        string fullName = module.moduleName;
                        if (fullName == null)
                        {
                            Log.Info(string.Format("{0} has a null moduleName, skipping it", part.name));
                            continue;
                        }
                        Log.Info(string.Format("scan part '{0}' module [{2}]'{1}'", part.name, fullName, fullName.Length));
                        string moduleName = UsefulModuleName(fullName);

                    }
                }
            }
        }

        Rect FilterWindowRect(string name, int buttons)
        {
            var lineHeight = (int)HighLogic.Skin.GetStyle("Toggle").CalcHeight(new GUIContent("XXQ"), 20);

            if (buttons > 20)
                buttons = 20;
            if (buttons < 10)
                buttons = 10;

            float height = lineHeight * buttons;

            Rect answer = new Rect(300, 200, 350, height);

            //Log.Info( string.Format("defining window {4} at ({0},{1},{2},{3})", answer.xMin, answer.yMin, answer.width, answer.height, name));
            return answer;
        }

        List<AvailablePart> GetPartsList()
        {
            List<AvailablePart> loadedParts = new List<AvailablePart>();
            loadedParts.AddRange(PartLoader.LoadedPartsList); // make a copy we can manipulate

            // these two parts are internal and just serve to mess up our lists and stuff
            //AvailablePart kerbalEVA = null;
            //AvailablePart flag = null;
            for (int i = loadedParts.Count - 1; i >= 0; i--)
            {
                var part = loadedParts[i];
                if (part.name.Contains("kerbalEVA"))
                {
                    //kerbalEVA = part;
                    loadedParts.Remove(part);
                    partInfos.Add(part.name, new PartInfo(part));
                }
                else if (part.name == "flag")
                {
                    //flag = part;
                    loadedParts.Remove(part);
                    partInfos.Add(part.name, new PartInfo(part));
                }
            }
#if false
            // still need to prevent errors with null refs when looking up these parts though
            if (kerbalEVA != null)
            {
                //loadedParts.Remove(kerbalEVA);
                partInfos.Add(kerbalEVA, new PartInfo(kerbalEVA));
            }
            if (flag != null)
            {
                //loadedParts.Remove(flag);
                partInfos.Add(flag, new PartInfo(flag));
            }
#endif
            return loadedParts;
        }

        bool PartInResourceFilteredButtons(string filter, AvailablePart part, SortedDictionary<string, ToggleState> buttons, SortedDictionary<string, HashSet<AvailablePart>> filterHash)
        {
            Log.Info("part: " + part.name);
            foreach (KeyValuePair<string, ToggleState> entry in buttons)
            {
                if (!entry.Value.enabledState || !filterHash.ContainsKey(entry.Key))
                    continue;
                try
                {
                    if (filterHash[entry.Key].Contains(part))
                    {
                        Log.Info("part: " + part.name + " has resource: " + entry.Key);
                        return true;
                    }
                }
                catch
                {
                    Log.Error("PartInResourceFilteredButtons, filter: " + filter + " entry.Key not in filterHash: " + entry.Key + ", part: " + part.name);

                }

            }
            return false;
        }

        bool PartInFilteredButtons(string filter, AvailablePart part, SortedDictionary<string, ToggleState> buttons, SortedDictionary<string, HashSet<AvailablePart>> filterHash)
        {
            foreach (KeyValuePair<string, ToggleState> entry in buttons)
            {
                if (!entry.Value.enabledState || !filterHash.ContainsKey(entry.Key))
                    continue;
                try
                {
                    if (filterHash[entry.Key].Contains(part))
                        return true;
                }
                catch
                {
                    Log.Error("PartInFilteredButtons, filter: " + filter + " entry.Key not in filterHash: " + entry.Key + ", part: " + part.name);
                }
            }
            return false;
        }

        bool PartInResourseExcludeButtons(string filter, AvailablePart part, SortedDictionary<string, ToggleState> buttons, SortedDictionary<string, HashSet<AvailablePart>> filterHash)
        {
            foreach (KeyValuePair<string, ToggleState> entry in buttons)
            {
                if (!entry.Value.inverse)
                    continue;
                if (!filterHash.ContainsKey(entry.Key))
                {
                    Log.Error("PartInResourseExcludeButtons, filter: " + filter + ", filterHash does not contain key: " + entry.Key);
                }
                else
                {
                    if (filterHash[entry.Key].Contains(part))
                        return false;
                }
            }
            return true;
        }

        bool PartInUnpurchasedButtons(string filter, AvailablePart part, SortedDictionary<string, ToggleState> buttons, SortedDictionary<string, HashSet<AvailablePart>> filterHash)
        {
            if (!hideUnpurchased)
                return true;
            return PartIsPurchased(part);
        }


        bool PartInModuleButtons(string filter, AvailablePart part, SortedDictionary<string, ToggleState> buttons, SortedDictionary<string, HashSet<AvailablePart>> filterHash)
        {
            foreach (KeyValuePair<string, ToggleState> entry in buttons)
            {
                if (!entry.Value.enabledState || !filterHash.ContainsKey(entry.Key))
                    continue;
                try
                {
                    if (filterHash[entry.Key].Contains(part))
                    {
                        return true;
                    }
                }
                catch
                {
                    Log.Error("PartInModuleButtons, filter: " + filter + ", entry.Key not in filterHash: " + entry.Key + ", part: " + part.name);
                }
            }
            return false;
        }

        bool PartInModuleExcludeButtons(string filter, AvailablePart part, SortedDictionary<string, ToggleState> buttons, SortedDictionary<string, HashSet<AvailablePart>> filterHash)
        {
            foreach (KeyValuePair<string, ToggleState> entry in buttons)
            {
                if (!entry.Value.inverse)
                    continue;

                if (!filterHash.ContainsKey(entry.Key))
                {
                    Log.Error("PartInModuleExcludeButtons, filter: " + filter + ", filterHash does not contain key: " + entry.Key);
                }
                else
                {
                    if (filterHash[entry.Key].Contains(part))
                        return false;
                }
            }
            return true;
        }

        void DefineFilters()
        {
            Log.Info("DefineFilters");
            if (configs == null)
                configs = GameDatabase.Instance.GetConfigs("PART");

            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Mod Filter", (part => PartInFilteredButtons("Mod Filter", part, modButtons, modHash))));

            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Size Filter", (part => PartInFilteredButtons("Size Filter", part, sizeButtons, sizeHash))));

            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Resource Filter", (part => PartInResourceFilteredButtons("Resource Filter", part, resourceButtons, resourceHash))));

            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Resource Exclude Filter", (part => PartInResourseExcludeButtons("Resource Exclude Filter", part, resourceButtons, resourceHash))));

            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Unpurchased Filter", (part => PartInUnpurchasedButtons("Unpurchased Filter", part, sizeButtons, sizeHash))));
#if true
            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Module Filter", (part => PartInModuleButtons("Module Filter", part, partModuleButtons, partModuleHash))));
            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Module Exclude Filter", (part => PartInModuleExcludeButtons("Module Exclude Filter", part, partModuleButtons, partModuleHash))));
#endif
            //EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Modules Filter", (part => !PartInFilteredButtons(part, moduleButtons, moduleHash))));
            EditorPartList.Instance.Refresh();
        }

        static GUIStyle styleButton = null;
        static GUIStyle styleButtonSettings;

        static Color origBackgroundColor;
        static GUIStyle styleButtonLeftAligned;

        static bool initted = false;
        void InitData()
        {
            if (!initted)
            {
                CONFIG_BASE_FOLDER = KSPUtil.ApplicationRootPath + "GameData/";
                JC_BASE_FOLDER = CONFIG_BASE_FOLDER + "JanitorsCloset/";
                JC_NODE = "JANITORSCLOSET";
                JC_CFG_FILE = JC_BASE_FOLDER + "PluginData/JCModfilter-v2-";
                JC_FILTER_CONFIG_FILE = JC_BASE_FOLDER + "PluginData/FiltersConfig.cfg";
                //JS_WINPOS_FILE = JC_BASE_FOLDER + "PluginData/WinPos.cfg";
                JC_READABLE_NAMES_NODE = "READABLENAMES";
                JC_BULKHEADPROFILES = "BULKHEADPROFILES";
                JC_BLACKLIST_NODE = "MODULE_BLACKLIST";
#if false
            JC_MERGELIST_NODE = "MERGELIST";
#endif

                initted = true;
                if (configs == null)
                    configs = GameDatabase.Instance.GetConfigs("PART");
                LoadFiltersConfig();

                List<AvailablePart> loadedParts = GetPartsList();
                InitialPartsScan(loadedParts);
                LoadValuesFromConfig(selectedFilterList);
            }
        }
        public void Start()
        {
            Log.Info("ModFilterWindow.Start");
            InitData();
            // DefineFilters();

            modwindowRectID = JanitorsCloset.getNextID();
            modFilterHelpWindowID = JanitorsCloset.getNextID();
            enabled = false;
            instance = this;

        }

        bool filtersDefined = false;
        public void Update()
        {
            if (filtersDefined)
                return;
            filtersDefined = true;
            DefineFilters();
        }

        void InitStyles()
        {
            Log.Info("InitStyles");
            styleButton = new GUIStyle(GUI.skin.button);
            styleButton.name = "ButtonGeneral";
            styleButton.normal.background = GUI.skin.button.normal.background;
            styleButton.hover.background = GUI.skin.button.hover.background;
            styleButton.normal.textColor = new Color(207, 207, 207);
            styleButton.fontStyle = FontStyle.Normal;
            styleButton.fixedHeight = 20;
            styleButton.padding.top = 2;

            styleButtonSettings = new GUIStyle(styleButton);
            styleButtonSettings.name = "ButtonSettings";
            styleButtonSettings.padding = new RectOffset(1, 1, 1, 1);
            styleButtonSettings.onNormal.background = styleButtonSettings.active.background;
            styleButtonSettings.alignment = TextAnchor.MiddleCenter;
            styleButtonSettings.normal.textColor = new Color32(177, 193, 205, 255);
            styleButtonSettings.fontStyle = FontStyle.Bold;

            origBackgroundColor = GUI.backgroundColor; // store value.
            styleButtonLeftAligned = new GUIStyle(GUI.skin.button);
            styleButtonLeftAligned.alignment = TextAnchor.MiddleLeft;
        }


        string _windowTitle = string.Empty;
        int modwindowRectID;
        int modFilterHelpWindowID;

        //bool showFilterHelpWindow = false;

        public void OnGUI()
        {
            if (!enabled)
                return;
            if (Event.current.type == EventType.Repaint)
                GUI.skin = HighLogic.Skin;
            if (styleButton == null)
                InitStyles();

            _windowTitle = string.Format("Mod Filter");
            var tstyle = new GUIStyle(GUI.skin.window);

            var newModWindowRect = ClickThruBlocker.GUILayoutWindow(modwindowRectID, modWindowRect, FilterChildWindowHandler, _windowTitle, tstyle);

            if (helpPopup != null)
                helpPopup.draw();

            if (newModWindowRect != modWindowRect)
            {
                modWindowRect = newModWindowRect;
                JanitorsCloset.SetModFilterWin = modWindowRect;
            }
        }

        int CompareEntries(string lft, string rght)
        {
            string left = GetReadableName(lft);
            string right = GetReadableName(rght);
            if (left == right)
                return 0;
            if (left == "None")
                return -1;
            if (right == "None")
                return 1;

            // use of "larger" now discontinued due to using bulkhead profiles instead of nodes
#if false
            // Special cases for adapters so they are in height order, from smaller to larger
            if (left.Contains("Adapter") && right.Contains("larger"))
                return 1;
            if (right.Contains("Adapter") && left.Contains("larger"))
                return -1;
            if (left.Contains("larger"))
                return 1;
            if (right.Contains("larger"))
                return -1;
#endif
            if (left.Contains("Adapter") && !right.Contains("Adapter"))
                return 1;
            if (right.Contains("Adapter") && !left.Contains("Adapter"))
                return -1;

            return String.Compare(left, right);
        }

        void resetAll()
        {
            var names = new List<string>(modButtons.Keys);
            SortedDictionary<string, ToggleState> states = modButtons;
            foreach (string name in names)
            {
                ToggleState state = states[name];
                state.enabledState = true;
                state.inverse = false;
                states[name] = state;
            }
            names = new List<string>(sizeButtons.Keys);
            states = sizeButtons;
            foreach (string name in names)
            {
                ToggleState state = states[name];
                state.enabledState = true;
                state.inverse = false;
                states[name] = state;
            }
            names = new List<string>(resourceButtons.Keys);
            states = resourceButtons;
            foreach (string name in names)
            {
                ToggleState state = states[name];
                state.enabledState = true;
                state.inverse = false;
                states[name] = state;
            }

            names = new List<string>(partModuleButtons.Keys);
            states = partModuleButtons;
            foreach (string name in names)
            {
                ToggleState state = states[name];
                state.enabledState = true;
                state.inverse = false;
                states[name] = state;
            }
        }

        HelpPopup helpPopup = null;
        private void FilterHelpWindow(int id)
        {
            if (helpPopup == null)
            {
                helpPopup = new HelpPopup(
                    "Mod Filter", "Mod Name - filter mods by name\n" +
                    "Module Size - filter mods by size\nResources - filter mods by resource\n" +
                    "Modules - filter mods by modules\n\n" +
                    "Resource filter can filter by two methods:\n" +
                    "\t1. The green toggle says that the resource must be contained in the part or\n" +
                    "\t   (if an engine module) used by the part\n" +
                    "\t2. The red toggle says that shown parts must NOT have or use the specified\n" +
                    "\t   resource.  Clicking the button with the resource name will cycle\n" +
                    "\t   through the three modes of filtering for that resource.\n\n" +

                    "Modules refers to the part modules which provide the functionality in each\n" +
                    "part.  Like the resources, you can either filter by requiring one or more specific\n" +
                    "part modules to be present (the green toggle), or you can filter by excluding\n" +
                    "part which have specific part modules (the red toggle)\n\n" +
                    "In essense, the filters work by requiring all values with Green toggles be present\n" +
                    "AND that all values with Red toggles be excluded.\n\n" +

                    "The Invert button will invert all the toggles and is only available on the Resources and Modules windows"
                    , JanitorsCloset.getNextID());
            }

            helpPopup.showMenu = true;
            helpPopup.SetWinName("FilterHelpWindow");

            //showFilterHelpWindow = false;
        }

        Vector2 scrollPosition;
        string[] filterType = new string[] { "Mod Name", "Module Size", "Resources", "Modules" };
        int selFilter = 0;

        private void FilterChildWindowHandler(int id)
        {
            SortedDictionary<string, ToggleState> states = modButtons;
            if (GUI.Button(new Rect(12, 2, 30, 20), "?", styleButtonSettings))
            {
                // showFilterHelpWindow = !showFilterHelpWindow;
                FilterHelpWindow(modFilterHelpWindowID);
            }
            if (GUI.Button(new Rect(modWindowRect.width - 32, 2, 30, 20), "X", styleButtonSettings))
            {
                enabled = false;
                //  Visible = false;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            selFilter = GUILayout.SelectionGrid(selFilter, filterType, filterType.Length);
            switch (selFilter)
            {
                case 0:
                    states = modButtons;
                    break;
                case 1:
                    states = sizeButtons;
                    break;
                case 2:
                    states = resourceButtons;
                    break;
                case 3:
                    states = partModuleButtons;
                    break;
            }

            GUILayout.EndHorizontal();
            bool updateNeeded = false;
            GUILayout.BeginHorizontal();
            if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX)
            {
                string s = " Hide Unpurchased ";
                if (hideUnpurchased)
                    s = " Show Unpurchased ";
                if (GUILayout.Button(s))
                {
                    hideUnpurchased = !hideUnpurchased;
                    updateNeeded = true;
                }
            }
            if (GUILayout.Button("Show All"))
            {
                var names = new List<string>(states.Keys);
                foreach (string name in names)
                {
                    ToggleState state = states[name];
                    state.enabledState = true;
                    states[name] = state;
                }
                updateNeeded = true;
            }
            if (GUILayout.Button("Hide All"))
            {
                var names = new List<string>(states.Keys);
                foreach (string name in names)
                {
                    ToggleState state = states[name];
                    state.enabledState = false;
                    states[name] = state;
                }
                updateNeeded = true;
            }
            if (selFilter == 2 || selFilter == 3)
            {
                if (GUILayout.Button("Invert"))
                {
                    var names = new List<string>(states.Keys);
                    foreach (string name in names)
                    {
                        ToggleState state = states[name];
                        state.inverse = !state.inverse;
                        state.enabledState = !state.inverse;
                        states[name] = state;
                    }
                    updateNeeded = true;
                }
            }
            if (GUILayout.Button("Reset All"))
            {
                resetAll();
                updateNeeded = true;
            }
            GUILayout.EndHorizontal();

            // saved filter lists
            var oldColor = GUI.backgroundColor;
            GUILayout.BeginHorizontal();
            for (int cnt = 1; cnt <= 10; cnt++)
            {
                if (selectedFilterList == cnt)
                {
                    GUI.backgroundColor = Color.green;
                }
                if (GUILayout.Button(cnt.ToString(), GUILayout.Width(30)))
                {
                    selectedFilterList = cnt;
                    LoadValuesFromConfig(selectedFilterList);
                    updateNeeded = true;
                }
                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndHorizontal();
            if (updateNeeded)
            {
                EditorPartList.Instance.Refresh();
                SaveConfig(selectedFilterList);
            }

            // This will hide the horizontal scrollbar
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            foreach (string name in states.Keys)
            {
                ToggleState state = states[name];
                //string truncatedName = (name.Length > 35) ? name.Remove(34) : name;
                bool before = state.enabledState;
                bool beforeInverse = state.inverse;

                string readableName = GetReadableName(name);

                if (readableName != "" && readableName != "None")
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (selFilter != 2 && selFilter != 3)
                    {
                        state.enabledState = GUILayout.Toggle(state.enabledState, readableName);
                        GUILayout.Space(20);
                        if (state.enabledState && state.enabledState != before)
                            state.inverse = false;
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        //Log.Info("FilterChildWindowHandler 3");
                        state.enabledState = GUILayout.Toggle(state.enabledState, "");
                        GUILayout.Space(20);
                        if (state.enabledState && state.enabledState != before)
                            state.inverse = false;

                        GUI.backgroundColor = Color.red;
                        state.inverse = GUILayout.Toggle(state.inverse, "");
                        GUI.backgroundColor = origBackgroundColor; // reset to old value.
                        if (state.inverse && state.inverse != beforeInverse)
                            state.enabledState = false;
                        GUILayout.Space(20);

                        if (GUILayout.Button(readableName, styleButtonLeftAligned, GUILayout.Width(300)))
                        {
                            if (state.enabledState)
                            {
                                state.enabledState = false;
                                state.inverse = true;
                            }
                            else
                            {
                                if (state.inverse)
                                {
                                    state.inverse = false;
                                }
                                else
                                    state.enabledState = true;
                            }
                        }

                        //Log.Info("FilterChildWindowHandler 4");

                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);
                        //Log.Info("FilterChildWindowHandler 5");

                    }

                    if (before != state.enabledState || beforeInverse != state.inverse)
                    {
                        states[name] = state;
                        EditorPartList.Instance.Refresh();
                        SaveConfig(selectedFilterList);
                        // Break here to avoid an error:  Collection was modified at the foreach
                        // since the state was updated.  Nothing visible on screen when 
                        // this happens
                        break;
                        //before = state.enabledState;
                        //beforeInverse = state.inverse;
                    }
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
            ModFilteredCount = modButtons.Where(p => p.Value.enabledState == false).Count();
            ModInverseCount = modButtons.Where(p => p.Value.inverse == true).Count();
            SizeFilteredCount = sizeButtons.Where(p => p.Value.enabledState == false).Count();
            ResourceFilteredCount = resourceButtons.Where(p => p.Value.enabledState == false).Count();
            ModuleFilteredCount = partModuleButtons.Where(p => p.Value.enabledState == false).Count();
            ModuleInverseCount = partModuleButtons.Where(p => p.Value.inverse == true).Count();

        }

        private static String CONFIG_BASE_FOLDER;
        private static String JC_BASE_FOLDER;
        private static String JC_NODE;
        private static String JC_READABLE_NAMES_NODE;
        private static string JC_BULKHEADPROFILES;
        private static String JC_BLACKLIST_NODE;
#if false
        private static String JC_MERGELIST_NODE;
#endif
        private static String JC_CFG_FILE;
        private static String JC_FILTER_CONFIG_FILE;
        //private static string JS_WINPOS_FILE;

        private static ConfigNode configFile = null;
        private static ConfigNode configFileNode = null;
        private static ConfigNode configSectionNode = null;


        //-------------------------------------------------------------------------------------------------------------------------------------------
        void SaveSelectType(string prefix, SortedDictionary<string, ToggleState> buttonList, string NodeName, ConfigNode fileNode)
        {
            configSectionNode = new ConfigNode(NodeName);

            foreach (var mod in buttonList)
            {
                configSectionNode.SetValue(mod.Key, mod.Value.enabledState, true);
                configSectionNode.SetValue(mod.Key + INVERSE, mod.Value.inverse, true);
            }
            configFileNode.SetNode(NodeName, configSectionNode, true);

        }

        void SaveConfig(int selectedCfg) //string sorting = null)
        {
            Log.Info("SaveConfig");
            configFile = new ConfigNode();
            configFileNode = new ConfigNode();

            SaveSelectType("Mod", modButtons, "MOD", configFileNode);
            SaveSelectType("Size", sizeButtons, "SIZE", configFileNode);
            SaveSelectType("Res", resourceButtons, "RESOURCE", configFileNode);
            SaveSelectType("Module", partModuleButtons, "MODULES", configFileNode);

            //config.save();
            configFile.SetNode(JC_NODE, configFileNode, true);
            //configFile.AddNode (KRASH_CUSTOM_NODE, configFileNode);
            configFile.Save(JC_CFG_FILE + selectedCfg.ToString() + ".cfg");
        }
        //-------------------------------------------------------------------------------------------------------------------------------------------

        void LoadValuesFromSectionNode(string NodeName, SortedDictionary<string, ToggleState> buttonList, ConfigNode configFileNode, string prefix)
        {
            configSectionNode = configFileNode.GetNode(NodeName);
            if (configSectionNode != null)
                LoadConfigSection(NodeName, configSectionNode, prefix, buttonList);
        }

        private void LoadValuesFromConfig(int selectedCfg)
        {
            Log.Info("LoadValuesFromConfig, selectedCfg: " + selectedCfg);
            resetAll();
            if (System.IO.File.Exists(JC_CFG_FILE + selectedCfg.ToString() + ".cfg"))
            {
                configFile = ConfigNode.Load(JC_CFG_FILE + selectedCfg.ToString() + ".cfg");
                configFileNode = configFile.GetNode(JC_NODE);
                if (configFileNode != null)
                {
                    //PluginConfiguration config = PluginConfiguration.CreateForType<ModFilterWindow>();
                    //config.load();
                    //LoadConfigSection(config, "Module", moduleButtons);
                    LoadValuesFromSectionNode("MOD", modButtons, configFileNode, "Mod");
                    LoadValuesFromSectionNode("SIZE", sizeButtons, configFileNode, "Size");
                    LoadValuesFromSectionNode("RESOURCE", resourceButtons, configFileNode, "Res");
                    LoadValuesFromSectionNode("MODULES", partModuleButtons, configFileNode, "Module");

                    ModFilteredCount = modButtons.Where(p => p.Value.enabledState == false).Count();
                    ModInverseCount = modButtons.Where(p => p.Value.inverse == true).Count();
                    SizeFilteredCount = sizeButtons.Where(p => p.Value.enabledState == false).Count();
                    ResourceFilteredCount = resourceButtons.Where(p => p.Value.enabledState == false).Count();
                    ModuleFilteredCount = partModuleButtons.Where(p => p.Value.enabledState == false).Count();
                    ModuleInverseCount = partModuleButtons.Where(p => p.Value.inverse == true).Count();

                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------
        void LoadConfigSection(string NodeName, ConfigNode cfgNode, string prefix, SortedDictionary<string, ToggleState> buttons)
        {
            for (int i = 0; i < cfgNode.CountValues; i++)
            {
                bool inverse = false;
                var e = cfgNode.values[i].name;
                if (e.EndsWith(INVERSE))
                {
                    inverse = true;
                    e = cfgNode.values[i].name.Substring(0, cfgNode.values[i].name.Length - 8);
                }
                ToggleState s;
                if (!buttons.ContainsKey(e))
                {
                    s = new ToggleState();
                    s.inverse = false;
                    s.enabledState = true;
                }
                else
                {
                    s = buttons[e];
                }

                if (!inverse)
                    s.enabledState = bool.Parse(cfgNode.values[i].value);
                else
                    s.inverse = bool.Parse(cfgNode.values[i].value);

                buttons[e] = s;
            }

        }


        /// <summary>
        /// Get a readable name from the dictionary.  If not there, create it
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string GetReadableName(string name, bool add = true)
        {
            string s;
            try
            {
                s = readableNamesDict[name];
                //Log.Info("GetReadableName, name: [" + name + "], readableName: [" + s + "]");
                return s;
            }
            catch
            {
#if false // for future use
                if (mergeListDict.ContainsKey(name))
                {
                    name = mergeListDict[name];
                    if (readableNamesDict.ContainsKey(name))
                        return ""; // readableNamesDict[name];
                }
#endif

                s = BestGuessReadableName(name);

                if (s != "" && add)
                {
                    readableNamesDict.Add(name, s);
                    //Log.Info("GetReadableName, adding to readableNamesDict, name: [" + name + "], readableName: [" + s + "]");
                    SaveReadableNames();
                }

                return s;
            }
        }


        static string[] deleteLeading = new string[] { "Module", "CModule" };

        /// <summary>
        /// Given the input string, create a readable name from it using the specified rules
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        static string BestGuessReadableName(string inputString, bool allowSpaces = false, bool capitalize = false)
        {
            //bool mk1 = (inputString == "mk1square");
            StringBuilder outputStr = new StringBuilder("", 25);

            // string outputString = "";
            // No change if any spaces in string
            // lower followed by upper = word break
            // numeric digit followed by upper = insert a dash
            // Delete leading words:
            //      Module
            //      CModule
            //      
            //  Exclude the names in the blacklist

            if (blacklistNames.Contains(inputString))
                return "";

            if (!allowSpaces && inputString.Contains(" "))
            {
                return inputString;
            }
            for (int i = 0; i < 2; i++)
            {
                if (inputString.Length >= deleteLeading[i].Length && inputString.Substring(0, deleteLeading[i].Length) == deleteLeading[i])
                {
                    inputString = inputString.Remove(0, deleteLeading[i].Length);
                }
            }

            while (inputString.Length > 0 &&
                   (!Char.IsDigit(inputString[0]) ||
                    (inputString.Length > 1 && char.IsDigit(inputString[0]) &&
                     ((!capitalize && char.IsUpper(inputString[1])) || (capitalize && char.IsLetter(inputString[1])))
                    )
                   )
                  )
            {
                // Capitalize the 2nd character if the first char is numeric
                if (inputString.Length > 1 &&
                    capitalize &&
                    char.IsDigit(inputString[0]) &&
                    char.IsLower(inputString[1]))
                {
                    //if (mk1) Log.Info("mk1Debug 1: " + inputString);

                    string tmp = inputString.Substring(0, 1);
                    tmp += Char.ToUpper(inputString[1]);
                    if (inputString.Length > 2)
                        tmp += inputString.Substring(2);
                    inputString = tmp;
                    //if (mk1) Log.Info("mk1Debug 2: " + inputString);
                }

                //  Capiutalize first char if requested
                if (capitalize && outputStr.Length == 0)
                    outputStr.Append(Char.ToUpper(inputString[0]));
                else
                    outputStr.Append(inputString[0]);
                if (inputString.Length > 1)
                {
                    if (Char.IsLower(inputString[0]) && char.IsUpper(inputString[1]))
                    {
                        outputStr.Append(" ");
                    }
                    if (char.IsDigit(inputString[0]) && char.IsUpper(inputString[1]))
                    {
                        outputStr.Append(" ");
                    }
                }
                inputString = inputString.Remove(0, 1);
            }
            if (capitalize && inputString.Length > 0)
                return outputStr.ToString() + "-" + inputString;
            else
                return outputStr.ToString() + inputString;
        }

        static List<string> blacklistNames = new List<string>();
#if false
        static private Dictionary<string, string> mergeListDict;
#endif
        static private SortedDictionary<string, string> readableNamesDict;
        static bool filtersConfigInitted = false;

        /// <summary>
        /// Load data from the file:FiltersConfig.cfg
        /// </summary>
        void LoadFiltersConfig()
        {
            if (!filtersConfigInitted)
            {
                filtersConfigInitted = true;
                readableNamesDict = new SortedDictionary<string, string>();
#if false
                mergeListDict = new Dictionary<string, string>();
#endif
                if (System.IO.File.Exists(JC_FILTER_CONFIG_FILE))
                {
                    configFile = ConfigNode.Load(JC_FILTER_CONFIG_FILE);
                    ConfigNode janitorsClosetNode = configFile.GetNode(JC_NODE);
                    if (janitorsClosetNode != null)
                    {
                        //
                        // Load the READABLENAMES node
                        //
                        configFileNode = janitorsClosetNode.GetNode(JC_READABLE_NAMES_NODE);
                        if (configFileNode != null)
                        {
                            for (int i = 0; i < configFileNode.CountValues; i++)
                            {
                                readableNamesDict[configFileNode.values[i].name] = configFileNode.values[i].value;
                            }
                        }


                        //
                        //
                        // Load the MODULE_BLACKLIST node
                        //
                        configFileNode = janitorsClosetNode.GetNode(JC_BLACKLIST_NODE);
                        if (configFileNode != null)
                        {
                            blacklistNames = configFileNode.GetValues("ignore").ToList();
                        }

                        //
                        // Now all the bulkhead profiles
                        //
                        if (partSizeDict == null)
                        {
                            partSizeDict = new SortedDictionary<string, PartSizeDescr>();

                            configFileNode = janitorsClosetNode.GetNode(JC_BULKHEADPROFILES);
                            if (configFileNode != null)
                            {
                                Log.Info(JC_BULKHEADPROFILES + " loaded: " + configFileNode.CountValues);
                                for (int i = 0; i < configFileNode.CountValues; i++)
                                {
                                    PartSizeDescr psd = new PartSizeDescr(configFileNode.values[i].name, configFileNode.values[i].value);

                                    partSizeDict[configFileNode.values[i].name] = psd;
                                }
                            }
                        }

#if false // Disabled for possible future update
                        //
                        // Load the MERGLIST node
                        //
                        configFileNode = janitorsClosetNode.GetNode(JC_MERGELIST_NODE);
                        if (configFileNode != null)
                        {
                            Log.Info("configFileNode is not null 3");

                            for (int i = 0; i < configFileNode.CountValues; i++)
                            {
                                mergeListDict[configFileNode.values[i].name] = configFileNode.values[i].value;
                            }
                        }
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Write the updated data to the file FiltersConfig.cfg
        /// </summary>
        void SaveReadableNames()
        {
            Log.Info("SaveReadableNames");
            ConfigNode configFile = new ConfigNode(JC_NODE);
            ConfigNode configJCnode = new ConfigNode(JC_NODE);
            ConfigNode configFileNode = new ConfigNode(JC_READABLE_NAMES_NODE);
            ConfigNode configBulkheadProfileNode = new ConfigNode(JC_BULKHEADPROFILES);

            if (blacklistNames != null)
            {
                ConfigNode configBLNode = new ConfigNode(JC_BLACKLIST_NODE);
                foreach (var s in blacklistNames)
                {
                    configBLNode.AddValue("ignore", s);
                }
                configJCnode.AddNode(JC_BLACKLIST_NODE, configBLNode);
            }
#if false // disabled for possible future update
            if (mergeListDict != null)
            {
                ConfigNode mergeListNode = new ConfigNode(JC_MERGELIST_NODE);
                foreach (var s in mergeListDict)
                {
                    mergeListNode.AddValue(s.Key, s.Value);
                }

                configJCnode.AddNode(JC_MERGELIST_NODE, mergeListNode);
            }
#endif
            if (readableNamesDict != null)
            {
                foreach (var s in readableNamesDict)
                {
                    configFileNode.AddValue(s.Key, s.Value);
                }
                configJCnode.SetNode(JC_READABLE_NAMES_NODE, configFileNode, true);
            }


            //
            // Now all the bulkhead profiles
            //
            if (partSizeDict != null)
            {
                foreach (var s in partSizeDict)
                {
                    configBulkheadProfileNode.AddValue(s.Key, s.Value.descr);
                }
                configJCnode.SetNode(JC_BULKHEADPROFILES, configBulkheadProfileNode, true);
            }


            configFile.SetNode(JC_NODE, configJCnode, true);
            configFile.Save(JC_FILTER_CONFIG_FILE);
        }
    }
}