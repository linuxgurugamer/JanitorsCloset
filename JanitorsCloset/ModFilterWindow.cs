using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using UnityEngine;
using KSP.UI;
using KSP.UI.Screens;
using KSP.IO;
using ClickThroughFix;


namespace JanitorsCloset
{

    class ModFilterWindow : MonoBehaviour
    {
        const string INVERSE = "-inverse";

        public class PartInfo
        {
            string[] partSizeDescr = new string[] { "Size 0 (0.625m)", "Size 1 (1.25m)", "Size 1.5 (1.875m)", "Size 2 (2.5m)", "Size 3 (3.75m)", "Size 4 (5m)" };


            //-------------------------------------------------------------------------------------------------------------------------------
            public PartInfo(AvailablePart part)
            {
                Log.Info("partSizeDescr.Length: " + partSizeDescr.Length.ToString());
                if (part.partPrefab == null)
                {
                    Log.Info(string.Format("{0} has no partPrefab", part.name));
                    partSize = "No Size";
                }
                else
                // if the attach points have different sizes then it's probably an adapter and we'll place
                // it half way between the smallest and largest attach point of the things it connects
                if (part.partPrefab.attachNodes == null)
                {
                    Log.Info(string.Format("{0} has no attach points", part.name));
                    partSize = "No Size";
                }
                else if (part.partPrefab.attachNodes.Count < 0)
                {
                    Log.Info(string.Format("{0} has negative attach points", part.name));
                    partSize = "No Size";
                }
                else if (part.partPrefab.attachNodes.Count < 1)
                {
                    Log.Info(string.Format("{0} has no attachNodes", part.name));
                    partSize = "No Size";
                }
                else
                {
                    double small = 99999;
                    double large = 0;
                    foreach (var attach in part.partPrefab.attachNodes)
                    {
                        small = Math.Min(small, attach.size);
                        large = Math.Max(large, attach.size);
                    }
                    if (small < 0)
                    {
                        Log.Error(string.Format("{0} has attach point with size < 0", part.name));
                        small = 0;
                    }

                    Log.Info("small: " + small.ToString() + "   large: " + large.ToString());
                    sortSize = (small + large) / 2;
                    int smallSize = (int)small;
                    string smallSizeStr;
                    if (smallSize < partSizeDescr.Length)
                        smallSizeStr = partSizeDescr[smallSize];
                    else
                        smallSizeStr = partSizeDescr[partSizeDescr.Length - 1] + " and larger";
                    Log.Info("smallSizeStr: " + smallSizeStr);
                    if (small == large)
                    {
                        partSize = smallSizeStr;
                        //partSize = Math.Round(small, 2).ToString("0.00");
                    }
                    else
                    {
                        int largeSize = (int)large;
                        string largeSizeStr;
                        if (largeSize < partSizeDescr.Length)
                            largeSizeStr = partSizeDescr[largeSize];
                        else
                            largeSizeStr = partSizeDescr[partSizeDescr.Length - 1];

                        partSize = "Adapter: " + smallSizeStr + " to " + largeSizeStr;
                        //partSize = Math.Round(small, 2).ToString("0.00") + " to " + Math.Round(large, 2).ToString("0.00");
                    }
                    Log.Info(string.Format("{0} is sortSize {1} partSize {2}", part.name, sortSize, partSize));
                }
            }

            //-------------------------------------------------------------------------------------------------------------------------------
            public string partSize;
            public double sortSize;

            public int defaultPos;

        };


        //Rect filterWindowRect = new Rect(200 + 90, Screen.height - 25 - 280, 150, 280);
        Rect modWindowRect;
        Rect filterHelpWindow = new Rect(200 + 90, Screen.height - 25 - 280, 150, 280);

        private const int MOD_WINDOW_ID = 94;
        private const int SIZE_WINDOW_ID = 95;

        private struct ToggleState
        {
            public bool enabledState;
            public bool latched;
            public bool inverse;
        }
        private Dictionary<string, ToggleState> modButtons = new Dictionary<string, ToggleState>();
        private Dictionary<string, ToggleState> sizeButtons = new Dictionary<string, ToggleState>();
        private Dictionary<string, ToggleState> resourceButtons = new Dictionary<string, ToggleState>();
        private Dictionary<string, ToggleState> partModuleButtons = new Dictionary<string, ToggleState>();

        private Dictionary<AvailablePart, PartInfo> partInfos = new Dictionary<AvailablePart, PartInfo>();

        private Dictionary<string, HashSet<AvailablePart>> modHash = new Dictionary<string, HashSet<AvailablePart>>();
        private Dictionary<string, HashSet<AvailablePart>> sizeHash = new Dictionary<string, HashSet<AvailablePart>>();
        private Dictionary<string, HashSet<AvailablePart>> resourceHash = new Dictionary<string, HashSet<AvailablePart>>();
        private Dictionary<string, HashSet<AvailablePart>> partModuleHash = new Dictionary<string, HashSet<AvailablePart>>();

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
            Log.Info("ModFilterWindow.Show()");
            this.enabled = !enabled;
        }
        public void Hide()
        {
            Log.Info("ModFilterWindow.Hide()");
            this.enabled = false;
        }

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
                partInfos.Add(part, partInfo);

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
                        foreach (var res in module.resHandler.outputResources)
                        {

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
                                            resourceHash[propellant.name].Add(part);
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
                                            resourceHash[propellant.name].Add(part);
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
                        modHash[partModName].Add(part);
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
            AvailablePart kerbalEVA = null;
            AvailablePart flag = null;
            foreach (var part in loadedParts)
            {
                if (part.name.Contains("kerbalEVA"))
                    kerbalEVA = part;
                else if (part.name == "flag")
                    flag = part;
            }

            // still need to prevent errors with null refs when looking up these parts though
            if (kerbalEVA != null)
            {
                loadedParts.Remove(kerbalEVA);
                partInfos.Add(kerbalEVA, new PartInfo(kerbalEVA));
            }
            if (flag != null)
            {
                loadedParts.Remove(flag);
                partInfos.Add(flag, new PartInfo(flag));
            }
            return loadedParts;
        }

        bool PartInResourceFilteredButtons(string filter, AvailablePart part, Dictionary<string, ToggleState> buttons, Dictionary<string, HashSet<AvailablePart>> filterHash)
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

        bool PartInFilteredButtons(string filter, AvailablePart part, Dictionary<string, ToggleState> buttons, Dictionary<string, HashSet<AvailablePart>> filterHash)
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

        bool PartInResourseExcludeButtons(string filter, AvailablePart part, Dictionary<string, ToggleState> buttons, Dictionary<string, HashSet<AvailablePart>> filterHash)
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

        bool PartInUnpurchasedButtons(string filter, AvailablePart part, Dictionary<string, ToggleState> buttons, Dictionary<string, HashSet<AvailablePart>> filterHash)
        {
            if (!hideUnpurchased)
                return true;
            return PartIsPurchased(part);
        }


        bool PartInModuleButtons(string filter, AvailablePart part, Dictionary<string, ToggleState> buttons, Dictionary<string, HashSet<AvailablePart>> filterHash)
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

        bool PartInModuleExcludeButtons(string filter, AvailablePart part, Dictionary<string, ToggleState> buttons, Dictionary<string, HashSet<AvailablePart>> filterHash)
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

        Color origBackgroundColor;
        GUIStyle styleButtonLeftAligned;


        void InitData()
        {
            if (configs == null)
                configs = GameDatabase.Instance.GetConfigs("PART");
            List<AvailablePart> loadedParts = GetPartsList();
            InitialPartsScan(loadedParts);
            LoadValuesFromConfig(selectedFilterList);
            LoadReadableNames();
        }
        public void Start()
        {
            CONFIG_BASE_FOLDER = KSPUtil.ApplicationRootPath + "GameData/";
            JC_BASE_FOLDER = CONFIG_BASE_FOLDER + "JanitorsCloset/";
            JC_NODE = "JANITORSCLOSET";
            JC_CFG_FILE = JC_BASE_FOLDER + "PluginData/JCModfilter";
            JC_FILTER_CONFIG_FILE = JC_BASE_FOLDER + "PluginData/FiltersConfig.cfg";
            JC_READABLE_NAMES_NODE = "READABLENAMES";
            JC_BLACKLIST_NODE = "MODULE_BLACKLIST";
#if false
            JC_MERGELIST_NODE = "MERGELIST";
#endif
            InitData();
            // DefineFilters();
            modWindowRect = FilterWindowRect("Mods", Math.Max(modButtons.Count, Math.Max(sizeButtons.Count, Math.Max(resourceButtons.Count, partModuleButtons.Count))));

            modwindowRectID = JanitorsCloset.getNextID();
            modFilterHelpWindowID = JanitorsCloset.getNextID();
            enabled = false;

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

        bool showFilterHelpWindow = false;

        public void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
                GUI.skin = HighLogic.Skin;
            if (!enabled)
                return;
            if (styleButton == null)
                InitStyles();

            _windowTitle = string.Format("Mod Filter");
            var tstyle = new GUIStyle(GUI.skin.window);

            modWindowRect = ClickThruBlocker.GUILayoutWindow(modwindowRectID, modWindowRect, FilterChildWindowHandler, _windowTitle, tstyle);
            if (showFilterHelpWindow)
                filterHelpWindow = ClickThruBlocker.GUILayoutWindow(modFilterHelpWindowID, filterHelpWindow, FilterHelpWindow, _windowTitle, tstyle);
            if (helpPopup != null)
                helpPopup.draw();
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
            // Special cases for adapters so they are in height order, from smaller to larger
            if (left.Contains("Adapter") && right.Contains("larger"))
                return 1;
            if (right.Contains("Adapter") && left.Contains("larger"))
                return -1;
            if (left.Contains("larger"))
                return 1;
            if (right.Contains("larger"))
                return -1;
            if (left.Contains("Adapter") && !right.Contains("Adapter"))
                return 1;
            if (right.Contains("Adapter") && !left.Contains("Adapter"))
                return -1;

            return String.Compare(left, right);
        }

        void resetAll()
        {
            var names = new List<string>(modButtons.Keys);
            Dictionary<string, ToggleState> states = modButtons;
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
            showFilterHelpWindow = false;
        }

        Vector2 scrollPosition;
        string[] filterType = new string[] { "Mod Name", "Module Size", "Resources", "Modules" };
        int selFilter = 0;

        private void FilterChildWindowHandler(int id)
        {
            Dictionary<string, ToggleState> states = modButtons;
            if (GUI.Button(new Rect(12, 2, 30, 20), "?", styleButtonSettings))
            {
                showFilterHelpWindow = !showFilterHelpWindow;
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
                Log.Info("FilterChildWindowHandler 1");
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
            var keys = new List<string>(states.Keys);

            keys.Sort(CompareEntries);

            // This will hide the horizontal scrollbar
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            foreach (string name in keys)
            {
                ToggleState state = states[name];
                //string truncatedName = (name.Length > 35) ? name.Remove(34) : name;
                bool before = state.enabledState;
                bool beforeInverse = state.inverse;

                string readableName = GetReadableName(name);
                if (readableName != "")
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
                        Log.Info("FilterChildWindowHandler 3");
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
                        Log.Info("readableName: " + readableName);
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

                        Log.Info("FilterChildWindowHandler 4");

                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);
                        Log.Info("FilterChildWindowHandler 5");

                    }

                    if (before != state.enabledState || beforeInverse != state.inverse)
                    {
                        states[name] = state;
                        EditorPartList.Instance.Refresh();
                        SaveConfig(selectedFilterList);
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
        private static String JC_BLACKLIST_NODE;
#if false
        private static String JC_MERGELIST_NODE;
#endif
        private static String JC_CFG_FILE;
        private static String JC_FILTER_CONFIG_FILE;

        private static ConfigNode configFile = null;
        private static ConfigNode configFileNode = null;
        private static ConfigNode configSectionNode = null;


        //-------------------------------------------------------------------------------------------------------------------------------------------
        void SaveSelectType(string prefix, Dictionary<string, ToggleState> buttonList, string NodeName, ConfigNode fileNode)
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
            //PluginConfiguration config = PluginConfiguration.CreateForType<ModFilterWindow>();

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

        void LoadValuesFromSectionNode(string NodeName, Dictionary<string, ToggleState> buttonList, ConfigNode configFileNode, string prefix)
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
        void LoadConfigSection(string NodeName, ConfigNode cfgNode, string prefix, Dictionary<string, ToggleState> buttons)
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
        string GetReadableName(string name)
        {
            string s;
            try
            {
                s = readableNamesDict[name];
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

                if (s != "")
                {
                    readableNamesDict.Add(name, s);
                    Log.Info("Adding " + name + " to readableNamesDict");
                    SaveReadableNames();
                }
                //else
                //{
                //    show = false;
                //}
                return s;
            }
        }


        string[] deleteLeading = new string[] { "Module", "CModule" };

        /// <summary>
        /// Given the input string, create a readable name from it using the specified rules
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        string BestGuessReadableName(string inputString)
        {
            string outputString = "";
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

            if (inputString.Contains(" "))
            {
                return inputString;
            }

            foreach (string e in deleteLeading)
            {
                if (inputString.Length >= e.Length && inputString.Substring(0, e.Length) == e)
                {
                    inputString = inputString.Remove(0, e.Length);
                }
            }
            while (inputString.Length > 1)
            {
                outputString += inputString[0];
                if (Char.IsLower(inputString[0]) && char.IsUpper(inputString[1]))
                {
                    outputString += " ";
                }
                if (char.IsDigit(inputString[0]) && char.IsUpper(inputString[1]))
                {
                    outputString += " ";
                }
                inputString = inputString.Remove(0, 1);
            }
            return outputString + inputString;
        }

        static List<string> blacklistNames = new List<string>();
#if false
        static private Dictionary<string, string> mergeListDict;
#endif
        static private Dictionary<string, string> readableNamesDict;
        static bool readableNamesInitted = false;

        /// <summary>
        /// Load data from the file:FiltersConfig.cfg
        /// </summary>
        void LoadReadableNames()
        {
            if (!readableNamesInitted)
            {
                readableNamesInitted = true;
                readableNamesDict = new Dictionary<string, string>();
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
                        // Load the MODULE_BLACKLIST node
                        //
                        configFileNode = janitorsClosetNode.GetNode(JC_BLACKLIST_NODE);
                        if (configFileNode != null)
                        {
                            blacklistNames = configFileNode.GetValues("ignore").ToList();
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
            configFile.SetNode(JC_NODE, configJCnode, true);
            configFile.Save(JC_FILTER_CONFIG_FILE);
        }
    }
}