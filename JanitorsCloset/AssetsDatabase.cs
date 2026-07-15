using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static JanitorsCloset.JanitorsClosetLoader;

namespace JanitorsCloset
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class AssetsDatabase : MonoBehaviour
    {
        public static AssetsDatabase Instance;
        public AssetsDatabase()
        {
            Instance = this;
            models = new AssetsDictionary(".mu");
            textures = new AssetsDictionary(".dds");
            internals = new AssetsDictionary(null);
        }

        public AssetsDictionary models;
        public AssetsDictionary textures;
        public AssetsDictionary internals;

        public struct AssetsDictionary
        {
            private string fileExt;
            private Dictionary<string, HashSet<string>> dictionary;

            public int AssetsCount;
            public long AssetsSize;

            public AssetsDictionary(string fileExtension)
            {
                fileExt = fileExtension;
                dictionary = new Dictionary<string, HashSet<string>>();
                AssetsCount = 0;
                AssetsSize = 0;
            }

            public void Clear()
            {
                AssetsCount = 0;
                AssetsSize = 0;
                if (dictionary != null)
                {
                    dictionary.Clear();
                }
            }

            public void Add(string assetURL, string partName)
            {
                if (String.IsNullOrEmpty(assetURL) || String.IsNullOrEmpty(partName))
                {
                    return;
                }
                if (Exists(assetURL))
                {
                    dictionary[assetURL].Add(partName);
                }
                else
                {
                    dictionary.Add(assetURL, new HashSet<string>());
                    AssetsCount++;
                    if (!String.IsNullOrEmpty(fileExt))
                    {
                        string fileName = FileOperations.CONFIG_BASE_FOLDER + assetURL + fileExt;
                        try
                        {
                            FileInfo fi = new FileInfo(fileName);
                            if (fi.Exists)
                            {
                                AssetsSize += fi.Length;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Failed to determine file size " + fileName + " Error: " + ex.Message);
                        }
                    }
                }
            }

            public bool Exists(string assetURL)
            {
                if (String.IsNullOrEmpty(assetURL))
                {
                    return false;
                }
                else
                {
                    return dictionary.ContainsKey(assetURL);
                }
            }

            public HashSet<string> PartsUsingAsset(string assetURL)
            {
                if (!String.IsNullOrEmpty(assetURL) && dictionary.ContainsKey(assetURL))
                {
                    return dictionary[assetURL];
                }
                else
                {
                    return null;
                }
            }
        }

        public int PartsCount = 0;

        private void Start()
        {
            PartsCount = PartLoader.LoadedPartsList.Count;
            BuildAssetsDatabase();
        }

        private void BuildAssetsDatabase()
        {
            Log.Info("Start Assets DB Creation");
            ConfigNode[] pNodes;

            models.Clear();
            textures.Clear();
            internals.Clear();

            Log.Info("Start building parts assets database");
            Log.Info("Parts count: " + PartsCount.ToString());
            foreach (AvailablePart pSearch in PartLoader.LoadedPartsList)
            {
                Log.Info("Part: " + pSearch.name);
                if (pSearch.partConfig != null)
                {
                    #region MODELS
                    // Find models and dependent textures
                    pNodes = pSearch.partConfig.GetNodes("MODEL");
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
                                    models.Add(model, pSearch.name);
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
                                                textures.Add(texture, pSearch.name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion MODELs

                    #region Mesh
                    // Find mesh and dependent textures
                    string mesh = Utilities.GetMeshURL(pSearch);
                    if (mesh != null)
                    {
                        Log.Info("Mesh: " + mesh);
                        if (!models.Exists(mesh))
                        {
                            models.Add(mesh, pSearch.name);
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
                                        textures.Add(texture, pSearch.name);
                                    }
                                }
                            }
                        }
                    }
                    #endregion Mesh

                    #region INTERNAL
                    // Find INTERNALs, their models and textures
                    pNodes = pSearch.partConfig.GetNodes("INTERNAL");
                    if (pNodes != null)
                    {
                        foreach (ConfigNode internalNode in pNodes)
                        {
                            if (internalNode != null)
                            {
                                string internalName = internalNode.GetValue("name");
                                if (internalName != null)
                                {
                                    Log.Info("Internal: " + internalName);
                                    internals.Add(internalName, pSearch.name);
                                    GetInternalSpaceConfigUrl.FindInternalSpaceConfigNode(internalName, out ConfigNode internalConfig);
                                    if (internalConfig != null)
                                    {
                                        ConfigNode[] modelNodes;
                                        modelNodes = internalConfig.GetNodes("MODEL");
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
                                                        models.Add(model, pSearch.name);
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
                                                                    textures.Add(texture, pSearch.name);
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
                    #endregion INTERNAL

                    #region Modules with textures variants
                    List<ConfigNode> mNodes;

                    #region ModulePartVariants
                    mNodes = Utilities.GetModuleConfigNodes(pSearch.partConfig, "ModulePartVariants");
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
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
                                                        }
                                                    }
                                                    if (tNode.TryGetValue("backTextureURL", ref texture))
                                                    {
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
                                                        }
                                                    }
                                                    if (tNode.TryGetValue("_MainTex", ref texture))
                                                    {
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
                                                        }
                                                    }
                                                    if (tNode.TryGetValue("_BumpMap", ref texture))
                                                    {
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
                                                        }
                                                    }
                                                    if (tNode.TryGetValue("_SpecMap", ref texture))
                                                    {
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
                                                        }
                                                    }
                                                    if (tNode.TryGetValue("_Emissive", ref texture))
                                                    {
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
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
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
                                                        }
                                                    }
                                                    if (tNode.TryGetValue("FairingsNormalURL", ref texture))
                                                    {
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
                                                        }
                                                    }
                                                    if (tNode.TryGetValue("BaseTextureName", ref texture))
                                                    {
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
                                                        }
                                                    }
                                                    if (tNode.TryGetValue("BaseNormalsName", ref texture))
                                                    {
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
                                                        }
                                                    }
                                                    if (tNode.TryGetValue("CapTextureURL", ref texture))
                                                    {
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (PartVariant): " + texture);
                                                            textures.Add(texture, pSearch.name);
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
                    #endregion ModulePartVariants

                    #region ModuleB9PartSwitch
                    mNodes = Utilities.GetModuleConfigNodes(pSearch.partConfig, "ModuleB9PartSwitch");
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
                                                        if (!String.IsNullOrEmpty(texture))
                                                        {
                                                            Log.Info("Texture (B9PartSwitch): " + texture);
                                                            textures.Add(texture, pSearch.name);
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
                    #endregion ModuleB9PartSwitch

                    #region FStextureSwitch2
                    mNodes = Utilities.GetModuleConfigNodes(pSearch.partConfig, "FStextureSwitch2");
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
                                            if (!String.IsNullOrEmpty(tex))
                                            {
                                                Log.Info("Texture (FStextureSwitch2): " + tex);
                                                textures.Add(tex, pSearch.name);
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
                                            if (!String.IsNullOrEmpty(tex))
                                            {
                                                Log.Info("Texture (FStextureSwitch2): " + tex);
                                                textures.Add(tex, pSearch.name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion FStextureSwitch2

                    #endregion Modules with textures variants
                }
            }
            Log.Info("Finished building parts assets database");
        }
    }
}
