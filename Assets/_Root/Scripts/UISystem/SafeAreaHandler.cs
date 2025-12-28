using UnityEngine;

namespace UISystem.Core
{
    /// <summary>
    /// Manages the RectTransform of a UI element to ensure it stays within the device's logical "Safe Area".
    /// <para>
    /// This is essential for modern mobile devices with physical limitations such as notches, camera cutouts, 
    /// rounded corners, or software system bars (e.g., iPhone Home indicator).
    /// </para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If enabled, the safe area constraints are calculated and applied immediately when the script initializes.")]
        [SerializeField] 
        private bool _applyOnStart = true;

        [Tooltip("If enabled, the script checks for resolution or safe area changes every frame. Useful for handling device rotation or foldable screen resizing at runtime.")]
        [SerializeField] 
        private bool _updateEveryFrame = false;
        
        [Header("Debug")]
        [SerializeField] 
        [Tooltip("If enabled, logs the calculated safe area pixel values and screen resolution to the console for debugging layout issues.")]
        private bool _showDebugInfo = false;
        
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }
        
        private void Start()
        {
            if (_applyOnStart)
            {
                ApplySafeArea();
            }
        }
        
        private void Update()
        {
            if (_updateEveryFrame)
            {
                CheckAndApplySafeArea();
            }
        }
        
        /// <summary>
        /// Monitors the screen state and applies the safe area only if a change in resolution or safe area dimensions is detected.
        /// </summary>
        private void CheckAndApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            
            if (safeArea != _lastSafeArea || screenSize != _lastScreenSize)
            {
                ApplySafeArea();
            }
        }
        
        /// <summary>
        /// Calculates the Safe Area boundaries in normalized anchor space (0 to 1) and applies them to the RectTransform.
        /// <para>
        /// This method converts the pixel-based <see cref="Screen.safeArea"/> into proportional anchor coordinates,
        /// ensuring the UI element stretches correctly within the safe zone regardless of the actual screen resolution.
        /// </para>
        /// </summary>
        public void ApplySafeArea()
        {
            if (_rectTransform == null)
                return;
            
            Rect safeArea = Screen.safeArea;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            
            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;
            
            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            
            if (_showDebugInfo)
            {
                Debug.Log($"[SafeAreaHandler] Applied Safe Area: {safeArea} on Screen Size: {screenSize}");
            }
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && _applyOnStart)
            {
                ApplySafeArea();
            }
        }
        #endif
    }
}