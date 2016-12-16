using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace JanitorsCloset
{
    partial class JanitorsCloset
    {

        const string CfgPath = "JanitorsCloset/PluginData";
        private static ConfigNode configFile = null;
        private static ConfigNode configBarNode = null;
        private static ConfigNode configButtonNode = null;


        public static readonly String ROOT_PATH = KSPUtil.ApplicationRootPath;
        private static readonly String CONFIG_BASE_FOLDER = ROOT_PATH + "GameData/";
        private static String JC_BASE_FOLDER = CONFIG_BASE_FOLDER + "JanitorsCloset/";
        private static String JC_NODE = "JANITORSCLOSET";
        private static String JC_CFG_FILE = JC_BASE_FOLDER + "PluginData/JanitorsCloset.cfg";

        static string SafeLoad(string value, float oldvalue)
        {
            if (value == null)
                return oldvalue.ToString();
            return value;
        }
        static string SafeLoad(string value, int oldvalue)
        {
            if (value == null)
                return oldvalue.ToString();
            return value;
        }
        static string SafeLoad(string value, bool oldvalue)
        {
            if (value == null)
                return oldvalue.ToString();
            return value;
        }
        static string SafeLoad(string value, string oldvalue)
        {
            if (value == null)
                return oldvalue;
            return value;
        }
        static string SafeLoad(string value, GameScenes oldvalue)
        {
            if (value == null)
                return oldvalue.ToString();
            return value;
        }
        static string SafeLoad(string value, Blocktype oldvalue)
        {
            if (value == null)
                return oldvalue.ToString();
            return value;
        }



        void saveButtonData()
        {
            Log.Info("saveButtonData");
            ConfigNode janitorsClosetNode = new ConfigNode();

            for (int i = 0; i < (int)GameScenes.PSYSTEM; i++)
            {
               
                var bbl = buttonBarList[i];
                if (bbl.Count > 0)
                {
                    configBarNode = new ConfigNode(((GameScenes)i).ToString()); // scene

                    foreach (var bbi in bbl)
                    {
                        configButtonNode = new ConfigNode(bbi.Value.buttonHash); // button on main toolbar
                        configButtonNode.AddValue("folderIcon", bbi.Value.folderIcon);
                        foreach (var bBlockl in bbi.Value.buttonBlockList)
                        {
                            ConfigNode button = new ConfigNode();

                            button.AddValue("scene", bBlockl.Value.scene.ToString());
                            button.AddValue("blocktype", bBlockl.Value.blocktype.ToString());
                            button.AddValue("buttonHash", bBlockl.Value.buttonHash);
                           

                            configButtonNode.AddNode(bBlockl.Value.buttonHash, button);

                        }
                        configBarNode.AddNode(bbi.Value.buttonHash, configButtonNode);
                    }
                    janitorsClosetNode.AddNode(((GameScenes)i).ToString(), configBarNode);
                   
                }

            }
            
            configFile = new ConfigNode();
            configFile.AddNode(JC_NODE, janitorsClosetNode);
            configFile.Save(JC_CFG_FILE);
        }

        void loadButtonData()
        {
            loadedCfgs = new Dictionary<string, Cfg>();
            Cfg cfg;
            if (File.Exists(JC_CFG_FILE))
            {
                configFile = ConfigNode.Load(JC_CFG_FILE);
                ConfigNode janitorsClosetNode = configFile.GetNode(JC_NODE);
                if (janitorsClosetNode != null)
                {
                    foreach (var n in janitorsClosetNode.GetNodes())  // n = scenes
                    {
                        foreach (var n1 in n.GetNodes()) 
                        {
                            foreach (var n2 in n1.GetNodes())
                            {
                                cfg = new Cfg();
                                cfg.scene = (GameScenes)Enum.Parse(typeof(GameScenes), n2.GetValue("scene"));
                                cfg.blocktype = (Blocktype)Enum.Parse(typeof(Blocktype), n2.GetValue("blocktype"));
                                cfg.buttonHash = n2.GetValue("buttonHash");
                                cfg.folderIcon = System.Int32.Parse(n1.GetValue("folderIcon"));

                                cfg.toolbarButtonHash = n1.name;
                                cfg.toolbarButtonIndex = cfg.folderIcon;
#if false
                                for (int i = 0; i < folderIcons.Count(); i++)
                                {
                                    if (cfg.toolbarButtonHash == folderIconHashes[i])
                                    {
                                        cfg.toolbarButtonIndex = i;
                                        
                                        break;
                                    }
                                }
#endif
                                loadedCfgs.Add(cfg.scene + cfg.buttonHash, cfg);
                            }
                        }
                    }
                }
            }
        }
    }
}
