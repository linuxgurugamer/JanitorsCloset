using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;
using System.IO;

namespace JanitorsCloset
{
    #region defines

    public class ButtonDictionaryItem
    {
        public ApplicationLauncherButton button;
        public string buttonHash;
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

        string[] folderIconHashes;

        public static Dictionary<string, Cfg> loadedCfgs;

        /// <summary>
        /// A dictionary of all buttons.  When a new button is found on the toolbar, the dictionary is searched, first
        /// for the button itself,  and then the hash.  If neither is found, then it is added here.  If the hash is found on a 
        /// different button, the old button is deleted and the new one is added
        /// </summary>
        public static Dictionary<ApplicationLauncherButton, string> buttonDictionary = new Dictionary<ApplicationLauncherButton, string>();



        /// <summary>
        /// allBlockedButtonsList contails ALL buttons that have been blocked, on any screen
        /// </summary>
        public static Dictionary<string, ButtonSceneBlock> allBlockedButtonsList = new Dictionary<string, ButtonSceneBlock>();

        /// <summary>
        /// buttonBarList is an array of buttonbars, one for each game scene (allocated in the StartToolbar() below)
        /// </summary>
        public static Dictionary<string, ButtonBarItem>[] buttonBarList;

        private ApplicationLauncherButton primaryAppButton = null;
        Dictionary<string, ButtonSceneBlock> primaryButtonBlockList;

        Dictionary<string, ButtonSceneBlock> activeButtonBlockList;
        ApplicationLauncherButton activeButton;
        string activeButtonHash;

        ApplicationLauncherButton ClickedButton;


        Rect toolbarMenuRect = new Rect();
        ShowMenuState showToolbarMenu = ShowMenuState.hidden;
        const float baseToolbarMenuHeight = 25f + 50f + 25f; // add 25 for each new button
        const float buttonHeight = 25f;
        const float toolbarMenuWidth = 150f;

        Rect toolbarRect = new Rect();
        ShowMenuState showToolbar = ShowMenuState.hidden;
        const int iconSize = 41;

        public static bool NoIncompatabilities = true;


        /// <summary>
        /// Find a button in a list, and return the hash.  If it isn't there, 
        /// create a new hash
        /// </summary>
        /// <param name="btn"></param>
        /// <returns></returns>
        public static string buttonId(ApplicationLauncherButton btn, bool addIfNotFound = true)
        {
            string hash;
            var b = buttonDictionary.TryGetValue(btn, out hash);

            if (!b)
            {
                Log.Error("Button not found in dictionary");
#if false
                foreach (var v in buttonDictionary)
                {
                    Log.Info("buttonDictionary hash: " + v.Value);
                }
#endif
                if (!addIfNotFound)
                    return "NotFound";
                hash = Button32hash(btn.sprite);
            }

            return hash;
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
                        NoIncompatabilities = false;
                    }
                }

                OnGuiAppLauncherReady();

                folderIconHashes = new string[folderIcons.Count()];
                for (int i = 0; i < folderIcons.Count(); i++)
                {
                    Log.Info("folderIcons, i: " + i.ToString() + "   name: " + "38_" + folderIcons[i]);
                    var a = GameDatabase.Instance.GetTexture(TexturePath + "38_" + folderIcons[i], false);
                    if (a == null)
                        Log.Info("Texture file: " + TexturePath + "38_" + folderIcons[i] + " not found");
                    else
                    {
                        folderIconHashes[i] = Button32hash(GetButtonTexture(a));
                    }
                }

                loadButtonData();
                buttonBarList = new Dictionary<string, ButtonBarItem>[(int)GameScenes.PSYSTEM + 1];
                for (int i = 0; i <= (int)GameScenes.PSYSTEM; i++)
                    buttonBarList[i] = new Dictionary<string, ButtonBarItem>();

            }
            GameEvents.onLevelWasLoadedGUIReady.Add(OnSceneLoadedGUIReady);
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

        /// <summary>
        /// Add the JanitorsToolbar button
        /// </summary>
        private void OnGuiAppLauncherReady()
        {
            if (this.primaryAppButton == null)
            {
                ApplicationLauncher.AppScenes validScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.TRACKSTATION;
                if (!NoIncompatabilities)
                    validScenes = ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;

                ButtonBarItem buttonBarEntry = new ButtonBarItem();
                buttonBarEntry.buttonHash = "";
                buttonBarEntry.buttonBlockList = new Dictionary<string, ButtonSceneBlock>();
                primaryButtonBlockList = buttonBarEntry.buttonBlockList;
                try
                {
                    this.primaryAppButton = ApplicationLauncher.Instance.AddModApplication(
                        () =>
                        {
                            hidable = false;
                            showByHover = false;
                            JanitorsCloset.Instance.ToolbarShow(this.primaryAppButton, "", buttonBarEntry.buttonBlockList);
                        },  //RUIToggleButton.onTrue
                        () =>
                        {
                            hidable = true;
                            JanitorsCloset.Instance.ToolbarHide();
                        },  //RUIToggleButton.onFalse
                        () =>
                        {
                            if (showToolbar == ShowMenuState.hidden)
                                JanitorsCloset.Instance.ShowMenu();
                        }, //RUIToggleButton.OnHover
                        () =>
                        {
                            if (showToolbar == ShowMenuState.hidden)
                                JanitorsCloset.Instance.HideMenu();
                        }, //RUIToggleButton.onHoverOut
                        null, //RUIToggleButton.onEnable
                        null, //RUIToggleButton.onDisable
                        validScenes,
                        GameDatabase.Instance.GetTexture(TexturePath + mainIcon, false) //texture
                    );
                    Log.Info("Added ApplicationLauncher button");

                    buttonBarEntry.button = primaryAppButton;
                    buttonBarList[0].Add(buttonBarEntry.buttonHash, buttonBarEntry);

                }
                catch (Exception ex)
                {
                    Log.Error("Error adding ApplicationLauncher button: " + ex.Message);
                }
            }
        }

        public ButtonBarItem AddAdditionalToolbarButton(int folderNum, GameScenes scene = GameScenes.LOADING)
        {
            ButtonBarItem buttonBarEntry = new ButtonBarItem();

            buttonBarEntry.buttonBlockList = new Dictionary<string, ButtonSceneBlock>();
            ApplicationLauncher.AppScenes appScene = 0;
            GameScenes curScene = HighLogic.LoadedScene;
            if (scene != GameScenes.LOADING)
                curScene = scene;

            switch (HighLogic.LoadedScene)
            {
                case GameScenes.SPACECENTER:
                    appScene = ApplicationLauncher.AppScenes.SPACECENTER; break;
                case GameScenes.EDITOR:
                    appScene = ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH; break;
                case GameScenes.FLIGHT:
                    appScene = ApplicationLauncher.AppScenes.FLIGHT; break;
                case GameScenes.TRACKSTATION:
                    appScene = ApplicationLauncher.AppScenes.TRACKSTATION; break;
            }

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
                            if (showByHover)
                               ToolbarHide();
                            if (showToolbar == ShowMenuState.hidden)
                            {
                                lasttimeToolBarRectShown = Time.fixedTime;
                                ToolbarShow(buttonBarEntry.button, buttonBarEntry.buttonHash, buttonBarEntry.buttonBlockList, true);
                            }
                            hidable = false;
                        }, //RUIToggleButton.OnHover
                        () =>
                        {
                            if (showByHover)
                            {
                               hidable = true;
                               ToolbarHide(true);
                            }
                        }, //RUIToggleButton.onHoverOut
                        null, //RUIToggleButton.onEnable
                        null, //RUIToggleButton.onDisable
                        appScene,

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
        int DisabledButtonsInToolbarCnt(Dictionary<string, ButtonSceneBlock> buttonBlockList)
        {
            int cnt = 0;
            foreach (var b in buttonBlockList)
            {
                if (b.Value.blocktype == Blocktype.moveToFolder ||
                    b.Value.blocktype == Blocktype.hideEverywhere ||
                    b.Value.scene == HighLogic.LoadedScene)
                    cnt++;
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
            Debug.Log("target location is " + screenPos.x.ToString() + ", " + screenPos.y.ToString());

            screenPos.y = Screen.height - screenPos.y;


            showToolbar = ShowMenuState.starting;

            int btnCnt = DisabledButtonsInToolbarCnt(buttonBlockList);

            if (ApplicationLauncher.Instance.IsPositionedAtTop)
            {
                // Assume vertical menu, therefor this needs to be horizontal
                toolbarRect = new Rect()
                {
                    xMin = screenPos.x - btnCnt * (iconSize),
                    xMax = screenPos.x + 5, // - offset,
                    yMin = screenPos.y,
                    yMax = screenPos.y + iconSize
                };
            }
            else
            {
                // Assume horizontal menu, therefor this needs to be vertical
                toolbarRect = new Rect()
                {
                    xMin = screenPos.x,
                    xMax = screenPos.x + iconSize,
                    yMin = screenPos.y - btnCnt * iconSize,
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
                    Log.Info("ToolbarHide, mouse in rect");
                    lasttimeToolBarRectShown = Time.fixedTime;
                    return;
                }

                //Log.Info("Time.fixedTime" + Time.fixedTime.ToString() + "   lasttimeToolBarRectShown: " + lasttimeToolBarRectShown.ToString());
                //Log.Info("Time.fixedTime - lasttimeToolBarRectShown: " + (Time.fixedTime - lasttimeToolBarRectShown).ToString());
                if (Time.fixedTime - lasttimeToolBarRectShown < 0.5)
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
                if (!Input.GetKey(GameSettings.MODIFIER_KEY.primary) && !Input.GetKey(GameSettings.MODIFIER_KEY.secondary))
                    return;
                ClickedButton = Clicked;
                Log.Info("Clicked Button hash: " + buttonId(Clicked));

                ShowToolbarMenu();
            }
        }



        private GUI.WindowFunction toolbarWindowFunction;

        int curScene
        {
            get { return (int)HighLogic.LoadedScene; }
        }

        public static Texture2D GetButtonTexture(Texture2D img)
        {
            Texture2D img2;
            Color32[] pixelBlock = null;

            // see: https://docs.unity3d.com/ScriptReference/Texture2D.GetPixels.html
            try
            {
                pixelBlock = img.GetPixels32();
                img2 = new Texture2D(img.width, img.height, TextureFormat.ARGB32, false);
                img2.SetPixels32(pixelBlock);

                //Log.Info("GetPixels32 loaded image");
            }
            catch (UnityException _e)
            {

                img.filterMode = FilterMode.Point;
                RenderTexture rt = RenderTexture.GetTemporary(img.width, img.height);
                rt.filterMode = FilterMode.Point;
                RenderTexture origrt = RenderTexture.active;
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
        public static Texture2D GetButtonTexture(RawImage sprite)
        {
            Texture2D img = sprite.texture as Texture2D;
            img.name = sprite.texture.name;
            return GetButtonTexture(img);

        }

        public static string Button32hash(Texture2D img2)
        {
            Crc32 crc32 = new Crc32();
            String hash = String.Empty;
            byte[] byteAR = img2.EncodeToPNG();

            foreach (byte b1 in crc32.ComputeHash(byteAR))
                hash += b1.ToString("x2").ToLower();
            return hash;
        }

        public static string Button32hash(RawImage sprite)
        {
            Texture2D img2 = GetButtonTexture(sprite);
            return Button32hash(img2);
        }

        public void ShowToolbarMenu()
        {
            Log.Info("ShowToolbarMenu");
            InputLockManager.SetControlLock(ControlTypes.EDITOR_ICON_PICK | ControlTypes.EDITOR_ICON_HOVER, "Pruner");

            Camera camera = UIMasterController.Instance.appCanvas.worldCamera;
            Vector3 screenPos;
            float toolbarMenuHeight = baseToolbarMenuHeight + buttonHeight * buttonBarList[curScene].Count;
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
        }

        public void HideToolbarMenu()
        {
            showToolbarMenu = ShowMenuState.hidden;
            toolbarMenuRect = new Rect();
            InputLockManager.RemoveControlLock("Pruner");
        }

        void addToButtonBlockList(Dictionary<string, ButtonSceneBlock> buttonBlockList, ApplicationLauncherButton selectedButton)
        {
            ButtonSceneBlock bsb = new ButtonSceneBlock();
            bsb.buttonHash = buttonId(selectedButton);
            Log.Info("hash of moved button: " + bsb.buttonHash);
            bsb.scene = HighLogic.LoadedScene;
            bsb.blocktype = Blocktype.moveToFolder;

            bsb.origButton = selectedButton;


            bsb.buttonTexture = GetButtonTexture(selectedButton.sprite);
            buttonBlockList.Add(bsb.buttonHash, bsb);

            allBlockedButtonsList.Add(bsb.buttonHash, bsb);

            showToolbarMenu = ShowMenuState.hiding;
        }

        public static ApplicationLauncher.AppScenes appScene
        {
            get
            {
                switch (HighLogic.LoadedScene)
                {
                    case GameScenes.SPACECENTER:
                        return ApplicationLauncher.AppScenes.SPACECENTER;
                    case GameScenes.EDITOR:
                        return ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
                    case GameScenes.FLIGHT:
                        return ApplicationLauncher.AppScenes.FLIGHT;
                    case GameScenes.TRACKSTATION:
                        return ApplicationLauncher.AppScenes.TRACKSTATION;
                }
                return ApplicationLauncher.AppScenes.NEVER;
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
                lastTimeShown = Time.fixedTime;
            //        if (ClickedButton.sprite.texture.name == "TextureReplacer/Plugins/AppIcon")
            //             return;
            if (GUILayout.Button("Hide here"))
            {
                ButtonSceneBlock bsb = new ButtonSceneBlock();

                bsb.buttonHash = buttonId(ClickedButton);
                bsb.scene = HighLogic.LoadedScene;
                bsb.blocktype = Blocktype.hideHere;
                bsb.origButton = ClickedButton;
                bsb.buttonTexture = GetButtonTexture(ClickedButton.sprite);

                if (ClickedButton.gameObject.activeSelf)
                    ClickedButton.gameObject.SetActive(false);
                if (ClickedButton.enabled)
                    ClickedButton.onDisable();

                primaryButtonBlockList.Add(bsb.buttonHash + bsb.scene.ToString(), bsb);
                Log.Info("primaryButtonBlockList count: " + primaryButtonBlockList.Count.ToString());

                allBlockedButtonsList.Add(bsb.buttonHash + bsb.scene.ToString(), bsb);
                showToolbarMenu = ShowMenuState.hiding;
                return;

            }
            if (GUILayout.Button("Hide everywhere"))
            {

                ButtonSceneBlock bsb = new ButtonSceneBlock();

                bsb.buttonHash = buttonId(ClickedButton);
                bsb.scene = HighLogic.LoadedScene;
                bsb.blocktype = Blocktype.hideEverywhere;
                bsb.origButton = ClickedButton;
                bsb.buttonTexture = GetButtonTexture(ClickedButton.sprite);


                if (ClickedButton.gameObject.activeSelf)
                    ClickedButton.gameObject.SetActive(false);
                if (ClickedButton.enabled)
                    ClickedButton.onDisable();

                primaryButtonBlockList.Add(bsb.buttonHash, bsb);
                allBlockedButtonsList.Add(bsb.buttonHash, bsb);
                showToolbarMenu = ShowMenuState.hiding;
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
                    addToButtonBlockList(newToolbarFolderButton.buttonBlockList, ClickedButton);

                    if (ClickedButton.gameObject.activeSelf)
                        ClickedButton.gameObject.SetActive(false);
                    if (ClickedButton.enabled)
                        ClickedButton.onDisable();

                    saveButtonData();
                    return;
                }
            }

            //int cnt = 0;
            foreach (var bb in buttonBarList[curScene])
            {
                
                if (GUILayout.Button(new GUIContent("Move to folder", GameDatabase.Instance.GetTexture(TexturePath + "20_" + folderIcons[bb.Value.folderIcon], false)), GUILayout.Height(22)))
                {
                    addToButtonBlockList(bb.Value.buttonBlockList, ClickedButton);

                    if (ClickedButton.gameObject.activeSelf)
                        ClickedButton.gameObject.SetActive(false);
                    if (ClickedButton.enabled)
                        ClickedButton.onDisable();

                    saveButtonData();
                    return;
                }
            }

        }

        int toolbarMenuRectID;
        int toolbarRectID;

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
                (showToolbarMenu == ShowMenuState.visible && (Time.fixedTime - lastTimeShown < 2.5f || toolbarMenuRect.Contains(Event.current.mousePosition)))
                )
                KSPUtil.ClampRectToScreen(GUILayout.Window(toolbarMenuRectID, toolbarMenuRect, toolbarWindowFunction, "Blocker Menu"));
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
               
                GUI.Window(toolbarRectID, toolbarRect, JCToolBar, (string)null, gs);
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
            foreach (var curButton in activeButtonBlockList)
            {
                if (curButton.Value.blocktype == Blocktype.hideEverywhere ||
                    curButton.Value.scene == HighLogic.LoadedScene)
                {
                    Rect brect;
                    if (!ApplicationLauncher.Instance.IsPositionedAtTop)
                        brect = new Rect(0, 41 * cnt, 41, 41);
                    else
                        brect = new Rect(41 * cnt, 0, 41, 41);


                    Log.Info("scene: " + HighLogic.LoadedScene.ToString() + "   cnt: " + cnt.ToString() + "   brect, x,y: " + brect.x.ToString() + ", " + brect.y.ToString() + "   width, height: " + brect.width.ToString() + ", " + brect.height.ToString());

                    cnt++;

                    // In case original button texture is changed

                   // if (curButton.Value.origButton.sprite.texture.name != "TextureReplacer/Plugins/AppIcon" &&
                   //     curButton.Value.origButton.sprite.texture.name != "Chatterer/Textures/chatterer_button_idle")
                    {
                        if (curButton.Value.buttonTexture != GetButtonTexture(curButton.Value.origButton.sprite))
                            curButton.Value.buttonTexture = GetButtonTexture(curButton.Value.origButton.sprite);
                    }
                    if (GUI.Button(brect, curButton.Value.buttonTexture as Texture /* , GUILayout.Width(41), GUILayout.Height(41)*/))
                    {
                        Log.Info("Clicking, keyCode: " + Event.current.keyCode.ToString());

                        if (Input.GetMouseButtonUp(0))
                        {
                            Log.Info("Mouse0");
                            Log.Info("curButton.Value.origButton.toggleButton.CurState: " + curButton.Value.origButton.toggleButton.CurrentState.ToString());
                            Log.Info("curButton.Value.origButton.toggleButton.CurState: " + curButton.Value.origButton.IsEnabled.ToString());
                            Log.Info("curButton.Value.origButton.toggleButton.CurState: " + curButton.Value.origButton.toggleButton.CurrentState.ToString());

                            Log.Info("curButton.Value.origButton.toggleButton.Value: " + curButton.Value.origButton.toggleButton.Value.ToString());
                            Log.Info("curButton.Value.origButton.toggleButton.StartState: " + curButton.Value.origButton.toggleButton.StartState.ToString());

                            if (curButton.Value.active)
                            //                            if (curButton.Value.origButton.toggleButton.Value == true)
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
                            if (!Input.GetKey(GameSettings.MODIFIER_KEY.primary) && !Input.GetKey(GameSettings.MODIFIER_KEY.secondary))
                            {
                                if (curButton.Value.active)
                                //                                if (curButton.Value.origButton.toggleButton.Value == true)
                                {
                                    curButton.Value.origButton.onFalse();
                                    curButton.Value.active = false;
                                }
                                else
                                {
                                    curButton.Value.origButton.onTrue();
                                    curButton.Value.active = true;
                                }
                                curButton.Value.origButton.onRightClick();
                            }
                            else
                                toRevert = curButton.Value;
                        }
                    }
                    if (IsMouseOver(brect))
                    {
                        if (!mouseOver)
                        {
                            mouseOver = true;
                            curButton.Value.origButton.onHover();
                            curButton.Value.origButton.onHoverBtn(curButton.Value.origButton.toggleButton);
                            curButton.Value.origButton.onHoverBtnActive(curButton.Value.origButton.toggleButton);
                        }
                        else
                        {

                            mouseOver = false;
                            curButton.Value.origButton.onHoverOut();
                            curButton.Value.origButton.onHoverOutBtn(curButton.Value.origButton.toggleButton);
                        }
                    }

                }
            }

            if (toRevert != null)
            {
                Log.Info("Removing hash from activeButtonBlockList: " + toRevert.buttonHash);
                activeButtonBlockList.Remove(toRevert.buttonHash);
                allBlockedButtonsList.Remove(toRevert.buttonHash);

                if (toRevert.blocktype == Blocktype.hideEverywhere || toRevert.blocktype == Blocktype.hideHere)
                {
                    string hash = toRevert.buttonHash;
                    if (toRevert.blocktype == Blocktype.hideHere)
                        hash += toRevert.scene.ToString();

                    primaryButtonBlockList.Remove(hash);
                    allBlockedButtonsList.Remove(hash);
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
            }

        }
    }
}