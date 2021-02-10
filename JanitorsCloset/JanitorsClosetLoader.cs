
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
using KSP_Log;


namespace JanitorsCloset
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class JanitorsClosetLoader : MonoBehaviour
    {
        static public Log Log;

        void Start()
        {
#if DEBUG
            Log = new Log("Janitor's Closet", Log.LEVEL.INFO);
#else
            Log = new Log("Janitor's Closet", Log.LEVEL.ERROR);
#endif

            FileOperations f = new FileOperations();
            // Allow loading the background in the loading screen
            //Application.runInBackground = true;
           // Log.Info("JanitorsClosetLoader.Awake");
            List<prunedPart> renamedFilesList = FileOperations.Instance.loadRenamedFiles();
           // Log.Info("sizeof renamedFilesList: " + renamedFilesList.Count.ToString());
            foreach (prunedPart pp in renamedFilesList)
            {
                if (pp != null)
                {
                    if (pp.partName != null && pp.path != null)
                    {
                        Log.Info("partName: " + pp.partName + "    path: " + pp.path);
                        int i = pp.path.IndexOf(".prune");
                        if (i > 0)
                        {
                            string fname = pp.path.Substring(0, i);
                            if (fname != null)
                                Log.Info("checking for: " + fname);
                            if (File.Exists(FileOperations.CONFIG_BASE_FOLDER + fname))
                            {
                                Log.Info("File exists, need to rename it");
                                // following function will delete the older ".prune" file and rename the new one
                                FileOperations.Instance.RenameFile(fname);
                            }
                        }
                    }
                }
            }

        }
    }

}
