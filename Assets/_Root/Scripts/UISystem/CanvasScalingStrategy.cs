using UnityEngine;

namespace UISystem.Core
{
    /// <summary>
    /// Defines a responsive scaling strategy for UI Canvases, dynamically adjusting the "Match Width Or Height" property
    /// based on the device's aspect ratio.
    /// <para>
    /// This configuration allows the UI to adapt seamlessly across different device form factors, 
    /// distinguishing between Tall (modern phones), Standard (16:9), and Wide (tablets) screens.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "CanvasScalingStrategy", menuName = "UI System/Canvas Scaling Strategy")]
    public class CanvasScalingStrategy : ScriptableObject
    {
        [Header("Reference Resolution")]
        [Tooltip("The base resolution used for UI layout calculations (e.g., 1080x1920).")]
        public Vector2 ReferenceResolution = new Vector2(1080, 1920);

        [Header("Match Settings")]
        [Tooltip("The match value applied for ultra-tall screens. 0 matches Width, 1 matches Height.")]
        [Range(0, 1)]
        public float MatchOnTall = 0f;
        
        [Tooltip("The match value applied for standard phone screens (approx. 16:9 aspect ratio).")]
        [Range(0, 1)] 
        public float MatchOnStandard = 0.5f;
        
        [Tooltip("The match value applied for tablet or square-like screens. 1 matches Height.")]
        [Range(0, 1)]
        public float MatchOnWide = 1f;

        [Header("Aspect Ratio Thresholds")]
        [Tooltip("Screens with an aspect ratio lower than this value will use the 'Tall' match setting.")]
        [SerializeField]
        private float _tallThreshold = 0.428f;

        /// <summary>
        /// The aspect ratio threshold (Width / Height) above which a screen is classified as 'Wide'.
        /// <br/> Example: 3:5 ratio is 0.6.
        /// </summary>
        [Tooltip("Screens with an aspect ratio higher than this value will use the 'Wide' match setting.")]
        [SerializeField]
        private float _wideThreshold = 0.6f;

        /// <summary>
        /// Public accessor for the Tall aspect ratio threshold.
        /// </summary>
        public float TallThreshold => _tallThreshold;

        /// <summary>
        /// Public accessor for the Wide aspect ratio threshold.
        /// </summary>
        public float WideThreshold => _wideThreshold;

        /// <summary>
        /// Determines the optimal 'Match Width Or Height' value for the CanvasScaler by evaluating 
        /// the current screen dimensions against the defined aspect ratio thresholds.
        /// </summary>
        /// <param name="screenWidth">The current width of the screen in pixels.</param>
        /// <param name="screenHeight">The current height of the screen in pixels.</param>
        /// <returns>A float value between 0 and 1 representing the match setting.</returns>
        public float CalculateMatchValue(float screenWidth, float screenHeight)
        {
            // Prevent division by zero
            if (screenHeight <= 0) return MatchOnStandard;
            
            float currentAspect = screenWidth / screenHeight;

            if (currentAspect >= _wideThreshold)
            {
                return MatchOnWide;
            }

            if (currentAspect <= _tallThreshold)
            {
                return MatchOnTall;
            }

            return MatchOnStandard;
        }

        /// <summary>
        /// Categorizes the current screen dimensions into a human-readable format (Tall, Standard, or Wide).
        /// Useful for debugging and analytics.
        /// </summary>
        /// <param name="screenWidth">The current width of the screen.</param>
        /// <param name="screenHeight">The current height of the screen.</param>
        /// <returns>A string description of the aspect ratio category.</returns>
        public string GetAspectCategory(float screenWidth, float screenHeight)
        {
            if (screenHeight <= 0) return "Unknown";
            
            float currentAspect = screenWidth / screenHeight;

            if (currentAspect >= _wideThreshold) return "Wide (Tablet)";
            if (currentAspect <= _tallThreshold) return "Tall (Ultra-wide Phone)";
            return "Standard (16:9)";
        }

        /// <summary>
        /// Validates and clamps threshold values in the Unity Editor to prevent logical errors in configuration.
        /// </summary>
        private void OnValidate()
        {
            _tallThreshold = Mathf.Clamp(_tallThreshold, 0.3f, 0.5f);
            _wideThreshold = Mathf.Clamp(_wideThreshold, 0.55f, 0.8f);
            
            // Ensure reference resolution is never zero or negative
            if (ReferenceResolution.x <= 0) ReferenceResolution.x = 1080;
            if (ReferenceResolution.y <= 0) ReferenceResolution.y = 1920;
        }
    }
}