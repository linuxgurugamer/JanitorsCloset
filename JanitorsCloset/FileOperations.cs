using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using UnityEngine;



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

        public static bool FileExists(string filePath)
        {
            try
            {
                FileInfo file = new FileInfo(filePath);
                return file.Exists;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to verify file " + filePath + " Error: " + ex.Message);
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
            if (HighLogic.SaveFolder == "DestructiblesTest")
                return "";
            return (ROOT_PATH + "saves/" + HighLogic.SaveFolder + "/" + TT_DATAFILE);
        }

        public Dictionary<string, blackListPart> loadData(string fname)
        {
            Dictionary<string, blackListPart> blpList = new Dictionary<string, blackListPart>();
            if (File.Exists(fname))
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

                            Log.Info("Blacklist mod: " + blp.modName);
                            blp.permapruned = false;

                            blpList.Add(blp.modName, blp);
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
            return loadData(fname);
        }


        public void saveData(string fname, Dictionary<string, blackListPart> blpList)
        {
            Log.Info("saveData: " + fname);

            using (StreamWriter f = File.CreateText(fname))
            {

                foreach (var blp in blpList)
                {
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
                        pp.modName = s[1];
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
                    f.WriteLine(l.path + "," + l.modName);
                }
            }
        }
        public void delRenamedFilesList()
        {
            System.IO.File.Delete(TT_RENAMEDFILESLIST);
        }

    }
}
