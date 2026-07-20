using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

#if true

namespace JanitorsCloset
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    public class JanitorsClosetSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "General Settings"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Janitor's Closet"; } }
        public override string DisplaySection { get { return "Janitor's Closet"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }


        [GameParameters.CustomParameterUI("Toolbar Enabled",
            toolTip = "Enables/hides button in scenes other than the editor (requires restart)")]
        public bool toolbarEnabled = true;

        [GameParameters.CustomParameterUI("Toolbar editor only",
            toolTip = "When enabled, toolbar hide/folder sync and the JC toolbar button only run in the VAB/SPH editor")]
        public bool toolbarEditorOnly = false;

        [GameParameters.CustomParameterUI("Toolbar popups enabled")]
        public bool toolbarPopupsEnabled = true; 

        [GameParameters.CustomParameterUI("Editor Menu Popup")]
        public bool editorMenuPopupEnabled = true; 

        [GameParameters.CustomFloatParameterUI("Hover timeout", minValue = 0.5f, maxValue = 5.0f, asPercentage = false, displayFormat = "0.0",
                   toolTip = "Time popup menu/toolbar stays around after mouse is moved away")]
        public float hoverTimeout = 0.5f;

        [GameParameters.CustomParameterUI("Auto UI scale by resolution",
            toolTip = "When enabled, UI scale follows screen height: 75% at 4K, 85% at 1440p, 100% at 1080p. Disable to use the slider below manually.")]
        public bool uiScaleAuto = true;

        [GameParameters.CustomFloatParameterUI("UI scale (%)", minValue = 50f, maxValue = 150f, stepCount = 100, displayFormat = "N0",
                   toolTip = "Extra multiplier for JC windows on top of KSP UI scale. 100 matches KSP; 50 is half size; 150 is 1.5x. Read-only while auto mode is enabled.")]
        public float uiScalePercent = 100f;

        [GameParameters.CustomParameterUI("Enable hover on icons in the JC toolbar")]
        public bool enabeHoverOnToolbarIcons = true;

        [GameParameters.CustomParameterUI("Use KerboKatz Toolbar for KerboKatz utilities")]
        public bool useKerboKatzToolbar = true;

        [GameParameters.CustomParameterUI("Enable Button Identification")]
        public bool buttonIdent = false;

        [GameParameters.CustomParameterUI("Enable Button Tooltip")]
        public bool buttonTooltip = true;

        [GameParameters.CustomParameterUI("Always show the parent mod in a tooltip", 
            toolTip = "If disabled, then the tooltip will be show when the modifier key is held down")]
        public bool showMod = true;


        [GameParameters.CustomParameterUI("Debug mode (spams the log file")]
        public bool debug = false;

        public void ApplyAutoUiScale()
        {
            if (!uiScaleAuto)
                return;
            uiScalePercent = UIScale.DefaultUiScalePercent;
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (uiScaleAuto)
                ApplyAutoUiScale();

            if (member.Name == "toolbarPopupsEnabled" || member.Name == "toolbarEditorOnly")
                return toolbarEnabled;

            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "uiScalePercent" && uiScaleAuto)
                return false;
            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }

    }
}

#endif