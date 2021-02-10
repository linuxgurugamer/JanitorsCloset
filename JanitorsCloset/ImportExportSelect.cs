using UnityEngine;
using KSP.UI.Screens;

using static JanitorsCloset.JanitorsClosetLoader;


namespace JanitorsCloset
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class ImportExportSelect : MonoBehaviour
    {
        public static ImportExportSelect Instance { get; private set; }

        protected string m_textPath;

        protected FileBrowser m_fileBrowser = null;
        bool fileBrowserEnabled = false;
        //bool getfileWin = false;
        //private Rect _window;
        // string suffix = "prnlst";
        string lastdir = FileOperations.EXPORTBLACKLISTDIR;

        private const int BR_WIDTH = 600;
        private const int BR_HEIGHT = 500;

        void Start()
        {
            Instance = this;
        }

        // Need for the 
       // private readonly GUILayoutOption[] _emptyOptions = new GUILayoutOption[0];
        // private GUI.WindowFunction _windowFunction;
        // enable/disable this to prevent the "No Target" from popping up by double-clicking on the window 
        private Mouse _mouseController;

        protected /*override */void Awake()
        {
            //_windowFunction = m_fileBrowser.Window;
            _mouseController = HighLogic.fetch.GetComponent<Mouse>();

            //_window.center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            //name = "ClickthroughExample";
        }

        public void show()
        {
            //getfileWin = true;
            getFile("Import", FileOperations.PRNLIST_SUFFIX, lastdir);            
        }

        public void hide()
        {

            fileBrowserEnabled = false;
        }

        private void OnDestroy()
        {
            if (_mouseController) _mouseController.enabled = true;
        }

        [SerializeField]
        protected Texture2D m_directoryImage,
                         m_fileImage;
        private string selectionType;

        void getFile(string title, string suffix, string dir = "")
        {
            fileBrowserEnabled = true;
            selectionType = suffix;
            browserWindowID = JanitorsCloset.getNextID();

            m_fileBrowser = new FileBrowser(
                    new Rect(Screen.width / 2 - BR_WIDTH / 2, Screen.height / 2 - BR_HEIGHT / 2, BR_WIDTH, BR_HEIGHT),
                    title,
                    FileSelectedCallback
            );

            if (!m_directoryImage)
            {
                Log.Info("folder icon: " + FileOperations.TEXTURE_DIR + "folder");
                if (GameDatabase.Instance.ExistsTexture(FileOperations.TEXTURE_DIR + "folder"))
                {
                    Log.Info("folder icon loaded");
                    m_directoryImage = GameDatabase.Instance.GetTexture(FileOperations.TEXTURE_DIR + "folder", false);
                }
            }
            if (!m_fileImage)
            {
                Log.Info("file icon: " + FileOperations.TEXTURE_DIR + "file");
                if (GameDatabase.Instance.ExistsTexture(FileOperations.TEXTURE_DIR + "AppLauncherIcon"))
                {
                    m_fileImage = GameDatabase.Instance.GetTexture(FileOperations.TEXTURE_DIR + "AppLauncherIcon", false);
                    Log.Info("file icon loaded");
                }
            }
            // Linux change may needed here
            m_fileBrowser.SelectionPattern = "*" + suffix;
            if (m_directoryImage != null)
                m_fileBrowser.DirectoryImage = m_directoryImage;
            if (m_fileImage != null)
                m_fileBrowser.FileImage = m_fileImage;
            m_fileBrowser.showDrives = true;
            m_fileBrowser.ShowNonmatchingFiles = false;
            //m_fileBrowser.BrowserType = FileBrowserType.Directory;

            if (dir != "")
            {
                m_fileBrowser.SetNewDirectory(dir);
            }
            else
            {
                if (m_textPath != "")
                    m_fileBrowser.SetNewDirectory(m_textPath);
            }
            //getfileWin = false;
        }

        bool _weLockedInputs = false;
        private bool MouseIsOverWindow()
        {
            if (m_fileBrowser != null)               
                return m_fileBrowser.m_screenRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            return false;
        }

        //Lifted this more or less directly from the Kerbal Engineer source. Thanks cybutek!
        private void PreventEditorClickthrough()
        {
            bool mouseOverWindow = MouseIsOverWindow();
            if (!_weLockedInputs && mouseOverWindow)
            {
                EditorLogic.fetch.Lock(true, true, true, "JanitorsCloset");
                _weLockedInputs = true;
            }
            if (!_weLockedInputs || mouseOverWindow) return;
            EditorLogic.fetch.Unlock("JanitorsCloset");
            _weLockedInputs = false;
        }

        int browserWindowID;

        void OnGUI()
        {
            if (m_fileBrowser != null)
            {
                if (!fileBrowserEnabled)
                {

                    m_fileBrowser = null;

                    //this one closes the dropdown if you click outside the window elsewhere
                    //	styleItems.CloseOnOutsideClick();

                }
                else
                {

                    //	GUI.skin = HighLogic.Skin;
#if true
                    GUIStyle s = new GUIStyle(HighLogic.Skin.textField);

                    s.onNormal = HighLogic.Skin.textField.onNormal;

                    //			s.fontSize = 15;
                    s.name = "listitem";
                    s.alignment = TextAnchor.MiddleLeft;
                    //s.fontStyle = FontStyle.Bold;
                    //s.fixedHeight = 50;
                    s.imagePosition = ImagePosition.ImageLeft;

                    GUI.skin = HighLogic.Skin;
                    GUI.skin.customStyles[0] = s;
#endif
                    //m_fileBrowser.m_screenRect = KSPUtil.ClampRectToScreen(ClickThruBlocker.GUILayoutWindow(this.GetInstanceID(), m_fileBrowser.m_screenRect, _windowFunction, "Blocker Menu"));
                    // m_fileBrowser.m_screenRect = ClickThruBlocker.GUILayoutWindow(GetInstanceID(), m_fileBrowser.m_screenRect, m_fileBrowser.Window, "");
                    m_fileBrowser.Window(browserWindowID);
                    // If the mouse is over our window, then lock the rest of the UI
                    if (HighLogic.LoadedSceneIsEditor) PreventEditorClickthrough();
                    return;
                }
            }
        }

        protected void FileSelectedCallback(string path)
        {
            m_fileBrowser = null;
            if (path == null)
                Log.Info("FileSelectedCallback path is null");
            if (path == null || path.Length == 0)
                return;
 
            m_textPath = path;

            int x = path.LastIndexOf("/");
            if (x < 0)
                x = path.LastIndexOf("\\");
            if (x > 0)
                lastdir = path.Substring(0, x);

            Log.Info("file selected: " + m_textPath);
            JanitorsCloset.blackList = FileOperations.Instance.importBlackListData(m_textPath);
            EditorPartList.Instance.Refresh();
        }
    }
}
