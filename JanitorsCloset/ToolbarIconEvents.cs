using System;
using System.Collections;
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
    //
    // Following code contributed by the awesum xEvilReeperx
    //
    public static class ToolbarIconEvents
    {
        public static readonly EventData<ApplicationLauncherButton> OnToolbarIconClicked =
            new EventData<ApplicationLauncherButton>("ToolbarIconClicked");


        [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
        private class InstallToolIconEvents : MonoBehaviour
        {
            GameScenes lastScene = GameScenes.MAINMENU;
            double lastTime = 0;

            //static List<ApplicationLauncherButton> appList;
            //static List<ApplicationLauncherButton> appListHidden;
            static List<ApplicationLauncherButton> appListMod;
            static List<ApplicationLauncherButton> appListModHidden;
            int appListModCount, appListModHiddenCount;


            private void Start()
            {
                if (!JanitorsCloset.NoIncompatabilities || !HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().toolbarEnabled)
                    return;
                // GameEvents.onLevelWasLoadedGUIReady.Add(OnSceneLoadedGUIReady);
                // GameEvents.onGUIApplicationLauncherReady.Add(OnGUIApplicationLauncherReady);
                DontDestroyOnLoad(this);

                //appList = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
                //appListHidden = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appListHidden", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
                appListMod = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appListMod", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
                appListModHidden = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appListModHidden", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
                RegisterSceneChanges(true);
            }

            private void RegisterSceneChanges(bool enable)
            {
                Log.Info("RegisterSceneChanges: " + enable.ToString());
                if (enable)
                {
                    GameEvents.onGameSceneLoadRequested.Add(this.CallbackGameSceneLoadRequested);
                    GameEvents.onLevelWasLoaded.Add(this.CallbackLevelWasLoaded);
                }
                else
                {
                    GameEvents.onGameSceneLoadRequested.Remove(this.CallbackGameSceneLoadRequested);
                    GameEvents.onLevelWasLoaded.Remove(this.CallbackLevelWasLoaded);
                }
            }
            private void CallbackGameSceneLoadRequested(GameScenes scene)
            {
                Log.Info("CallbackGameSceneLoadRequested");
                JanitorsCloset.Instance.ToolbarHide(false);
                JanitorsCloset.Instance.primaryAppButton.SetFalse();
                if (JanitorsCloset.Instance.activeButtonBlockList != null)
                {
                    foreach (var b in JanitorsCloset.Instance.activeButtonBlockList)
                    {
                        Log.Info("origbutton: " + b.Value.origButton.enabled.ToString());
                        b.Value.origButton.onFalse();
                        b.Value.active = false;

                    }
                }
            }

            private void CallbackLevelWasLoaded(GameScenes scene)
            {
                lasttimecheck = 0;
                Log.Info("CallbackLevelWasLoaded");
               
            }

            bool mapIsEnabled = false;
            double lasttimecheck = 0;
            private void FixedUpdate()
            {
                if (!JanitorsCloset.NoIncompatabilities || !HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().toolbarEnabled)
                    return;

                if (HighLogic.LoadedScene != lastScene)
                {
                    //Log.Info("InstallToolIconEvents.FixedUpdate, new scene: " + HighLogic.LoadedScene.ToString());
                    lastScene = HighLogic.LoadedScene;
                    lastTime = Time.fixedTime;
                    appListModCount = 0;
                    appListModHiddenCount = 0;
                    if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                        mapIsEnabled = MapView.MapIsEnabled;
                }
                
                if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    if (mapIsEnabled != MapView.MapIsEnabled)
                    {
                        lastTime = Time.fixedTime;
                        appListModCount = 0;
                        appListModHiddenCount = 0;
                        mapIsEnabled = MapView.MapIsEnabled;
                    }

                }
                // Keep checking for 10 seconds to be sure all mods have finished installinng their buttons
                // for performance, don't do it more than once a second
                if (Time.fixedTime - lastTime < 10)
                {
                    if (Time.fixedTime - lasttimecheck > 1)
                    {
                        lasttimecheck = Time.fixedTime;

                        OnGUIApplicationLauncherReady();
                        UpdateButtonDictionary();

                        CheckToolbarButtons();
                    }
                }
            }

            /// <summary>
            /// A dictionary of all buttons.  When a new button is found on the toolbar, the dictionary is searched, first
            /// for the button itself,  and then the hash.  If neither is found, then it is added here.  If the hash is found on a 
            /// different button, the old button is deleted and the new one is added
            /// </summary>
            void updateButtonDictionary(List<ApplicationLauncherButton> appListMod)
            {
                int cnt = 0;
                if (appListMod == null)
                {
                    Log.Info("appListMod == null");
                    return;
                }
                foreach (var a1 in appListMod)
                {
                    cnt++;
                    if (a1 == null)
                    {
                        Log.Info("a1 == null");
                        continue;
                    }
                    if (a1.gameObject == null)
                    {
                        Log.Info("gameObject == null");
                        continue;
                    }
                    var o = a1.gameObject.GetComponent<ReplacementToolbarClickHandler>();

                    if (o == null)
                    {
                        Log.Error("button missing ReplacementToolbarClickHandler");
                        return;
                    }


                    if (!JanitorsCloset.buttonDictionary.ContainsKey(a1))
                    {
                        Log.Info("Not in buttonDictionary");
                        string hash = JanitorsCloset.buttonId(a1);
                        bool doThisOne = true;
                        Log.Info("Checking buttonBarList");
                        foreach (var z in JanitorsCloset.buttonBarList)
                        {
                            if (z.ContainsKey(hash))
                            {
                                doThisOne = false;
                                break;
                            }
                        }

                        if (doThisOne)
                        {
                            if (JanitorsCloset.buttonDictionary.ContainsValue(hash))
                            {
                                Log.Info("hash found, deleting entry");
                                var key = JanitorsCloset.buttonDictionary.FirstOrDefault(m => m.Value == hash).Key;
                                JanitorsCloset.buttonDictionary.Remove(key);
                            }
                            JanitorsCloset.buttonDictionary.Add(a1, hash);
                        }
                    }
                }
            }

            // public static Dictionary<ApplicationLauncherButton, string> buttonDictionary = new Dictionary<ApplicationLauncherButton, string>();
            private void UpdateButtonDictionary()
            {
                updateButtonDictionary(appListMod);
                updateButtonDictionary(appListModHidden);
                for (int i = 0; i <= (int)GameScenes.PSYSTEM; i++)
                    updateButtonDictionary(JanitorsCloset.hiddenButtonBlockList[i].Select(i1 => i1.Value.origButton).ToList());
            }

            void OnGUIApplicationLauncherReady()
            {
                //Log.Info("InstallToolIconEvents.OnSceneLoadedGUIReady,scene: " + scene.ToString());

                //List<ApplicationLauncherButton> appListMod = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appListMod", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
                //List<ApplicationLauncherButton> appListModHidden = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appListModHidden", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);

                // some icons have already been instantiated, need to fix those too. Only needed this first time;
                // after that, the prefab will already contain the changes we want to make
                foreach (var icon in appListMod)
                {
                    InstallReplacementToolbarHandler(icon);
                    Log.Info("appListMod, icon.name: " + icon.sprite.texture.name + "    Hash: " + JanitorsCloset.Button32hash(icon.sprite));
                }
                foreach (var icon in appListModHidden)
                {
                    InstallReplacementToolbarHandler(icon);
                    Log.Info("appListModHidden, icon.name: " + icon.sprite.texture.name + "     Hash: " + JanitorsCloset.Button32hash(icon.sprite));
                }

            }

            /// <summary>
            /// 
            /// </summary>
            void CheckToolbarButtons()
            {
                // First look for buttons which are not visible and add them to the buttonsToModify list
                // then go through the list to hid them
                // This is needed in case the same button is hidden on multiple screens, otherwise we
                // get an exception error
                List<ButtonSceneBlock> buttonsToModify = new List<ButtonSceneBlock>();

                Log.Info("CheckToolbarbuttons, LoadedScene: " + HighLogic.LoadedScene.ToString());
                Log.Info("buttons in scene: " + JanitorsCloset.buttonBarList[(int)HighLogic.LoadedScene].Count.ToString());

                // foreach (var bbl in JanitorsCloset.buttonBarList[(int)HighLogic.LoadedScene])
                //     Log.Info("bbl hashkey: " + bbl.Key);

                ButtonSceneBlock s;

                bool done = true;
                do
                {
                    done = true;
                    foreach (var a1 in appListMod)
                    {
                        Log.Info("a1 hash: " + JanitorsCloset.Button32hash(a1.sprite));

                        // foreach (var lc in JanitorsCloset.loadedCfgs)
                        {
                            Cfg cfg;

                            if (JanitorsCloset.loadedCfgs.TryGetValue(HighLogic.LoadedScene.ToString() + JanitorsCloset.Button32hash(a1.sprite), out cfg))
                            {
                                Log.Info("button to folder found in save file");
                                ButtonBarItem bbi;
                                if (!JanitorsCloset.buttonBarList[(int)cfg.scene].TryGetValue(cfg.toolbarButtonHash, out bbi))
                                {
                                    bbi = JanitorsCloset.Instance.AddAdditionalToolbarButton(cfg.toolbarButtonIndex, cfg.scene);
                                }
                                JanitorsCloset.Instance.addToButtonBlockList(bbi.buttonBlockList, a1);


                                Log.Info("Added button to toolbar, buttonHash: " + bbi.buttonHash + "   a1.buttonId: " + JanitorsCloset.buttonId(a1));

                                JanitorsCloset.loadedCfgs.Remove(HighLogic.LoadedScene.ToString() + JanitorsCloset.Button32hash(a1.sprite));
                                done = false;
                                break;
                            }
                            if (JanitorsCloset.loadedHiddenCfgs.TryGetValue(JanitorsCloset.Button32hash(a1.sprite) + HighLogic.LoadedScene.ToString(), out s))
                            {
                                Log.Info("Button hidden, scene found in save file: " + JanitorsCloset.Button32hash(a1.sprite) + HighLogic.LoadedScene.ToString());
                                JanitorsCloset.Instance.addToHiddenBlockList(a1, Blocktype.hideHere);

                                JanitorsCloset.loadedHiddenCfgs.Remove(JanitorsCloset.Button32hash(a1.sprite) + HighLogic.LoadedScene.ToString());
                            }
                            else
                            {
                                if (JanitorsCloset.loadedHiddenCfgs.TryGetValue(JanitorsCloset.Button32hash(a1.sprite), out s))
                                {
                                    Log.Info("Button hidden, everywhere found in save file: " + JanitorsCloset.Button32hash(a1.sprite));
                                    JanitorsCloset.Instance.addToHiddenBlockList(a1, Blocktype.hideEverywhere);

                                    JanitorsCloset.loadedHiddenCfgs.Remove(JanitorsCloset.Button32hash(a1.sprite));
                                }
                            }
                        }
                    }
                } while (!done);

                //  for (int i = 0; i <= (int)GameScenes.PSYSTEM; i++)
                //      updateButtonDictionary(JanitorsCloset.hiddenButtonBlockList[i].Select(i1 => i1.Value.origButton).ToList());

                foreach (var a1 in appListMod)
                {
                    foreach (var bbl in JanitorsCloset.buttonBarList[(int)HighLogic.LoadedScene])
                    {
                        if (a1 != bbl.Value.button)
                        {
                            if (bbl.Value.buttonBlockList.TryGetValue(JanitorsCloset.buttonId(a1), out s) ||
                                JanitorsCloset.primaryButtonBlockList.TryGetValue(JanitorsCloset.buttonId(a1), out s) ||
                                JanitorsCloset.primaryButtonBlockList.TryGetValue(JanitorsCloset.buttonId(a1) + HighLogic.LoadedScene.ToString(), out s)
                                )
                            {
                                s.origButton = a1;
                                buttonsToModify.Add(s);
                            }
                            else
                            {
                                Log.Info("Button not found to remove from toolbar, hash: " + JanitorsCloset.buttonId(a1));
                                foreach (var v in bbl.Value.buttonBlockList)
                                {
                                    Log.Info("buttonBlockList hash: " + v.Value.buttonHash);
                                }
                                foreach (var v in JanitorsCloset.primaryButtonBlockList)
                                {
                                    Log.Info("primaryButtonBlockList hash: " + v.Value.buttonHash);
                                }
                            }
                        }
                    }
                }

                int[] il = { 0, (int)JanitorsCloset.appScene };
                foreach (int i in il)
                {
                    updateButtonDictionary(JanitorsCloset.hiddenButtonBlockList[i].Select(i1 => i1.Value.origButton).ToList());
                    foreach (var bbl in JanitorsCloset.hiddenButtonBlockList[i])
                    {
                        var sl = appListMod.Where(b => JanitorsCloset.buttonId(b) == bbl.Key).ToList();
                        Log.Info("sl count: " + sl.Count().ToString());

                        if (sl.Count > 1)
                        {
                            Log.Error("Multiple identical buttons found in toolbar");
                            foreach (var sli in sl)
                            {
                                Log.Info("sli.buttonhash: " + JanitorsCloset.buttonId(sli));
                            }
                        }
                        if (sl.Count == 1)
                        {
                            s = bbl.Value;
                            s.origButton = sl.First();
                            buttonsToModify.Add(s);
                        }
                        if (sl.Count == 0)
                        {
                            Log.Info("Hidden Button not found to remove from toolbar, hash: " + bbl.Key);
                            foreach (var v in JanitorsCloset.hiddenButtonBlockList[i])
                                Log.Info("hiddenButtonBlockList hash: " + v.Value.buttonHash);

                            foreach (var v in JanitorsCloset.primaryButtonBlockList)
                                Log.Info("primaryButtonBlockList hash: " + v.Value.buttonHash);
                            foreach (var v in appListMod)
                                Log.Info("appListMod hash: " + JanitorsCloset.buttonId(v));

                        }
                    }
                }

                foreach (var btm in buttonsToModify)
                {

                    if (btm.origButton.gameObject.activeSelf)
                        btm.origButton.gameObject.SetActive(false);
                    if (btm.origButton.enabled)
                        btm.origButton.onDisable();
                };

                // following fixes a stock bug
                foreach (var a1 in appListModHidden)
                {
                    if (a1.gameObject.activeSelf)
                        a1.gameObject.SetActive(false);
                    if (a1.enabled)
                        a1.onDisable();
                }
            }

            private static void InstallReplacementToolbarHandler(ApplicationLauncherButton icon)
            {

                if (icon.gameObject.GetComponent<ReplacementToolbarClickHandler>() == null)
                    icon.gameObject.AddComponent<ReplacementToolbarClickHandler>();
            }
        }

        public class ReplacementToolbarClickHandler : MonoBehaviour
        {
            private ApplicationLauncherButton appButton;

            public Callback savedHandler = delegate
            { };

            void onRightClick()
            {
                Log.Info("ToolbarIconEvents.OnRightClick");
                if (!Input.GetKey(GameSettings.MODIFIER_KEY.primary))
                {
                    Log.Info("Calling savedHandler");
                    savedHandler();
                }
                else
                {
                    Log.Info("Mod key pressed, not calling savedHandler");

                    OnToolbarIconClicked.Fire(appButton);
                }
            }

            private void Start()
            {
                if (!JanitorsCloset.NoIncompatabilities || !HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().toolbarEnabled)
                    return;

                appButton = GetComponent<ApplicationLauncherButton>();

                if (appButton == null)
                {
                    Log.Error("Couldn't find an expected component");
                    Destroy(this);
                    return;
                }

                savedHandler = appButton.onRightClick;
                appButton.onRightClick = onRightClick;

            }
        }
    }
}
