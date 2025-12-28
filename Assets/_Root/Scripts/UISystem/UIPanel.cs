using System;
using UnityEngine;

namespace UISystem.Core
{
    /// <summary>
    /// The fundamental abstract base class for all UI elements managed by the <see cref="UIManager"/>.
    /// <para>
    /// This class enforces a standard lifecycle (Initialize -> Show -> Hide -> Cleanup) and handles 
    /// common functionality such as Canvas management, visibility states, and interaction blocking.
    /// Concrete implementations (Screens, Popups, Toasts) must inherit from this class.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField]
        [Tooltip("Reference to the Canvas component controlling render order.")]
        private Canvas _canvas;
        
        [SerializeField] 
        [Tooltip("Reference to the CanvasGroup component controlling opacity and interaction.")]
        private CanvasGroup _canvasGroup;
        
        private UIManager _uiManager;
        private bool _isInitialized;
        private Action<object> _onCloseCallback;
        
        #region Properties
        
        /// <summary>
        /// Direct reference to the Canvas component. Used for sorting order manipulation.
        /// </summary>
        public Canvas Canvas => _canvas;

        /// <summary>
        /// Direct reference to the CanvasGroup component. Used for alpha and interaction control.
        /// </summary>
        public CanvasGroup CanvasGroup => _canvasGroup;
        
        /// <summary>
        /// The configuration asset that defines this panel's behavior and settings.
        /// </summary>
        public UIPanelConfig Config { get; private set; }

        /// <summary>
        /// Returns true if the panel is currently in the 'Shown' state (alpha = 1, interactable).
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Returns true if the panel is currently playing an entrance or exit animation.
        /// <br/> Interactions are typically blocked during transitions.
        /// </summary>
        public bool IsTransitioning { get; private set; }

        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_canvas == null)
                _canvas = GetComponent<Canvas>();
            
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            
            if (_canvas == null || _canvasGroup == null)
            {
                Debug.LogError($"UIPanel {gameObject.name} is missing Canvas or CanvasGroup component!", this);
            }
        }
        
        private void OnDestroy()
        {
            if (_isInitialized)
            {
                OnCleanup();
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Performs the one-time initialization of the panel.
        /// <para>
        /// This method acts as a pseudo-constructor, injecting dependencies like the UIManager and Config
        /// before calling the concrete <see cref="OnInitialize"/> implementation.
        /// </para>
        /// </summary>
        /// <param name="panelConfig">The configuration asset for this panel.</param>
        /// <param name="manager">The central UIManager instance.</param>
        public void Initialize(UIPanelConfig panelConfig, UIManager manager)
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"Panel {gameObject.name} is already initialized!");
                return;
            }
            
            Config = panelConfig;
            _uiManager = manager;
            
            SetVisibility(false, immediate: true);
            
            OnInitialize();
            _isInitialized = true;
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Initiates the sequence to display the panel.
        /// <br/> Flow: State Check -> OnShow() Hook -> PlayShowAnimation() -> Visibility Enable.
        /// </summary>
        /// <param name="data">Optional context data passed to the panel (e.g., an Item ID).</param>
        /// <param name="closeCallback">Optional callback to be invoked when this panel is eventually closed.</param>
        public void Show(object data = null, Action<object> closeCallback = null)
        {
            if (!_isInitialized)
            {
                Debug.LogError($"Cannot show panel {gameObject.name} - not initialized!");
                return;
            }
            
            if (IsTransitioning)
            {
                Debug.LogWarning($"Panel {gameObject.name} is already transitioning!");
                return;
            }
            
            _onCloseCallback = closeCallback;
            IsTransitioning = true;
            
            OnShow(data);
            
            PlayShowAnimation(() =>
            {
                SetVisibility(true);
                IsTransitioning = false;
            });
        }
        
        /// <summary>
        /// Initiates the sequence to hide the panel.
        /// <br/> Flow: State Check -> OnHide() Hook -> PlayHideAnimation() -> Visibility Disable -> Callback.
        /// </summary>
        /// <param name="resultData">Optional data to return to the caller via the close callback.</param>
        public void Hide(object resultData = null)
        {
            if (!_isInitialized)
            {
                Debug.LogError($"Cannot hide panel {gameObject.name} - not initialized!");
                return;
            }
            
            if (IsTransitioning)
            {
                Debug.LogWarning($"Panel {gameObject.name} is already transitioning!");
                return;
            }
            
            IsTransitioning = true;
            
            OnHide();
            
            PlayHideAnimation(() =>
            {
                SetVisibility(false);
                IsTransitioning = false;
                
                _onCloseCallback?.Invoke(resultData);
                _onCloseCallback = null;
            });
        }
        
        #endregion
        
        #region Visibility & Interaction
        
        /// <summary>
        /// Internal method to toggle the visual state of the panel using CanvasGroup.
        /// </summary>
        private void SetVisibility(bool visible, bool immediate = false)
        {
            IsVisible = visible;
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
        }
        
        /// <summary>
        /// Modifies the interactability of the panel without changing its visibility.
        /// Used by global interaction blockers (e.g., during cutscenes).
        /// </summary>
        /// <param name="interactable">If true, the panel receives input events.</param>
        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = interactable;
                _canvasGroup.blocksRaycasts = interactable;
            }
        }
        
        /// <summary>
        /// Dynamically updates the sorting order of the panel's Canvas.
        /// Used for managing the z-order of stacked popups.
        /// </summary>
        public void SetSortOrder(int sortOrder)
        {
            if (_canvas != null)
            {
                _canvas.sortingOrder = sortOrder;
            }
        }
        
        #endregion
        
        #region Lifecycle Hooks - Virtual Methods for Override

        /// <summary>
        /// A concrete implementation hook executed exactly once during initialization.
        /// <br/> Use this method to subscribe to events, find child references, or perform one-time setup (similar to Awake/Start).
        /// </summary>
        protected abstract void OnInitialize();
        
        /// <summary>
        /// A concrete implementation hook executed every time the panel is requested to show.
        /// <br/> Use this method to refresh UI elements, update text, or process the passed <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The optional context data payload.</param>
        protected abstract void OnShow(object data = null);
        
        /// <summary>
        /// A concrete implementation hook executed when the panel begins its hiding sequence.
        /// <br/> Use this method to reset scroll positions, clear input fields, or stop coroutines.
        /// </summary>
        protected abstract void OnHide();

        /// <summary>
        /// A concrete implementation hook executed when the panel is being destroyed.
        /// <br/> Use this method to unsubscribe from events and release resources.
        /// </summary>
        protected abstract void OnCleanup();
        
        #endregion
        
        #region Animation Hooks - Virtual Methods for Override
        
        /// <summary>
        /// Defines the entrance animation logic.
        /// <para>
        /// Override this method to implement custom animations (e.g., DOTween).
        /// <b>IMPORTANT:</b> You MUST invoke <paramref name="onComplete"/> when the animation finishes, or the UI state will hang.
        /// </para>
        /// </summary>
        /// <param name="onComplete">Callback to invoke when animation ends.</param>
        protected virtual void PlayShowAnimation(Action onComplete)
        {
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Defines the exit animation logic.
        /// <para>
        /// Override this method to implement custom animations (e.g., DOTween).
        /// <b>IMPORTANT:</b> You MUST invoke <paramref name="onComplete"/> when the animation finishes, or the UI state will hang.
        /// </para>
        /// </summary>
        /// <param name="onComplete">Callback to invoke when animation ends.</param>
        protected virtual void PlayHideAnimation(Action onComplete)
        {
            onComplete?.Invoke();
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// A convenience shortcut to close this panel instance via the UIManager.
        /// </summary>
        protected void CloseSelf(object resultData = null)
        {
            _uiManager?.HidePanel(this, resultData);
        }
        
        /// <summary>
        /// A convenience shortcut to request opening another panel from within this panel.
        /// </summary>
        protected void RequestOpenPanel<T>(object data = null, Action<object> callback = null) where T : UIPanel
        {
            _uiManager?.ShowPanel<T>(data, callback);
        }
        
        #endregion
    }
}