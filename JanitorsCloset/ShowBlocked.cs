using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;
namespace JanitorsCloset
{
    class ShowBlocked : MonoBehaviour
    {
        const int WIDTH = 400;
        const int HEIGHT = 500;

        List<blackListPart> blpList;

        Rect _windowRect = new Rect()
        {
            xMin = 0,
            xMax = WIDTH,
            yMin = 0,
            yMax = HEIGHT
        };


        public static ShowBlocked Instance { get; private set; }

        void Awake()
        {
            Log.Info("ShowBlocked Awake()");
            this.enabled = false;
            Instance = this;
            _windowRect.center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        public void Show()
        {
            enabled = true;
            //blpList.AddRange(JanitorsCloset.blackList.values);
            blpList = new List<blackListPart>(JanitorsCloset.blackList.Values);
            blpList.Sort((x, y) => x.modName.CompareTo(y.modName));

        }

        public bool isEnabled()
        {
            return this.enabled;
        }

        void CloseWindow()
        {
            this.enabled = false;
            
            Log.Info("ShowBlocked.CloseWindow enabled: " + this.enabled.ToString());
        }

        int blockedWindowContentID = 0;

        void OnGUI()
        {
            if (isEnabled())
            {
                if (blockedWindowContentID == 0)
                    blockedWindowContentID = JanitorsCloset.getNextID();
                var tstyle = new GUIStyle(GUI.skin.window);
                
                _windowRect = GUILayout.Window(blockedWindowContentID, _windowRect, BlockedWindowContent, "Show Blocked Parts", tstyle);
            }
        }


        public void clearBlackList()
        {
            JanitorsCloset.blackList.Clear();
            EditorPartList.Instance.Refresh();
            FileOperations.Instance.saveBlackListData(JanitorsCloset.blackList);
        }

        Vector2 sitesScrollPosition;
        const int LINEHEIGHT = 30;

        const int MODNAMEWIDTH = 225;
        const int WHEREWIDTH = 55;

        bool sortAscending = true;
        string lastSort = "";

        void BlockedWindowContent(int windowID)
        {
            string unblock = "";
            blackListPart unblockBlp = null;
             
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mod Name", GUILayout.Width(MODNAMEWIDTH)))
            {
                if (lastSort != "modname")
                    sortAscending = true;
                else
                    sortAscending = !sortAscending;
                if (sortAscending)
                    blpList.Sort((x, y) => x.title.CompareTo(y.title));
                else
                    blpList.Sort((y, x) => x.title.CompareTo(y.title));
                lastSort = "modname";
            }
            if (GUILayout.Button("Where", GUILayout.Width(WHEREWIDTH)))
            {
                if (lastSort != "where")
                    sortAscending = true;
                else
                    sortAscending = !sortAscending;
                if (sortAscending)
                    blpList.Sort((x, y) => x.where.CompareTo(y.where));
                else
                    blpList.Sort((y, x) => x.where.CompareTo(y.where));
                lastSort = "where";
            }
            if (GUILayout.Button("Unblock All"))
            {
                clearBlackList();
                CloseWindow();
            }
            GUILayout.EndHorizontal();
            sitesScrollPosition = GUILayout.BeginScrollView(sitesScrollPosition, false, true, GUILayout.Height(HEIGHT - LINEHEIGHT));

           // foreach (var blp in JanitorsCloset.blackList)
           foreach (var blp in blpList)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(blp.title, GUILayout.Width(MODNAMEWIDTH));
                GUILayout.Label(blp.where.ToString(), GUILayout.Width(WHEREWIDTH));
                
                GUILayout.FlexibleSpace();
                //AvailablePart p = PartLoader.Instance.parts.Find(item => item.name.Equals(blp.modName));
                AvailablePart p = PartLoader.getPartInfoByName(blp.modName);
  
               
                if (p == null || blp.permapruned)
                    GUI.enabled = false;
                if (GUILayout.Button("Unblock", GUILayout.ExpandWidth(true)))
                {
                    unblock = blp.modName;
                    unblockBlp = blp;
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                CloseWindow();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUI.DragWindow();

            if (unblock != "")
            {
                JanitorsCloset.blackList.Remove(unblock);
                EditorPartList.Instance.Refresh();
                blpList.Remove(unblockBlp);

            }

        }
    }
}
