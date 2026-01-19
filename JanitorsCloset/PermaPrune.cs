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
            //xMin = Screen.width - 325,
            //xMax = Screen.width - 175,
            xMin = Screen.width - 350,
            xMax = Screen.width - 150,
            yMin = Screen.height - 300,
            yMax = 50 //0 height, GUILayout resizes it
        };

        public bool permapruneInProgress = false;
        string _windowTitle = string.Empty;

        public static PermaPruneWindow Instance { get; private set; }

        long RenamedFilesSize = 0;

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

        HashSet<string> prunedPartsHashSet = new HashSet<string>();

        bool CheckIfModelIsBeingUsed(string modelURL)
        {
            if (String.IsNullOrEmpty(modelURL))
            {
                return false;
            }
            HashSet<string> partsUsingModel = AssetsDatabase.Instance.models.PartsUsingAsset(modelURL);
            if (partsUsingModel != null && prunedPartsHashSet != null)
            {
                partsUsingModel.ExceptWith(prunedPartsHashSet);
            }
            if (partsUsingModel.Count > 0)
            {
                return true;
            }
            return false;
        }

        bool CheckIfTextureIsBeingUsed(string textureURL)
        {
            if (String.IsNullOrEmpty(textureURL))
            {
                return false;
            }
            HashSet<string> partsUsingTexture = AssetsDatabase.Instance.textures.PartsUsingAsset(textureURL);
            if (partsUsingTexture != null && prunedPartsHashSet != null)
            {
                partsUsingTexture.ExceptWith(prunedPartsHashSet);
            }
            if (partsUsingTexture.Count > 0)
            {
                return true;
            }
            return false;
        }

        bool CheckIfInternalIsBeingUsed(string internalName)
        {
            if (String.IsNullOrEmpty(internalName))
            {
                return false;
            }
            HashSet<string> partsUsingInternal = AssetsDatabase.Instance.internals.PartsUsingAsset(internalName);
            if (partsUsingInternal != null && prunedPartsHashSet != null)
            {
                partsUsingInternal.ExceptWith(prunedPartsHashSet);
            }
            if (partsUsingInternal.Count > 0)
            {
                return true;
            }
            return false;
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

        private const string PRUNED = ".prune";
        public IEnumerator pruning()
        {
            permapruneInProgress = true;

            Log.Info("PermaPrune.pruner");
            renamedFilesList = FileOperations.Instance.loadRenamedFiles();
            Log.Info("sizeof renamedFilesList: " + renamedFilesList.Count.ToString());
            Log.Info("pruner, sizeof blacklist:" + JanitorsCloset.blackList.Count.ToString());
            ShowRenamed.Instance.Show();

            List<string> prunedParts = new List<string>();
            prunedPartsHashSet.Clear();
            foreach (blackListPart blp in JanitorsCloset.blackList.Values)
            {
                //                yield return 0;
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
                prunedPartsHashSet.Add(blp.modName);
            }
            foreach (string partName in prunedParts)
            {
                AvailablePart part = PartLoader.getPartInfoByName(partName);

                #region Rename CFG file
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
                #endregion Rename CFG File

                #region Rename models and textures
                Log.Info("Part: " + part.name);
                Log.Info("Searching for models and textures");
                // Find models and dependent textures
                ConfigNode[] pNodes;
                pNodes = part.partConfig.GetNodes("MODEL");
                if (pNodes != null)
                {
                    foreach (ConfigNode modelNode in pNodes)
                    {
                        if (modelNode != null)
                        {
                            string model = Utilities.GetModelURL(modelNode);
                            if (model != null)
                            {
                                Log.Info("Model: " + model);
                                if (!CheckIfModelIsBeingUsed(model))
                                {
                                    RenameFile(model + ".mu", part.name);
                                }
                                var go = GameDatabase.Instance.GetModel(model);
                                if (go == null)
                                {
                                    Log.Error("Could not find game object for model " + model);
                                }
                                else
                                {
                                    foreach (string texture in FindTexturePathFromModel.GetUrlsOfTextureDependencies(go))
                                    {
                                        if (texture != null)
                                        {
                                            Log.Info("Texture: " + texture);
                                            if (!CheckIfTextureIsBeingUsed(texture))
                                            {
                                                RenameFile(texture + ".dds", part.name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion Rename models and textures

                #region Rename mesh and textures
                // Find mesh and dependent textures
                Log.Info("Searching for mesh and dependent textures");
                string mesh = Utilities.GetMeshURL(part);
                if (mesh != null)
                {
                    Log.Info("Mesh: " + mesh);
                    if (!CheckIfModelIsBeingUsed(mesh))
                    {
                        RenameFile(mesh + ".mu", part.name);
                    }
                    var go = GameDatabase.Instance.GetModel(mesh);
                    if (go == null)
                    {
                        Log.Error("Could not find game object for model " + mesh);
                    }
                    else
                    {
                        foreach (string texture in FindTexturePathFromModel.GetUrlsOfTextureDependencies(go))
                        {
                            if (texture != null)
                            {
                                Log.Info("Texture: " + texture);
                                if (!CheckIfTextureIsBeingUsed(texture))
                                {
                                    RenameFile(texture + ".dds", part.name);
                                }
                            }
                        }
                    }
                }
                #endregion Rename mesh and textures

                #region Rename Internal
                // this gets the model
                Log.Info("searching for (INTERNAL) node");
                pNodes = part.partConfig.GetNodes("INTERNAL");
                if (pNodes != null)
                {
                    foreach (ConfigNode internalNode in pNodes)
                    {
                        if (internalNode != null)
                        {
                            string internalName = internalNode.GetValue("name");
                            UrlDir.UrlConfig internalConfig;
                            if (!CheckIfInternalIsBeingUsed(internalName) && GetInternalSpaceConfigUrl.FindInternalSpaceConfigByName(internalName, out internalConfig))
                            {
                                if (internalConfig != null)
                                {
                                    string internalCFGFileName = internalConfig.url.Substring(0, internalConfig.url.LastIndexOf("/")) + ".cfg";
                                    RenameFile(internalCFGFileName, part.name);
                                    ConfigNode internalCFGNode;
                                    GetInternalSpaceConfigUrl.FindInternalSpaceConfigNode(internalName, out internalCFGNode);
                                    if (internalCFGNode != null)
                                    {
                                        ConfigNode[] modelNodes;
                                        modelNodes = internalNode.GetNodes("MODEL");
                                        if (modelNodes != null)
                                        {
                                            foreach (ConfigNode modelNode in modelNodes)
                                            {
                                                if (modelNode != null)
                                                {
                                                    string model = Utilities.GetModelURL(modelNode);
                                                    if (model != null)
                                                    {
                                                        Log.Info("Model (Internal): " + model);
                                                        if (!CheckIfModelIsBeingUsed(model))
                                                        {
                                                            RenameFile(model + ".mu", part.name);
                                                        }
                                                        var go = GameDatabase.Instance.GetModel(model);
                                                        if (go == null)
                                                        {
                                                            Log.Error("Could not find game object for model " + model);
                                                        }
                                                        else
                                                        {
                                                            foreach (string texture in FindTexturePathFromModel.GetUrlsOfTextureDependencies(go))
                                                            {
                                                                if (texture != null)
                                                                {
                                                                    Log.Info("Texture (Internal): " + texture);
                                                                    if (!CheckIfTextureIsBeingUsed(texture))
                                                                    {
                                                                        RenameFile(texture + ".dds", part.name);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion Rename Internal

                #region Rename textures from modules with texture variants
                List<ConfigNode> mNodes;

                #region Rename textures from ModulePartVariants
                mNodes = Utilities.GetModuleConfigNodes(part.partConfig, "ModulePartVariants");
                if (mNodes != null && mNodes.Count > 0)
                {
                    foreach (ConfigNode moduleNode in mNodes)
                    {
                        if (moduleNode != null)
                        {
                            // Nodes/values of interest:
                            // VARIANT / TEXTURE / mainTextureURL
                            // VARIANT / TEXTURE / backTextureURL
                            // VARIANT / TEXTURE / _MainTex
                            // VARIANT / TEXTURE / _BumpMap
                            // VARIANT / TEXTURE / _SpecMap
                            // VARIANT / TEXTURE / _Emissive 
                            // VARIANT / EXTRA_INFO / FairingsTextureURL
                            // VARIANT / EXTRA_INFO / FairingsNormalURL
                            // VARIANT / EXTRA_INFO / BaseTextureName
                            // VARIANT / EXTRA_INFO / BaseNormalsName
                            // VARIANT / EXTRA_INFO / CapTextureURL
                            ConfigNode[] variantNodes = moduleNode.GetNodes("VARIANT");
                            if (variantNodes != null)
                            {
                                foreach (ConfigNode variantNode in variantNodes)
                                {
                                    if (variantNode != null)
                                    {
                                        ConfigNode[] pTNodes;
                                        pTNodes = variantNode.GetNodes("TEXTURE");
                                        if (pTNodes != null)
                                        {
                                            foreach (ConfigNode tNode in pTNodes)
                                            {
                                                string texture = null;
                                                if (tNode.TryGetValue("mainTextureURL", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                                if (tNode.TryGetValue("backTextureURL", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                                if (tNode.TryGetValue("_MainTex", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                                if (tNode.TryGetValue("_BumpMap", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                                if (tNode.TryGetValue("_SpecMap", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                                if (tNode.TryGetValue("_Emissive", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        ConfigNode[] pEINodes;
                                        pEINodes = variantNode.GetNodes("EXTRA_INFO");
                                        if (pEINodes != null)
                                        {
                                            foreach (ConfigNode tNode in pEINodes)
                                            {
                                                string texture = null;
                                                if (tNode.TryGetValue("FairingsTextureURL", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                                if (tNode.TryGetValue("FairingsNormalURL", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                                if (tNode.TryGetValue("BaseTextureName", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                                if (tNode.TryGetValue("BaseNormalsName", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                                if (tNode.TryGetValue("CapTextureURL", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (PartVariant): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion Rename textures from modules with texture variants

                #region Rename textures from B9PartSwitch variants
                mNodes = Utilities.GetModuleConfigNodes(part.partConfig, "ModuleB9PartSwitch");
                if (mNodes != null && mNodes.Count > 0)
                {
                    foreach (ConfigNode moduleNode in mNodes)
                    {
                        if (moduleNode != null)
                        {
                            // Nodes/values of interest:
                            // SUBTYPE / TEXTURE / texture
                            ConfigNode[] subtypeNodes = moduleNode.GetNodes("SUBTYPE");
                            if (subtypeNodes != null)
                            {
                                foreach (ConfigNode subtypeNode in subtypeNodes)
                                {
                                    if (subtypeNode != null)
                                    {
                                        ConfigNode[] pTNodes;
                                        pTNodes = subtypeNode.GetNodes("TEXTURE");
                                        if (pTNodes != null)
                                        {
                                            foreach (ConfigNode tNode in pTNodes)
                                            {
                                                string texture = null;
                                                if (tNode.TryGetValue("texture", ref texture))
                                                {
                                                    if (texture != null)
                                                    {
                                                        Log.Info("Texture (B9PartSwitch subtype): " + texture);
                                                        if (!CheckIfTextureIsBeingUsed(texture))
                                                        {
                                                            RenameFile(texture + ".dds", part.name);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion Rename textures from B9PartSwitch variants

                #region Rename textures from FStextureSwitch2
                mNodes = Utilities.GetModuleConfigNodes(part.partConfig, "FStextureSwitch2");
                if (mNodes != null && mNodes.Count > 0)
                {
                    foreach (ConfigNode moduleNode in mNodes)
                    {
                        if (moduleNode != null)
                        {
                            // Values of interest:
                            // textureNames
                            // mapNames
                            string tValue = null;
                            if (moduleNode.TryGetValue("textureNames", ref tValue))
                            {
                                if (tValue != null)
                                {
                                    string[] pTextures = tValue.Split(';');
                                    foreach (string texture in pTextures)
                                    {
                                        string tex = texture.Trim();
                                        Log.Info("Texture (FireSpitter Switch): " + tex);
                                        if (!CheckIfTextureIsBeingUsed(tex))
                                        {
                                            RenameFile(tex + ".dds", part.name);
                                        }
                                    }
                                }
                            }
                            if (moduleNode.TryGetValue("mapNames", ref tValue))
                            {
                                if (tValue != null)
                                {
                                    string[] pTextures = tValue.Split(';');
                                    foreach (string texture in pTextures)
                                    {
                                        string tex = texture.Trim();
                                        Log.Info("Texture (FireSpitter Switch): " + tex);
                                        if (!CheckIfTextureIsBeingUsed(tex))
                                        {
                                            RenameFile(tex + ".dds", part.name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion Rename textures from FStextureSwitch2

                #endregion Rename textures from modules with texture variants

                yield return 0;
                if (!permapruneInProgress)
                    break;

            }
            foreach (var s in prunedParts)
            {
                blackListPart blp = JanitorsCloset.blackList[s];
                blp.permapruned = true;
                JanitorsCloset.blackList[s] = blp;
            }

            Log.Info("before saveRenamedFiles");
            FileOperations.Instance.saveRenamedFiles(renamedFilesList);
            UpdateRenamedFilesSize();
            permapruneInProgress = false;

            UpdateCKANFilters(Path.Combine(KSPUtil.ApplicationRootPath,
                                           "CKAN",
                                           "install_filters.json"),
                              renamedFilesList);

            yield break;
            //JanitorsCloset.Instance.clearBlackList();
        }

        private void UpdateCKANFilters(string filterPath,
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
            => "GameData/" + pp.path.Replace("\\", "/")
                                    .Replace(PRUNED, "");

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
            renamedFilesList.Clear();
            UpdateRenamedFilesSize();
        }

        void UpdateRenamedFilesSize()
        {
            RenamedFilesSize = 0;
            foreach (prunedPart pp in renamedFilesList)
            {
                try
                {
                    FileInfo fi = new FileInfo(FileOperations.CONFIG_BASE_FOLDER + pp.path);
                    if (fi.Exists)
                    {
                        RenamedFilesSize += fi.Length;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to determine file size " + FileOperations.CONFIG_BASE_FOLDER + pp.path + " Error: " + ex.Message);
                }
            }
        }

        void WindowContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (renamedFilesList == null)
            {
                renamedFilesList = FileOperations.Instance.loadRenamedFiles();
            }
            if (RenamedFilesSize <= 0)
            {
                UpdateRenamedFilesSize();
            }
            GUILayout.Label("Renamed files: " + renamedFilesList.Count.ToString() + " (" + Utilities.FormatFileSize(RenamedFilesSize) + ")");
            GUILayout.EndHorizontal();
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
