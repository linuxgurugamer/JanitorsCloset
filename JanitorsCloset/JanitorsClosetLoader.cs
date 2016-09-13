
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Diagnostics;
using System.IO;

using System.Reflection;

using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace JanitorsCloset
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    class JanitorsClosetLoader : MonoBehaviour
    {
        void Awake()
        {
            FileOperations f = new FileOperations();
            // Allow loading the background in the loading screen
            //Application.runInBackground = true;
            Log.Info("JanitorsClosetLoader.Awake");
            List<prunedPart> renamedFilesList = FileOperations.Instance.loadRenamedFiles();
            Log.Info("sizeof renamedFilesList: " + renamedFilesList.Count.ToString());
            foreach (prunedPart pp in renamedFilesList)
            {
                Log.Info("partName: " + pp.partName + "    path: " + pp.path);
                int i = pp.path.IndexOf(".prune");
                if (i > 0)
                {
                    string fname = pp.path.Substring(0, i);
                    Log.Info("checking for: " + fname);
                    if (File.Exists(FileOperations.CONFIG_BASE_FOLDER + fname))
                    {
                        Log.Info("File exists, need to rename it");
                        // following function will delete the older ".prune" file and rename the new one
                        FileOperations.Instance.RenameFile(fname);
                    }
                }
            }
#if false
            LoadingScreen screen = FindObjectOfType<LoadingScreen>();
            if (screen == null)
            {
                Log.Error("Can't find LoadingScreen type. Aborting ModuleManager execution");
                return;
            }
            List<LoadingSystem> list = LoadingScreen.Instance.loaders;

            if (list != null)
            {
                // So you can insert a LoadingSystem object in this list at any point.
                // GameDatabase is first in the list, and PartLoader is second
                // 
                // JCPruner will be inserted at the beginning so that it will run BEFORE the GameDatabase
                // is run
                GameObject aGameObject = new GameObject("JanitorsClosetLoader");
                JCPruner loader = aGameObject.AddComponent<JCPruner>();
#if false
                int insertpos = 1;

                int cnt = 0;
                foreach (LoadingSystem ls in list)
                {
                   
                    Log.Info(ls.name);
                    if (ls.name == "ModuleManager")
                    {
                        insertpos = insertpos;
                        break;
                    }
                    cnt++;
                }
#else
                int insertpos = 1;
#endif
                Log.Info(string.Format("Adding JanitorsClosetLoader to the loading screen "));
                list.Insert(insertpos, loader);
                foreach (LoadingSystem ls in list)
                    Log.Info(ls.name);
            }
#endif
        }

#if false
        void Update()
        {

        }

        public void OnGUI()
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING && JCPruner.Instance != null)
            {
            }
        }
#endif
    }
#if false
    public class JCPruner : LoadingSystem
    {
        public static JCPruner Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                DestroyImmediate(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private bool ready = true;

        public override bool IsReady()
        {
            if (ready)
            {
               
            }
            return ready;
        }

        public override void StartLoad()
        {
            Log.Info("JCPruner.StartLoad");
        }
    }
#endif
}
