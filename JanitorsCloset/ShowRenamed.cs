using System.Collections.Generic;
using UnityEngine;
using ClickThroughFix;

using static JanitorsCloset.JanitorsClosetLoader;


namespace JanitorsCloset
{
    class ShowRenamed : MonoBehaviour
    {
        //const int WIDTH = Screen.width - 200;
        const int HEIGHT = 500;

        List<string> renamedList;

        Rect renamedWindowRect;

        void UpdateWindowRect()
        {
            var size = UIScale.GuiScreenSize();
            renamedWindowRect = new Rect()
            {
                xMin = 0,
                xMax = size.x - 300,
                yMin = 0,
                yMax = HEIGHT
            };
            renamedWindowRect.center = size * 0.5f;
        }


        public static ShowRenamed Instance { get; private set; }

        void Awake()
        {
            Log.Info("ShowRenamed Awake()");
            this.enabled = false;
            Instance = this;
            UpdateWindowRect();
        }

        public void Show()
        {
            UpdateWindowRect();
            enabled = true;
            renamedList = new List<string>();
        }

        public void addLine(string str)
        {
            renamedList.Add(str);
        }

        public bool isEnabled()
        {
            return this.enabled;
        }

        void CloseWindow()
        {
            this.enabled = false;

            Log.Info("ShowRenamed.CloseWindow enabled: " + this.enabled.ToString());
        }

        int renamedWindowContentID = 0;
        void OnGUI()
        {
            if (isEnabled())
            {
                if (renamedWindowContentID == 0)
                    renamedWindowContentID = JanitorsCloset.getNextID();
                var tstyle = new GUIStyle(GUI.skin.window);
                UIScale.BeginGUI();
                renamedWindowRect = ClickThruBlocker.GUILayoutWindow(renamedWindowContentID, renamedWindowRect, ShowRenamedWindowContent, "Show PermaPruned Parts", tstyle);
                UIScale.EndGUI();
            }
        }

        Vector2 sitesScrollPosition;
        const int LINEHEIGHT = 30;

        const int MODNAMEWIDTH = 225;
        const int WHEREWIDTH = 55;

        private Rect innerCoords;

        void ShowRenamedWindowContent(int windowID)
        {
            innerCoords = new Rect(0, LINEHEIGHT, renamedWindowRect.width - 4, HEIGHT - 2 * LINEHEIGHT);
            
            GUILayout.BeginVertical();

#if false
            sitesScrollPosition = GUILayout.BeginScrollView(sitesScrollPosition, false, true, GUILayout.Height(HEIGHT - LINEHEIGHT));
            foreach (var blp in renamedList)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(blp);
                
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
#endif
            string t = "";
            foreach (var blp in renamedList)
                t += blp + "\n";
//            GUILayout.BeginArea(innerCoords);
            sitesScrollPosition = GUILayout.BeginScrollView(sitesScrollPosition, false, true, GUILayout.Height(HEIGHT - LINEHEIGHT));
            //GUI.enabled = false;
            GUILayout.Label(t);
            //GUI.enabled = true;
            GUILayout.EndScrollView();
 //           GUILayout.EndArea();

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (!PermaPruneWindow.Instance.permapruneInProgress)
            {
                if (GUILayout.Button(" Close "))
                {
                    CloseWindow();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Copy to clipboard"))
                {
                    GUIUtility.systemCopyBuffer = t;
                }
            }
            else
            {
                if (GUILayout.Button(" Cancel "))
                {
                    PermaPruneWindow.Instance.stopPruner();
                }

            }
            
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
