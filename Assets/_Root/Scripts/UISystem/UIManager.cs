using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem.Core
{
    /// <summary>
    /// The central orchestration component of the UI architecture responsible for the entire lifecycle of the user interface.
    /// <para>
    /// Key responsibilities include:
    /// <list type="bullet">
    /// <item><description><b>Panel Management:</b> Instantiating, showing, hiding, and destroying UI panels based on configuration.</description></item>
    /// <item><description><b>Navigation State:</b> Managing the history stacks for Screens and Popups to handle navigation flow.</description></item>
    /// <item><description><b>Device Adaptation:</b> Applying resolution-independent scaling strategies via <see cref="CanvasScalingStrategy"/>.</description></item>
    /// <item><description><b>Memory Management:</b> Handling Preload vs. LazyLoad strategies to optimize RAM usage.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Configuration")] 
        [SerializeField]
        [Tooltip("The registry of all available UI panels and their configuration assets.")]
        private List<UIPanelConfig> _panelConfigs = new List<UIPanelConfig>();

        [Header("Opening Configuration")]
        [Tooltip("The specific Screen panel configuration to instantiate and display immediately upon application startup. Must utilize 'Preload' LoadType.")]
        [SerializeField]
        private UIPanelConfig _openingScreen;

        [Header("Canvas Scaling")]
        [SerializeField]
        [Tooltip("The strategy asset defining how the UI scales across different aspect ratios (e.g., Tablets, Tall Phones).")]
        private CanvasScalingStrategy _scalingStrategy;

        [SerializeField]
        [Tooltip("If enabled, the manager checks for resolution changes every frame to update canvas scaling dynamically (useful for foldable devices or window resizing).")]
        private bool _enableDynamicScaling = true;

        [Header("Container Parents")]
        [Tooltip("The root Transform under which all 'Screen' type panels will be instantiated.")]
        [SerializeField]
        private Transform _screenContainer;

        [Tooltip("The root Transform under which all 'Popup' type panels will be instantiated.")]
        [SerializeField]
        private Transform _popupContainer;

        [Tooltip("The root Transform under which all 'Toast' type panels will be instantiated.")]
        [SerializeField]
        private Transform _toastContainer;

        [Header("Settings")]
        [SerializeField] 
        [Tooltip("If enabled, the manager processes the hardware back button (or Escape key) to navigate the UI stack.")]
        private bool _handleBackButton = true;
        
        [SerializeField] 
        [Tooltip("The key code assigned to trigger the 'Back' navigation logic.")]
        private KeyCode _backButtonKey = KeyCode.Escape;
        
        [SerializeField]
        [Tooltip("The duration (in seconds) the system waits after a 'LazyLoad' panel is hidden before destroying the GameObject to release memory resources.")]
        private float _lazyLoadDestroyDelay = 30f;

        private readonly Stack<UIPanel> _screenStack = new Stack<UIPanel>();
        private readonly Stack<UIPanel> _popupStack = new Stack<UIPanel>();

        private readonly Dictionary<Type, UIPanel> _loadedPanels = new Dictionary<Type, UIPanel>();
        private readonly Dictionary<Type, UIPanelConfig> _configRegistry = new Dictionary<Type, UIPanelConfig>();
        private readonly Dictionary<UIPanel, Coroutine> _lazyLoadCleanupCoroutines = new Dictionary<UIPanel, Coroutine>();

        private int _currentPopupSortOrder = 100;
        private const int POPUP_SORT_ORDER_INCREMENT = 10;

        private InteractionLayer _currentBlockedLayer = InteractionLayer.Normal;
        private bool _isGlobalInteractionEnabled = true;

        private readonly HashSet<CanvasScaler> _managedScalers = new HashSet<CanvasScaler>();
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private bool _isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            UIEvents.OnShowPanelRequested += HandleShowPanelRequest;
            UIEvents.OnHidePanelRequested += HandleHidePanelRequest;
            UIEvents.OnHideAllPopupsRequested += HideAllPopups;
            UIEvents.OnBackButtonPressed += HandleBackButton;
            UIEvents.OnSetGlobalInteraction += SetGlobalInteraction;
        }

        private void OnDisable()
        {
            UIEvents.OnShowPanelRequested -= HandleShowPanelRequest;
            UIEvents.OnHidePanelRequested -= HandleHidePanelRequest;
            UIEvents.OnHideAllPopupsRequested -= HideAllPopups;
            UIEvents.OnBackButtonPressed -= HandleBackButton;
            UIEvents.OnSetGlobalInteraction -= SetGlobalInteraction;
        }

        private void Update()
        {
            if (_handleBackButton && Input.GetKeyDown(_backButtonKey))
            {
                HandleBackButton();
            }

            if (_enableDynamicScaling)
            {
                CheckAndApplyScaling();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Bootstraps the UI system. Validates references, builds registries, sets up scaling, and loads the initial screen.
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
                return;

            ValidateParents();
            ValidateOpeningScreen();
            BuildConfigRegistry();
            InitializeScaling();
            PreloadPanelsAndShowOpening();

            _isInitialized = true;
        }

        private void ValidateParents()
        {
            if (_screenContainer == null) Debug.LogError("[UIManager] Screen container is not assigned!");
            if (_popupContainer == null) Debug.LogError("[UIManager] Popup container is not assigned!");
            if (_toastContainer == null) Debug.LogError("[UIManager] Toast container is not assigned!");
        }

        private void ValidateOpeningScreen()
        {
            if (_openingScreen == null)
            {
                Debug.LogWarning("[UIManager] No opening screen assigned. No panel will be shown at start.");
                return;
            }

            if (_openingScreen.panelType != PanelType.Screen)
            {
                Debug.LogError($"[UIManager] Opening screen '{_openingScreen.panelID}' must be of type Screen, not {_openingScreen.panelType}!");
                _openingScreen = null;
                return;
            }

            if (_openingScreen.loadType != LoadType.Preload)
            {
                Debug.LogError($"[UIManager] Opening screen '{_openingScreen.panelID}' must have LoadType.Preload!");
                _openingScreen = null;
                return;
            }
        }

        /// <summary>
        /// Indexes the panel configurations into a Dictionary for O(1) retrieval during runtime.
        /// </summary>
        private void BuildConfigRegistry()
        {
            _configRegistry.Clear();

            foreach (var config in _panelConfigs)
            {
                if (config == null || config.panelPrefab == null)
                {
                    Debug.LogWarning("[UIManager] Null config or prefab found in panel configs!");
                    continue;
                }

                var panelComponent = config.panelPrefab.GetComponent<UIPanel>();
                if (panelComponent == null)
                {
                    Debug.LogError($"[UIManager] Panel prefab {config.panelPrefab.name} does not have UIPanel component!");
                    continue;
                }

                Type panelType = panelComponent.GetType();

                if (_configRegistry.ContainsKey(panelType))
                {
                    Debug.LogWarning($"[UIManager] Duplicate config for panel type {panelType.Name}. Using first occurrence.");
                    continue;
                }

                _configRegistry.Add(panelType, config);
            }
        }

        /// <summary>
        /// Instantiates all panels marked as 'Preload' and immediately displays the defined Opening Screen.
        /// </summary>
        private async void PreloadPanelsAndShowOpening()
        {
            Type openingScreenType = null;
            if (_openingScreen != null && _openingScreen.panelPrefab != null)
            {
                var openingComponent = _openingScreen.panelPrefab.GetComponent<UIPanel>();
                if (openingComponent != null)
                {
                    openingScreenType = openingComponent.GetType();
                }
            }

            foreach (var kvp in _configRegistry)
            {
                var config = kvp.Value;

                if (config.loadType == LoadType.Preload)
                {
                    UIPanel panel = await LoadPanelAsync(kvp.Key);

                    if (panel != null)
                    {
                        bool isOpeningPanel = (openingScreenType != null && kvp.Key == openingScreenType);

                        if (isOpeningPanel)
                        {
                            ShowScreen(panel, null, null);
                        }
                        else
                        {
                            panel.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        #endregion

        #region Canvas Scaling

        /// <summary>
        /// Initializes the scaling logic by collecting all relevant CanvasScalers and applying the strategy.
        /// </summary>
        private void InitializeScaling()
        {
            if (_scalingStrategy == null)
            {
                Debug.LogWarning("[UIManager] No scaling strategy assigned. Using default Unity scaling.");
                return;
            }

            CollectCanvasScalers();
            ApplyScaling();
        }

        private void CollectCanvasScalers()
        {
            _managedScalers.Clear();

            AddScalerFromTransform(_screenContainer);
            AddScalerFromTransform(_popupContainer);
            AddScalerFromTransform(_toastContainer);

            Canvas mainCanvas = GetComponentInParent<Canvas>();
            if (mainCanvas != null)
            {
                CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    _managedScalers.Add(scaler);
                }
            }

            CanvasScaler selfScaler = GetComponent<CanvasScaler>();
            if (selfScaler != null)
            {
                _managedScalers.Add(selfScaler);
            }
        }

        private void AddScalerFromTransform(Transform container)
        {
            if (container == null) return;

            CanvasScaler scaler = container.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                _managedScalers.Add(scaler);
            }

            Canvas parentCanvas = container.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                CanvasScaler parentScaler = parentCanvas.GetComponent<CanvasScaler>();
                if (parentScaler != null)
                {
                    _managedScalers.Add(parentScaler);
                }
            }
        }

        private void CheckAndApplyScaling()
        {
            if (_scalingStrategy == null) return;

            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                ApplyScaling();
            }
        }

        private void ApplyScaling()
        {
            if (_scalingStrategy == null || _managedScalers.Count == 0) return;

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            float matchValue = _scalingStrategy.CalculateMatchValue(_lastScreenWidth, _lastScreenHeight);

            foreach (CanvasScaler scaler in _managedScalers)
            {
                if (scaler == null) continue;

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = _scalingStrategy.ReferenceResolution;
                scaler.matchWidthOrHeight = matchValue;
            }
        }

        /// <summary>
        /// Forces a recalculation and application of the Canvas Scaling Strategy.
        /// Useful when the device orientation changes or window is resized.
        /// </summary>
        public void RefreshScaling()
        {
            _lastScreenWidth = 0;
            _lastScreenHeight = 0;
            CheckAndApplyScaling();
        }

        /// <summary>
        /// Registers a new CanvasScaler to be managed by the UIManager's scaling logic.
        /// </summary>
        public void RegisterCanvasScaler(CanvasScaler scaler)
        {
            if (scaler != null && _managedScalers.Add(scaler))
            {
                if (_scalingStrategy != null)
                {
                    float matchValue = _scalingStrategy.CalculateMatchValue(Screen.width, Screen.height);
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = _scalingStrategy.ReferenceResolution;
                    scaler.matchWidthOrHeight = matchValue;
                }
            }
        }

        /// <summary>
        /// Unregisters a CanvasScaler from the UIManager.
        /// </summary>
        public void UnregisterCanvasScaler(CanvasScaler scaler)
        {
            _managedScalers.Remove(scaler);
        }

        #endregion

        #region Panel Loading

        /// <summary>
        /// Asynchronously retrieves a panel instance. Instantiates it if it doesn't exist, or returns the cached instance.
        /// <para>Currently uses <see cref="Task.Yield"/> to simulate async behavior, supporting future integration with Addressables.</para>
        /// </summary>
        /// <param name="panelType">The Type of the UIPanel to load.</param>
        private async Task<UIPanel> LoadPanelAsync(Type panelType)
        {
            if (_loadedPanels.TryGetValue(panelType, out UIPanel existingPanel))
            {
                if (_lazyLoadCleanupCoroutines.TryGetValue(existingPanel, out Coroutine cleanup))
                {
                    StopCoroutine(cleanup);
                    _lazyLoadCleanupCoroutines.Remove(existingPanel);
                }

                return existingPanel;
            }

            if (!_configRegistry.TryGetValue(panelType, out UIPanelConfig config))
            {
                Debug.LogError($"[UIManager] No configuration found for panel type {panelType.Name}");
                return null;
            }

            if (config.panelPrefab == null)
            {
                Debug.LogError($"[UIManager] Panel prefab is null for {panelType.Name}");
                return null;
            }

            Transform parent = GetParentForPanelType(config.panelType);

            await Task.Yield();
            GameObject panelObj = Instantiate(config.panelPrefab, parent);
            panelObj.name = panelType.Name;

            UIPanel panel = panelObj.GetComponent<UIPanel>();
            if (panel == null)
            {
                Debug.LogError($"[UIManager] Instantiated prefab does not have UIPanel component: {panelType.Name}");
                Destroy(panelObj);
                return null;
            }

            SetupPanelCanvas(panel, config);
            panel.Initialize(config, this);

            _loadedPanels.Add(panelType, panel);

            return panel;
        }

        private void SetupPanelCanvas(UIPanel panel, UIPanelConfig config)
        {
            Canvas canvas = panel.Canvas;

            if (canvas == null)
            {
                Debug.LogError($"[UIManager] Panel {panel.GetType().Name} does not have a Canvas component!");
                return;
            }

            canvas.renderMode = config.renderMode;
            canvas.overrideSorting = config.overrideSorting;
            canvas.pixelPerfect = config.pixelPerfect;
            canvas.sortingOrder = config.baseSortOrder;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private Transform GetParentForPanelType(PanelType panelType)
        {
            return panelType switch
            {
                PanelType.Screen => _screenContainer,
                PanelType.Popup => _popupContainer,
                PanelType.Toast => _toastContainer,
                _ => _popupContainer
            };
        }

        #endregion

        #region Show/Hide Panels

        /// <summary>
        /// The primary entry point to show any UI panel.
        /// Handles loading, stack management, activation, and transition logic.
        /// </summary>
        /// <typeparam name="T">The type of the Panel to show.</typeparam>
        /// <param name="data">Optional data object to pass to the panel's Setup method.</param>
        /// <param name="closeCallback">Optional callback invoked when this specific panel is closed.</param>
        public async void ShowPanel<T>(object data = null, Action<object> closeCallback = null) where T : UIPanel
        {
            Type panelType = typeof(T);

            UIPanel panel = await GetOrLoadPanel(panelType);
            if (panel == null)
                return;

            if (!panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
            }

            if (!_configRegistry.TryGetValue(panelType, out UIPanelConfig config))
                return;

            switch (config.panelType)
            {
                case PanelType.Screen:
                    ShowScreen(panel, data, closeCallback);
                    break;

                case PanelType.Popup:
                    ShowPopup(panel, data, closeCallback);
                    break;

                case PanelType.Toast:
                    ShowToast(panel, data, config.toastDuration);
                    break;
            }
        }

        private void ShowScreen(UIPanel panel, object data, Action<object> closeCallback)
        {
            Type previousScreenType = _screenStack.Count > 0 ? _screenStack.Peek().GetType() : null;

            if (_screenStack.Count > 0)
            {
                var previousScreen = _screenStack.Peek();
                previousScreen.gameObject.SetActive(false);
            }

            ClosePopupsOnScreenChange();

            if (!panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
            }

            _screenStack.Push(panel);
            panel.Show(data, closeCallback);
            ApplyGlobalInteractionToPanel(panel);

            UIEvents.NotifyPanelShown(panel.GetType());
            UIEvents.NotifyScreenChanged(panel.GetType(), previousScreenType);
        }

        private void ShowPopup(UIPanel panel, object data, Action<object> closeCallback)
        {
            if (_popupStack.Contains(panel))
            {
                panel.Show(data, closeCallback);
                Debug.Log($"Refreshed popup: {panel.GetType().Name}");
                return;
            }

            if (!panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
            }

            _currentPopupSortOrder += POPUP_SORT_ORDER_INCREMENT;
            panel.SetSortOrder(_currentPopupSortOrder);

            _popupStack.Push(panel);
            panel.Show(data, closeCallback);
            ApplyGlobalInteractionToPanel(panel);

            UIEvents.NotifyPanelShown(panel.GetType());
            
            if (_popupStack.Count > 5)
            {
                Debug.LogWarning($"[UIManager] High popup stack count ({_popupStack.Count}). Check for potential logic errors.");
            }
        }

        private void ShowToast(UIPanel panel, object data, float duration)
        {
            if (!panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(true);
            }
            
            panel.Show(data);
            ApplyGlobalInteractionToPanel(panel);

            UIEvents.NotifyPanelShown(panel.GetType());

            StartCoroutine(AutoCloseToast(panel, duration));
        }

        private IEnumerator AutoCloseToast(UIPanel panel, float duration)
        {
            yield return new WaitForSeconds(duration);

            if (panel != null && panel.IsVisible)
            {
                panel.Hide();

                UIEvents.NotifyPanelHidden(panel.GetType());

                if (panel.Config.loadType == LoadType.LazyLoad)
                {
                    ScheduleLazyLoadCleanup(panel);
                }
            }
        }

        /// <summary>
        /// Hides the specified panel and handles stack cleanup.
        /// If the panel is a Screen, it restores the previous screen.
        /// </summary>
        /// <param name="panel">The panel instance to hide.</param>
        /// <param name="resultData">Optional result data to pass back to the caller via callback.</param>
        public void HidePanel(UIPanel panel, object resultData = null)
        {
            if (panel == null)
                return;

            RemoveFromStack(panel);
            panel.Hide(resultData);

            UIEvents.NotifyPanelHidden(panel.GetType());

            if (panel.Config.loadType == LoadType.LazyLoad)
            {
                ScheduleLazyLoadCleanup(panel);
            }
            else if (panel.Config.loadType == LoadType.Preload)
            {
                panel.gameObject.SetActive(false);
            }

            if (panel.Config.panelType == PanelType.Screen && _screenStack.Count > 0)
            {
                var previousScreen = _screenStack.Peek();
                if (!previousScreen.gameObject.activeSelf)
                {
                    previousScreen.gameObject.SetActive(true);
                    previousScreen.Show(null, null);
                }
            } 
        }

        private void RemoveFromStack(UIPanel panel)
        {
            if (_screenStack.Contains(panel))
            {
                List<UIPanel> temp = new List<UIPanel>();
                while (_screenStack.Count > 0)
                {
                    var popped = _screenStack.Pop();
                    if (popped == panel) break;
                    temp.Add(popped);
                }

                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    _screenStack.Push(temp[i]);
                }
            }

            if (_popupStack.Contains(panel))
            {
                List<UIPanel> temp = new List<UIPanel>();
                while (_popupStack.Count > 0)
                {
                    var popped = _popupStack.Pop();
                    if (popped == panel) break;
                    temp.Add(popped);
                }

                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    _popupStack.Push(temp[i]);
                }

                if (_popupStack.Count > 0)
                {
                    _currentPopupSortOrder = _popupStack.Peek().Canvas.sortingOrder;
                }
                else
                {
                    _currentPopupSortOrder = 100;
                }
            }
        }

        #endregion

        #region Stack Management

        private void ClosePopupsOnScreenChange()
        {
            List<UIPanel> popupsToClose = new List<UIPanel>();

            foreach (var popup in _popupStack)
            {
                if (!popup.Config.keepOnScreenChange)
                {
                    popupsToClose.Add(popup);
                }
            }

            foreach (var popup in popupsToClose)
            {
                HidePanel(popup);
            }
        }

        /// <summary>
        /// Closes all active popups immediately. Typically used when returning to a main menu or home screen.
        /// </summary>
        public void HideAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                var popup = _popupStack.Pop();
                popup.Hide();
                UIEvents.NotifyPanelHidden(popup.GetType());

                if (popup.Config.loadType == LoadType.LazyLoad)
                {
                    ScheduleLazyLoadCleanup(popup);
                }
                else if (popup.Config.loadType == LoadType.Preload)
                {
                    popup.gameObject.SetActive(false);
                }
            }

            _currentPopupSortOrder = 100;
        }

        #endregion

        #region Back Button

        /// <summary>
        /// Processes the 'Back' logic based on the current stack state.
        /// <br/> Order of operations: Close Top Popup -> Close Top Screen -> Game Exit/Event.
        /// </summary>
        public void HandleBackButton()
        {
            if (_popupStack.Count > 0)
            {
                var topPopup = _popupStack.Peek();

                if (topPopup.Config.allowBackButton && !topPopup.IsTransitioning)
                {
                    HidePanel(topPopup);
                    return;
                }
            }

            if (_screenStack.Count > 1)
            {
                var topScreen = _screenStack.Peek();

                if (topScreen.Config.allowBackButton && !topScreen.IsTransitioning)
                {
                    HidePanel(topScreen);
                }
            }
        }

        #endregion

        #region Global Interaction Control

        /// <summary>
        /// Controls the interactivity of the entire UI system.
        /// Useful for blocking input during cinematics, tutorials, or network requests.
        /// </summary>
        /// <param name="enabled">If false, panels below the blocked layer become non-interactive.</param>
        /// <param name="blockedLayer">The minimum interaction layer that remains active.</param>
        public void SetGlobalInteraction(bool enabled, InteractionLayer blockedLayer = InteractionLayer.Normal)
        {
            _isGlobalInteractionEnabled = enabled;
            _currentBlockedLayer = blockedLayer;

            foreach (var panel in _loadedPanels.Values)
            {
                if (panel.IsVisible)
                {
                    ApplyGlobalInteractionToPanel(panel);
                }
            }
        }

        private void ApplyGlobalInteractionToPanel(UIPanel panel)
        {
            if (!_isGlobalInteractionEnabled)
            {
                bool shouldBeInteractable = panel.Config.interactionLayer > _currentBlockedLayer;
                panel.SetInteractable(shouldBeInteractable);
            }
            else
            {
                panel.SetInteractable(true);
            }
        }

        #endregion

        #region Lazy Load Cleanup

        private void ScheduleLazyLoadCleanup(UIPanel panel)
        {
            if (_lazyLoadCleanupCoroutines.TryGetValue(panel, out Coroutine existingCoroutine))
            {
                StopCoroutine(existingCoroutine);
            }

            Coroutine cleanup = StartCoroutine(LazyLoadCleanupRoutine(panel));
            _lazyLoadCleanupCoroutines[panel] = cleanup;
        }

        private IEnumerator LazyLoadCleanupRoutine(UIPanel panel)
        {
            yield return new WaitForSeconds(_lazyLoadDestroyDelay);

            if (panel != null && !panel.IsVisible)
            {
                Type panelType = panel.GetType();
                _loadedPanels.Remove(panelType);
                _lazyLoadCleanupCoroutines.Remove(panel);

                Destroy(panel.gameObject);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleShowPanelRequest(Type panelType, object data, Action<object> closeCallback)
        {
            var method = typeof(UIManager).GetMethod(nameof(ShowPanel));
            var genericMethod = method?.MakeGenericMethod(panelType);
            genericMethod?.Invoke(this, new object[] { data, closeCallback });
        }

        private void HandleHidePanelRequest(Type panelType, object resultData)
        {
            if (_loadedPanels.TryGetValue(panelType, out UIPanel panel))
            {
                HidePanel(panel, resultData);
            }
        }

        #endregion

        #region Helper Methods

        private async Task<UIPanel> GetOrLoadPanel(Type panelType)
        {
            if (_loadedPanels.TryGetValue(panelType, out UIPanel panel))
            {
                return panel;
            }

            return await LoadPanelAsync(panelType);
        }

        #endregion

        #region Public Accessors

        /// <summary>
        /// Retrieves the configuration for the opening screen.
        /// </summary>
        public UIPanelConfig GetOpeningScreenConfig() => _openingScreen;

        /// <summary>
        /// Retrieves the active Canvas Scaling Strategy.
        /// </summary>
        public CanvasScalingStrategy GetScalingStrategy() => _scalingStrategy;

        #endregion

        #region Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            RemoveDuplicateConfigs();
        }

        /// <summary>
        /// Ensures the configuration list does not contain duplicate entries for the same panel, keeping the list clean.
        /// </summary>
        private void RemoveDuplicateConfigs()
        {
            if (_panelConfigs == null || _panelConfigs.Count <= 1)
                return;

            HashSet<UIPanelConfig> seen = new HashSet<UIPanelConfig>();
            List<int> duplicateIndices = new List<int>();

            for (int i = 0; i < _panelConfigs.Count; i++)
            {
                var config = _panelConfigs[i];

                if (config == null)
                    continue;

                if (!seen.Add(config))
                {
                    duplicateIndices.Add(i);
                    Debug.LogWarning($"[UIManager] Duplicate config '{config.panelID}' auto-removed from list at index {i}.");
                }
            }

            for (int i = duplicateIndices.Count - 1; i >= 0; i--)
            {
                _panelConfigs.RemoveAt(duplicateIndices[i]);
            }
        }
#endif

        #endregion

        #region Debug

        public int GetScreenStackCount() => _screenStack.Count;
        public int GetPopupStackCount() => _popupStack.Count;
        public int GetLoadedPanelCount() => _loadedPanels.Count;

        public List<UIPanel> GetScreenStack() => _screenStack.ToList();
        public List<UIPanel> GetPopupStack() => _popupStack.ToList();
        public Dictionary<Type, UIPanel> GetLoadedPanels() => new Dictionary<Type, UIPanel>(_loadedPanels);

        #endregion
    }
}