//
// Copied with permission from Firespitter
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace JanitorsCloset
{

    internal class HelpPopup
    {
        private string popupWinName;
        private string text = string.Empty;
        private bool textInitialized = false;
        public string windowTitle = string.Empty;
        private Rect helpPopupWindow = new Rect(500f, 300f, 600f, 500f);
        private Rect scrollRect;
        private Rect textRect;
        public bool scrollBar = true;
        public bool showMenu = false;
        public bool showCloseButton = true;
        public int GUIlayer = 0;
        public GUIStyle style;
        Color textColor = Color.white;
        private Vector2 scrollPosition = Vector2.zero;
        private GUIContent content;

        private float textAreaHeight;


        internal void SetWinName(string s)
        {
            helpPopupWindow = new Rect(500f, 300f, 600f, 500f);

            popupWinName = s;

            if (popupWinName == "FilterHelpWindow")
            {
                if (JanitorsCloset.GetModFilterWin != null)
                {
                    if (JanitorsCloset.GetFilterHelpWindow != null)
                    {
                        var rect = (Rect)JanitorsCloset.GetFilterHelpWindow;
                        helpPopupWindow.x = rect.x;
                        helpPopupWindow.y = rect.y;
                    }
                }
            }
            else
            {
                if (JanitorsCloset.GetHelpPopupWinRect != null)
                {
                    if (JanitorsCloset.GetHelpPopupWinRect != null)
                    {
                        var rect = (Rect)JanitorsCloset.GetHelpPopupWinRect;
                        helpPopupWindow.x = rect.x;
                        helpPopupWindow.y = rect.y;
                    }
                }
            }
        }

        void doHelpPopup(string _windowTitle, string _text, int layer)
        {
            text = _text;
            windowTitle = _windowTitle;
            GUIlayer = layer;
        }
        public HelpPopup(string _windowTitle, string _text, int layer)
        {
            doHelpPopup(_windowTitle, _text, layer);
        }
        public HelpPopup(string _windowTitle, string _text, int layer, Rect winRect)
        {
            helpPopupWindow = winRect;
            doHelpPopup(_windowTitle, _text, layer);
        }

        public void setText(string _text)
        {
            content = new GUIContent(_text);
            scrollRect = new Rect(2f, 25f, helpPopupWindow.width - 4f, helpPopupWindow.height - 25f);
            textAreaHeight = style.CalcHeight(content, scrollRect.width - 20f);
            textRect = new Rect(0f, 0f, scrollRect.width - 20f, textAreaHeight);
        }

        private void drawWindow(int ID)
        {
            if (showCloseButton)
            {
                if (GUI.Button(new Rect(helpPopupWindow.width - 18f, 2f, 16f, 16f), ""))
                {
                    showMenu = false;
                }
            }

            scrollPosition = GUI.BeginScrollView(scrollRect, scrollPosition, textRect);
            GUI.TextArea(textRect, content.text, style);
            GUI.EndScrollView();
            GUI.DragWindow();
        }

        private void createStyle()
        {
            style = new GUIStyle(GUI.skin.textArea);
            style.wordWrap = true;
            style.richText = true;
            style.normal.textColor = textColor;
        }

        public void draw()
        {
            if (showMenu)
            {
                if (style == null)
                {
                    createStyle();
                }
                if (!textInitialized)
                {
                    setText(text);
                    textInitialized = true;
                }
                var newHelpPopupWindow = GUI.Window(GUIlayer, helpPopupWindow, drawWindow, windowTitle);
                if (newHelpPopupWindow != helpPopupWindow)
                {
                    helpPopupWindow = newHelpPopupWindow;
                    if (popupWinName == "FilterHelpWindow")
                        JanitorsCloset.SetFilterHelpWindow = helpPopupWindow;
                    else
                        JanitorsCloset.SetHelpPopupWinRect = helpPopupWindow;
                }
            }
        }
    }
}
