using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using KSP.UI.Screens;

using static JanitorsCloset.JanitorsClosetLoader;

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
            static List<ApplicationLauncherButton> appListMod;
            static List<ApplicationLauncherButton> appListModHidden;

            Coroutine syncCoroutine;
            const float SyncMaxWaitSeconds = 10f;
            const float SyncIntervalSeconds = 1f;

            static readonly List<ApplicationLauncherButton> scratchButtonList = new List<ApplicationLauncherButton>();

            private void Start()
            {
                if (!ShouldRunToolbarEvents())
                    return;

                DontDestroyOnLoad(this);

                RefreshAppLists();
                RegisterSceneChanges(true);
                GameEvents.onGUIApplicationLauncherReady.Add(OnGUIApplicationLauncherReadyEvent);

                if (ApplicationLauncher.Instance != null)
                    OnGUIApplicationLauncherReadyEvent();
            }

            void OnDestroy()
            {
                GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIApplicationLauncherReadyEvent);
                RegisterSceneChanges(false);
            }

            private bool ShouldRunToolbarEvents()
            {
                return HighLogic.CurrentGame != null
                    && JanitorsCloset.NoIncompatabilities
                    && HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>().toolbarEnabled;
            }

            private bool ShouldRunToolbarSyncInCurrentScene()
            {
                if (!ShouldRunToolbarEvents())
                    return false;

                var settings = HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>();
                if (settings.toolbarEditorOnly && HighLogic.LoadedScene != GameScenes.EDITOR)
                    return false;

                return true;
            }

            private void RegisterSceneChanges(bool enable)
            {
                Log.Info("RegisterSceneChanges: " + enable.ToString());
                if (enable)
                {
                    GameEvents.onGameSceneLoadRequested.Add(this.CallbackGameSceneLoadRequested);
                    GameEvents.onGameStatePostLoad.Add(this.CallbackOnGameStatePostLoad);
                }
                else
                {
                    GameEvents.onGameSceneLoadRequested.Remove(this.CallbackGameSceneLoadRequested);
                    GameEvents.onGameStatePostLoad.Remove(this.CallbackOnGameStatePostLoad);
                }
            }

            void OnEnable()
            {
                SceneManager.sceneLoaded += CallbackLevelWasLoaded;
            }

            void OnDisable()
            {
                SceneManager.sceneLoaded -= CallbackLevelWasLoaded;
            }

            private void CallbackGameSceneLoadRequested(GameScenes scene)
            {
                Log.Info("CallbackGameSceneLoadRequested");
                if (JanitorsCloset.Instance == null)
                {
                    Log.Error("JanitorsCloset.Instance is null");
                    return;
                }
                if (JanitorsCloset.Instance.primaryAppButton == null)
                {
                    Log.Error("JanitorsCloset.Instance.primaryAppButton is null");
                    return;
                }

                JanitorsCloset.Instance.ToolbarHide(false);
                JanitorsCloset.Instance.primaryAppButton.SetFalse();
                if (JanitorsCloset.Instance.activeButtonBlockList != null)
                {
                    foreach (var b in JanitorsCloset.Instance.activeButtonBlockList)
                    {
                        if (b.Value.origButton != null)
                        {
                            Log.Info("origbutton: " + b.Value.origButton.enabled.ToString());
                            b.Value.active = false;
                        }
                    }
                }
            }

            private void CallbackOnGameStatePostLoad(ConfigNode n)
            {
                Log.Info("CallbackOnGameStatePostLoad");
                OnToolbarEnvironmentChanged();
            }

            private void CallbackLevelWasLoaded(Scene scene, LoadSceneMode mode)
            {
                Log.Info("CallbackLevelWasLoaded");
                OnToolbarEnvironmentChanged();
            }

            private void OnGUIApplicationLauncherReadyEvent()
            {
                OnToolbarEnvironmentChanged();
            }

            private void OnToolbarEnvironmentChanged()
            {
                if (!ShouldRunToolbarSyncInCurrentScene())
                    return;

                RefreshAppLists();
                // Always install click handlers so Alt+RightClick hide menu works
                // even when no buttons have been hidden/folded yet.
                InstallMissingHandlers();
                RequestToolbarSync();
            }

            private void RequestToolbarSync()
            {
                if (!ShouldRunToolbarSyncInCurrentScene())
                    return;

                if (syncCoroutine != null)
                    StopCoroutine(syncCoroutine);

                syncCoroutine = StartCoroutine(SyncToolbarAfterSceneChange());
            }

            private IEnumerator SyncToolbarAfterSceneChange()
            {
                float elapsed = 0f;

                while (elapsed < SyncMaxWaitSeconds)
                {
                    if (!ShouldRunToolbarSyncInCurrentScene())
                        yield break;

                    RefreshAppLists();
                    // Keep installing handlers for late-appearing toolbar buttons.
                    InstallMissingHandlers();

                    // Heavy hide/folder sync only when there is saved customization work.
                    if (HasToolbarCustomizationData() && HasPendingToolbarWork())
                    {
                        UpdateButtonDictionary();
                        CheckToolbarButtons();
                    }

                    yield return new WaitForSeconds(SyncIntervalSeconds);
                    elapsed += SyncIntervalSeconds;
                }

                syncCoroutine = null;
            }

            static bool HasToolbarCustomizationData()
            {
                if (JanitorsCloset.loadedCfgs != null && JanitorsCloset.loadedCfgs.Count > 0)
                    return true;
                if (JanitorsCloset.loadedHiddenCfgs != null && JanitorsCloset.loadedHiddenCfgs.Count > 0)
                    return true;
                if (JanitorsCloset.primaryButtonBlockList != null && JanitorsCloset.primaryButtonBlockList.Count > 0)
                    return true;

                if (JanitorsCloset.buttonBarList != null)
                {
                    for (int i = 0; i <= (int)GameScenes.PSYSTEM; i++)
                    {
                        if (JanitorsCloset.buttonBarList[i].Count > 0)
                            return true;
                    }
                }

                if (JanitorsCloset.hiddenButtonBlockList != null)
                {
                    for (int i = 0; i <= (int)GameScenes.PSYSTEM; i++)
                    {
                        if (JanitorsCloset.hiddenButtonBlockList[i].Count > 0)
                            return true;
                    }
                }

                return false;
            }

            static bool HasPendingToolbarWork()
            {
                if (!HasToolbarCustomizationData())
                    return false;

                var scene = HighLogic.LoadedScene;
                var sceneStr = scene.ToString();

                if (JanitorsCloset.loadedCfgs != null)
                {
                    foreach (var key in JanitorsCloset.loadedCfgs.Keys)
                    {
                        if (key.StartsWith(sceneStr))
                            return true;
                    }
                }

                if (JanitorsCloset.loadedHiddenCfgs != null)
                {
                    foreach (var kvp in JanitorsCloset.loadedHiddenCfgs)
                    {
                        if (kvp.Value.blocktype == Blocktype.hideEverywhere)
                            return true;
                        if (kvp.Value.scene == scene)
                            return true;
                    }
                }

                return HasUnresolvedBlockListEntries();
            }

            static bool HasUnresolvedBlockListEntries()
            {
                if (JanitorsCloset.Instance == null)
                    return false;

                int sceneIdx = (int)HighLogic.LoadedScene;

                if (JanitorsCloset.hiddenButtonBlockList != null)
                {
                    foreach (int i in new[] { 0, sceneIdx })
                    {
                        if (JanitorsCloset.hiddenButtonBlockList[i] == null)
                            continue;

                        foreach (var entry in JanitorsCloset.hiddenButtonBlockList[i])
                        {
                            if (entry.Value.origButton == null)
                                return true;
                        }
                    }
                }

                if (JanitorsCloset.buttonBarList != null && JanitorsCloset.buttonBarList[sceneIdx] != null)
                {
                    foreach (var folder in JanitorsCloset.buttonBarList[sceneIdx])
                    {
                        foreach (var block in folder.Value.buttonBlockList)
                        {
                            if (block.Value.origButton == null)
                                return true;
                        }
                    }
                }

                if (JanitorsCloset.primaryButtonBlockList != null)
                {
                    foreach (var block in JanitorsCloset.primaryButtonBlockList)
                    {
                        if (block.Value.origButton == null)
                            return true;
                    }
                }

                return false;
            }

            private void RefreshAppLists()
            {
                if (ApplicationLauncher.Instance == null)
                    return;

                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var launcherType = typeof(ApplicationLauncher);
                appListMod = (List<ApplicationLauncherButton>)launcherType.GetField("appListMod", flags).GetValue(ApplicationLauncher.Instance);
                appListModHidden = (List<ApplicationLauncherButton>)launcherType.GetField("appListModHidden", flags).GetValue(ApplicationLauncher.Instance);
            }

            /// <summary>
            /// A dictionary of all buttons.  When a new button is found on the toolbar, the dictionary is searched, first
            /// for the button itself,  and then the hash.  If neither is found, then it is added here.  If the hash is found on a 
            /// different button, the old button is deleted and the new one is added
            /// </summary>
            void updateButtonDictionary(List<ApplicationLauncherButton> buttons)
            {
                if (buttons == null)
                {
                    Log.Info("appListMod == null");
                    return;
                }

                foreach (var a1 in buttons)
                {
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

                    if (a1.gameObject.GetComponent<ReplacementToolbarClickHandler>() == null)
                    {
                        Log.Info("button missing ReplacementToolbarClickHandler");
                        return;
                    }

                    if (!JanitorsCloset.buttonDictionary.ContainsKey(a1))
                    {
                        Log.Info("Not in buttonDictionary");
                        string hash = JanitorsCloset.Instance.buttonId(a1);
                        ButtonDictionaryItem bdi = new ButtonDictionaryItem();
                        bdi.buttonHash = hash;

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
                            Log.Info("hash found, deleting entry, hash: " + hash);
                            foreach (var i in JanitorsCloset.buttonDictionary)
                                Log.Info("buttonDictionaryHash: " + i.Value.buttonHash);

                            ApplicationLauncherButton keyToRemove = null;
                            foreach (var i in JanitorsCloset.buttonDictionary)
                            {
                                if (i.Value.buttonHash == hash)
                                {
                                    keyToRemove = i.Key;
                                    bdi.identifier = i.Value.identifier;
                                    break;
                                }
                            }

                            if (keyToRemove != null)
                            {
                                Log.Info("deleting key from buttonDictionary");
                                JanitorsCloset.buttonDictionary.Remove(keyToRemove);
                            }

                            bdi.button = a1;
                            JanitorsCloset.buttonDictionary.Add(a1, bdi);
                        }
                    }
                }
            }

            private void UpdateButtonDictionary()
            {
                updateButtonDictionary(appListMod);
                updateButtonDictionary(appListModHidden);
                for (int i = 0; i <= (int)GameScenes.PSYSTEM; i++)
                    UpdateButtonDictionaryForHiddenList(i);
            }

            private void UpdateButtonDictionaryForHiddenList(int listIndex)
            {
                scratchButtonList.Clear();
                foreach (var entry in JanitorsCloset.hiddenButtonBlockList[listIndex])
                {
                    if (entry.Value.origButton != null)
                        scratchButtonList.Add(entry.Value.origButton);
                }
                updateButtonDictionary(scratchButtonList);
            }

            void InstallMissingHandlers()
            {
                Log.Info("InstallToolIconEvents.InstallMissingHandlers, scene: " + HighLogic.LoadedScene.ToString());

                if (appListMod == null || appListModHidden == null)
                    return;

                try
                {
                    foreach (var icon in appListMod)
                    {
                        if (JanitorsCloset.blacklistIcons != null)
                        {
                            if (JanitorsCloset.blacklistIcons.ContainsKey(icon.sprite.texture.name) || JanitorsCloset.blacklistIcons.ContainsKey(JanitorsCloset.Instance.Button32hash(icon.sprite)))
                            {
                                Log.Info("Icon blacklisted in InstallMissingHandlers: " + icon.sprite.texture.name);
                            }
                            else
                            {
                                InstallReplacementToolbarHandler(icon);
                                Log.Info("appListMod, icon.name: " + icon.sprite.texture.name + "    Hash: " + JanitorsCloset.Instance.Button32hash(icon.sprite));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("InstallMissingHandlers, 1, exception: " + e.Message);
                }

                try
                {
                    foreach (var icon in appListModHidden)
                    {
                        if (icon.sprite.texture != null)
                        {
                            InstallReplacementToolbarHandler(icon);
                            Log.Info("appListModHidden, icon.name: " + icon.sprite.texture.name + "     Hash: " + JanitorsCloset.Instance.Button32hash(icon.sprite));
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("InstallMissingHandlers, 2, exception: " + e.Message);
                }
            }

            void CheckToolbarButtons()
            {
                // First look for buttons which are not visible and add them to the buttonsToModify list
                // then go through the list to hid them
                // This is needed in case the same button is hidden on multiple screens, otherwise we
                // get an exception error
                List<ButtonSceneBlock> buttonsToModify = new List<ButtonSceneBlock>();

                Log.Info("CheckToolbarbuttons, LoadedScene: " + HighLogic.LoadedScene.ToString());
                Log.Info("buttons in scene: " + JanitorsCloset.buttonBarList[(int)HighLogic.LoadedScene].Count.ToString());

                ButtonSceneBlock s;
                string sceneStr = HighLogic.LoadedScene.ToString();

                bool done = true;
                do
                {
                    done = true;
                    foreach (var a1 in appListMod)
                    {
                        string spriteHash = JanitorsCloset.Instance.Button32hash(a1.sprite);
                        Log.Info("a1 hash: " + spriteHash);

                        Cfg cfg;
                        if (JanitorsCloset.loadedCfgs.TryGetValue(sceneStr + spriteHash, out cfg))
                        {
                            Log.Info("button to folder found in save file");
                            ButtonBarItem bbi;
                            if (!JanitorsCloset.buttonBarList[(int)cfg.scene].TryGetValue(cfg.toolbarButtonHash, out bbi))
                            {
                                bbi = JanitorsCloset.Instance.AddAdditionalToolbarButton(cfg.toolbarButtonIndex, cfg.scene);
                                if (bbi == null)
                                    return;
                            }

                            if (JanitorsCloset.Instance.addToButtonBlockList(bbi.buttonBlockList, a1))
                            {
                                Log.Info("Added button to toolbar, buttonHash: " + bbi.buttonHash + "   a1.buttonId: " + JanitorsCloset.Instance.buttonId(a1));

                                JanitorsCloset.loadedCfgs.Remove(sceneStr + spriteHash);
                                done = false;
                            }
                            else
                            {
                                Log.Error("Error adding to button block list");
                            }
                            break;
                        }

                        if (JanitorsCloset.loadedHiddenCfgs.TryGetValue(spriteHash + sceneStr, out s))
                        {
                            Log.Info("Button hidden, scene found in save file: " + spriteHash + sceneStr);
                            JanitorsCloset.Instance.addToHiddenBlockList(a1, Blocktype.hideHere);

                            JanitorsCloset.loadedHiddenCfgs.Remove(spriteHash + sceneStr);
                        }
                        else if (JanitorsCloset.loadedHiddenCfgs.TryGetValue(spriteHash, out s))
                        {
                            Log.Info("Button hidden, everywhere found in save file: " + spriteHash);
                            JanitorsCloset.Instance.addToHiddenBlockList(a1, Blocktype.hideEverywhere);

                            JanitorsCloset.loadedHiddenCfgs.Remove(spriteHash);
                        }
                    }
                } while (!done);

                foreach (var a1 in appListMod)
                {
                    string buttonId = JanitorsCloset.Instance.buttonId(a1);
                    string buttonIdWithScene = buttonId + sceneStr;

                    foreach (var bbl in JanitorsCloset.buttonBarList[(int)HighLogic.LoadedScene])
                    {
                        if (a1 != bbl.Value.button)
                        {
                            if (bbl.Value.buttonBlockList.TryGetValue(buttonId, out s) ||
                                JanitorsCloset.primaryButtonBlockList.TryGetValue(buttonId, out s) ||
                                JanitorsCloset.primaryButtonBlockList.TryGetValue(buttonIdWithScene, out s)
                                )
                            {
                                s.origButton = a1;
                                buttonsToModify.Add(s);
                            }
                            else
                            {
                                Log.Info("Button not found to remove from toolbar, hash: " + buttonId);
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
                    UpdateButtonDictionaryForHiddenList(i);
                    foreach (var bbl in JanitorsCloset.hiddenButtonBlockList[i])
                    {
                        ApplicationLauncherButton matched = null;
                        int matchCount = 0;
                        foreach (var b in appListMod)
                        {
                            if (JanitorsCloset.Instance.buttonId(b) == bbl.Key)
                            {
                                matched = b;
                                matchCount++;
                            }
                        }

                        Log.Info("hiddenButtonBlockList[" + i.ToString() + ", sl count: " + matchCount.ToString());

                        if (matchCount > 1)
                        {
                            Log.Error("Multiple identical buttons found in toolbar");
                            foreach (var b in appListMod)
                            {
                                if (JanitorsCloset.Instance.buttonId(b) == bbl.Key)
                                    Log.Info("sli.buttonhash: " + JanitorsCloset.Instance.buttonId(b));
                            }
                        }
                        if (matchCount == 1)
                        {
                            s = bbl.Value;
                            s.origButton = matched;
                            buttonsToModify.Add(s);
                        }
                        if (matchCount == 0)
                        {
                            Log.Info("Hidden Button not found to remove from toolbar, hash: " + bbl.Key);
                            foreach (var v in JanitorsCloset.hiddenButtonBlockList[i])
                                Log.Info("hiddenButtonBlockList hash: " + v.Value.buttonHash);

                            foreach (var v in JanitorsCloset.primaryButtonBlockList)
                                Log.Info("primaryButtonBlockList hash: " + v.Value.buttonHash);
                            foreach (var v in appListMod)
                                Log.Info("appListMod hash: " + JanitorsCloset.Instance.buttonId(v));
                        }
                    }
                }

                foreach (var btm in buttonsToModify)
                {
                    if (btm.origButton.gameObject.activeSelf)
                        btm.origButton.gameObject.SetActive(false);
                    if (btm.origButton.enabled)
                        btm.origButton.onDisable();
                }

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
                if (!ExtendedInput.GetKey(GameSettings.MODIFIER_KEY.primary))
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
                if (HighLogic.CurrentGame == null || !JanitorsCloset.NoIncompatabilities)
                    return;

                var settings = HighLogic.CurrentGame.Parameters.CustomParams<JanitorsClosetSettings>();
                if (!settings.toolbarEnabled)
                    return;
                if (settings.toolbarEditorOnly && HighLogic.LoadedScene != GameScenes.EDITOR)
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
