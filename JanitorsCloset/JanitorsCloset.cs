using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using KSP.UI.Screens;
using ClickThroughFix;
using System.Collections;

using static JanitorsCloset.JanitorsClosetLoader;

namespace JanitorsCloset
{
    #region defines
    public enum blackListType
    {
        VAB,
        SPH,
        ALL
    }

    public class blackListPart
    {
        public string modName;
        public string title;
        public blackListType where;
        public bool permapruned;
    }

    #endregion

    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    partial class JanitorsCloset : BaseRaycaster
    {
        private static JanitorsCloset instance;

        static int lastUsedID = 6050;

        public static int getNextID()
        {
            lastUsedID++;
            Log.Info("lastUsedID: " + lastUsedID.ToString());
            return lastUsedID;
        }

        static public Dictionary<string, blackListPart> blackList;

        public readonly static string MOD = Assembly.GetAssembly(typeof(JanitorsCloset)).GetName().Name;
        public static EditorPartListFilter<AvailablePart> searchFilterParts;

        public static Dictionary<string, string> blacklistIcons;

        bool _showMenu = false;
        Rect _menuRect = new Rect();
        const float _menuWidth = 200.0f;
        const float _menuHeight = 200.0f;
        //const float _menuHeight = 165.0f;
        const int _toolbarHeight = 42;
        //37

        public void ShowMenu()
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR &&
                (helpPopup == null ||
                (helpPopup != null && !helpPopup.showMenu)))
            { 
                Vector2 position = UIScale.GuiMousePosition();
                Vector2 screen = UIScale.GuiScreenSize();
                int toolbarHeight = UIScale.Scale(_toolbarHeight);
                _menuRect = new Rect()
                {
                    xMin = position.x - _menuWidth / 2,
                    xMax = position.x + _menuWidth / 2,
                    yMin = screen.y - toolbarHeight - _menuHeight,
                    yMax = screen.y - toolbarHeight
                };

                _showMenu = true;
                menuContentID = JanitorsCloset.getNextID();
            }
        }

        public void HideMenu()
        {
            Log.Info("HideMenu");
            // _menuRect = new Rect();
            _showMenu = false;
            showToolbar = ShowMenuState.hidden;
        }


        internal bool FindPart(AvailablePart part)
        {
            if (part == null)
            {
                return false;
            }
            // Log.Info("Entered FindPart: " + part.name);
            string _partinfo = part.name;
            if (blackList.ContainsKey(part.name))
            {
                blackListPart blp;
                blackList.TryGetValue(part.name, out blp);
                if ((blp.where == blackListType.ALL) ||
                    (blp.where == blackListType.SPH && EditorDriver.editorFacility == EditorFacility.SPH) ||
                    (blp.where == blackListType.VAB && EditorDriver.editorFacility == EditorFacility.VAB)
                    )
                    return false;
            }

            return true;
        }

        public static JanitorsCloset Instance
        {
            get
            {
                return instance;
            }
        }
        PermaPruneWindow permaPruneWindow = null;
        ModFilterWindow modFilterWindow = null;
        ShowBlocked showBlocked = null;
        ShowRenamed showRenamed = null;
        public bool guiInitialized = false;

        void InitializeGUI()
        {
            permaPruneWindow = this.gameObject.AddComponent<PermaPruneWindow>();
            modFilterWindow = this.gameObject.AddComponent<ModFilterWindow>();
            showBlocked = this.gameObject.AddComponent<ShowBlocked>();
            showRenamed = this.gameObject.AddComponent<ShowRenamed>();
        }

        void OnSceneLoadedGUIReady(Scene scene, LoadSceneMode mode)
        {
            Log.Info("OnSceneLoadedGUIReady");
            if (HighLogic.LoadedSceneIsEditor)
            {
                StartCoroutine(sceneReady());
            }
            else
            {
                EditorIconEvents.OnEditorPartIconHover.Remove(IconHover);
                EditorIconEvents.OnEditorPartIconClicked.Remove(IconClicked);
                if (modFilterWindow != null)
                    modFilterWindow.Hide();
                _showMenu = false;
            }
        }

        public IEnumerator sceneReady()
        {
            while (EditorPartList.Instance == null)
                yield return new WaitForSeconds(0.2f);

            Log.Info("sceneReady");
            blackList = FileOperations.Instance.loadBlackListData();

            EditorIconEvents.OnEditorPartIconHover.Add(IconHover);
            EditorIconEvents.OnEditorPartIconClicked.Add(IconClicked);

            Func<AvailablePart, bool> _criteria = (_aPart) => FindPart(_aPart);
            searchFilterParts = new EditorPartListFilter<AvailablePart>(MOD, _criteria);

            EditorPartList.Instance.ExcludeFilters.AddFilter(searchFilterParts);

            InitializeGUI();
            //yield return 0; 
            guiInitialized = true;
        }

        new void Start()
        {
            // Log.setTitle("Janitor's Closet");
            Log.Info("JanitorsCloset.Start");
            blacklistIcons = JanitorsCloset.Instance.loadBlacklistData();

            lastUsedID = this.GetInstanceID();
            StartToolbar();
        }

        new void OnDestroy()
        {
            Log.Info("JanitorsCloset.OnDestroy");
            FileOperations.Instance.saveBlackListData(blackList);

            OnDestroyToolbar();
        }

        EditorPartIcon _icon;
        private void IconClicked(EditorPartIcon icon, EditorIconEvents.EditorIconClickEvent evt)
        {
            Log.Info("IconClicked");
            if (!ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.primary) && !ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.secondary))
                return;

            Log.Info("Icon was clicked for " + icon.partInfo.name + " (" + icon.partInfo.title + "), icon.name: " + icon.name);

            _icon = icon;
            ShowPruneMenu();

            evt.Veto(); // prevent part from being spawned
        }

        private void IconHover(EditorPartIcon icon, bool hover)
        {
            if (icon == null || icon.partInfo == null)
                return;
            Log.Info("IconHover,  ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.primary): " + ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.primary).ToString());
            if (HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().showMod ||
                ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.primary) || ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.secondary))
            {
                string mod = ModFilterWindow.FindPartMod(icon.partInfo);
             
                showPartModTooltip = hover;
                partModTooltip = hover ? mod : "";
            }
            else
            {
                showPartModTooltip = false;
                partModTooltip = "";
            }
        }


        protected override void Awake()
        {
            instance = this;
            DontDestroyOnLoad(this);

            //getPartData();

            _windowFunction = PruneMenuContent;
            AwakeToolbar();

            _mouseController = HighLogic.fetch.GetComponent<Mouse>();
        }


        enum ShowMenuState { hidden, starting, visible, hiding };
        ShowMenuState _showPruneMenu = ShowMenuState.hidden;
        Rect _pruneMenuRect = new Rect();
        const float _pruneMenuWidth = 100.0f;
        const float _pruneMenuHeight = 73.0f;



        // these two save a bit on garbage created in OnGUI
        //    private readonly GUILayoutOption[] _emptyOptions = new GUILayoutOption[0];
        private GUI.WindowFunction _windowFunction;


        // enable/disable this to prevent the "No Target" from popping up by double-clicking on the window 
        private Mouse _mouseController;
        private bool _mouseControllerDisabledByRaycast;

        #region PruneParts
        public void ShowPruneMenu()
        {
            InputLockManager.SetControlLock(ControlTypes.EDITOR_ICON_PICK | ControlTypes.EDITOR_ICON_HOVER, "Pruner");

            Vector2 position = UIScale.GuiMousePosition();
            Vector2 screen = UIScale.GuiScreenSize();

            if (position.y + _pruneMenuHeight > screen.y)
                position.y = screen.y - _pruneMenuHeight;

            position.y -= 10;
            _pruneMenuRect = new Rect()
            {
                xMin = position.x - _pruneMenuWidth / 2,
                xMax = position.x + _pruneMenuWidth / 2,
                yMin = position.y,
                yMax = position.y + _pruneMenuHeight
            };
            _showPruneMenu = ShowMenuState.starting;
            pruneMenuID = JanitorsCloset.getNextID();
        }

        public void HidePruneMenu()
        {
            _showPruneMenu = ShowMenuState.hidden;
            _pruneMenuRect = new Rect();
            InputLockManager.RemoveControlLock("Pruner");
        }

        #endregion

        int pruneMenuID;
        int menuContentID;


        //Unity GUI loop
        void OnGUI()
        {
            if (HighLogic.CurrentGame == null)
                return;
            UIScale.BeginGUI();
            string tooltipText = null;
            if (drawTooltip && tooltip != null && tooltip.Trim().Length > 0)
                tooltipText = tooltip;
            else if (showPartModTooltip && partModTooltip != null && partModTooltip.Trim().Length > 0)
                tooltipText = partModTooltip;

            if (tooltipText != null && HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().buttonTooltip)
            {
                // Draw on Repaint only; GUI.Window would capture mouse events and cause
                // part tooltips and editor UI buttons to flicker.
                if (Event.current.type == EventType.Repaint)
                {
                    SetupTooltip(tooltipText);
                    DrawTooltip(tooltipText);
                }
            }
            //Log.Info("Scene: " + HighLogic.LoadedScene.ToString());
            if ((_showPruneMenu == ShowMenuState.starting) || (_showPruneMenu == ShowMenuState.visible && _pruneMenuRect.Contains(UIScale.GuiMousePosition())))
                _pruneMenuRect = UIScale.ClampToGuiScreen(ClickThruBlocker.GUILayoutWindow(pruneMenuID, _pruneMenuRect, _windowFunction, "Blocker Menu"));
            else
                if (_showPruneMenu != ShowMenuState.hidden)
                HidePruneMenu();

            OnGUIToolbar();

            if (HighLogic.LoadedSceneIsEditor && (_showMenu || _menuRect.Contains(UIScale.GuiMousePosition()) || (Time.fixedTime - lastTimeShown < 0.5f)))
            {
                if (_menuRect.x > 0 && _menuRect.y > 0)
                    _menuRect = ClickThruBlocker.GUILayoutWindow(menuContentID, _menuRect, MenuContent, "Janitor's Closet");
            }
            else
                _menuRect = new Rect();

            UIScale.EndGUI();
        }

        void addToBlackList(string p, string title, blackListType type)
        {
            blackListPart blp = new blackListPart();

            blp.modName = p;
            blp.where = type;
            blp.title = title;
            blp.permapruned = false;

            Log.Info("addToBlackList: " + p);
            // If it's already there, then delete it and chagne the "where" to ALL

            blackListPart p1;
            if (blackList.TryGetValue(p, out p1))
            {
                if (!p1.permapruned)
                {


                    blackList.Remove(p);
                    blp.where = blackListType.ALL;
                }
            }


            blackList.Add(p, blp);
            EditorPartList.Instance.Refresh();
            HidePruneMenu();
            FileOperations.Instance.saveBlackListData(blackList);
        }
#if false
        public void clearBlackList()
        {
            blackList.Clear();
            EditorPartList.Instance.Refresh();
            HidePruneMenu();
            FileOperations.Instance.saveBlackListData(blackList);
        }
#endif

        void PruneMenuContent(int WindowID)
        {
            _showPruneMenu = ShowMenuState.visible;
            if (GUILayout.Button("Block all"))
            {
                if (!blackList.ContainsKey(_icon.partInfo.name))
                {
                    addToBlackList(_icon.partInfo.name, _icon.partInfo.title, blackListType.ALL);
                }
            }
            if (EditorDriver.editorFacility == EditorFacility.VAB)
            {
                if (GUILayout.Button("Block VAB"))
                {
                    Log.Info("Block VAB: " + _icon.partInfo.name);
                    addToBlackList(_icon.partInfo.name, _icon.partInfo.title, blackListType.VAB);
                }
            }
            if (EditorDriver.editorFacility == EditorFacility.SPH)
            {
                if (GUILayout.Button("Block SPH"))
                {
                    addToBlackList(_icon.partInfo.name, _icon.partInfo.title, blackListType.SPH);
                }
            }
        }



        float lastTimeShown = 0;

        void MenuContent(int WindowID)
        {
            if (_showMenu || _menuRect.Contains(UIScale.GuiMousePosition()))
            {
                Log.Info("lastTimeShown 1");
                lastTimeShown = Time.fixedTime;
            }
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            string AssetsLabel;
            AssetsLabel = "Parts count: " + AssetsDatabase.Instance.PartsCount.ToString() + "\n" +
              "Models: " + AssetsDatabase.Instance.models.AssetsCount + " (" + Utilities.FormatFileSize(AssetsDatabase.Instance.models.AssetsSize) + ")\n" +
              "Textures: " + AssetsDatabase.Instance.textures.AssetsCount + " (" + Utilities.FormatFileSize(AssetsDatabase.Instance.textures.AssetsSize) + ")";
            GUILayout.Label(AssetsLabel);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Show Blocked"))
            {
                // Dialog to show list of blocked parts, with buttons to temp/perm unblock a part
                showBlocked.Show();
            }
#if false
            if (GUILayout.Button("Unblock"))
            {
                clearBlackList();
            }
#endif
            if (GUILayout.Button("PermaPrune"))
            {
                permaPruneWindow.Show();
            }
            GUIStyle styleButton = new GUIStyle(GUI.skin.button);
            string modFilter = "Mod Filter";
#if DEBUG
            Log.Info("modFilterWindow.ModFilteredCount: " + modFilterWindow.ModFilteredCount +
                "   modFilterWindow.ModInverseCount: " + modFilterWindow.ModInverseCount +
                "   modFilterWindow.SizeFilteredCount: " + modFilterWindow.SizeFilteredCount +
                "   modFilterWindow.ResourceFilteredCount: " + modFilterWindow.ResourceFilteredCount +
                "   modFilterWindow.ModuleFilteredCount: " + modFilterWindow.ModuleFilteredCount +
                "   modFilterWindow.ModuleInverseCount: " + modFilterWindow.ModuleInverseCount);
#endif
            if (modFilterWindow.ModFilteredCount > 0 ||
                modFilterWindow.SizeFilteredCount > 0 ||
                modFilterWindow.ResourceFilteredCount > 0 ||
                modFilterWindow.ModInverseCount > 0 ||
                modFilterWindow.ModuleFilteredCount > 0)
            {
                styleButton.normal.textColor = Color.yellow;
                styleButton.hover.textColor = Color.yellow;
                modFilter = "Mod Filter (" + modFilterWindow.ModFilteredCount + ", " +
                    modFilterWindow.ModInverseCount + ", " +
                    modFilterWindow.SizeFilteredCount + ", " +
                    modFilterWindow.ResourceFilteredCount + ", " +
                    modFilterWindow.ModuleFilteredCount + ", " +
                    modFilterWindow.ModuleInverseCount + ")";
            }
            else
            {
                styleButton.normal.textColor = GUI.skin.button.normal.textColor;
                styleButton.hover.textColor = GUI.skin.button.hover.textColor;
            }



            if (GUILayout.Button(modFilter, styleButton))
            {
                modFilterWindow.Show();
            }

            if (GUILayout.Button("Export/Import"))
            {
                ImportExport.Instance.SetVisible(true);
            }

            GUILayout.EndVertical();
        }



        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            var screenPos = UIScale.GuiMousePosition();

            if (!_pruneMenuRect.Contains(screenPos))
            {
                if (_mouseControllerDisabledByRaycast)
                {
                    _mouseController.enabled = true;
                    _mouseControllerDisabledByRaycast = false;
                }
                return;
            }

            if (!_mouseControllerDisabledByRaycast)
            {
                _mouseController.enabled = false;
                _mouseControllerDisabledByRaycast = true;
            }
            Mouse.Left.ClearMouseState();
            Mouse.Middle.ClearMouseState();
            Mouse.Right.ClearMouseState();

            resultAppendList.Add(new RaycastResult
            {
                depth = 0,
                distance = 0f,
                gameObject = gameObject,
                module = this,
                sortingLayer = MainCanvasUtil.MainCanvas.sortingLayerID,
                screenPosition = screenPos
            });
        }

        public override Camera eventCamera
        {
            get { return null; }
        }

        public override int sortOrderPriority
        {
            get { return MainCanvasUtil.MainCanvas.sortingOrder - 1; }
        }

    }
}