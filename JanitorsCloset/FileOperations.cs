﻿using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

using static JanitorsCloset.JanitorsClosetLoader;



namespace JanitorsCloset
{
    public class FileOperations
    {
        public static FileOperations Instance;

        public static readonly String ROOT_PATH = KSPUtil.ApplicationRootPath;
        public static readonly String CONFIG_BASE_FOLDER = ROOT_PATH + "GameData/";
        public static String TT_BASE_FOLDER = CONFIG_BASE_FOLDER + JanitorsCloset.MOD + "/";
        public static string TEXTURE_DIR = JanitorsCloset.MOD + "/" + "Textures/";
        public static string TT_DATAFILE = JanitorsCloset.MOD + ".dat";
        static string TT_RENAMEDFILESLIST = TT_BASE_FOLDER + "PluginData/RenamedFiles.dat";
        public static string EXPORTBLACKLISTDIR = TT_BASE_FOLDER + "PluginData/";
        public static String TT_NODENAME = JanitorsCloset.MOD;
        public static String TT_CFG_FILE = FileOperations.TT_BASE_FOLDER + JanitorsCloset.MOD + ".cfg";
        public static string PRNLIST_SUFFIX = ".prnlst";

        public static bool dataRead = false;

        public FileOperations()
        {
            Instance = this;
        }
        public static bool FileExists(string filePath)
        {
            try
            {
                FileInfo file = new FileInfo(filePath);
                return file.Exists;
            }
            catch (Exception ex)
            {
                Log.Info("Failed to verify file " + filePath + " Error: " + ex.Message);
                return false;
            }
        }


        private static string getExportedBlackListDataFile(string str)
        {
            // This happens when this is called before a save is loaded or created
            if (HighLogic.SaveFolder == "DestructiblesTest")
                return "";
            return (EXPORTBLACKLISTDIR + str + PRNLIST_SUFFIX);
        }

        private static string getBlackListDataFile()
        {
            // This happens when this is called before a save is loaded or created
            if (HighLogic.SaveFolder == "DestructiblesTest" || HighLogic.SaveFolder == "")
                return "";
            return (ROOT_PATH + "saves/" + HighLogic.SaveFolder + "/" + TT_DATAFILE);
        }

        public Dictionary<string, blackListPart> loadData(string fname)
        {
            Dictionary<string, blackListPart> blpList = new Dictionary<string, blackListPart>();

            List<AvailablePart> loadedParts = new List<AvailablePart>();
            if (PartLoader.Instance != null)
                loadedParts.AddRange(PartLoader.LoadedPartsList);
      
#if false
            // This code is used to export various information about parts and their resources
            // used to rebalance fuel tanks
            foreach (AvailablePart part in loadedParts)
            {
                List<Bounds> list = new List<Bounds>();
                if (!(part.partPrefab.Modules.GetModule<LaunchClamp>(0) != null))
                {
                    
                    Bounds[] partRendererBounds = PartGeometryUtil.GetPartRendererBounds(part.partPrefab);
                    int num = partRendererBounds.Length;
                    for (int j = 0; j < num; j++)
                    {
                        Bounds bounds2 = partRendererBounds[j];
                        Bounds bounds3 = bounds2;
                        bounds3.size *= part.partPrefab.boundsMultiplier;
                        Vector3 size = bounds3.size;
                        bounds3.Expand(part.partPrefab.GetModuleSize(size, ModifierStagingSituation.CURRENT));
                        list.Add(bounds2);
                    }
                }

                var pg = PartGeometryUtil.MergeBounds(list.ToArray(), part.partPrefab.transform.root).size;
                string resources = "";
                foreach (AvailablePart.ResourceInfo r in part.resourceInfos)
                {
                    if (r.resourceName != "ElectricCharge" && r.resourceName != "Ablator")
                        resources += r.resourceName + ",";
                }
                if (resources != "")
                {
                    
                    Log.Info("part: " + part.name + ", part.title: " + part.title + ", descr: " + part.description.Replace(",", ".") +
                        ", mass: " + part.partPrefab.mass + ", techRequired: " + part.TechRequired +
                        ", height x,y,z: " + pg.x.ToString() + ", " + pg.y.ToString() + ", " + pg.z.ToString() + "," + resources);
                }
                
            }
#endif
            Log.Info("loadData, fname: " + fname);
            if (fname != "" && File.Exists(fname))
            {
                using (StreamReader f = File.OpenText(fname))
                {
                    string l = "";
                    while ((l = f.ReadLine()) != null)
                    {
                        string[] s = l.Split(',');
                        if (s.Length >= 2)
                        {
                            blackListPart blp = new blackListPart();
                            blp.modName = s[0];
                            if (s[1] == "ALL")
                                blp.where = blackListType.ALL;
                            if (s[1] == "SPH")
                                blp.where = blackListType.SPH;
                            if (s[1] == "VAB")
                                blp.where = blackListType.VAB;
                            
                            AvailablePart p = loadedParts.Find(part => part.name == blp.modName);
                            if (p != null)
                            {
                                blp.title = p.title;
                                Log.Info("Blacklist mod: " + blp.modName);
                                Log.Info("partTitle: " + blp.title);
                                blp.permapruned = false;

                                blpList.Add(blp.modName, blp);
                            }
                        }
                    }
                }
                return blpList;
            }
            else
            {
                return blpList;
            }
        }

        public Dictionary<string, blackListPart> loadBlackListData()
        {
            return loadData(getBlackListDataFile());
        }

        public Dictionary<string, blackListPart> importBlackListData(string fname)
        {
            Dictionary<string, blackListPart> blpD = loadData(fname);
            foreach (KeyValuePair<string, blackListPart> entry in blpD)
            {
                JanitorsCloset.blackList[entry.Key]= entry.Value;
                // do something with entry.Value or entry.Key
                //if (!JanitorsCloset.blackList.ContainsKey(entry.Key))
                //    JanitorsCloset.blackList.Add(entry.Key, entry.Value);
            }
            return blpD;
        }


        public void saveData(string fname, Dictionary<string, blackListPart> blpList)
        {
            Log.Info("saveData: " + fname);
            if (fname == "" || blpList == null)
                return;
            
            using (StreamWriter f = File.CreateText(fname))
            {
                foreach (var blp in blpList)
                {
                    Log.Info("blp.Key: " + blp.Key);
                    Log.Info("modName: " + blp.Value.modName + ",  where: " + blp.Value.where);
                    f.WriteLine(blp.Value.modName + "," + blp.Value.where);
                }
            }
        }

        public void saveBlackListData(Dictionary<string, blackListPart> blpList)
        {
            Log.Info("saveBlackListData: " + getBlackListDataFile());
            saveData(getBlackListDataFile(), blpList);
        }

        public void exportBlackListData(string fname, Dictionary<string, blackListPart> blpList)
        {
            Log.Info("exportBlackListData: " + fname);
            saveData(getExportedBlackListDataFile(fname), blpList);
            GUIUtility.systemCopyBuffer = getExportedBlackListDataFile(fname);
        }


        // /////////////////////////////////////////////////////////////////////////////////////////////////////
        const string PRUNED = ".prune";
        public prunedPart RenameFile(string path, string name, bool addToShowRenamed = true)
        {
            Log.Info("RenameFile, path: " + path + "    name: " + name);
            if (File.Exists(FileOperations.CONFIG_BASE_FOLDER + path))
            {
                if (File.Exists(FileOperations.CONFIG_BASE_FOLDER + path + PRUNED))
                {
                    System.IO.File.Delete(FileOperations.CONFIG_BASE_FOLDER + path + PRUNED);
                }

                Log.Info("Renaming: " + path + "  to  " + path + PRUNED);
                if (addToShowRenamed)
                    ShowRenamed.Instance.addLine("Renaming: " + path + "  to  " + path + PRUNED);
                System.IO.File.Move(FileOperations.CONFIG_BASE_FOLDER + path, FileOperations.CONFIG_BASE_FOLDER + path + PRUNED);
                prunedPart pp = new prunedPart();
                pp.path = path + PRUNED;
                pp.partName = name;
                return pp;
                
            }
            return null;
        }

        public void RenameFile(string path)
        {
            Log.Info("RenameFile 2, path: " + path);
            prunedPart p = RenameFile(path, "n/a", false);
        }

        public List<prunedPart> loadRenamedFiles()
        {
            List<prunedPart> renamedFilesList = new List<prunedPart>();
            if (File.Exists(TT_RENAMEDFILESLIST))
            {
                using (StreamReader f = File.OpenText(TT_RENAMEDFILESLIST))
                {
                    string l = "";
                    while ((l = f.ReadLine()) != null)
                    {
                        prunedPart pp = new prunedPart();
                        string[] s = l.Split(',');
                        pp.path = s[0];
                        pp.partName = s[1];
                        renamedFilesList.Add(pp);
                    }
                }
            }

            return renamedFilesList;
        }

        public void saveRenamedFiles(List<prunedPart> renamedFilesList)
        {
            using (StreamWriter f = File.CreateText(TT_RENAMEDFILESLIST))
            {

                foreach (prunedPart l in renamedFilesList)
                {
                    f.WriteLine(l.path + "," + l.partName);
                }
            }
        }
        public void delRenamedFilesList()
        {
            System.IO.File.Delete(TT_RENAMEDFILESLIST);
        }

    }
}
