using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;

namespace JanitorsCloset
{
    public enum blackListType
    {
        VAB,
        SPH,
        ALL
    }

    public class blackListPart
    {
        public string modName;
        public blackListType where;
        public bool permapruned;
    }


    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class JanitorsCloset :  BaseRaycaster
    {
        private static JanitorsCloset instance;
        private ApplicationLauncherButton button = null;
        const string texPathDefault = "JanitorsCloset/Textures/AppLauncherIcon";

        static public Dictionary<string, blackListPart> blackList;

        public readonly static string MOD = Assembly.GetAssembly(typeof(JanitorsCloset)).GetName().Name;
        public static EditorPartListFilter<AvailablePart> searchFilterParts;

        private void OnGuiAppLauncherReady()
        {
            if (this.button == null)
            {
                try
                {
                    this.button = ApplicationLauncher.Instance.AddModApplication(
                        () => {
                            JanitorsCloset.Instance.Show();
                        },  //RUIToggleButton.onTrue
                        () => {
                            JanitorsCloset.Instance.Hide();
                        },  //RUIToggleButton.onFalse
                        () => {
                            JanitorsCloset.Instance.ShowMenu();
                        }, //RUIToggleButton.OnHover
                        () => {
                            JanitorsCloset.Instance.HideMenu();
                        }, //RUIToggleButton.onHoverOut
                        null, //RUIToggleButton.onEnable
                        null, //RUIToggleButton.onDisable
                        ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, //visibleInScenes
                        GameDatabase.Instance.GetTexture(texPathDefault, false) //texture
                    );
                    Log.Info("Added ApplicationLauncher button");
                }
                catch (Exception ex)
                {
                    Log.Error("Error adding ApplicationLauncher button: " + ex.Message);
                }
            }

        }

        //bool Visible = false;
        //show the addon's GUI
        public void Show()
        {

            //this.Visible = true;
            Log.Info("Show()");
            //if (!_settingsWindow.enabled) {
            //	_settingsWindow.Show (cfg, _configFilePath, pluginVersion);
            //}
        }

        //hide the addon's GUI
        public void Hide()
        {

            //this.Visible = false;
            //Log.Debug ("Hide()");
            //if (_settingsWindow.enabled) {
            //	_settingsWindow.enabled = false;
            //}
        }

        bool _showMenu = false;
        Rect _menuRect = new Rect();
        const float _menuWidth = 100.0f;
        const float _menuHeight = 125.0f;
        const int _toolbarHeight = 42;
        //37

        public void ShowMenu()
        {
            Vector3 position = Input.mousePosition;
            int toolbarHeight = (int)(_toolbarHeight * GameSettings.UI_SCALE);
            _menuRect = new Rect()
            {
                xMin = position.x - _menuWidth / 2,
                xMax = position.x + _menuWidth / 2,
                yMin = Screen.height - toolbarHeight - _menuHeight,
                yMax = Screen.height - toolbarHeight
            };
            
            _showMenu = true;
        }

        public void HideMenu()
        {
            Log.Info("HideMenu");
            _showMenu = false;
        }


        internal static bool FindPart(AvailablePart part)
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
                if ( (blp.where == blackListType.ALL) ||
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
        ShowBlocked showBlocked = null;
        ShowRenamed showRenamed = null;

        void InitializeGUI()
        {
            permaPruneWindow = this.gameObject.AddComponent<PermaPruneWindow>();
            showBlocked = this.gameObject.AddComponent<ShowBlocked>();
            showRenamed = this.gameObject.AddComponent<ShowRenamed>();
        }

        new private void Start()
        {
            //FileOperations f = new FileOperations();
           // FileOperations.Instance = f;
            Log.setTitle("Janitor's Closet");
            Log.Info("JanitorsCloset.Start");
            blackList = FileOperations.Instance.loadBlackListData();

            EditorIconEvents.OnEditorPartIconClicked.Add(IconClicked);

            Func<AvailablePart, bool> _criteria = (_aPart) => FindPart(_aPart);
            searchFilterParts = new EditorPartListFilter<AvailablePart>(MOD, _criteria);
            EditorPartList.Instance.ExcludeFilters.AddFilter(searchFilterParts);

            if (button == null)
            {
                OnGuiAppLauncherReady();
            }
            InitializeGUI();
        }

        protected new virtual void OnDestroy()
        {
            Log.Info("JanitorsCloset.OnDestroy");
            FileOperations.Instance.saveBlackListData(blackList);
            EditorIconEvents.OnEditorPartIconClicked.Remove(IconClicked);
            if (this.button != null)
            {
                Log.Info("Removng button");
                ApplicationLauncher.Instance.RemoveModApplication(this.button);

            }

            //if (_mouseController) _mouseController.enabled = true;
        }

        EditorPartIcon _icon;
        private void IconClicked(EditorPartIcon icon, EditorIconEvents.EditorIconClickEvent evt)
        {
            if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
                return;

            Log.Info("Icon was clicked for " + icon.partInfo.name + " (" + icon.partInfo.title + ")");
            _icon = icon;
            ShowPruneMenu();
            
            

            evt.Veto(); // prevent part from being spawned
        }

        protected override void Awake()
        {
            instance = this;
           // DontDestroyOnLoad(this);

            //getConfigs();
            getPartData();

            _windowFunction = PruneMenuContent;

            _mouseController = HighLogic.fetch.GetComponent<Mouse>();

           
        }

        private void getPartData()
        {
            List<string> modNames = new List<string>();

            for (int i = 0; i < PartLoader.Instance.parts.Count; i++)
            {
                AvailablePart p = PartLoader.Instance.parts[i];
                if (p == null)
                    continue;
                //Log.InfoWarning("Part Manufacturer: " + p.manufacturer);
                //Log.InfoWarning("Part name: " + p.name);
                //Log.InfoWarning("Part config: " + p.partConfig);

                //Log.InfoWarning("Part path: " + p.partPath);
                //Log.InfoWarning("Part url: " + p.partUrl);
                //Log.InfoWarning("Part title: " + p.title);
            }
        }
        enum ShowMenuState { hidden, starting, visible};
        ShowMenuState _showPruneMenu = ShowMenuState.hidden;
        Rect _pruneMenuRect = new Rect();
        const float _pruneMenuWidth = 100.0f;
        const float _pruneMenuHeight = 73.0f;

        // these two save a bit on garbage created in OnGUI
        private readonly GUILayoutOption[] _emptyOptions = new GUILayoutOption[0];
        private GUI.WindowFunction _windowFunction;

        // enable/disable this to prevent the "No Target" from popping up by double-clicking on the window 
        private Mouse _mouseController;


        public void ShowPruneMenu()
        {
            InputLockManager.SetControlLock(ControlTypes.EDITOR_ICON_PICK | ControlTypes.EDITOR_ICON_HOVER, "Pruner");
            
            Vector3 position = Input.mousePosition;
            //Log.Info("X, Y: " + position.x.ToString() + ", " + position.y.ToString());

            if (position.y + _pruneMenuHeight > Screen.height)
                position.y = Screen.height - _pruneMenuHeight;

            position.y -= 10;
            _pruneMenuRect = new Rect()
            {
                xMin = position.x - _pruneMenuWidth / 2,
                xMax = position.x + _pruneMenuWidth / 2,
                yMin = Screen.height - position.y - _pruneMenuHeight,
                yMax = Screen.height - position.y
            };
            _showPruneMenu = ShowMenuState.starting;
        }

        public void HidePruneMenu()
        {
            _showPruneMenu = ShowMenuState.hidden;
            _pruneMenuRect = new Rect();
            InputLockManager.RemoveControlLock("Pruner");
        }

        //Unity GUI loop
        void OnGUI()
        {
           // if (HighLogic.LoadedScene != GameScenes.EDITOR)
           //     return;

            if ((_showPruneMenu == ShowMenuState.starting) || (_showPruneMenu == ShowMenuState.visible && _pruneMenuRect.Contains(Event.current.mousePosition) ))
                _pruneMenuRect = KSPUtil.ClampRectToScreen(GUILayout.Window(this.GetInstanceID(), _pruneMenuRect, _windowFunction, "Blocker Menu"));
            else
                HidePruneMenu();
            
            if (_showMenu || _menuRect.Contains(Event.current.mousePosition) || (Time.fixedTime - lastTimeShown < 0.5f))
                _menuRect = GUILayout.Window(this.GetInstanceID(), _menuRect, MenuContent, "Janitor's Closet");
            else
                _menuRect = new Rect();
        }

        void addToBlackList(string p, blackListType type)
        {
            blackListPart blp = new blackListPart();

            blp.modName = p;
            blp.where = type;
            blp.permapruned = false;

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

        public void clearBlackList()
        {
            blackList.Clear();
            EditorPartList.Instance.Refresh();
            HidePruneMenu();
            FileOperations.Instance.saveBlackListData(blackList);
        }

        void PruneMenuContent(int WindowID)
        {
            _showPruneMenu = ShowMenuState.visible;
            if (GUILayout.Button("Block all"))
            {
                if (!blackList.ContainsKey(_icon.partInfo.name))
                {
                    addToBlackList(_icon.partInfo.name, blackListType.ALL);
                }
            }
            if (EditorDriver.editorFacility == EditorFacility.VAB)
            {
                if (GUILayout.Button("Block VAB"))
                {
                    addToBlackList(_icon.partInfo.name, blackListType.VAB);
                }
            }
            if (EditorDriver.editorFacility == EditorFacility.SPH)
            {
                if (GUILayout.Button("Block SPH"))
                {
                    addToBlackList(_icon.partInfo.name, blackListType.SPH);
                }
            }
        }


        float lastTimeShown = 0.0f;

        void MenuContent(int WindowID)
        {
            if (_showMenu || _menuRect.Contains(Event.current.mousePosition))
                lastTimeShown = Time.fixedTime;
            GUILayout.BeginVertical();
            if (GUILayout.Button("Show Blocked"))
            {
                // Dialog to show list of blocked parts, with buttons to temp/perm unblock a part

                // _partInfoWindow.Show();
                showBlocked.Show();
            }
            if (GUILayout.Button("Unblock"))
            {
                clearBlackList();
            }
            if (GUILayout.Button("PermaPrune"))
            {
                permaPruneWindow.Show();
            }



            if (GUILayout.Button("Export/Import"))
            {
                ImportExport.Instance.SetVisible(true);
                // Import dialog
                //_fineAdjustWindow.Show();

            }
            
            GUILayout.EndVertical();
        }



        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            var mouse = Input.mousePosition;
            var screenPos = new Vector2(mouse.x, Screen.height - mouse.y);

            if (!_pruneMenuRect.Contains(screenPos))
            {
                _mouseController.enabled = true;
                return;
            }

            _mouseController.enabled = false;
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