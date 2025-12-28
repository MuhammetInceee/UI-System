using UnityEngine;
using UnityEditor;
using System.Linq;
using UISystem.Core;

namespace UISystem.EditorTools
{
    /// <summary>
    /// A custom Editor Window designed to facilitate the debugging and visualization of the runtime UI system.
    /// <para>
    /// This tool provides real-time insights into the UI Manager's state, including the active screen stack,
    /// popup stack, and all loaded panels. It also offers utility functions for manipulating the UI flow during development.
    /// </para>
    /// </summary>
    public class UIDebugWindow : EditorWindow
    {
        /// <summary>
        /// Current scroll position for the main editor window view.
        /// </summary>
        private Vector2 _scrollPosition;

        /// <summary>
        /// Reference to the runtime UIManager instance. Found dynamically when Play Mode starts.
        /// </summary>
        private UIManager _uiManager;

        /// <summary>
        /// Toggles whether the window should automatically repaint itself at set intervals.
        /// </summary>
        private bool _autoRefresh = true;

        /// <summary>
        /// Tracks the timestamp of the last window repaint to control refresh rate.
        /// </summary>
        private double _lastRefreshTime;

        /// <summary>
        /// The interval (in seconds) between automatic window repaints to conserve editor performance.
        /// </summary>
        private const double AUTO_REFRESH_INTERVAL = 0.5;

        /// <summary>
        /// Opens the UI Debug Window via the Unity Editor menu.
        /// </summary>
        [MenuItem("IdleTemplate/Tools/UI Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<UIDebugWindow>("UI Debug");
            window.minSize = new Vector2(400, 300);
        }

        /// <summary>
        /// Called when the window becomes enabled. Subscribes to play mode state changes.
        /// </summary>
        private void OnEnable()
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Called when the window is disabled. Unsubscribes from events to prevent memory leaks.
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        /// <summary>
        /// Handles the cleanup of references when transitioning between Edit and Play modes.
        /// </summary>
        /// <param name="state">The new state of the editor.</param>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Reset the cached manager reference when exiting play mode or entering edit mode
            if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                _uiManager = null;
            }
        }

        /// <summary>
        /// Standard Unity Update loop. Handles the automatic refreshing of the editor window.
        /// </summary>
        private void Update()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > AUTO_REFRESH_INTERVAL)
            {
                Repaint();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        /// <summary>
        /// Renders the GUI for the Editor Window.
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see UI system state.", MessageType.Info);
                return;
            }
            
            if (_uiManager == null)
            {
                _uiManager = FindFirstObjectByType<UIManager>();
            }
            
            if (_uiManager == null)
            {
                EditorGUILayout.HelpBox("UIManager instance not found in the current scene!", MessageType.Warning);
                return;
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawOverview();
            EditorGUILayout.Space(10);
            
            DrawScreenStack();
            EditorGUILayout.Space(10);
            
            DrawPopupStack();
            EditorGUILayout.Space(10);
            
            DrawLoadedPanels();
            EditorGUILayout.Space(10);
            
            DrawQuickActions();
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Renders the top toolbar containing global controls like Refresh and Auto-Refresh toggle.
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Repaint();
            }
            
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Clear All Popups", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                if (_uiManager != null)
                {
                    _uiManager.HideAllPopups();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Displays high-level statistics about the current UI state.
        /// </summary>
        private void DrawOverview()
        {
            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Screen Stack Count:", _uiManager.GetScreenStackCount().ToString());
            EditorGUILayout.LabelField("Popup Stack Count:", _uiManager.GetPopupStackCount().ToString());
            EditorGUILayout.LabelField("Loaded Panels:", _uiManager.GetLoadedPanelCount().ToString());
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Visualizes the current navigation stack (Screens).
        /// </summary>
        private void DrawScreenStack()
        {
            EditorGUILayout.LabelField("Screen Stack", EditorStyles.boldLabel);
            
            var screenStack = _uiManager.GetScreenStack();
            
            if (screenStack.Count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("(Empty)");
                EditorGUI.indentLevel--;
                return;
            }
            
            EditorGUI.indentLevel++;
            for (int i = screenStack.Count - 1; i >= 0; i--)
            {
                var panel = screenStack[i];
                DrawPanelInfo(panel, i == screenStack.Count - 1 ? "TOP" : $"#{screenStack.Count - i}");
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Visualizes the active popup stack (Overlays).
        /// </summary>
        private void DrawPopupStack()
        {
            EditorGUILayout.LabelField("Popup Stack", EditorStyles.boldLabel);
            
            var popupStack = _uiManager.GetPopupStack();
            
            if (popupStack.Count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("(Empty)");
                EditorGUI.indentLevel--;
                return;
            }
            
            EditorGUI.indentLevel++;
            for (int i = popupStack.Count - 1; i >= 0; i--)
            {
                var panel = popupStack[i];
                DrawPanelInfo(panel, i == popupStack.Count - 1 ? "TOP" : $"#{popupStack.Count - i}");
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Lists all UI Panels that have been instantiated and are currently managed by the UIManager.
        /// </summary>
        private void DrawLoadedPanels()
        {
            EditorGUILayout.LabelField("All Loaded Panels", EditorStyles.boldLabel);
            
            var loadedPanels = _uiManager.GetLoadedPanels();
            
            if (loadedPanels.Count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("(None)");
                EditorGUI.indentLevel--;
                return;
            }
            
            EditorGUI.indentLevel++;
            foreach (var kvp in loadedPanels.OrderBy(x => x.Key.Name))
            {
                var panel = kvp.Value;
                DrawPanelInfo(panel, panel.IsVisible ? "VISIBLE" : "Hidden");
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Renders a single row of information for a specific UI Panel, including status indicators and action buttons.
        /// </summary>
        /// <param name="panel">The UI Panel instance to display.</param>
        /// <param name="label">Prefix label indicating the panel's position or state (e.g., "TOP").</param>
        private void DrawPanelInfo(UIPanel panel, string label)
        {
            if (panel == null)
            {
                EditorGUILayout.LabelField($"[{label}] (null panel)");
                return;
            }
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.color = panel.IsVisible ? Color.green : Color.gray;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.color = Color.white;
            
            string info = $"[{label}] {panel.GetType().Name}";
            
            if (panel.Config != null)
            {
                info += $" ({panel.Config.panelType})";
                
                if (panel.Canvas != null)
                {
                    info += $" [Sort: {panel.Canvas.sortingOrder}]";
                }
                
                if (panel.IsTransitioning)
                {
                    info += " [Transitioning...]";
                }
            }
            
            EditorGUILayout.LabelField(info);
            
            if (panel.IsVisible)
            {
                if (GUILayout.Button("Hide", GUILayout.Width(50)))
                {
                    _uiManager.HidePanel(panel);
                }
            }
            
            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeGameObject = panel.gameObject;
                EditorGUIUtility.PingObject(panel.gameObject);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Renders buttons for common global UI operations to speed up testing.
        /// </summary>
        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Back Button"))
            {
                _uiManager.HandleBackButton();
            }
            
            if (GUILayout.Button("Clear All Popups"))
            {
                _uiManager.HideAllPopups();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Enable Global Interaction"))
            {
                UIEvents.SetGlobalInteraction(true);
            }
            
            if (GUILayout.Button("Disable Global Interaction"))
            {
                UIEvents.SetGlobalInteraction(false);
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}