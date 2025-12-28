using System;

namespace UISystem.Core
{
    /// <summary>
    /// A static event bus responsible for decoupled communication between gameplay systems and the UI architecture.
    /// <para>
    /// This class acts as a centralized messaging hub, allowing external systems to request UI operations (like showing a panel) 
    /// without holding direct references to the <see cref="UIManager"/>. It also broadcasts UI lifecycle events 
    /// that other systems can subscribe to.
    /// </para>
    /// </summary>
    public static class UIEvents
    {
        #region Generic Panel Events
        
        /// <summary>
        /// Invoked when a system requests to open a specific UI panel.
        /// <br/> Parameters: [Panel Type], [Context Data], [Close Callback].
        /// </summary>
        public static event Action<Type, object, Action<object>> OnShowPanelRequested;
        
        /// <summary>
        /// Invoked when a system requests to close a specific UI panel.
        /// <br/> Parameters: [Panel Type], [Result Data].
        /// </summary>
        public static event Action<Type, object> OnHidePanelRequested;
        
        /// <summary>
        /// Invoked when a request is made to close all currently active popup overlays immediately.
        /// </summary>
        public static event Action OnHideAllPopupsRequested;
        
        /// <summary>
        /// Invoked when the hardware back button (or Escape key) logic is triggered.
        /// </summary>
        public static event Action OnBackButtonPressed;
        
        #endregion
        
        #region Global Interaction Events
        
        /// <summary>
        /// Invoked to modify the global interactivity of the UI system.
        /// <br/> Used to block input during cutscenes, tutorials, or critical loading states.
        /// </summary>
        public static event Action<bool, InteractionLayer> OnSetGlobalInteraction;
        
        #endregion
        
        #region Panel Lifecycle Notifications
        
        /// <summary>
        /// Broadcasts when a UI panel has fully completed its opening sequence and is visible.
        /// </summary>
        public static event Action<Type> OnPanelShown;
        
        /// <summary>
        /// Broadcasts when a UI panel has fully completed its closing sequence and is hidden.
        /// </summary>
        public static event Action<Type> OnPanelHidden;
        
        /// <summary>
        /// Broadcasts when the active full-screen view (Screen) changes.
        /// <br/> Parameters: [New Screen Type], [Previous Screen Type].
        /// </summary>
        public static event Action<Type, Type> OnScreenChanged;
        
        #endregion
        
        #region Public Methods - For Gameplay Code
        
        /// <summary>
        /// Publishes a request to display a UI panel of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the UIPanel to show.</typeparam>
        /// <param name="data">Optional payload data to pass to the panel (e.g., an Item ID for a details popup).</param>
        /// <param name="closeCallback">Optional callback to execute when this specific panel instance is closed.</param>
        public static void ShowPanel<T>(object data = null, Action<object> closeCallback = null) where T : UIPanel
        {
            OnShowPanelRequested?.Invoke(typeof(T), data, closeCallback);
        }
        
        /// <summary>
        /// Publishes a request to hide a specific UI panel.
        /// </summary>
        /// <typeparam name="T">The type of the UIPanel to hide.</typeparam>
        /// <param name="resultData">Optional result data to return to the caller (if a callback was registered).</param>
        public static void HidePanel<T>(object resultData = null) where T : UIPanel
        {
            OnHidePanelRequested?.Invoke(typeof(T), resultData);
        }
        
        /// <summary>
        /// Publishes a request to close all currently open popups (useful for returning to the main screen).
        /// </summary>
        public static void HideAllPopups()
        {
            OnHideAllPopupsRequested?.Invoke();
        }
        
        /// <summary>
        /// Simulates a back button press, triggering the UIManager's navigation logic.
        /// </summary>
        public static void PressBackButton()
        {
            OnBackButtonPressed?.Invoke();
        }
        
        /// <summary>
        /// Sets the global interaction state for the UI.
        /// </summary>
        /// <param name="enabled">If true, UI is interactive. If false, input is blocked.</param>
        /// <param name="layer">The minimum priority layer that remains interactive (if blocking is active).</param>
        public static void SetGlobalInteraction(bool enabled, InteractionLayer layer = InteractionLayer.Normal)
        {
            OnSetGlobalInteraction?.Invoke(enabled, layer);
        }
        
        #endregion
        
        #region Internal Methods - For UIManager to Fire Events
        
        /// <summary>
        /// Internal method used by the UIManager to notify listeners that a panel has been shown.
        /// </summary>
        internal static void NotifyPanelShown(Type panelType)
        {
            OnPanelShown?.Invoke(panelType);
        }
        
        /// <summary>
        /// Internal method used by the UIManager to notify listeners that a panel has been hidden.
        /// </summary>
        internal static void NotifyPanelHidden(Type panelType)
        {
            OnPanelHidden?.Invoke(panelType);
        }
        
        /// <summary>
        /// Internal method used by the UIManager to notify listeners that the active screen has changed.
        /// </summary>
        internal static void NotifyScreenChanged(Type newScreen, Type previousScreen)
        {
            OnScreenChanged?.Invoke(newScreen, previousScreen);
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Clears all subscribers from all events.
        /// <para>
        /// This is critical for preventing memory leaks when reloading scenes or restarting the game domain,
        /// as static events hold strong references to their subscribers.
        /// </para>
        /// </summary>
        public static void ClearAllEvents()
        {
            OnShowPanelRequested = null;
            OnHidePanelRequested = null;
            OnHideAllPopupsRequested = null;
            OnBackButtonPressed = null;
            OnSetGlobalInteraction = null;
            OnPanelShown = null;
            OnPanelHidden = null;
            OnScreenChanged = null;
        }
        
        #endregion
    }
}