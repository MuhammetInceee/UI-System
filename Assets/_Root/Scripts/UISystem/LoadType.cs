namespace UISystem.Core
{
    /// <summary>
    /// Determines the instantiation and memory management strategy for UI Panels.
    /// <para>
    /// This setting controls whether a panel is created immediately during the game's initialization phase 
    /// or deferred until it is explicitly requested by the user.
    /// </para>
    /// </summary>
    public enum LoadType
    {
        /// <summary>
        /// The panel is instantiated immediately during the application's initialization or bootstrap phase.
        /// <br/> Use this for frequently accessed panels (e.g., HUD, Inventory) to ensure zero-latency when opening them, 
        /// at the cost of a slightly longer initial loading time and higher baseline memory usage.
        /// </summary>
        Preload,
        
        /// <summary>
        /// The panel is instantiated on-demand only when the first request to show it is made.
        /// <br/> Use this for rarely accessed panels (e.g., Settings, Credits, Popups) to reduce initial load times 
        /// and keep the memory footprint low until necessary.
        /// </summary>
        LazyLoad
    }
}