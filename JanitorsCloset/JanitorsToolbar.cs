﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;
using ClickThroughFix;

using static JanitorsCloset.JanitorsClosetLoader;
using JetBrains.Annotations;


namespace JanitorsCloset
{
    #region defines

    public class ButtonDictionaryItem
    {
        public ApplicationLauncherButton button;
        public string identifier;
        public string buttonHash;

        public ButtonDictionaryItem()
        {
            identifier = "";
            buttonHash = "";
            button = null;
        }
    }

    public enum Blocktype { none, moveToFolder, hideHere, hideEverywhere }

    public class ButtonSceneBlock
    {
        public GameScenes scene;
        public Blocktype blocktype;
        public string buttonHash;
        public ApplicationLauncherButton origButton;
        public bool active;
        public Texture buttonTexture;
        public Texture buttonTexture2;

        public ButtonSceneBlock()
        {
            active = false;
        }

    }

    public class ButtonBarItem
    {
        public ApplicationLauncherButton button;
        public string buttonHash;
        public int folderIcon;
        public Dictionary<string, ButtonSceneBlock> buttonBlockList = new Dictionary<string, ButtonSceneBlock>();
    }

    class Cfg
    {
        // from ButtonSceneBlock
        public GameScenes scene;
        public Blocktype blocktype;
        public string buttonHash;
        public int folderIcon;
        // ApplicationLauncherButton origButton;

        // from ButtonBarItem
        public string toolbarButtonHash;
        public int toolbarButtonIndex;

    }
    #endregion
    public class Resizer : MonoBehaviour
    {
        public Texture2D inputtexture2D;
        public RawImage rawImage;

        public static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
        {
            RenderTexture rt = new RenderTexture(targetX, targetY, 24);
            RenderTexture.active = rt;
            Graphics.Blit(texture2D, rt);
            Texture2D result = new Texture2D(targetX, targetY);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Apply();
            return result;
        }
    }

    //   [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    partial class JanitorsCloset// : BaseRaycaster
    {
        const string TexturePath = "JanitorsCloset/Textures/";
        const string mainIcon = "AppLauncherIcon";
        string[] folderIcons = new string[]{
            "bluebright-black",
            "green-black",
            "orangebright-black",
            "pink-black",
            "pinkbright-black",
            "plum-black",
            "purplebright-black",
            "red-black",
            "reddark-black",
            "black-white",
            "blue-black"};

        public Texture2D toolbarIcon;
        string[] folderIconHashes;

        public static Dictionary<string, Cfg> loadedCfgs;
        public static Dictionary<string, ButtonSceneBlock> loadedHiddenCfgs;

        /// <summary>
        /// A dictionary of all buttons.  When a new button is found on the toolbar, the dictionary is searched, first
        /// for the button itself,  and then the hash.  If neither is found, then it is added here.  If the hash is found on a 
        /// different button, the old button is deleted and the new one is added
        /// </summary>
        public static Dictionary<ApplicationLauncherButton, ButtonDictionaryItem> buttonDictionary = new Dictionary<ApplicationLauncherButton, ButtonDictionaryItem>();



        /// <summary>
        /// allBlockedButtonsList contails ALL buttons that have been blocked, on any screen
        /// </summary>
        public static Dictionary<string, ButtonSceneBlock> allBlockedButtonsList = new Dictionary<string, ButtonSceneBlock>();


        /// <summary>
        /// buttonBarList is an array of buttonbars, one for each game scene (allocated in the StartToolbar() below)
        /// </summary>
        public static Dictionary<string, ButtonBarItem>[] buttonBarList;

        public ApplicationLauncherButton primaryAppButton = null;
        public static Dictionary<string, ButtonSceneBlock> primaryButtonBlockList;
        public static Dictionary<string, ButtonSceneBlock>[] hiddenButtonBlockList;


        public Dictionary<string, ButtonSceneBlock> activeButtonBlockList;
        ApplicationLauncherButton activeButton;
        string activeButtonHash;

        ApplicationLauncherButton ClickedButton;


        Rect toolbarMenuRect = new Rect();
        ShowMenuState showToolbarMenu = ShowMenuState.hidden;
        const float baseToolbarMenuHeight = 25f + 50f + 25f + 25f; // add 25 for each new button
        const float buttonHeight = 25f;
        const float toolbarMenuWidth = 150f;
        GUIStyle toolbarButtonStyle = new GUIStyle();

        Rect toolbarRect = new Rect();
        ShowMenuState showToolbar = ShowMenuState.hidden;
        const int iconSize = 42;
        public int ScaledSize;

        public static bool NoIncompatabilities = true;


        /// <summary>
        /// Find a button in a list, and return the hash.  If it isn't there, 
        /// create a new hash
        /// </summary>
        /// <param name="btn"></param>
        /// <returns></returns>
        public string buttonId(ApplicationLauncherButton btn, bool addIfNotFound = true)
        {
            ButtonDictionaryItem bdi;
            var b = buttonDictionary.TryGetValue(btn, out bdi);

            if (!b)
            {
                Log.Info("Button not found in dictionary, hash: " + Instance.Button32hash(btn.sprite));
#if false
                Log.Info("buttonDictionary size: " + buttonDictionary.Count().ToString());
                foreach (var v in buttonDictionary)
                {
                    Log.Info("buttonDictionary hash: " + v.Value.buttonHash);
                }
#endif
                if (!addIfNotFound)
                    return "NotFound";

                return Instance.Button32hash(btn.sprite);
            }

            return bdi.buttonHash;
        }
#if false
        ButtonDictionaryItem buttonIdBDI(ApplicationLauncherButton btn)
        {
            ButtonDictionaryItem bdi;
            var b = buttonDictionary.TryGetValue(btn, out bdi);
            if (b)
                return bdi;
            else
                return null;
        }
#endif
        public ButtonDictionaryItem buttonIdBDI(string hash)
        {
            var b = buttonDictionary.Where(i => i.Value.buttonHash == hash);
            if (b != null)
                return b.FirstOrDefault().Value;
            else
                return null;
        }

        #region StartAwake


        public string hasMod(string modIdent)
        {
            foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
            {
                if (modIdent == a.name)
                    return a.assembly.GetName().Version.ToString();

            }
            return "";
        }

        void StartToolbar()
        {
            ToolbarIconEvents.OnToolbarIconClicked.Add(ToolbarClicked);
            if (primaryAppButton == null)
            {
                string textureReplacerVersion = hasMod("Texture Replacer");

                if (textureReplacerVersion != "")
                {
                    if (String.Compare(textureReplacerVersion, "2.5.4.0", StringComparison.Ordinal) < 0)
                    {
                        Log.Error("***** Incompatible version of TextureReplacer installed ******");
                        helpPopup = new HelpPopup("Janitor's Toolbar Warning", "Incompatible version of Texture Replacer is installed (needs to be 2.5.4 or greater)\n\nToolbar functionality disabled!", JanitorsCloset.getNextID());
                        helpPopup.showMenu = true;
                        helpPopup.SetWinName( "HelpPopupWindow");
                        NoIncompatabilities = false;
                    }
                }
                GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
                if (ApplicationLauncher.Instance != null)
                    OnGuiAppLauncherReady();
                GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);

                folderIconHashes = new string[folderIcons.Count()];
                for (int i = 0; i < folderIcons.Count(); i++)
                {
                    Log.Info("folderIcons, i: " + i.ToString() + "   name: " + "38_" + folderIcons[i]);
                    var a = GameDatabase.Instance.GetTexture(TexturePath + "38_" + folderIcons[i], false);
                    if (a == null)
                        Log.Info("Texture file: " + TexturePath + "38_" + folderIcons[i] + " not found");
                    else
                    {
                        var b = GetButtonTexture(a);
                        folderIconHashes[i] = Button32hash(b);
                        Destroy(b);
                    }
                }

                loadButtonData();
                buttonBarList = new Dictionary<string, ButtonBarItem>[(int)GameScenes.PSYSTEM + 1];
                hiddenButtonBlockList = new Dictionary<string, ButtonSceneBlock>[(int)GameScenes.PSYSTEM + 1];
                for (int i = 0; i <= (int)GameScenes.PSYSTEM; i++)
                {
                    buttonBarList[i] = new Dictionary<string, ButtonBarItem>();
                    hiddenButtonBlockList[i] = new Dictionary<string, ButtonSceneBlock>();

                }

            }

            float toolbarButtonStyleSize = 38 * GameSettings.UI_SCALE;
            toolbarButtonStyle.onActive.background = HighLogic.Skin.button.onActive.background;
            toolbarButtonStyle.onFocused.background = HighLogic.Skin.button.onFocused.background;
            toolbarButtonStyle.onNormal.background = HighLogic.Skin.button.onNormal.background;
            toolbarButtonStyle.onHover.background = HighLogic.Skin.button.active.background;
            toolbarButtonStyle.active.background = HighLogic.Skin.button.active.background;
            toolbarButtonStyle.focused.background = HighLogic.Skin.button.focused.background;
            toolbarButtonStyle.hover.background = HighLogic.Skin.button.hover.background;
            toolbarButtonStyle.normal.background = HighLogic.Skin.button.normal.background;
            toolbarButtonStyle.fixedHeight = toolbarButtonStyleSize;
            toolbarButtonStyle.fixedWidth = toolbarButtonStyleSize;
            //GameEvents.onLevelWasLoadedGUIReady.Add(OnSceneLoadedGUIReady);
        }

        new void  OnEnable()
        {
            base.OnEnable();
            //Tell our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
            SceneManager.sceneLoaded += OnSceneLoadedGUIReady;
        }

        new void OnDisable()
        {
            base.OnDisable();
            //Tell our 'OnLevelFinishedLoading' function to stop listening for a scene change as soon as this script is disabled. Remember to always have an unsubscription for every delegate you subscribe to!
            SceneManager.sceneLoaded -= OnSceneLoadedGUIReady;
        }

        int folderIndex(string hash)
        {
            for (int i = 0; i < folderIcons.Count(); i++)
            {
                if (hash == folderIconHashes[i])
                    return i;
            }
            Log.Error("Folder Icon Hash not found");
            return -1;
        }


        void AwakeToolbar()
        {
            toolbarWindowFunction = HideToolbarButtonMenu;
        }

        #endregion
        string tooltip = "";
        bool drawTooltip = false;
        // Vector2 mousePosition;
        Vector2 tooltipSize;
        float tooltipX, tooltipY;
        Rect tooltipRect;
        void SetupTooltip()
        {
            Vector2 mousePosition;
            mousePosition.x = Input.mousePosition.x;
            mousePosition.y = Screen.height - Input.mousePosition.y;
            Log.Info("SetupTooltip, tooltip: " + tooltip);
            if (tooltip != null && tooltip.Trim().Length > 0)
            {
                tooltipSize = HighLogic.Skin.label.CalcSize(new GUIContent(tooltip));
                tooltipX = (mousePosition.x + tooltipSize.x > Screen.width) ? (Screen.width - tooltipSize.x) : mousePosition.x;
                tooltipY = mousePosition.y;
                if (tooltipX < 0) tooltipX = 0;
                if (tooltipY < 0) tooltipY = 0;
                tooltipRect = new Rect(tooltipX - 1, tooltipY - tooltipSize.y, tooltipSize.x + 4, tooltipSize.y);
                Log.Info("display x: " + tooltipX.ToString() + ", y: " + tooltipY.ToString() + ",  size.x,y: " + tooltipSize.x.ToString() + ", " + tooltipSize.y.ToString() + ", tooltip: " + tooltip);

                //  GUI.Label(new Rect(x, y, size.x, size.y), tooltip);
            }
        }
        protected void DrawTooltip()
        {
            if (tooltip != null && tooltip.Trim().Length > 0)
            {
                GUI.Label(tooltipRect, tooltip, HighLogic.Skin.label);
            }
        }

        void OnGameSettingsApplied()
        {
            if (this.primaryAppButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(this.primaryAppButton);
            this.primaryAppButton = null;
            OnGuiAppLauncherReady();
        }
        /// <summary>
        /// Add the JanitorsToolbar button
        /// </summary>
        private void OnGuiAppLauncherReady()
        {

            if (this.primaryAppButton == null && HighLogic.CurrentGame != null && ApplicationLauncher.Instance!= null)
            {
                ApplicationLauncher.AppScenes validScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.TRACKSTATION;
                if (!NoIncompatabilities || !HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().toolbarEnabled)
                    validScenes = ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;
                if ((GameSceneToLoadedScene() & validScenes) == ApplicationLauncher.AppScenes.NEVER)
                    return;

                ButtonBarItem buttonBarEntry = new ButtonBarItem();
                buttonBarEntry.buttonHash = "";
                buttonBarEntry.buttonBlockList = new Dictionary<string, ButtonSceneBlock>();
                primaryButtonBlockList = buttonBarEntry.buttonBlockList;
                toolbarIcon = GameDatabase.Instance.GetTexture(TexturePath + mainIcon, false);
                Log.Info("in OnGuiAppLauncherReady, before try");
                try
                {
                    this.primaryAppButton = ApplicationLauncher.Instance.AddModApplication(
                        () =>
                        {
                            showToolbarRightClickToggle();
                            hidable = false;
                            showByHover = false;
                            Dictionary<string, ButtonSceneBlock> hiddenButtons = new Dictionary<string, ButtonSceneBlock>(JanitorsCloset.hiddenButtonBlockList[0]);
                            //hiddenButtons = hiddenButtons.Concat(JanitorsCloset.hiddenButtonBlockList[(int)JanitorsCloset.appScene]).ToDictionary(x => x.Key, x => x.Value);


                            foreach (var i in hiddenButtonBlockList[(int)JanitorsCloset.appScene])
                                hiddenButtons[i.Key]= i.Value;
                            //if (!hiddenButtons.ContainsKey(i.Key))
                            //        hiddenButtons.Add(i.Key, i.Value);

                            ToolbarShow(this.primaryAppButton, "", hiddenButtons);

                            // JanitorsCloset.Instance.ToolbarShow(buttonBarEntry.button, buttonBarEntry.buttonHash, buttonBarEntry.buttonBlockList);

                        },  //RUIToggleButton.onTrue
                        () =>
                        {
                            hidable = true;
                            JanitorsCloset.Instance.ToolbarHide();
                        },  //RUIToggleButton.onFalse
                        () =>
                        {
                            if (HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().editorMenuPopupEnabled)
                            {
                                if (showToolbar == ShowMenuState.hidden)
                                    JanitorsCloset.Instance.ShowMenu();
                            }
                        }, //RUIToggleButton.OnHover
                        () =>
                        {
                            if (HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().editorMenuPopupEnabled)
                            {
                                if (showToolbar == ShowMenuState.hidden)
                                    JanitorsCloset.Instance.HideMenu();
                            }
                        }, //RUIToggleButton.onHoverOut
                        null, //RUIToggleButton.onEnable
                        null, //RUIToggleButton.onDisable
                        validScenes,
                        toolbarIcon //texture
                    );
                    Log.Info("Added ApplicationLauncher button");
                    this.primaryAppButton.onRightClick = showToolbarRightClickToggle;

                    buttonBarEntry.button = primaryAppButton;
                    buttonBarList[0][buttonBarEntry.buttonHash]= buttonBarEntry;
                    //if (!buttonBarList[0].ContainsKey(buttonBarEntry.buttonHash))
                    //    buttonBarList[0].Add(buttonBarEntry.buttonHash, buttonBarEntry);

                }
                catch (Exception ex)
                {
                    if (ex != null && ex.Message != null)
                        Log.Error("Error adding ApplicationLauncher button: " + ex.Message);
                    else
                        Log.Error("Error adding ApplicationLauncher button");
                }
            }
        }

        void showToolbarRightClickToggle()
        {
            if (!_showMenu)
            {
                JanitorsCloset.Instance.ShowMenu();
            }
            else
            {
                JanitorsCloset.Instance.HideMenu();
            }
        }

        public ButtonBarItem AddAdditionalToolbarButton(int folderNum, GameScenes scene = GameScenes.LOADING)
        {
            ButtonBarItem buttonBarEntry = new ButtonBarItem();

            buttonBarEntry.buttonBlockList = new Dictionary<string, ButtonSceneBlock>();
            //   ApplicationLauncher.AppScenes appScene = 0;
            GameScenes curScene = HighLogic.LoadedScene;
            if (scene != GameScenes.LOADING)
                curScene = scene;

            try
            {
                buttonBarEntry.button = ApplicationLauncher.Instance.AddModApplication(
                        () =>
                        {
                            ToolbarHide();
                            hidable = false;
                            showByHover = false;
                            ToolbarShow(buttonBarEntry.button, buttonBarEntry.buttonHash, buttonBarEntry.buttonBlockList);
                        },  //RUIToggleButton.onTrue
                        () =>
                        {
                            //JanitorsCloset.Instance.
                            ToolbarHide();
                            hidable = true;
                        },  //RUIToggleButton.onFalse
                        () =>
                        {
                            if (HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().toolbarPopupsEnabled)
                            {
                                if (showByHover)
                                    ToolbarHide();
                                if (showToolbar == ShowMenuState.hidden)
                                {
                                    lasttimeToolBarRectShown = Time.fixedTime;
                                    ToolbarShow(buttonBarEntry.button, buttonBarEntry.buttonHash, buttonBarEntry.buttonBlockList, true);
                                }
                                hidable = false;

                            }
                        }, //RUIToggleButton.OnHover
                        () =>
                        {
                            if (HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().toolbarPopupsEnabled)
                            {
                                if (showByHover)
                                {
                                    hidable = true;
                                    ToolbarHide(true);
                                }
                            }
                        }, //RUIToggleButton.onHoverOut
                        null, //RUIToggleButton.onEnable
                        null, //RUIToggleButton.onDisable
                        GameSceneToLoadedScene(),

                        GameDatabase.Instance.GetTexture(TexturePath + "38_" + folderIcons[folderNum], false) //texture
                    );

                Log.Info("Added ApplicationLauncher button");
                if (buttonBarEntry.button == null)
                    Log.Info("button is null");


                buttonBarEntry.folderIcon = folderNum;
                buttonBarEntry.buttonHash = buttonId(buttonBarEntry.button);
                buttonBarList[(int)curScene].Add(buttonBarEntry.buttonHash, buttonBarEntry);
                return buttonBarEntry;

            }
            catch (Exception ex)
            {
                Log.Error("Error adding ApplicationLauncher button: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buttonBlockList"></param>
        /// <returns></returns>
        int DisabledButtonsInToolbarCnt(Dictionary<string, ButtonSceneBlock> buttonBlockList, bool hide)
        {
            int cnt = 0;
            foreach (var b in buttonBlockList)
            {
                if (ApplicationLauncher.Instance.ShouldBeVisible(b.Value.origButton))
                {
                    if ((!hide && b.Value.blocktype == Blocktype.moveToFolder) ||
                        (hide && b.Value.blocktype != Blocktype.moveToFolder))
                        cnt++;
                }
                //                if ( (b.Value.blocktype == Blocktype.moveToFolder /* ||
                //                    b.Value.blocktype == Blocktype.hideEverywhere*/ ) &&
                //                    ApplicationLauncher.Instance.ShouldBeVisible(b.Value.origButton))
                //                    //b.Value.scene == HighLogic.LoadedScene)
                //                    cnt++;
            }
            return cnt;
        }

        bool showByHover = false;
        bool hidable = true;

        public HelpPopup helpPopup = null;
        string[] helpText = {
"The button for The Janitor’s Closet is now available on all screens.   There are several modes of",
"operation, depending on various factors.  The toolbar button looks like a broom & dustbin.",
"Toolbar buttons can be hidden, either in the current screen or everywhere.  This is somewhat",
"misleading, since all hidden buttons are still available by clicking on the Janitor’s Closet",
"toolbar button.",
"Toolbar buttons can also be moved to folders, either a new folder or an existing one.",
"",
"M o d e s   o f   O p e r a t i o n",
"",
"No Hidden Buttons(ie: when first starting)",
"    If no buttons have been hidden, then clicking on the Janitor’s Closet button will open a help",
"    window with these instructions",
"    If in the editor, hovering over the button will bring up the toolbar popup menu, with the",
"    following buttons:",
"        Show Blocked",
"        Unblock",
"        PermaPrune",
"        Mod Filter",
"        Export/Import",
"",
"    ShowBlocked will display a list of all parts blocked, and, if a soft basis, a button so you",
"    can unblock a single part",
"    Unblock will unblock all parts which are blocked via the soft basis.",
"    PermaPrune will display a new menu",
"    Mod Filter will display a window where you can select which mods you want to filter(ie:  show",
"    parts from)",
"    Import/Export will display a new menu",
"",
"Hidden Buttons",
"    Clicking on the Janitor’s Closet button will show all buttons which are hidden in the current",
"    scene",
"",
"Toolbar Operations",
"    Any toolbar button can be hidden, either on the current scene or all scenes.Any toolbar button ",
"    can be moved, either to a new or existing button folder.To activate this, hold the ALT button",
"    (on Windows) and right -click the button you want to hide/move.Linux and OSX users need to",
"    use the MOD button for their system.A popup menu will be displayed, with the following:",
"        Hide here",
"        Hide everywhere",
"        Move to new folder",
"        and, if there are already folders created and displayed, there will be an additional button for",
"        each folder on the toolbar, with the folder displayed followed by the words “Move to folder”",
"    Hide here will remove the button from the toolbar in this scene only",
"    Hide everywhere will remove the button from the toolbar in all scenes",
"    Move to new folder will create a new button folder and move this button into the new button",
"    folder",
"    Move to folder will move this button into the selected button folder",
"",
"Button Folders",
"Button folders are new buttons added to hold other buttons.At present you can’t select which icon",
"to use for a new folder.They look like a colored folder with the broom & dustbin on top.",
"Moving the mouse over a button folder will do an instant popup toolbar perpendicular to the current",
"toolbar showing all the buttons in the toolbar.Moving the mouse away will hide the popup.To keep",
"the popup, click on the button folder.",
"Once a popup toolbar is visible, you can click on the button just like normal.  Hovering over a",
"button works as well.",
        };

        public void ToolbarShow(ApplicationLauncherButton button, string buttonHash, Dictionary<string, ButtonSceneBlock> buttonBlockList, bool hover = false)
        {
            Log.Info("Show(), size of buttonBlockList: " + buttonBlockList.Count.ToString());
            if (buttonBlockList.Count == 0 && !hover)
            {
                string htext = "";
                foreach (var s in helpText)
                    htext += s + "\n";
                if (helpPopup == null)
                    helpPopup = new HelpPopup("Janitor's Toolbar Help", htext, JanitorsCloset.getNextID());
                helpPopup.showMenu = !helpPopup.showMenu;
                button.SetFalse();
                HideMenu();
                showByHover = false;
                return;
            }

            if (showByHover)
                HideMenu();
            showByHover = hover;

            activeButtonBlockList = buttonBlockList;
            activeButton = button;
            activeButtonHash = buttonHash;

            Log.Info("IsPositionedAtTop: " + ApplicationLauncher.Instance.IsPositionedAtTop.ToString());
            _menuRect = new Rect();
            _showMenu = false;

            Camera camera = UIMasterController.Instance.appCanvas.worldCamera;
            Vector3 screenPos = camera.WorldToScreenPoint(activeButton.transform.position);
            Log.Info("target location is " + screenPos.x.ToString() + ", " + screenPos.y.ToString());

            screenPos.y = Screen.height - screenPos.y;
            float iconSizeScaled = iconSize * GameSettings.UI_SCALE;
            showToolbar = ShowMenuState.starting;

            int btnCnt;
            if (this.primaryAppButton != button)
                btnCnt = DisabledButtonsInToolbarCnt(buttonBlockList, this.primaryAppButton == button);
            else
                btnCnt = buttonBlockList.Count();
            Log.Info("btnCnt: " + btnCnt);
            if (ApplicationLauncher.Instance.IsPositionedAtTop)
            {
                // Assume vertical menu, therefor this needs to be horizontal
                toolbarRect = new Rect()
                {
                    xMin = screenPos.x - btnCnt * iconSizeScaled,
                    xMax = screenPos.x + 5, // - offset,
                    yMin = screenPos.y + 2,
                    yMax = screenPos.y + iconSizeScaled
                };
            }
            else
            {
                // Assume horizontal menu, therefor this needs to be vertical
                toolbarRect = new Rect()
                {
                    xMin = screenPos.x + 2,
                    xMax = screenPos.x + iconSizeScaled,
                    yMin = screenPos.y - btnCnt * iconSizeScaled,
                    yMax = screenPos.y + 5
                };
            }

            toolbarRectID = JanitorsCloset.getNextID();
            Log.Info("rect dimensions: xMin: " + toolbarRect.xMin.ToString() + ", xMax: " + toolbarRect.xMax.ToString() + ", yMin: " + toolbarRect.yMin.ToString() + ", yMax: " + toolbarRect.yMax.ToString());
        }

        //hide the addon's GUI
        float lasttimeToolBarRectShown = 0;
        public void ToolbarHide(bool hover = false)
        {
            if (hover)
            {
                if (toolbarRect.Contains(Event.current.mousePosition))
                {
                    lasttimeToolBarRectShown = Time.fixedTime;
                    return;
                }

                //Log.Info("Time.fixedTime" + Time.fixedTime.ToString() + "   lasttimeToolBarRectShown: " + lasttimeToolBarRectShown.ToString());
                //Log.Info("Time.fixedTime - lasttimeToolBarRectShown: " + (Time.fixedTime - lasttimeToolBarRectShown).ToString());
                if (Time.fixedTime - lasttimeToolBarRectShown < HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().hoverTimeout)
                    return;
                if (!hidable)
                {
                    lasttimeToolBarRectShown = Time.fixedTime;
                    return;
                }
            }
            Log.Info("Hiding");
            showToolbar = ShowMenuState.hidden;
            toolbarRect = new Rect();
            showByHover = false;
        }


        void OnDestroyToolbar()
        {
            Log.Info("JanitorsCloset.OnDestroy");


            EditorIconEvents.OnEditorPartIconHover.Remove(IconHover);
            EditorIconEvents.OnEditorPartIconClicked.Remove(IconClicked);
            ToolbarIconEvents.OnToolbarIconClicked.Remove(ToolbarClicked);
            if (this.primaryAppButton != null)
            {
                Log.Info("Removng button");
                // ApplicationLauncher.Instance.RemoveModApplication(this.button);


                for (int i = 0; i <= (int)GameScenes.PSYSTEM; i++)
                {
                    foreach (var b in buttonBarList[i])
                        ApplicationLauncher.Instance.RemoveModApplication(b.Value.button);

                }
            }
        }



        private void ToolbarClicked(ApplicationLauncherButton Clicked)
        {
            Log.Info("JanitorsCloset.OnRightClick");
            if (Clicked != primaryAppButton)
            {
                if (!ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.primary) && !ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.secondary))
                    return;
                ClickedButton = Clicked;
                Log.Info("Clicked Button hash: " + buttonId(Clicked) + "   name: " + ClickedButton.sprite.texture.name);

                if (blacklistIcons.ContainsKey(ClickedButton.sprite.texture.name) || blacklistIcons.ContainsKey(buttonId(Clicked)))
                {
                    Log.Info("Icon is blacklisted inToolbarClickedToolbarClicked ");
                    return;
                }

                ShowToolbarMenu();
            }
        }



        private GUI.WindowFunction toolbarWindowFunction;

        int curScene
        {
            get { return (int)HighLogic.LoadedScene; }
        }



        // static Texture2D img2;
        static Color32[] pixelBlock = null;
        static RenderTexture rt, origrt;
        
        public Texture2D GetButtonTexture(Texture2D img)
        {
            Texture2D img2 = new Texture2D(img.width, img.height, TextureFormat.ARGB32, false);
            // see: https://docs.unity3d.com/ScriptReference/Texture2D.GetPixels.html
            try
            {
                pixelBlock = img.GetPixels32();
                img2.SetPixels32(pixelBlock);
                Log.Info("GetPixels32 loaded image");
            }
            catch 
            {

                img.filterMode = FilterMode.Point;
                rt = RenderTexture.GetTemporary(img.width, img.height);
                rt.filterMode = FilterMode.Point;
                origrt = RenderTexture.active;
                RenderTexture.active = rt;
                Graphics.Blit(img, rt);
                img2 = new Texture2D(img.width, img.height, TextureFormat.ARGB32, false);
                img2.ReadPixels(new Rect(0, 0, img.width, img.height), 0, 0);
                //Log.Info("GetPixels32 had Exception, img name: " + img.name);
                RenderTexture.ReleaseTemporary(rt);
                RenderTexture.active = origrt;
            }
            img2.Apply();
            return img2;
        }
#if false
        Texture2D ConvertSpriteToTexture(Sprite sprite)
        {
            try
            {
                if (sprite.rect.width != sprite.texture.width)
                {
                    Texture2D newText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
                    Color[] colors = newText.GetPixels();
                    Color[] newColors = sprite.texture.GetPixels((int)System.Math.Ceiling(sprite.textureRect.x),
                                                                 (int)System.Math.Ceiling(sprite.textureRect.y),
                                                                 (int)System.Math.Ceiling(sprite.textureRect.width),
                                                                 (int)System.Math.Ceiling(sprite.textureRect.height));
                    Debug.Log(colors.Length + "_" + newColors.Length);
                    newText.SetPixels(newColors);
                    newText.Apply();
                    return newText;
                }
                else
                    return sprite.texture;
            }
            catch
            {
                return sprite.texture;
            }
        }
#endif
        public Texture2D GetButtonTexture(RawImage sprite)
        {
            // return ConvertSpriteToTexture(sprite);
            if (sprite != null && sprite.texture != null)
            {
                Texture2D img = sprite.texture as Texture2D;
                if (sprite.texture.name != null)
                    img.name = sprite.texture.name;

                return GetButtonTexture(img);
            }
            else
            {
                Texture2D img = new Texture2D(2, 2);
                return img;
            }

        }

        public string Button32hash(Texture2D img2)
        {
            if (img2 == null)
                return "null";
            Crc32 crc32 = new Crc32();
            String hash = String.Empty;
            byte[] byteAR = img2.EncodeToPNG();
            Log.Info("byteAR size: " + byteAR.Length.ToString());
            foreach (byte b1 in crc32.ComputeHash(byteAR))
                hash += b1.ToString("x2").ToLower();
            Log.Info("byteAR size: " + byteAR.Length.ToString() + "   hash: " + hash);
            return hash;
        }
#if false
        public string Button32hash(Texture img2)
        {


            Crc32 crc32 = new Crc32();
            String hash = String.Empty;
            byte[] byteAR = img2.EncodeToPNG();

            foreach (byte b1 in crc32.ComputeHash(byteAR))
                hash += b1.ToString("x2").ToLower();
            return hash;
        }
#endif
        public string Button32hash(RawImage sprite)
        {
            if (sprite == null)
                return "spritenull";
            Texture2D img2 = GetButtonTexture(sprite);
            
            string s = Button32hash(img2);
            Destroy(img2);
            return s;
        }

        public void ShowToolbarMenu()
        {
            Log.Info("ShowToolbarMenu");
            InputLockManager.SetControlLock(ControlTypes.EDITOR_ICON_PICK | ControlTypes.EDITOR_ICON_HOVER, "Pruner");

            Camera camera = UIMasterController.Instance.appCanvas.worldCamera;
            Vector3 screenPos;
            float toolbarMenuHeight = baseToolbarMenuHeight + buttonHeight * buttonBarList[curScene].Count;
            if (HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().buttonIdent)
                toolbarMenuHeight += buttonHeight;
            if (ApplicationLauncher.Instance.IsPositionedAtTop)
            {
                // Assume vertical menu, therefor this needs to be at the left
                screenPos = camera.WorldToScreenPoint(ClickedButton.transform.position);



                Log.Info("screenPos.x: " + screenPos.x.ToString() + "   toolbarMenuHeight: " + toolbarMenuHeight.ToString());
                screenPos.y = Screen.height - screenPos.y;
                toolbarMenuRect = new Rect()
                {
                    xMax = screenPos.x + 2,
                    xMin = screenPos.x - toolbarMenuWidth, // _pruneMenuWidth, // - toolbarMenuHeight,

                    yMin = screenPos.y - toolbarMenuHeight / 3,
                    yMax = screenPos.y + toolbarMenuHeight / 3 * 2
                };

            }
            else
            {
                Vector3 position = Input.mousePosition;
                screenPos = camera.WorldToScreenPoint(ClickedButton.transform.position);
                screenPos.y = Screen.height - screenPos.y;

                toolbarMenuRect = new Rect()
                {
                    xMin = position.x - _pruneMenuWidth / 2,
                    xMax = position.x + _pruneMenuWidth / 2,
                    yMin = screenPos.y - toolbarMenuHeight,
                    yMax = screenPos.y + 2
                };

            }

            toolbarMenuRectID = JanitorsCloset.getNextID();
            showToolbarMenu = ShowMenuState.starting;
            lastTimeShown = Time.fixedTime;
            Log.Info("lastTimeShown 2");
        }

        public void HideToolbarMenu()
        {
            showToolbarMenu = ShowMenuState.hidden;
            toolbarMenuRect = new Rect();
            InputLockManager.RemoveControlLock("Pruner");
        }

        public bool addToButtonBlockList(Dictionary<string, ButtonSceneBlock> buttonBlockList, ApplicationLauncherButton selectedButton)
        {
            ButtonSceneBlock bsb = new ButtonSceneBlock();
            bsb.buttonHash = buttonId(selectedButton);
            Log.Info("hash of moved button: " + bsb.buttonHash);
            bsb.scene = HighLogic.LoadedScene;
            bsb.blocktype = Blocktype.moveToFolder;

            bsb.origButton = selectedButton;


            bsb.buttonTexture = GetButtonTexture(selectedButton.sprite);
            bsb.buttonTexture2 = selectedButton.sprite.texture;
#if DEBUG
            foreach (var s in buttonBlockList)
                Log.Info("addToButtonBlockList,  buttonBlockList key: " + s.Key);
#endif
            buttonBlockList[bsb.buttonHash] = bsb;
            //try
            //{
            //    buttonBlockList.Add(bsb.buttonHash, bsb);
            //} catch
            //{
            //    Log.Error("Buttonhash: [" + bsb.buttonHash + "] already in buttonBlockList, not being added");
            //    return false;
            //}

#if DEBUG
            foreach (var s in allBlockedButtonsList)
                Log.Info("addToButtonBlockList, allBlockedButtonsList key: " + s.Key);
#endif
            allBlockedButtonsList[bsb.buttonHash] = bsb;
            //try
            //{
            //    allBlockedButtonsList.Add(bsb.buttonHash, bsb);
            //} catch
            //{
            //    Log.Error("Buttonhash: [" + bsb.buttonHash + "] already in allBlockedButtonsList, not being added");
            //    return false;
            //}

            showToolbarMenu = ShowMenuState.hiding;
            return true;
        }

        public void addToHiddenBlockList(ApplicationLauncherButton selectedButton, Blocktype btype)
        {
            // hiddenButtonBlockList

            ButtonSceneBlock bsb = new ButtonSceneBlock();
            bsb.buttonHash = buttonId(selectedButton);
            Log.Info("hash of hidden button: " + bsb.buttonHash);

            bsb.scene = HighLogic.LoadedScene;
            bsb.blocktype = btype;
            bsb.origButton = selectedButton;

            bsb.buttonTexture = GetButtonTexture(selectedButton.sprite);
            bsb.buttonTexture2 = selectedButton.sprite.texture;
            if (btype == Blocktype.hideHere)
            {
                hiddenButtonBlockList[(int)appScene][bsb.buttonHash] = bsb;
                //if (!hiddenButtonBlockList[(int)appScene].ContainsKey(bsb.buttonHash))
                //    hiddenButtonBlockList[(int)appScene].Add(bsb.buttonHash, bsb);
            }
            else
            {
                hiddenButtonBlockList[0][bsb.buttonHash] = bsb;
                //if (!hiddenButtonBlockList[0].ContainsKey(bsb.buttonHash))
                //    hiddenButtonBlockList[0].Add(bsb.buttonHash, bsb);
            }
            allBlockedButtonsList[bsb.buttonHash] = bsb;
            //if (!allBlockedButtonsList.ContainsKey(bsb.buttonHash))
            //    allBlockedButtonsList.Add(bsb.buttonHash, bsb);

            showToolbarMenu = ShowMenuState.hiding;
        }

        public static ApplicationLauncher.AppScenes GameSceneToLoadedScene(GameScenes scene = GameScenes.LOADING)
        {
            if (scene == GameScenes.LOADING)
                scene = HighLogic.LoadedScene;
            switch (HighLogic.LoadedScene)
            {
                case GameScenes.SPACECENTER:
                    return ApplicationLauncher.AppScenes.SPACECENTER;
                case GameScenes.EDITOR:
                    return ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
                case GameScenes.FLIGHT:
                    return ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW;
                case GameScenes.TRACKSTATION:
                    return ApplicationLauncher.AppScenes.TRACKSTATION;
            }
            return ApplicationLauncher.AppScenes.NEVER;
        }

        public static GameScenes appScene
        {
            get
            {
                return HighLogic.LoadedScene;
            }
        }

        int getNextAvailableFolder()
        {
            int i = -1;
            //  buttonBarList[(int)curScene].Add(buttonBarEntry.buttonHash, buttonBarEntry);
            for (int i2 = 0; i2 < folderIcons.Count(); i2++)
            {
                i = -1;
                foreach (var j in buttonBarList[(int)curScene])
                {
                    if (j.Value.folderIcon == i2)
                    {
                        i = -2;
                        break;
                    }
                }
                if (i == -1)
                {
                    i = i2;
                    break;
                }
            }
            return i;
        }

        void HideToolbarButtonMenu(int WindowID)
        {
            showToolbarMenu = ShowMenuState.visible;
            if (toolbarMenuRect.Contains(Event.current.mousePosition))
            {
                lastTimeShown = Time.fixedTime;
                Log.Info("lastTimeShown 1");
            }
                //        if (ClickedButton.sprite.texture.name == "TextureReplacer/Plugins/AppIcon")
                //             return;
                if (GUILayout.Button("Hide here"))
            {
                addToHiddenBlockList(ClickedButton, Blocktype.hideHere);

                if (ClickedButton.gameObject.activeSelf)
                    ClickedButton.gameObject.SetActive(false);
                if (ClickedButton.enabled)
                    ClickedButton.onDisable();


                saveButtonData();
                return;

            }
            if (GUILayout.Button("Hide everywhere"))
            {
                addToHiddenBlockList(ClickedButton, Blocktype.hideEverywhere);

                if (ClickedButton.gameObject.activeSelf)
                    ClickedButton.gameObject.SetActive(false);
                if (ClickedButton.enabled)
                    ClickedButton.onDisable();


                saveButtonData();
                return;
            }
            int i = getNextAvailableFolder();
            if (i >= 0)
            {
                if (GUILayout.Button("Move to new folder"))
                {
                    Log.Info("new toolbarbutton index: " + i.ToString());
                    var newToolbarFolderButton = AddAdditionalToolbarButton(i);
                    if (addToButtonBlockList(newToolbarFolderButton.buttonBlockList, ClickedButton))
                    {

                        if (ClickedButton.gameObject.activeSelf)
                            ClickedButton.gameObject.SetActive(false);
                        if (ClickedButton.enabled)
                            ClickedButton.onDisable();

                        saveButtonData();
                        return;
                    }
                    else
                    {
                        Log.Error("Error adding to button block list");
                    }
                }
            }
            if (GUILayout.Button("Add to Blacklist"))
                blacklistbutton = true;

            if (HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().buttonIdent)
            {
                if (GUILayout.Button("Identify"))
                {
                    identifyButton = true;
                    identifyButtonHash = buttonId(ClickedButton);
                }

            }

            //int cnt = 0;
            foreach (var bb in buttonBarList[curScene])
            {

                if (GUILayout.Button(new GUIContent("Move to folder", GameDatabase.Instance.GetTexture(TexturePath + "20_" + folderIcons[bb.Value.folderIcon], false)), GUILayout.Height(22)))
                {
                    if (addToButtonBlockList(bb.Value.buttonBlockList, ClickedButton))
                    {

                        if (ClickedButton.gameObject.activeSelf)
                            ClickedButton.gameObject.SetActive(false);
                        if (ClickedButton.enabled)
                            ClickedButton.onDisable();

                        saveButtonData();
                    } else
                    {
                        Log.Error("Error adding to button block list");
                    }
                    return;
                }
            }

        }

        bool blacklistbutton = false;
        bool identifyButton = false;
        string identifyButtonHash = "";
        string identifier;


        private void FixedUpdate()
        {
            // In case original button texture is changed
            if (activeButtonBlockList!= null) // && Time.fixedTime - lastButtonUpdateTime > 10)
            { 
              
                foreach (var curButton in activeButtonBlockList)
                {
                    if (curButton.Value.origButton != null)
                    {
                       // lastButtonUpdateTime = Time.fixedTime;
                        // The following line doesn't work
                        if (curButton.Value.origButton.sprite.texture != curButton.Value.buttonTexture2)
                        {
                            Log.Info("Check found difference");
                            curButton.Value.buttonTexture2 = curButton.Value.origButton.sprite.texture;
                        }
#if false
                        var b = GetButtonTexture(curButton.Value.origButton.sprite);
                        if (curButton.Value.buttonTexture != b)
                        {
                            Destroy(curButton.Value.buttonTexture);
                            curButton.Value.buttonTexture = b;
                        }
                        else
                        {
                            Destroy(b);
                        }
#endif
                        
                        //Log.Info("Button32hash(curButton.Value.buttonTexture)" + Button32hash(curButton.Value.buttonTexture).ToString());
                        //Log.Info("Button32hash(curButton.Value.buttonTexture2)" + Button32hash(curButton.Value.buttonTexture2).ToString());
                        
                    }
                }
            }
        }

        private void Update()
        {
            if (blacklistbutton)
            {
                blacklistbutton = false;
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog("janitorsToolbar1",
                        "",
                        "Janitor's Toolbar",
                        HighLogic.UISkin,
                        new Rect(0.5f, 0.5f, 150f, 60f),
                        new DialogGUIFlexibleSpace(),
                        new DialogGUIVerticalLayout(
                            new DialogGUIFlexibleSpace(),
                            new DialogGUIButton("Confirm blacklisting button",
                                delegate
                                {
                                    string s = buttonId(ClickedButton);
                                    Log.Info("blacklistIconHash: " + s);
                                    blacklistIcons.Add(s, s);
                                    saveBlacklistData(blacklistIcons);
                                }, 140.0f, 30.0f, true),

                            new DialogGUIButton("Cancel", () => { }, 140.0f, 30.0f, true)
                            )),
                    false,
                    HighLogic.UISkin);
            }
            if (identifyButton)
            {
                identifyButton = false;
                identifier = "n/a";
                ButtonDictionaryItem bdi = buttonIdBDI(JanitorsCloset.Instance.buttonId(ClickedButton));
                if (bdi != null)
                    identifier = bdi.identifier;
                else
                {
                    bdi = new ButtonDictionaryItem();
                    bdi.buttonHash = JanitorsCloset.Instance.buttonId(ClickedButton);
                    buttonDictionary.Add(ClickedButton, bdi);
                }
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog("janitorsToolbar2",
                        "\nEnter Button identity:",
                        "Janitor's Toolbar",
                        HighLogic.UISkin,
                        new Rect(0.5f, 0.5f, 150f, 60f),
                        new DialogGUIFlexibleSpace(),

                        new DialogGUIVerticalLayout(
                            new DialogGUIFlexibleSpace(),

                             new DialogGUITextInput(identifier, false, 64, delegate (string n)
                             {
                                 identifier = string.Copy(n);
                                 if (bdi == null)
                                     Log.Info("BDI is null");
                                 bdi.identifier = string.Copy(n);
                                 saveButtonData();
                                 return identifier;
                             }, 24f),


                            new DialogGUIButton("OK", () => { }, 140.0f, 30.0f, true)
                            )),
                    false,
                    HighLogic.UISkin);
            }
        }
        int toolbarMenuRectID;
        int toolbarRectID;

       // double lastButtonUpdateTime = 0;
       // bool updateButtons = false;
        /// <summary>
        /// Called by OnGUI
        /// </summary>
        void OnGUIToolbar()
        {

            if (helpPopup != null && helpPopup.showMenu)
            {
                helpPopup.draw();
                return;
            }
            if (
                (showToolbarMenu == ShowMenuState.starting) ||
                (showToolbarMenu == ShowMenuState.visible && (Time.fixedTime - lastTimeShown < HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().hoverTimeout || toolbarMenuRect.Contains(Event.current.mousePosition)))
                )
                KSPUtil.ClampRectToScreen(ClickThruBlocker.GUILayoutWindow(toolbarMenuRectID, toolbarMenuRect, toolbarWindowFunction, "Blocker Menu"));
            else
                if (showToolbarMenu != ShowMenuState.hidden)
                HideToolbarMenu();

            // ToolbarHide checks to see if the hover has timed out
            if (showToolbar == ShowMenuState.visible)
                ToolbarHide(true);
            if (showToolbar == ShowMenuState.starting || showToolbar == ShowMenuState.visible)
            {
                GUIStyle gs = new GUIStyle();
                gs.margin.top = 0;
                gs.margin.bottom = 0;
                gs.padding = new RectOffset(0, 0, 0, 0);
                showToolbar = ShowMenuState.visible;

              //  if (Time.fixedTime - lastButtonUpdateTime > 5)
              //      updateButtons = true;
                Log.Info("toolbarRect.x: " + toolbarRect.x.ToString() + " y: " + toolbarRect.y.ToString() + "  height: " + toolbarRect.height.ToString() + "  width: " + toolbarRect.width.ToString());
                ClickThruBlocker.GUIWindow(toolbarRectID, toolbarRect, JCToolBar, (string)null, gs);
            }
        }


        bool IsMouseOver(Rect brect)
        {
            return Event.current.type == EventType.Repaint &&
               brect.Contains(Event.current.mousePosition);
        }


        bool mouseOver = false;
        void JCToolBar(int WindowID)
        {
            Log.Info("JCtoolBar, button count: " + activeButtonBlockList.Count.ToString());
            ButtonSceneBlock toRevert = null;
            // lasttimeToolBarRectShown = Time.fixedTime;

            int cnt = 0;
            drawTooltip = false;

            ScaledSize = (int)(38 * GameSettings.UI_SCALE);
            int iconSizeScaled = (int)(iconSize * GameSettings.UI_SCALE);
            foreach (var curButton in activeButtonBlockList)
            {
#if true
                if (curButton.Value.blocktype == Blocktype.hideEverywhere ||
                    ApplicationLauncher.Instance.ShouldBeVisible(curButton.Value.origButton))
                //  curButton.Value.scene == HighLogic.LoadedScene)
                {
                    Rect brect;
                    if (!ApplicationLauncher.Instance.IsPositionedAtTop)
                        brect = new Rect(0, iconSizeScaled * cnt, ScaledSize, ScaledSize);
                    else
                        brect = new Rect(iconSizeScaled * cnt, 0, ScaledSize, ScaledSize);

                    Log.Info("scene: " + HighLogic.LoadedScene.ToString() + "   cnt: " + cnt.ToString() + "   brect, x,y: " + brect.x.ToString() + ", " + brect.y.ToString() + "   width, height: " + brect.width.ToString() + ", " + brect.height.ToString());

                    cnt++;

                   // ReScale button
                   if (curButton.Value.buttonTexture2 != null &&
                        curButton.Value.buttonTexture2.width != ScaledSize &&
                        curButton.Value.buttonTexture2.height != ScaledSize)
                    {
                        Texture2D img = Resizer.Resize((Texture2D)curButton.Value.buttonTexture2, ScaledSize, ScaledSize);
                        curButton.Value.buttonTexture2 = img as Texture;
                        curButton.Value.origButton.sprite.texture = img as Texture;
                    }

                    if ( GUI.Button(brect, curButton.Value.buttonTexture2, toolbarButtonStyle) )
                    {
                        Log.Info("Clicking, keyCode: " + Event.current.keyCode.ToString());

                        if (Input.GetMouseButtonUp(0))
                        {
                            if (curButton.Value.active)
                            // if (curButton.Value.origButton.toggleButton.Value == true)
                            {
                                curButton.Value.origButton.onFalse();
                                curButton.Value.active = false;
                            }
                            else
                            {
                                curButton.Value.origButton.onTrue();
                                curButton.Value.active = true;
                            }
                            curButton.Value.origButton.onLeftClick();
                            curButton.Value.origButton.onLeftClickBtn(curButton.Value.origButton.toggleButton);
                        }
                        if (Input.GetMouseButtonUp(1))
                        {
                            Log.Info("Mouse1");
                            if (!ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.primary) && !ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.secondary))
                                curButton.Value.origButton.onRightClick();
                            else
                            {
                                toRevert = curButton.Value;
                                Log.Info("toRevert: " + curButton.Value.buttonHash);
                            }
                        }
                    }
                    if (IsMouseOver(brect))
                    {
                        var b = buttonIdBDI(curButton.Value.buttonHash);
                        if (b != null)
                        {
                            var x = buttonIdBDI(curButton.Value.buttonHash);
                            if (x == null || x.identifier == null)
                                Log.Info("buttonIdBDI returned a null");
                            Log.Info("Hover over button: " + buttonIdBDI(curButton.Value.buttonHash).identifier);

                            tooltip = b.identifier;
                            //  tooltip = curButton.Value.buttonHash;
                            drawTooltip = true;

                        }
                    }



                    if (HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().enabeHoverOnToolbarIcons)
                    {
                        if (IsMouseOver(brect))
                        {
                            if (!mouseOver)
                            {
                                mouseOver = true;
                                curButton.Value.origButton.onHover();
                                curButton.Value.origButton.onHoverBtn(curButton.Value.origButton.toggleButton);
                                curButton.Value.origButton.onHoverBtnActive(curButton.Value.origButton.toggleButton);
                            }
                        }
                        else
                        {
                            if (mouseOver)
                            {
                                mouseOver = false;
                                curButton.Value.origButton.onHoverOut();
                                curButton.Value.origButton.onHoverOutBtn(curButton.Value.origButton.toggleButton);
                            }
                        }

                    }
                }
#endif
            }
           // updateButtons = false;

            if (toRevert != null)
            {
                Log.Info("Removing hash from activeButtonBlockList: " + toRevert.buttonHash + "   blocktype: " + toRevert.blocktype.ToString());
                activeButtonBlockList.Remove(toRevert.buttonHash);
                allBlockedButtonsList.Remove(toRevert.buttonHash);

                if (toRevert.blocktype == Blocktype.hideEverywhere || toRevert.blocktype == Blocktype.hideHere)
                {
                    string hash = toRevert.buttonHash;
                    if (toRevert.blocktype == Blocktype.hideHere)
                        hash += toRevert.scene.ToString();

                    Log.Info("Removing button hash: " + hash + " from primaryButtonBlockList");
                    primaryButtonBlockList.Remove(hash);
                    allBlockedButtonsList.Remove(hash);

                    if (toRevert.blocktype == Blocktype.hideHere)
                        hiddenButtonBlockList[(int)appScene].Remove(toRevert.buttonHash);
                    else
                        hiddenButtonBlockList[0].Remove(toRevert.buttonHash);
#if false
                    ButtonSceneBlock s;
                    if (JanitorsCloset.loadedHiddenCfgs.TryGetValue(hash, out s))
                    {
                        Log.Info("Removing from loadedHiddenCfgs: " + hash);
                        JanitorsCloset.loadedHiddenCfgs.Remove(hash);
                    }
#endif
                }

                if (!toRevert.origButton.gameObject.activeSelf)
                    toRevert.origButton.gameObject.SetActive(true);
                toRevert.origButton.onEnable();

                ToolbarHide();
                activeButton.SetFalse();
                if (activeButtonBlockList.Count == 0 && activeButton != primaryAppButton)
                {
                    if (buttonBarList[curScene].Remove(activeButtonHash) == false)
                    {
                        Log.Error("Failure to remove activeButton");
                    }
                    ApplicationLauncher.Instance.RemoveModApplication(activeButton);

                    activeButton = null;

                }
                saveButtonData();
            }

        }
    }
}