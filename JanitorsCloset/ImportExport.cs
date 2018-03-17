using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;
using ClickThroughFix;

namespace JanitorsCloset
{

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class ImportExport : MonoBehaviour
    {
        private const int WIDTH = 200;
        private const int HEIGHT = 50;
        private Rect configBounds;
        private bool isVisible = false;
        public static ImportExport Instance;

        Rect _windowRect = new Rect()
        {
            xMin = Screen.width - 325,
            xMax = Screen.width - 185,
            yMin = Screen.height - 300,
            yMax = 50 //0 height, GUILayout resizes it
        };



        void Awake()
        {
            //configBounds = new Rect(0,0, WIDTH, HEIGHT);
            //configBounds.center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            configBounds = _windowRect;
        }
        void Start()
        {
            InitializeGUI();
        }

        int configWindowID;

        public void SetVisible(bool b)
        {
            isVisible = b;
            if (b)
                configWindowID = JanitorsCloset.getNextID();
        }

        void OnGUI()
        {
            if (isVisible)
            {
                //this.configBounds = ClickThruBlocker.GUILayoutWindow(GetInstanceID(), configBounds, ConfigWindow, "Import/Export", HighLogic.Skin.window);
                string _windowTitle = string.Format("Import/Export");
                var tstyle = new GUIStyle(GUI.skin.window);

                configBounds.yMax = _windowRect.yMin;
                configBounds = ClickThruBlocker.GUILayoutWindow(configWindowID, configBounds, ConfigWindow, _windowTitle, tstyle);
            }
        }

        ImportExportSelect ies = null;
        void InitializeGUI()
        {
            ies = this.gameObject.AddComponent<ImportExportSelect>();
            Instance = this;
        }

        //static GetExportName gen;

        private void ConfigWindow(int id)
        {

            GUILayout.BeginVertical();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export"))
            {
                //GameObject gObj = new GameObject("GetExportName", typeof(GetExportName));
                //DontDestroyOnLoad(gObj);
                //gen = (GetExportName)gObj.GetComponent(typeof(GetExportName));

                //gen.Invoke();
                GetExportName.Instance.Invoke();
                SetVisible(false);
                return;
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Import"))
            {
                SetVisible(false);
                ies.show();

            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Cancel"))
            {
                SetVisible(false);
                return;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

    }
}
