using System;
using System.Collections;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;

namespace JanitorsCloset
{
    //
    // Following code contributed by the awesum xEvilReeperx
    //
    public static class EditorIconEvents
    {
        public static readonly EventData<EditorPartIcon, EditorIconClickEvent> OnEditorPartIconClicked =
            new EventData<EditorPartIcon, EditorIconClickEvent>("EditorPartIconClicked");

        public static readonly EventData<EditorPartIcon, bool> OnEditorPartIconHover =
            new EventData<EditorPartIcon, bool>("EditorPartIconHover");

        public class EditorIconClickEvent
        {
            public void Veto() { Vetoed = true; }
            public bool Vetoed { get; private set ; }
        }

        [KSPAddon(KSPAddon.Startup.EditorAny, true)]
        private class InstallEditorIconEvents : MonoBehaviour
        {
            private IEnumerator Start()
            {
                while (EditorPartList.Instance == null) yield return null;

                var prefab = EditorPartList.Instance.partPrefab; 

                InstallReplacementHandler(prefab);

                // some icons have already been instantiated, need to fix those too. Only needed this first time;
                // after that, the prefab will already contain the changes we want to make
                foreach (var icon in EditorPartList.Instance.gameObject.GetComponentsInChildren<EditorPartIcon>(true))
                    InstallReplacementHandler(icon);

                Destroy(gameObject);
            }

            private static void InstallReplacementHandler(EditorPartIcon icon)
            {
                icon.gameObject.AddComponent<ReplacementClickHandler>();
            }
        }

        private class ReplacementClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
        {
            private EditorPartIcon _icon;
            
            private PointerClickHandler _originalClickHandler;
            private Button _button;

            private void Start()
            {
                _button = GetComponent<Button>();
                _originalClickHandler = GetComponent<PointerClickHandler>();
                _icon = GetComponent<EditorPartIcon>();
                

                if (_button == null || _originalClickHandler == null || _icon == null)
                {
                    Log.Error("Couldn't find an expected component");
                    Destroy(this);
                    return;
                }

                _originalClickHandler.enabled = false; // we'll be managing these events instead

                // unhook EditorPartIcon's listener from the button
                // this will allow us to veto any clicks
                _button.onClick.RemoveListener(_icon.MouseInput_SpawnPart);
            }
            public void OnPointerEnter(PointerEventData eventData)
            {                
                OnEditorPartIconHover.Fire(_icon, true);
            }
            public void OnPointerExit(PointerEventData eventData)
            {
                OnEditorPartIconHover.Fire(_icon, false);
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                var evt = new EditorIconClickEvent();

                OnEditorPartIconClicked.Fire(_icon, evt);
                
                if (evt.Vetoed) return;
                _originalClickHandler.OnPointerClick(eventData);

                if (_button.interactable && eventData.button == PointerEventData.InputButton.Left) _icon.MouseInput_SpawnPart();
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                _originalClickHandler.OnPointerDown(eventData);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                _originalClickHandler.OnPointerUp(eventData);
            }
        }
    }
}
