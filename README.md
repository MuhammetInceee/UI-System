# 🎮 UI System

A powerful, stack-based UI management framework for Unity, designed with mobile-first principles and scalability in mind.

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity)
![License](https://img.shields.io/badge/License-MIT-green)
![Version](https://img.shields.io/badge/Version-1.0.0-blue)

---

## ✨ Features

- **Stack-Based Navigation** - Separate stacks for Screens and Popups with automatic history management
- **Three Panel Types** - Screens (full-screen), Popups (overlays), and Toasts (notifications)
- **Smart Memory Management** - Preload frequently used panels, LazyLoad rarely used ones
- **Responsive Scaling** - Automatic canvas scaling for phones, tablets, and foldables
- **Safe Area Support** - Built-in notch and rounded corner handling
- **Event-Driven Architecture** - Decoupled communication via static event bus
- **Interaction Layers** - Priority-based input blocking for cinematics and loading states
- **Editor Tools** - Real-time debug window for development
- **Zero Dependencies** - Pure Unity, no external packages required

---

## 📦 Installation

1. Clone or download this repository
2. Copy the `UISystem` folder into your Unity project's `Assets/Scripts` directory
3. Done!

```
Assets/
└── Scripts/
    └── UISystem/
        ├── CanvasScalingStrategy.cs
        ├── InteractionLayer.cs
        ├── LoadType.cs
        ├── PanelType.cs
        ├── SafeAreaHandler.cs
        ├── UIEvents.cs
        ├── UIManager.cs
        ├── UIPanel.cs
        ├── UIPanelConfig.cs
        └── Editor/
            └── UIDebugWindow.cs
```

---

## 🚀 Quick Start

### 1. Setup Hierarchy

```
Canvas
└── UIManager
    ├── ScreenContainer
    ├── PopupContainer
    └── ToastContainer
```

### 2. Create a Panel

```csharp
using UISystem;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : UIPanel
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _settingsButton;

    protected override void OnInitialize()
    {
        _playButton.onClick.AddListener(() => RequestOpenPanel<GameplayPanel>());
        _settingsButton.onClick.AddListener(() => RequestOpenPanel<SettingsPopup>());
    }

    protected override void OnShow(object data = null) { }
    protected override void OnHide() { }
    protected override void OnCleanup()
    {
        _playButton.onClick.RemoveAllListeners();
        _settingsButton.onClick.RemoveAllListeners();
    }
}
```

### 3. Create Config Asset

`Right Click → Create → UI System → Panel Config`

### 4. Open Panels from Anywhere

```csharp
// Simple open
UIEvents.ShowPanel<ShopPopup>();

// With data
UIEvents.ShowPanel<ItemDetailPopup>(new ItemData { id = "sword_01" });

// With callback
UIEvents.ShowPanel<ConfirmPopup>(null, result => 
{
    if ((bool)result) Debug.Log("Confirmed!");
});
```

---

## 📖 Panel Types

| Type | Description | Stack Behavior |
|------|-------------|----------------|
| **Screen** | Full-screen views (Menu, Gameplay, Shop) | Pushed to screen stack, previous screen deactivated |
| **Popup** | Modal overlays (Dialogs, Item details) | Pushed to popup stack, supports multiple |
| **Toast** | Brief notifications (Rewards, Errors) | No stack, auto-dismisses |

---

## 🧠 Load Strategies

| Strategy | When Created | When Destroyed | Best For |
|----------|--------------|----------------|----------|
| **Preload** | App start | Never | HUD, Main Menu, Inventory |
| **LazyLoad** | First request | After delay when hidden | Settings, Credits, Rare popups |

---

## 🎯 Interaction Layers

Block UI input during cinematics while keeping critical elements accessible:

```csharp
// Block normal UI, keep skip button active
UIEvents.SetGlobalInteraction(false, InteractionLayer.Normal);

// Restore all interaction
UIEvents.SetGlobalInteraction(true);
```

| Layer | Priority | Example Usage |
|-------|----------|---------------|
| `Normal` | 0 | Standard gameplay UI |
| `AboveCinematic` | 100 | Skip button, pause menu |
| `Critical` | 200 | Network error, force update |

---

## 📱 Responsive Scaling

Create a `CanvasScalingStrategy` asset to handle different aspect ratios:

| Device Type | Aspect Ratio | Match Value |
|-------------|--------------|-------------|
| Tall Phones | < 9:21 | 0 (Width) |
| Standard | 16:9 | 0.5 (Balanced) |
| Tablets | > 3:5 | 1 (Height) |

---

## 🔧 Editor Tools

Open the debug window: `UISystem → Tools → UI Debug Window`

- 📊 View active Screen and Popup stacks
- 👁️ See all loaded panels and states
- ⚡ Quick actions (Back, Close All, Toggle Interaction)
- 🎯 Select and ping GameObjects

---

## 📚 Documentation

For complete documentation, see [UISystem_Documentation.md](./UISystem_Documentation.md)

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────┐
│                 Game Code                   │
└──────────────────┬──────────────────────────┘
                   │ UIEvents (Static Bus)
                   ▼
┌─────────────────────────────────────────────┐
│                 UIManager                   │
│  • Panel Loading    • Stack Management      │
│  • Canvas Scaling   • Interaction Control   │
└──────────────────┬──────────────────────────┘
                   │
     ┌─────────────┼─────────────┐
     ▼             ▼             ▼
┌─────────┐  ┌─────────┐  ┌─────────┐
│ Screens │  │ Popups  │  │ Toasts  │
└─────────┘  └─────────┘  └─────────┘
```

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Inspired by best practices from mobile game development

---
