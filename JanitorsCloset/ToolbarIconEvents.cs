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
                if (!JanitorsCloset.NoIncompatabilities)
                    return;
                // GameEvents.onLevelWasLoadedGUIReady.Add(OnSceneLoadedGUIReady);
                // GameEvents.onGUIApplicationLauncherReady.Add(OnGUIApplicationLauncherReady);
                DontDestroyOnLoad(this);

                //appList = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
                //appListHidden = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appListHidden", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
                appListMod = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appListMod", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
                appListModHidden = (List<ApplicationLauncherButton>)typeof(ApplicationLauncher).GetField("appListModHidden", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ApplicationLauncher.Instance);
            }


            private void FixedUpdate()
            {
                if (!JanitorsCloset.NoIncompatabilities)
                    return;

                if (HighLogic.LoadedScene != lastScene)
                {
                    //Log.Info("InstallToolIconEvents.FixedUpdate, new scene: " + HighLogic.LoadedScene.ToString());
                    lastScene = HighLogic.LoadedScene;
                    lastTime = Time.fixedTime;
                    appListModCount = 0;
                    appListModHiddenCount = 0;
                }
                bool doit = false;
                // Keep checking for 10 seconds to be sure all mods have finished installinng their buttons
                if (Time.fixedTime - lastTime < 10)
                {
                    if (appListModCount != appListMod.Count)
                    {
                        appListModCount = appListMod.Count;
                        doit = true;
                    }
                    if (appListModHiddenCount != appListModHidden.Count)
                    {
                        appListModHiddenCount = appListModHidden.Count;
                        doit = true;
                    }
                    if (doit)
                    {
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
                                Log.Info("button found in save file");
                                ButtonSceneBlock bsb = new ButtonSceneBlock();
                                bsb.scene = cfg.scene;
                                bsb.blocktype = cfg.blocktype;
                                bsb.buttonHash = cfg.buttonHash;
                                bsb.origButton = a1;

                                bsb.buttonTexture = JanitorsCloset.GetButtonTexture(a1.sprite);
                               // var zz = JanitorsCloset.buttonBarList[(int)bsb.scene];
                                ButtonBarItem bbi;
                                if (!JanitorsCloset.buttonBarList[(int)bsb.scene].TryGetValue(cfg.toolbarButtonHash, out bbi))
                                {
                                    bbi = JanitorsCloset.Instance.AddAdditionalToolbarButton(cfg.toolbarButtonIndex, cfg.scene);
                                }

                                bbi.buttonBlockList.Add(bsb.buttonHash, bsb);
                                Log.Info("Added button to toolbar, buttonHash: " + bsb.buttonHash + "   a1.buttonId: " + JanitorsCloset.buttonId(a1));

                                JanitorsCloset.loadedCfgs.Remove(HighLogic.LoadedScene.ToString() + JanitorsCloset.Button32hash(a1.sprite));
                                done = false;
                                break;
                            }
                        }
                    }
                } while (!done);

                foreach (var a1 in appListMod)
                {
                    foreach (var bbl in JanitorsCloset.buttonBarList[(int)HighLogic.LoadedScene])
                    {
                        if (a1 != bbl.Value.button)
                        {
                            if (bbl.Value.buttonBlockList.TryGetValue(JanitorsCloset.buttonId(a1), out s))
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
                            }
                        }
                    }
                }

                foreach (var i in JanitorsCloset.allBlockedButtonsList)
                {
                    //if (i != null)
                    {
                        var v = i.Value;
                        if (v != null && ( v.scene == HighLogic.LoadedScene || v.blocktype == Blocktype.hideEverywhere))
                        {
                            foreach (var a1 in appListMod)
                            {
                                if (a1 != null)
                                {
                                    if (JanitorsCloset.buttonId(a1, false) == v.buttonHash && v.origButton != null && v.origButton.gameObject != null)
                                    {
                                        if (v.origButton.gameObject.activeSelf)
                                            v.origButton.gameObject.SetActive(false);
                                        if (v.origButton.enabled)
                                            v.origButton.onDisable();
                                    }
                                }
                            }
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
            // private Button _button;

            public delegate void Del();
            //Del savedHandler;

            void onRightClick()
            {
                Log.Info("ToolbarIconEvents.OnRightClick");
                OnToolbarIconClicked.Fire(appButton);
            }

            private void Start()
            {
                if (!JanitorsCloset.NoIncompatabilities)
                    return;

                appButton = GetComponent<ApplicationLauncherButton>();

                if (appButton == null)
                {
                    Log.Error("Couldn't find an expected component");
                    Destroy(this);
                    return;
                }                

                Del handler = onRightClick;
                appButton.onRightClick = onRightClick;


            }


        }
    }
}
