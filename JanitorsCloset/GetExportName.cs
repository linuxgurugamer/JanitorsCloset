using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;


namespace JanitorsCloset
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class GetExportName : MonoBehaviour
    {
        public static GetExportName Instance;

        private int myWindowId;
        // must be unique for Unity to not mash two nametag windows togehter.
        private new bool enabled = false;
        private Rect windowRect;

        const int WINDOW_WIDTH = 300;
        const int WINDOW_HEIGHT = 100;

        string blackListName = "";

        public string getBlackListName()
        {
            return blackListName;
        }

        void Start()
        {
            Instance = this;
        }

        void Awake()
        {

        }

        public void Invoke()
        {
            blackListName = "";
            myWindowId = JanitorsCloset.getNextID(); // GetInstanceID(); // Use the Id of this MonoBehaviour to guarantee unique window ID.

            windowRect = new Rect(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT);
            windowRect.center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            SetEnabled(true);
        }

        private void SetEnabled(bool newVal)
        {
            // ReSharper disable once RedundantCheckBeforeAssignment
            if (newVal != enabled)
                enabled = newVal;
        }

        public void Close()
        {
            SetEnabled(false);
        }

        public void OnDestroy()
        {
            SetEnabled(false);
        }

        void OnGUI()
        {

            if (!enabled)
                return;

            //	Log.Info("PDPNWindow.OnGUI");
            GUI.skin = HighLogic.Skin;
            windowRect = GUILayout.Window(myWindowId, windowRect, Window, "Enter Export Blacklist Name");

        }

        bool IsValidFilename(string testName)
        {
            Regex containsABadCharacter = new Regex("["
                  + Regex.Escape(new string(System.IO.Path.GetInvalidPathChars())) + "]");
            if (containsABadCharacter.IsMatch(testName)) { return false; };

            // other checks for UNC, drive-path format, etc

            return true;
        }

        public void Window(int windowID)
        {
            if (!enabled)
                return;
            GUI.skin = HighLogic.Skin;

            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Enter name:");
            GUILayout.FlexibleSpace();
            string nblackListName = GUILayout.TextField(blackListName, GUILayout.MinWidth(160f));
            if (IsValidFilename(nblackListName))
                blackListName = nblackListName;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
             if (GUILayout.Button("Export"))
            {
                FileOperations.Instance.exportBlackListData(blackListName, JanitorsCloset.blackList);
                
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                blackListName = "";

                Close();
            }

            GUILayout.EndHorizontal();
            GUI.DragWindow();



        }
    }
}
