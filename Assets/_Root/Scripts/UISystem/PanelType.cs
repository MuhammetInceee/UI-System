namespace UISystem.Core
{
    /// <summary>
    /// Categorizes UI panels based on their structural role and navigation behavior within the UI Manager.
    /// <para>
    /// This classification determines how the panel interacts with the navigation stack, z-ordering, 
    /// and whether it obscures or deactivates underlying content.
    /// </para>
    /// </summary>
    public enum PanelType
    {
        /// <summary>
        /// Represents a primary, full-screen view that typically serves as the base layer of the UI.
        /// <br/> When a Screen is pushed to the stack, it usually deactivates or hides the previous Screen.
        /// Examples: Main Menu, Gameplay HUD, Shop Page.
        /// </summary>
        Screen,
        
        /// <summary>
        /// Represents a modal or non-modal overlay displayed on top of the active Screen.
        /// <br/> Popups support independent stacking (Popup on top of Popup) and do not deactivate the underlying Screen.
        /// Examples: Settings Dialog, Inventory Item Details, Confirmation Box.
        /// </summary>
        Popup,
        
        /// <summary>
        /// A transient, informative notification that appears briefly and does not participate in the navigation history.
        /// <br/> Toasts are completely independent of the screen/popup stacks and automatically dismiss themselves after a duration.
        /// Examples: "Game Saved", "Not Enough Currency", "Level Up".
        /// </summary>
        Toast
    }
}