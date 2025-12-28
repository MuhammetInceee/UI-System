using UISystem.Attributes;
using UnityEngine;

namespace UISystem.Core
{
    /// <summary>
    /// A ScriptableObject that defines the metadata, instantiation rules, and runtime behavior for a specific UI Panel.
    /// <para>
    /// This configuration acts as the "blueprint" for the <see cref="UIManager"/>, separating the panel's data 
    /// from its logic. It dictates how the panel is loaded into memory, how it interacts with the navigation stack, 
    /// and its visual rendering properties.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "PanelConfig", menuName = "UI System/Panel Config", order = 0)]
    public class UIPanelConfig : ScriptableObject
    {
        [Header("Panel Identity")]
        [Tooltip("A unique string identifier for this panel. If left empty, it will be auto-generated from the prefab name.")]
        [ID]
        [ReadOnly]
        public string panelID = System.Guid.NewGuid().ToString();
        
        [Tooltip("The actual UI prefab GameObject (must contain a class inheriting from UIPanel) to be instantiated.")]
        public GameObject panelPrefab;
        
        [Header("Panel Behavior")]
        [Tooltip("Defines the structural role of this panel (e.g., Full Screen, Overlay Popup, or Ephemeral Toast).")]
        public PanelType panelType = PanelType.Popup;
        
        [Tooltip("Determines the memory management strategy. 'Preload' creates it at startup; 'LazyLoad' creates it only when requested.")]
        public LoadType loadType = LoadType.LazyLoad;
        
        [Tooltip("Defines the input priority. Higher layers (like 'Critical') remain interactive even when the global game state blocks lower layers.")]
        public InteractionLayer interactionLayer = InteractionLayer.Normal;
        
        [Header("Navigation")]
        [Tooltip("If true, the hardware Back button (or Escape key) will close this panel when it is at the top of the stack.")]
        public bool allowBackButton = true;
        
        [Tooltip("If true, this popup will persist and remain open even when the underlying 'Screen' is changed. Useful for persistent overlays like chat or music controls.")]
        public bool keepOnScreenChange = false;
        
        [Header("Canvas Settings")]
        [Tooltip("The starting sorting order for this panel's Canvas. Popups will automatically increment this value to stack correctly.")]
        public int baseSortOrder = 100;
        
        [Tooltip("The rendering mode for the Canvas. Typically 'ScreenSpaceOverlay' for standard UI.")]
        public RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
        
        [Tooltip("If true, this panel's Canvas will handle its own sorting order, independent of parent canvases.")]
        public bool overrideSorting = true;
        
        [Tooltip("If true, UI elements will snap to the nearest pixel boundaries for sharper rendering. May have a slight performance cost.")]
        public bool pixelPerfect = false;
        
        [Header("Toast Settings (Only for Toast type)")]
        [Tooltip("The duration (in seconds) before a Toast notification automatically closes itself.")]
        public float toastDuration = 3f;
        
        /// <summary>
        /// Unity Editor validation hook.
        /// Ensures the Panel ID is valid and sanitizes configuration values to prevent runtime logic errors.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(panelID) && panelPrefab != null)
            {
                panelID = panelPrefab.name;
            }
            
            if (panelType == PanelType.Toast)
            {
                if (toastDuration <= 0)
                {
                    Debug.LogWarning($"[UIPanelConfig] Toast panel '{panelID}' has an invalid duration ({toastDuration}). Resetting to default 3 seconds.");
                    toastDuration = 3f;
                }
            }
        }
    }
}