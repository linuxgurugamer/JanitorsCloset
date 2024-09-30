using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using UnityEngine;
using EdyCommonTools;
using KSP.UI;
using KSP.UI.Screens;
using ClickThroughFix;

using static JanitorsCloset.JanitorsClosetLoader;


namespace JanitorsCloset
{
    class PermaPruneWindow : MonoBehaviour
    {
        Rect _windowRect = new Rect()
        {
            xMin = Screen.width - 325,
            xMax = Screen.width - 175,
            yMin = Screen.height - 300,
            yMax = 50 //0 height, GUILayout resizes it
        };

        public bool permapruneInProgress = false;
        string _windowTitle = string.Empty;

        public static PermaPruneWindow Instance { get; private set; }

        void Awake()
        {
            Log.Info("PermaPruneWindow Awake()");
            this.enabled = false;
            Instance = this;
        }

        void Start()
        {

        }

        void OnEnable()
        {
            Log.Info("PermaPruneWindow OnEnable()");

        }

        public bool isEnabled()
        {
            return this.enabled;
        }

        void CloseWindow()
        {
            this.enabled = false;
            winState = winContent.menu;
            Log.Info("PermaPruneWindow.CloseWindow enabled: " + this.enabled.ToString());
        }

        void OnDisable()
        {

        }

        enum winContent
        {
            menu,
            permaprune,
            undo,
            dialog,
            close
        }


        int windowContentID = 0;

        winContent winState = winContent.menu;
        MultiOptionDialog dialog;
        void OnGUI()
        {

            if (isEnabled())
            {
                switch (winState)
                {
                    case winContent.menu:
                        if (windowContentID == 0)
                            windowContentID = JanitorsCloset.getNextID();
                        _windowTitle = string.Format("PermaPrune");
                        var tstyle = new GUIStyle(GUI.skin.window);

                        _windowRect.yMax = _windowRect.yMin;
                        _windowRect = ClickThruBlocker.GUILayoutWindow(windowContentID, _windowRect, WindowContent, _windowTitle, tstyle);
                        break;

                    case winContent.permaprune:
                        dialog = new MultiOptionDialog("janitorsToolbar3",
                                                        "This will permanently rename files to prevent them from being loaded", "Permanent Prune", HighLogic.UISkin, new DialogGUIBase[] {
                                                        new DialogGUIButton ("OK", () => {
                                                             winState = winContent.close;
                                                             startPruner();
                                                             // pruner();

                                                        }),
                                                        new DialogGUIButton ("Cancel", () => {
                                                            winState = winContent.close;
                                                        })
                                                });
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog, false, HighLogic.UISkin, true);
                        winState = winContent.dialog;
                        break;

                    case winContent.undo:
                        dialog = new MultiOptionDialog("janitorsToolbar4",
                                                        "This will permanently rename pruned files to allow them to be loaded", "Unprune (restore)", HighLogic.UISkin, new DialogGUIBase[] {
                                                        new DialogGUIButton ("OK", () => {
                                                             unpruner();
                                                             winState = winContent.close;
                                                        }),
                                                        new DialogGUIButton ("Cancel", () => {
                                                            winState = winContent.close;
                                                        })
                                                });
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), dialog, false, HighLogic.UISkin, true);
                        winState = winContent.dialog;
                        break;

                    case winContent.close:
                        CloseWindow();
                        break;
                }

            }
        }

        List<prunedPart> renamedFilesList = null;
        public void RenameFile(string path, string name)
        {
            Log.Info("RenameFile, path: " + path + "    name: " + name);
#if false
            Log.Info("RenameFile, path: " + path + "    name: " + name);
            if (File.Exists(FileOperations.CONFIG_BASE_FOLDER + path))
            {
                if (File.Exists(FileOperations.CONFIG_BASE_FOLDER + path + PRUNED))
                {
                    System.IO.File.Delete(FileOperations.CONFIG_BASE_FOLDER + path + PRUNED);
                }

                Log.Info("Renaming: " + path + "  to  " + path + PRUNED);
                ShowRenamed.Instance.addLine("Renaming: " + path + "  to  " + path + PRUNED);
                System.IO.File.Move(FileOperations.CONFIG_BASE_FOLDER + path, FileOperations.CONFIG_BASE_FOLDER + path + PRUNED);
                prunedPart pp = new prunedPart();
                pp.path = path + PRUNED;
                pp.partName = name;
                renamedFilesList.Add(pp);
            }
#endif 
            prunedPart pp = FileOperations.Instance.RenameFile(path, name);
            if (pp != null)
                renamedFilesList.Add(pp);
        }

        
        public void stopPruner()
        {
            permapruneInProgress = false;
            //StopCoroutine(pruning());
        }
       
        private void startPruner()
        {
            Log.Info("startPruner");

            StartCoroutine(pruning());
        }

        /// <summary>
        /// Get the partURL for the mesh
        /// </summary>
        /// <param name="pSearch"></param>
        /// <returns>string</returns>
        string GetMeshURL(AvailablePart pSearch)
        {
            if (pSearch == null)
            {
                Log.Info("GetMeshURL, pSearch is null");
                return "";
            }
            if (pSearch.partConfig == null)
            {
                Log.Info("GetMeshURL, pSearch.partConfig is null");
                return "";
            }
            string s = pSearch.partConfig.GetValue("mesh");

            if (s != null && s != "")
            {
                string partUrl = pSearch.partUrlConfig.parent.url.Substring(0, pSearch.partUrlConfig.parent.url.LastIndexOf('/'));
                s = partUrl + "/" + s.Substring(0, s.Length - 3);
            }
            return s;
        }
        
        /// <summary>
        /// Get the partURL for the part
        /// </summary>
        /// <param name="part"></param>
        /// <param name="modelNode"></param>
        /// <returns>string</returns>
        string GetModelURL(AvailablePart part, ConfigNode modelNode)
        { 
            string model = modelNode.GetValue("model");

            return model;
        }

        private const string PRUNED = ".prune";
        public  IEnumerator pruning()
        {
            permapruneInProgress = true;

            Log.Info("PermaPrune.pruner");
            renamedFilesList = FileOperations.Instance.loadRenamedFiles();
            Log.Info("sizeof renamedFilesList: " + renamedFilesList.Count.ToString());
            Log.Info("pruner, sizeof blacklist:" + JanitorsCloset.blackList.Count.ToString());
            ShowRenamed.Instance.Show();
            
            List<string> prunedParts = new List<string>();
            foreach (blackListPart blp in JanitorsCloset.blackList.Values)
            {

                yield return 0;
                Log.Info("permapruneInProgress: " + permapruneInProgress.ToString());
                if (!permapruneInProgress)
                    break;
                if (blp.where != blackListType.ALL || blp.permapruned)
                    continue;
                Log.Info("pruned part: " + blp.modName);

                //AvailablePart part = PartLoader.Instance.parts.Find(item => item.name.Equals(blp.modName));
                AvailablePart part = PartLoader.getPartInfoByName(blp.modName);
                if (part == null)
                    continue;
                prunedParts.Add(blp.modName);

                // Rename cfg file

                string s1 = part.configFileFullName.Substring(part.configFileFullName.IndexOf("GameData") + 9);

                RenameFile(s1, part.name);

                string partPath = part.partUrl;

                for (int x = 0; x < 1; x++)
                {
                    int backslash = partPath.LastIndexOf('\\');
                    int slash = partPath.LastIndexOf('/');
                    int i = Math.Max(backslash, slash);
                    partPath = partPath.Substring(0, i);
                }
                partPath += "/";

                // rename resource file
                // Look for model =
                //  model has complete path
                // Look for mesh =
                //      with mesh, get patch from cfg file path

                Log.Info("searching for model");
                Log.Info("Part: " + part.name);

                ConfigNode[] nodes = part.partConfig.GetNodes("MODEL");
                ConfigNode[] nodes2;
                bool b;
#if true
                if (nodes != null)
                {
                    Log.Info("Nodes count: " + nodes.Length.ToString());
                    foreach (ConfigNode modelNode in nodes)
                    {
                        
                        b = false;
                        if (modelNode != null)
                        {
                            Log.Info("modelNode: " + modelNode.name);
                            string model = GetModelURL(part, modelNode);
                            Log.Info("ModelUrl: " + GetModelURL(part, modelNode));
                            if (model != null)
                            {
                                Log.Info("model: " + model);
                                // Make sure it isn't being used in another part
                                b = false;
                                Log.Info("Part count: " + PartLoader.LoadedPartsList.Count.ToString());
                                
                                string s;
                                foreach (AvailablePart pSearch in PartLoader.LoadedPartsList)
                                {
                                    Log.Info("pSearch: " + pSearch.name);
                                    if (part != pSearch && pSearch.partConfig != null)
                                    {
                                        nodes2 = pSearch.partConfig.GetNodes("MODEL");
                                        if (nodes2 != null)
                                        {
                                            Log.Info("nodes2");
                                            foreach (ConfigNode searchNode in nodes2)
                                            {
                                                if (searchNode != null)
                                                {
                                                    Log.Info("searchNode");
                                                    if (model == GetModelURL(pSearch, searchNode))
                                                    {
                                                        b = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        
                                        s = GetMeshURL(pSearch);
                                        Log.Info("Mesh URL: " + GetMeshURL(pSearch) + "   ModelUrl: " + GetModelURL(part, modelNode));
                                       
                                        if (GetMeshURL(pSearch) == model)
                                        {
                                            b = true;
                                            break;
                                        }
                                    }
                                    if (b)
                                        break;
                                }
                            }

                            if (!b)
                            {
                                Log.Info("MODEL: " + model);
                                string mURL = FindTexturePathFromModel.getModelURL(model);
                                Log.Info("MODEL URL: " + mURL);
                                model = model + ".mu";

                                RenameFile(model, part.name);
                            }
                        }
                    }
                }
#endif
                yield return 0;
                if (!permapruneInProgress)
                    break;
                Log.Info("searching for meshes");
                string mesh = GetMeshURL(part); 
                if (mesh != null && mesh != "")
                {
                    // Make sure it isn't being used in another part
                    b = false;
                    foreach (AvailablePart pSearch in PartLoader.LoadedPartsList)
                    {
                        if (part != pSearch)
                        {
                            string searchMesh = GetMeshURL(pSearch); 

                            if (searchMesh == mesh)
                            {
                                b = true;
                                break;
                            }


                            if (pSearch.partConfig != null)
                            {
                                nodes2 = pSearch.partConfig.GetNodes("MODEL");
                                if (nodes2 != null)
                                {
                                    foreach (ConfigNode searchNode in nodes2)
                                    {
                                        if (searchNode != null)
                                        {
                                            string model = GetModelURL(pSearch, searchNode);
                                            
                                            if (mesh ==  model)
                                            {
                                                b = true;
                                                break;
                                            }
                                            
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (!b)
                    {
                        Log.Info("Renaming mesh: " + mesh + "    partPath: " + partPath);

                        string mURL = FindTexturePathFromModel.getModelURL(mesh);
                        partPath = partPath.Substring(0, partPath.LastIndexOf("/")) + "/";
                        if (!(mesh.Contains("/") || mesh.Contains("\\")))
                            mesh = partPath + mesh;

                        RenameFile(mesh, part.name);
                    }
                }

                // this gets the model
                Log.Info("searching for model (INTERNAL)");
                nodes = part.partConfig.GetNodes("INTERNAL");
                if (nodes != null)
                    foreach (ConfigNode internalNode in nodes)
                    {
                        if (internalNode != null)
                        {
                            UrlDir.UrlConfig config;
                            if (GetInternalSpaceConfigUrl.FindInternalSpaceConfigByName(internalNode.GetValue("name"), out config))
                            {
                                // Make sure it isn't being used in another part
                                b = false;
                                foreach (AvailablePart pSearch in PartLoader.Instance.parts)
                                {
                                    if (part != pSearch)
                                    {
                                        nodes2 = part.partConfig.GetNodes("INTERNAL");
                                        if (nodes2 != null)
                                            foreach (ConfigNode internalNodeSearch in nodes2)
                                            {
                                                UrlDir.UrlConfig configSearch;
                                                if (GetInternalSpaceConfigUrl.FindInternalSpaceConfigByName(internalNode.GetValue("name"), out configSearch))
                                                {
                                                    if (configSearch.url == config.url)
                                                    {
                                                        b = true;
                                                        break;
                                                    }
                                                }
                                                if (b) break;
                                            }
                                    }
                                    if (b) break;
                                }
                                if (!b)
                                {
                                    string s = config.url.Substring(0, config.url.LastIndexOf("/")) + ".cfg";
                                    RenameFile(s, part.name);
                                }
                            }

                            //
                            // We aren't going to check to see if the different models inside the space are
                            // used elsewhere.  An assumption that the same model won't be used by multiple spaces
                            //
                            Log.Info("searching for internal space nodes");
                            ConfigNode cfgNode;
                            bool b1 = GetInternalSpaceConfigUrl.FindInternalSpaceConfigNode(config.name, out cfgNode);
                            if (b1)
                            {
                                nodes = cfgNode.GetNodes("MODEL");
                                if (nodes != null)
                                    foreach (ConfigNode modelNode in nodes)
                                    {
                                        string model = modelNode.GetValue("model");
                                        //Log.Info("MODEL: " + model);
                                        string mURL = FindTexturePathFromModel.getModelURL(model);
                                        // Log.Info("MODEL URL: " + mURL);
                                        model = model + ".mu";
                                        RenameFile(model, part.name);
                                    }

                            }
                        }
                    }
            }
            foreach (var s in prunedParts)
            {
                blackListPart blp = JanitorsCloset.blackList[s];
                blp.permapruned = true;
                JanitorsCloset.blackList[s] = blp;
            }

            Log.Info("before saveRenamedFiles");
            FileOperations.Instance.saveRenamedFiles(renamedFilesList);
            permapruneInProgress = false;

            UpdateCKANFilters(Path.Combine(KSPUtil.ApplicationRootPath,
                                           "CKAN",
                                           "install_filters.json"),
                              renamedFilesList);

            yield break;
            //JanitorsCloset.Instance.clearBlackList();
        }

        private void UpdateCKANFilters(string                  filterPath,
                                       IEnumerable<prunedPart> pruned)
        {
            try
            {
                // Create or overwrite if parent directory exists
                if (Directory.Exists(Path.GetDirectoryName(filterPath)))
                {
                    File.WriteAllText(filterPath,
                                      MiniJSON.jsonEncode(
                                          GetCKANFilters(filterPath)
                                              .Concat(pruned.Select(OriginalGameDataRelativePath))
                                              .Distinct()
                                              .ToArray()));
                }
            }
            catch
            {
                // Never disrupt the outer program with exceptions
            }
        }

        private IEnumerable<string> GetCKANFilters(string path)
            => File.Exists(path)
                ? MiniJsonExtensions.arrayListFromJson(File.ReadAllText(path))
                                    .OfType<string>()
                : Enumerable.Empty<string>();

        private static string OriginalGameDataRelativePath(prunedPart pp)
            => Path.Combine("GameData", pp.path.Replace("\\", "/")
                                               .Replace(PRUNED, ""));

        void unpruner()
        {
            ShowRenamed.Instance.Show();
            renamedFilesList = FileOperations.Instance.loadRenamedFiles();
            foreach (prunedPart l in renamedFilesList)
            {
                l.path = FileOperations.CONFIG_BASE_FOLDER + l.path;
                Log.Info("Renaming " + l.path + "  to  " + l.path.Substring(0, l.path.Length - PRUNED.Length));
                if (File.Exists(l.path))
                {
                    Log.Info("Renaming " + l.path + "  to  " + l.path.Substring(0, l.path.Length - PRUNED.Length));
                    ShowRenamed.Instance.addLine("Renaming " + l.path + "  to  " + l.path.Substring(0, l.path.Length - PRUNED.Length));
                    if (!File.Exists(l.path.Substring(0, l.path.Length - PRUNED.Length)))
                        System.IO.File.Move(l.path, l.path.Substring(0, l.path.Length - PRUNED.Length));
                    else
                        System.IO.File.Delete(l.path);
                }
                if (JanitorsCloset.blackList.ContainsKey(l.partName))
                {
                    blackListPart blp;
                    JanitorsCloset.blackList.TryGetValue(l.partName, out blp);
                    blp.permapruned = false;
                    
                }
            }
            FileOperations.Instance.delRenamedFilesList();
        }


        void WindowContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Permanent Prune"))
            {
                winState = winContent.permaprune;
                //                        pruner();
                // CloseWindow();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Undo Permanent Prune"))
            {
                winState = winContent.undo;
                //unpruner();
                //CloseWindow();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                CloseWindow();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public void Show()
        {
            Log.Info("PermaPrune Show()");
            this.enabled = true;
        }
    }
}
