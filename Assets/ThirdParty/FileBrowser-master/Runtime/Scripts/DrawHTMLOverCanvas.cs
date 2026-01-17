using System.Runtime.InteropServices;
using UnityEngine;


namespace Netherlands3D.JavascriptConnection
{
    public class DrawHTMLOverCanvas : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void AddFileInput(string inputName, string fileExtentions, bool multiSelect);

        [DllImport("__Internal")]
        private static extern void DisplayDOMObjectWithID(string id = "htmlID", string display = "none", float x = 0,
            float y = 0, float width = 0, float height = 0, float offsetX = 0, float offsetY = 0);

        [SerializeField] private string htmlObjectID = "";
        [SerializeField] private bool alignEveryUpdate = true;
        /// <summary>
        /// If this behaviour is headless, then we assume there is no Unity UI element to attach to and the HTML
        /// element will remain hidden by keeping display on none.
        ///
        /// This field supercedes the alignEveryUpdate field, if this is true then the value in that field is not used.
        /// </summary>
        [SerializeField] private bool headless = false;

        private RectTransform rectTransform;
        private RectTransform canvasRectTransform;
        private Canvas rootCanvas;

#if !UNITY_EDITOR && UNITY_WEBGL
		private void Update()
		{
			if (!headless && alignEveryUpdate)
				AlignHTMLOverlay();
		}

		private void OnEnable()
        {
			rectTransform = GetComponent<RectTransform>();
            if (!rectTransform) {
                headless = true;
            } else {
			    rootCanvas = rectTransform.root.GetComponent<Canvas>();
			    canvasRectTransform = rootCanvas.GetComponentInParent<RectTransform>();
            }

            AlignHTMLOverlay();
        }

        private void OnDisable()
        {
            DisplayDOMObjectWithID(htmlObjectID, "none");
        }
#endif
        /// <summary>
        /// Sets the target html DOM id to follow.
        /// </summary>
        /// <param name="id">The ID (without #)</param>
        /// <param name="headless">
        /// Whether the HTML element should be hidden because it is invoked headless; if there is no rectTransform
        /// then this is always true
        /// </param>
        public void AlignObjectID(string id, bool alignEveryUpdate = true, bool headless = false)
        {
            htmlObjectID = id;
            this.alignEveryUpdate = alignEveryUpdate;
            this.headless = !rectTransform || headless;
        }

        public void SetupInput(string fileInputName, string fileExtentions, bool multiSelect)
        {
            AddFileInput(fileInputName, fileExtentions, multiSelect);
        }

        /// <summary>
        /// Tell JavaScript to make a DOM object with htmlObjectID to align with the Image component
        /// </summary>
        private void AlignHTMLOverlay()
        {
            // If this is a headless component, ensure the DOM element is present, but hidden. 
            if (headless)
            {
                DisplayDOMObjectWithID(htmlObjectID, "none");
                return;
            }

            var canvasScaleFactor = rootCanvas.scaleFactor;
            float canvasheight = canvasRectTransform.rect.height * canvasScaleFactor;
            float canvaswidth = canvasRectTransform.rect.width * canvasScaleFactor;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            DisplayDOMObjectWithID(htmlObjectID, "inline",
                corners[0].x / canvaswidth,
                corners[0].y / canvasheight,
                (corners[2].x - corners[0].x) / canvaswidth,
                (corners[2].y - corners[0].y) / canvasheight
            );
        }
    }
}