using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using UnityEngine;
using KSP.UI;
using KSP.UI.Screens;

namespace JanitorsCloset
{

    class ModFilterWindow : MonoBehaviour
    {

        public class PartInfo
        {
            string[] partSizeDescr = new string[] { "Size 0 (0.625m)", "Size 1 (1.25m)", "Size 2 (2.5m)", "Size 3 (3.75m)", "Size 4 (5m)" };
            //-------------------------------------------------------------------------------------------------------------------------------
            public PartInfo(AvailablePart part)
            {
                Log.Info("partSizeDescr.Length: " + partSizeDescr.Length.ToString());
                if (part.partPrefab == null)
                {
                    Log.Info(string.Format("{0} has no partPrefab", part.name));
                    partSize = "No Size";
                } else
                // if the attach points have different sizes then it's probably an adapter and we'll place
                // it half way between the smallest and largest attach point of the things it connects
                if (part.partPrefab.attachNodes == null)
                {
                    Log.Info( string.Format("{0} has no attach points", part.name));
                    partSize = "No Size";
                }
                else if (part.partPrefab.attachNodes.Count < 0)
                {
                    Log.Info( string.Format("{0} has negative attach points", part.name));
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


        Rect filterWindowRect = new Rect(200 + 90, Screen.height - 25 - 280, 150, 280);
        Rect modWindowRect;

        private const int MOD_WINDOW_ID = 94;
        private const int SIZE_WINDOW_ID = 95;

        private struct ToggleState
        {
            public bool enabled;
            public bool latched;
        }
        private Dictionary<string, ToggleState> modButtons = new Dictionary<string, ToggleState>();
        private Dictionary<string, ToggleState> sizeButtons = new Dictionary<string, ToggleState>();
        private Dictionary<AvailablePart, PartInfo> partInfos = new Dictionary<AvailablePart, PartInfo>();
        private Dictionary<string, HashSet<AvailablePart>> modHash = new Dictionary<string, HashSet<AvailablePart>>();
        private Dictionary<string, HashSet<AvailablePart>> sizeHash = new Dictionary<string, HashSet<AvailablePart>>();
        private UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs("PART");


        //-------------------------------------------------------------------------------------------------------------------------------------------
        string FindPartMod(AvailablePart part)
        {
            Debug.Log("ModFilterWindow.FindPartMod");
            UrlDir.UrlConfig config = Array.Find<UrlDir.UrlConfig>(configs, (c => (part.name == c.name.Replace('_', '.').Replace(' ','.'))));
            if (config == null)
                return "";
            var id = new UrlDir.UrlIdentifier(config.url);
            return id[0];
        }

        public void Show()
        {
           Debug.Log("ModFilterWindow.Show()");
            this.enabled = !enabled;
        }

        string UsefulModuleName(string longName)
        {
            Debug.Log("ModFilterWindow.UsefulModuleName");

            if (longName.StartsWith("Module"))
                return longName.Substring(6);
            if (longName.StartsWith("FXModule"))
                return "FX" + longName.Substring(8);
            return longName;
        }


        void InitialPartsScan(List<AvailablePart> loadedParts)
        {
            Debug.Log("ModFilterWindow.InitialPartsScan");

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
                if (!sizeButtons.ContainsKey(partInfo.partSize))
                {
                    Log.Info( string.Format("define new size filter key {0}", partInfo.partSize));
                    sizeButtons.Add(partInfo.partSize, new ToggleState() { enabled = true, latched = false });
                    sizeHash.Add(partInfo.partSize, new HashSet<AvailablePart>());
                }
                Log.Info( string.Format("add {0} to sizeHash for {1}", part.name, partInfo.partSize));
                sizeHash[partInfo.partSize].Add(part);


                // the part's base directory name is used to filter entire mods in and out
                string partModName = FindPartMod(part);
                if (!modButtons.ContainsKey(partModName))
                {
                    Log.Info(string.Format("define new mod filter key {0}", partModName));
                    modButtons.Add(partModName, new ToggleState() { enabled = true, latched = false });
                    modHash.Add(partModName, new HashSet<AvailablePart>());
                }
                Log.Info(string.Format("add {0} to modHash for {1}", part.name, partModName));
                modHash[partModName].Add(part);

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

        Rect FilterWindowRect(string name, int buttons)
        {
            var lineHeight = (int)HighLogic.Skin.GetStyle("Toggle").CalcHeight(new GUIContent("XXQ"), 20);

            if (buttons > 20)
                buttons = 20;
            if (buttons < 10)
                buttons = 10;

            float height = lineHeight * buttons;

            Rect answer = new Rect(300, 200, 350, height);
           
            //Log(LogLevel.INFO, string.Format("defining window {4} at ({0},{1},{2},{3})", answer.xMin, answer.yMin, answer.width, answer.height, name));
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
                if (part.name == "kerbalEVA")
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

        bool PartInFilteredButtons(AvailablePart part, Dictionary<string, ToggleState> buttons, Dictionary<string, HashSet<AvailablePart>> filterHash)
        {
            foreach (string name in buttons.Keys)
            {
                if (!buttons[name].enabled)
                    continue;
                if (filterHash[name].Contains(part))
                    return true;
            }
            return false;
        }

        void DefineFilters()
        {
            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Mod Filter", (part => PartInFilteredButtons(part, modButtons, modHash))));
            EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Size Filter", (part => PartInFilteredButtons(part, sizeButtons, sizeHash))));
            //EditorPartList.Instance.ExcludeFilters.AddFilter(new EditorPartListFilter<AvailablePart>("Modules Filter", (part => !PartInFilteredButtons(part, moduleButtons, moduleHash))));
            EditorPartList.Instance.Refresh();
        }

        static GUIStyle styleButton = null;
        static GUIStyle styleButtonSettings;
        public void Start()
        {
            List<AvailablePart> loadedParts = GetPartsList();
            InitialPartsScan(loadedParts);
            DefineFilters();
            modWindowRect = FilterWindowRect("Mods", Math.Max(modButtons.Count, sizeButtons.Count));

            modwindowRectID = JanitorsCloset.getNextID();
            enabled = false;
        }
        void InitStyles()
        { 
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
        }
       


        string _windowTitle = string.Empty;
        int modwindowRectID;

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

            modWindowRect = GUILayout.Window(modwindowRectID, modWindowRect, FilterChildWindowHandler, _windowTitle, tstyle);
        }

        int CompareEntries(string left, string right)
        {
            if (left == right)
                return 0;
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
        Vector2 scrollPosition;
        string[] filterType = new string[] { "Mod Name", "Module Size" };
        int selFilter = 0;

        private void FilterChildWindowHandler(int id)
        {
            Dictionary<string, ToggleState> states = modButtons;

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
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button( "Show All"))
            {
                var names = new List<string>(states.Keys);
                foreach (string name in names)
                {
                    ToggleState state = states[name];
                    state.enabled = true;
                    states[name] = state;
                }
               // SaveConfig();
            }
            if (GUILayout.Button( "Hide All"))
            {
                var names = new List<string>(states.Keys);
                foreach (string name in names)
                {
                    ToggleState state = states[name];
                    state.enabled = false;
                    states[name] = state;
                }
              //  SaveConfig();
            }
            if (GUILayout.Button("Reset All"))
            {
                var names = new List<string>(modButtons.Keys);
                states = modButtons;
                foreach (string name in names)
                {
                    ToggleState state = states[name];
                    state.enabled = true;
                    states[name] = state;
                }
                names = new List<string>(sizeButtons.Keys);
                states = sizeButtons;
                foreach (string name in names)
                {
                    ToggleState state = states[name];
                    state.enabled = true;
                    states[name] = state;
                }
                //  SaveConfig();
            }
            GUILayout.EndHorizontal();
            
            var keys = new List<string>(states.Keys);

            keys.Sort(CompareEntries);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (string name in keys)
            {
                ToggleState state = states[name];
                //string truncatedName = (name.Length > 35) ? name.Remove(34) : name;
                bool before = state.enabled;
                GUILayout.BeginHorizontal();
                state.enabled = GUILayout.Toggle(state.enabled, name);
                GUILayout.EndHorizontal();
               // if (before != state.enabled)
               //     SaveConfig();
               

                if (state.enabled && !state.latched)
                {
                    state.latched = true;
                    states[name] = state;
                    EditorPartList.Instance.Refresh();
                }
                else if (!state.enabled && state.latched)
                {
                    state.latched = false;
                    states[name] = state;
                    EditorPartList.Instance.Refresh();
                }
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

    }
}