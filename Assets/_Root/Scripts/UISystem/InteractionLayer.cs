namespace UISystem.Core
{
    /// <summary>
    /// Defines the hierarchy of interaction priorities for UI panels.
    /// <para>
    /// This enum is utilized by the Input System to determine which UI layers remain interactive 
    /// when global blocking states (such as cutscenes, tutorials, or loading screens) are active.
    /// </para>
    /// </summary>
    public enum InteractionLayer
    {
        /// <summary>
        /// The default layer for standard gameplay UI elements (e.g., HUD, Inventory, Shop).
        /// <br/> Elements on this layer will be blocked and become non-interactive when the system enters a restricted state like a cinematic.
        /// </summary>
        Normal = 0,
        
        /// <summary>
        /// A high-priority layer designed for UI elements that must remain interactive during cinematic sequences or cutscenes.
        /// <br/> Common examples include "Skip Cinematic" buttons, Pause menus, or subtitle controls.
        /// </summary>
        AboveCinematic = 100,
        
        /// <summary>
        /// The absolute highest priority layer, reserved for system-critical overlays that must never be blocked by game state.
        /// <br/> Usage includes network disconnection alerts, fatal error dialogs, or forced update prompts requiring immediate user attention.
        /// </summary>
        Critical = 200
    }
}