# IdleTemplate UI System

A robust, stack-based UI management framework designed for Unity projects. This system provides a complete solution for handling screens, popups, and toast notifications with support for responsive scaling, memory management, and decoupled event-driven architecture.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Core Components](#core-components)
   - [UIManager](#uimanager)
   - [UIPanel](#uipanel)
   - [UIPanelConfig](#uipanelconfig)
   - [UIEvents](#uievents)
3. [Panel Types](#panel-types)
4. [Load Types](#load-types)
5. [Interaction Layers](#interaction-layers)
6. [Canvas Scaling Strategy](#canvas-scaling-strategy)
7. [Safe Area Handler](#safe-area-handler)
8. [Setup Guide](#setup-guide)
9. [Usage Examples](#usage-examples)
10. [Creating Custom Panels](#creating-custom-panels)
11. [Editor Tools](#editor-tools)
12. [Best Practices](#best-practices)

---

## Architecture Overview

The UI System follows a centralized management pattern where the `UIManager` orchestrates all UI operations. Panels are configured via ScriptableObjects (`UIPanelConfig`) and communicate through a static event bus (`UIEvents`), ensuring loose coupling between gameplay systems and the UI layer.

```
┌─────────────────────────────────────────────────────────────┐
│                        UIManager                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ScreenStack  │  │ PopupStack  │  │LoadedPanels │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
└─────────────────────────────────────────────────────────────┘
           │                │                │
           ▼                ▼                ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│  Screen Panels  │ │  Popup Panels   │ │  Toast Panels   │
│  (Full Screen)  │ │   (Overlays)    │ │ (Notifications) │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

---

## Core Components

### UIManager

The central orchestrator responsible for the entire UI lifecycle.

**Responsibilities:**
- Panel instantiation, showing, hiding, and destruction
- Navigation stack management for Screens and Popups
- Canvas scaling across different device aspect ratios
- Memory management via Preload and LazyLoad strategies
- Global interaction control
- Back button handling

**Inspector Configuration:**

| Field | Description |
|-------|-------------|
| `Panel Configs` | List of all `UIPanelConfig` assets available to the system |
| `Opening Screen` | The Screen panel displayed on application start (must be Preload type) |
| `Scaling Strategy` | Reference to a `CanvasScalingStrategy` asset |
| `Enable Dynamic Scaling` | Updates scaling when screen resolution changes |
| `Screen Container` | Parent Transform for Screen panels |
| `Popup Container` | Parent Transform for Popup panels |
| `Toast Container` | Parent Transform for Toast panels |
| `Handle Back Button` | Enables Escape/Back button navigation |
| `Back Button Key` | KeyCode for back navigation (default: Escape) |
| `Lazy Load Destroy Delay` | Seconds before destroying hidden LazyLoad panels |

**Public Methods:**

```csharp
// Show a panel by type
void ShowPanel<T>(object data = null, Action<object> closeCallback = null) where T : UIPanel

// Hide a specific panel instance
void HidePanel(UIPanel panel, object resultData = null)

// Close all active popups
void HideAllPopups()

// Process back button logic
void HandleBackButton()

// Control global UI interaction
void SetGlobalInteraction(bool enabled, InteractionLayer blockedLayer = InteractionLayer.Normal)

// Force recalculation of canvas scaling
void RefreshScaling()

// Register/Unregister additional CanvasScalers
void RegisterCanvasScaler(CanvasScaler scaler)
void UnregisterCanvasScaler(CanvasScaler scaler)
```

---

### UIPanel

The abstract base class that all UI panels must inherit from.

**Required Components:**
- `Canvas` - Controls render order
- `CanvasGroup` - Controls opacity and interaction

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Canvas` | Canvas | Reference to the panel's Canvas component |
| `CanvasGroup` | CanvasGroup | Reference to the panel's CanvasGroup component |
| `Config` | UIPanelConfig | The configuration asset assigned to this panel |
| `IsVisible` | bool | True if the panel is currently shown |
| `IsTransitioning` | bool | True if an animation is in progress |

**Lifecycle Hooks (Abstract - Must Implement):**

```csharp
// Called once during initialization
protected abstract void OnInitialize();

// Called every time the panel is shown
protected abstract void OnShow(object data = null);

// Called when the panel begins hiding
protected abstract void OnHide();

// Called when the panel is destroyed
protected abstract void OnCleanup();
```

**Animation Hooks (Virtual - Optional Override):**

```csharp
// Custom show animation (MUST call onComplete when finished)
protected virtual void PlayShowAnimation(Action onComplete)

// Custom hide animation (MUST call onComplete when finished)
protected virtual void PlayHideAnimation(Action onComplete)
```

**Helper Methods:**

```csharp
// Close this panel
protected void CloseSelf(object resultData = null)

// Request to open another panel
protected void RequestOpenPanel<T>(object data = null, Action<object> callback = null) where T : UIPanel
```

---

### UIPanelConfig

A ScriptableObject that defines the metadata and behavior settings for a UI Panel.

**Create via:** `Right Click → Create → UI System → Panel Config`

**Configuration Fields:**

| Field | Description |
|-------|-------------|
| `Panel ID` | Auto-generated unique identifier (read-only) |
| `Panel Prefab` | The GameObject prefab containing a UIPanel component |
| `Panel Type` | Screen, Popup, or Toast |
| `Load Type` | Preload or LazyLoad |
| `Interaction Layer` | Normal, AboveCinematic, or Critical |
| `Allow Back Button` | Whether back button can close this panel |
| `Keep On Screen Change` | Popup persists when Screen changes |
| `Base Sort Order` | Starting Canvas sorting order |
| `Render Mode` | Canvas render mode |
| `Override Sorting` | Enable independent sorting order |
| `Pixel Perfect` | Enable pixel-perfect rendering |
| `Toast Duration` | Auto-close time for Toast panels (seconds) |

---

### UIEvents

A static event bus for decoupled communication between gameplay systems and the UI.

**Request Methods (Fire and Forget):**

```csharp
// Request to show a panel
UIEvents.ShowPanel<T>(object data = null, Action<object> closeCallback = null)

// Request to hide a panel
UIEvents.HidePanel<T>(object resultData = null)

// Close all popups
UIEvents.HideAllPopups()

// Simulate back button press
UIEvents.PressBackButton()

// Control global interaction
UIEvents.SetGlobalInteraction(bool enabled, InteractionLayer layer = InteractionLayer.Normal)
```

**Notification Events (Subscribe to Listen):**

```csharp
// Fired when any panel is shown
event Action<Type> OnPanelShown

// Fired when any panel is hidden
event Action<Type> OnPanelHidden

// Fired when the active Screen changes
event Action<Type, Type> OnScreenChanged  // (newScreen, previousScreen)
```

**Cleanup:**

```csharp
// Clear all event subscriptions (call on scene unload)
UIEvents.ClearAllEvents()
```

---

## Panel Types

| Type | Behavior |
|------|----------|
| **Screen** | Full-screen panels that form the primary navigation stack. When a new Screen is shown, the previous Screen is deactivated. Only one Screen is visible at a time. |
| **Popup** | Overlay panels that stack on top of Screens. Multiple Popups can be open simultaneously. Each Popup receives an incremented sorting order. |
| **Toast** | Transient notifications that auto-dismiss after a configured duration. Toasts do not participate in navigation stacks. |

---

## Load Types

| Type | Behavior | Use Case |
|------|----------|----------|
| **Preload** | Instantiated during initialization, deactivated until shown. Never destroyed. | Frequently accessed panels (HUD, Main Menu, Inventory) |
| **LazyLoad** | Instantiated on first request. Destroyed after being hidden for a configured delay period. | Rarely accessed panels (Settings, Credits, One-time Popups) |

---

## Interaction Layers

Controls which panels remain interactive during global blocking states.

| Layer | Priority | Use Case |
|-------|----------|----------|
| **Normal** | 0 | Standard UI elements (blocked during cinematics) |
| **AboveCinematic** | 100 | Skip buttons, pause menus during cutscenes |
| **Critical** | 200 | Network error dialogs, force update prompts |

**Usage:**

```csharp
// Block all Normal layer panels (AboveCinematic and Critical remain interactive)
UIEvents.SetGlobalInteraction(false, InteractionLayer.Normal);

// Re-enable all interaction
UIEvents.SetGlobalInteraction(true);
```

---

## Canvas Scaling Strategy

A ScriptableObject that defines responsive scaling behavior for different device aspect ratios.

**Create via:** `Right Click → Create → UI System → Canvas Scaling Strategy`

**Configuration:**

| Field | Description |
|-------|-------------|
| `Reference Resolution` | Base design resolution (e.g., 1080x1920) |
| `Match On Tall` | Match value for ultra-tall phones (0 = width) |
| `Match On Standard` | Match value for 16:9 devices (0.5 = balanced) |
| `Match On Wide` | Match value for tablets (1 = height) |
| `Tall Threshold` | Aspect ratio below which Tall mode applies |
| `Wide Threshold` | Aspect ratio above which Wide mode applies |

**Aspect Ratio Categories:**

| Category | Aspect Ratio | Match Value |
|----------|--------------|-------------|
| Tall | < 0.428 (9:21) | MatchOnTall |
| Standard | 0.428 - 0.6 | MatchOnStandard |
| Wide | > 0.6 (3:5) | MatchOnWide |

---

## Safe Area Handler

A component that adjusts RectTransform anchors to respect device safe areas (notches, rounded corners).

**Usage:**
1. Attach `SafeAreaHandler` to any RectTransform that should stay within the safe area
2. Configure options in the Inspector

**Options:**

| Field | Description |
|-------|-------------|
| `Apply On Start` | Apply safe area constraints immediately on Start |
| `Update Every Frame` | Continuously check for resolution/safe area changes |
| `Show Debug Info` | Log safe area calculations to console |

---

## Setup Guide

### 1. Create the UI Hierarchy

```
UIRoot (GameObject)
├── Canvas (Canvas + CanvasScaler + GraphicRaycaster)
│   └── UIManager (UIManager component)
│       ├── ScreenContainer (Empty GameObject)
│       ├── PopupContainer (Empty GameObject)
│       └── ToastContainer (Empty GameObject)
```

### 2. Configure UIManager

1. Assign the three container Transforms in the Inspector
2. Create a `CanvasScalingStrategy` asset and assign it
3. Create `UIPanelConfig` assets for each panel
4. Add all configs to the `Panel Configs` list
5. Assign the `Opening Screen` config

### 3. Create Panel Prefabs

Each panel prefab must have:
- A component inheriting from `UIPanel`
- A `Canvas` component
- A `CanvasGroup` component

---

## Usage Examples

### Opening a Panel with Data

```csharp
public class GameController : MonoBehaviour
{
    public void OpenShop()
    {
        var shopData = new ShopData
        {
            categoryId = "weapons",
            currency = 1500
        };
        
        UIEvents.ShowPanel<ShopPanel>(shopData, OnShopClosed);
    }
    
    private void OnShopClosed(object result)
    {
        if (result is ShopResult shopResult && shopResult.purchased)
        {
            Debug.Log($"Purchased item: {shopResult.itemId}");
        }
    }
}
```

### Listening to UI Events

```csharp
public class AudioManager : MonoBehaviour
{
    private void OnEnable()
    {
        UIEvents.OnPanelShown += HandlePanelShown;
        UIEvents.OnScreenChanged += HandleScreenChanged;
    }
    
    private void OnDisable()
    {
        UIEvents.OnPanelShown -= HandlePanelShown;
        UIEvents.OnScreenChanged -= HandleScreenChanged;
    }
    
    private void HandlePanelShown(Type panelType)
    {
        PlaySound("ui_open");
    }
    
    private void HandleScreenChanged(Type newScreen, Type previousScreen)
    {
        PlayTransitionMusic(newScreen);
    }
}
```

### Blocking UI During Cutscene

```csharp
public class CutsceneManager : MonoBehaviour
{
    public void StartCutscene()
    {
        // Block Normal layer, AboveCinematic remains active for skip button
        UIEvents.SetGlobalInteraction(false, InteractionLayer.Normal);
    }
    
    public void EndCutscene()
    {
        UIEvents.SetGlobalInteraction(true);
    }
}
```

---

## Creating Custom Panels

### Step 1: Create the Panel Class

```csharp
using UISystem;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : UIPanel
{
    [Header("UI References")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Transform _itemContainer;
    
    private InventoryData _data;
    
    protected override void OnInitialize()
    {
        // One-time setup - subscribe to events, cache references
        _closeButton.onClick.AddListener(OnCloseClicked);
    }
    
    protected override void OnShow(object data = null)
    {
        // Called every time panel opens - refresh UI
        if (data is InventoryData inventoryData)
        {
            _data = inventoryData;
            RefreshItems();
        }
    }
    
    protected override void OnHide()
    {
        // Cleanup temporary state
        ClearItems();
    }
    
    protected override void OnCleanup()
    {
        // Final cleanup - unsubscribe from events
        _closeButton.onClick.RemoveListener(OnCloseClicked);
    }
    
    private void OnCloseClicked()
    {
        CloseSelf();
    }
    
    private void RefreshItems() { /* ... */ }
    private void ClearItems() { /* ... */ }
}
```

### Step 2: Create the Prefab

1. Create a new GameObject with the `InventoryPanel` script
2. Add `Canvas` and `CanvasGroup` components
3. Design the UI hierarchy
4. Save as a prefab

### Step 3: Create the Config

1. `Right Click → Create → UI System → Panel Config`
2. Name it `InventoryPanelConfig`
3. Assign the prefab to `Panel Prefab`
4. Configure `Panel Type`, `Load Type`, and other settings
5. Add the config to `UIManager.Panel Configs` list

### Step 4: Implement Custom Animations (Optional)

```csharp
protected override void PlayShowAnimation(Action onComplete)
{
    // Example: Scale animation using DOTween
    transform.localScale = Vector3.zero;
    transform.DOScale(Vector3.one, 0.3f)
        .SetEase(Ease.OutBack)
        .OnComplete(() => onComplete?.Invoke());
}

protected override void PlayHideAnimation(Action onComplete)
{
    CanvasGroup.DOFade(0f, 0.2f)
        .OnComplete(() => onComplete?.Invoke());
}
```

> **Important:** Always invoke `onComplete` when the animation finishes, or the panel state will hang.

---

## Editor Tools

### UI Debug Window

A custom editor window for real-time UI system debugging.

**Open via:** `UISystem → Tools → UI Debug Window`

**Features:**
- View current Screen and Popup stacks
- See all loaded panels and their states
- Quick actions: Hide panels, Back button, Clear popups
- Toggle global interaction on/off
- Select and ping panel GameObjects in hierarchy

---

## Best Practices

### Panel Configuration

- Use **Preload** for panels accessed frequently (HUD, main menus)
- Use **LazyLoad** for panels accessed rarely (settings, credits, one-time dialogs)
- Set appropriate **Interaction Layers** for critical UI elements

### Memory Management

- LazyLoad panels are automatically destroyed after the configured delay
- Preload panels are deactivated but never destroyed
- Monitor loaded panel count via the Debug Window

### Event Handling

- Always unsubscribe from `UIEvents` in `OnDisable` to prevent memory leaks
- Call `UIEvents.ClearAllEvents()` when reloading scenes if not using `DontDestroyOnLoad`

### Animation Guidelines

- Keep animations short (0.2-0.4 seconds) for responsive feel
- Always call `onComplete` callback when animation finishes
- Consider disabling interaction during transitions (`IsTransitioning` property)

### Duplicate Prevention

- The UIManager automatically removes duplicate configs in the Inspector
- Each panel type should have exactly one config entry

---

## File Structure

```
UISystem/
├── CanvasScalingStrategy.cs    # Responsive scaling configuration
├── InteractionLayer.cs         # Interaction priority enum
├── LoadType.cs                 # Memory management enum
├── PanelType.cs                # Panel category enum
├── SafeAreaHandler.cs          # Device safe area support
├── UIEvents.cs                 # Static event bus
├── UIManager.cs                # Central UI orchestrator
├── UIPanel.cs                  # Abstract panel base class
├── UIPanelConfig.cs            # Panel configuration asset
└── Editor/
    └── UIDebugWindow.cs        # Editor debugging tool
```

---

## Version

**UI System v1.0**  
Namespace: `UISystem`
