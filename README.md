# 🐠 BlockBoost

**An ocean-themed match-3 puzzle game built in Unity 6, designed for mobile with cross-platform browser support.**

BlockBoost is a polished, feature-complete match-3 game inspired by titles like Candy Crush and Tiny Heroes. Swap fish to create matches, trigger powerful special tiles, clear obstacles, and complete level objectives — all wrapped in a lively underwater world.

---

## 🎮 Play the Game

▶️ **[Play in your browser (desktop & mobile)](https://itch.io/)** *(link coming soon)*

Runs directly in the browser — no download required. Playable with **touch on mobile** and **mouse on desktop**.

---

## ✨ Features

### Core Gameplay
- **8×8 grid** with match-3 detection, cascading combos, and invalid-swap undo
- **Special tiles** created from larger matches:
  - 🚀 **Rocket** (clears a full row or column)
  - 💥 **Bomb** (3×3 area blast)
  - 🌈 **Color Bomb** (clears all fish of one type)
- **Special tile combos** - combine two specials for screen-clearing chain reactions
- **Score system** with combo multipliers and rising-pitch audio feedback

### Level System
- **ScriptableObject-based level design** for easy content creation
- **Multiple objective types**: collect fish, clear obstacles, deliver collectibles
- **Move limit** and **3-star rating** based on final score
- **Obstacles**: cages, seaweed, coral, ice, and fishing nets

### Game Feel & Polish
- **Juicy animations** throughout (powered by DOTween): button feedback, animated score counter, punch effects
- **"Leftover moves → bonus"** signature sequence - remaining moves convert into special tiles that detonate one by one (Candy Crush–style)
- **Redesigned Win/Lose panels** with animated star reveals and sound
- **Screen shake, particle bursts, and combo callouts** for satisfying feedback
- **Idle animation** - fish sway gently when the board is untouched
- **"GOAL COMPLETE!"** celebration banner on level clear

### Atmosphere
- 🐟 **Background fish school** that swims across the screen and scatters when you make a match
- 🔓 **Cage escape** - freed fish swim off-screen before a new one drops in

### Audio & UI
- Full **AudioManager** with separate **music** and **SFX** toggles (persisted between sessions)
- In-game HUD with **back button**, **music toggle**, and **SFX toggle**
- Main menu, level select (with locked / active / completed states), and gameplay HUD

---

## 🛠️ Built With

| Tool | Purpose |
|------|---------|
| **Unity 6** (6000.3.6f1) | Game engine |
| **C#** | Gameplay programming |
| **DOTween** | Animation & tweening |
| **Kenney.nl assets** | Ocean/treasure art |
| **TextMeshPro** | UI typography |

---

## 🏗️ Architecture Highlights

The project is organized around clear, single-responsibility managers communicating through C# events:

- **`GridManager`**-board state, matching, cascades, gravity, special-tile logic
- **`LevelManager`**-level loading, objectives, move tracking, win/lose flow (event-driven)
- **`ScoreManager`**-scoring with combo multipliers
- **`InputManager`**-touch/mouse swipe input with race-condition-safe swap handling
- **`AudioManager`**-pooled SFX playback, music, and persistent audio settings (`DontDestroyOnLoad`)
- **`MatchVFXManager`**-particle bursts, score popups, and combo/feedback text

Design decisions worth noting:
- **Event-driven UI** - managers fire events; UI listens and updates, keeping systems decoupled
- **Race-condition-safe input**-the grid locks during animations and safely releases in all code paths, preventing board corruption from rapid input
- **ScriptableObject levels**-new levels are authored in the Inspector without touching code

---

## 📱 Platform Support

- **Mobile-ready**: Canvas Scaler configured for all screen sizes (portrait, 1080×1920 reference), touch input supported
- **Cross-platform browser build** (WebGL): playable on desktop and mobile browsers alike
- Input works seamlessly with both **touch** and **mouse**

---

## 🚀 Getting Started

```bash
git clone https://github.com/rumeysasevben/BlockBoost.git
```

1. Open **Unity Hub** → **Add project from disk** → select the cloned folder
2. Use **Unity 6 (6000.3.6f1)** or compatible
3. Open `SampleScene` and press **Play**

> The entire game runs in a single scene-<img width="227" height="440" alt="play_panel" src="https://github.com/user-attachments/assets/b71b546c-e722-4dbd-91ef-356c255b942d" />
<img width="201" height="430" alt="level_select_panel" src="https://github.com/user-attachments/assets/4107ee17-b9cb-4063-92e4-17659d779d0c" />
<img width="200" height="443" alt="game_play_panel" src="https://github.com/user-attachments/assets/04f1f30e-69eb-4522-af86-65cd23fab377" />
menus and gameplay are separate Canvases toggled at runtime.

---

## 📸 Screenshots

*(Add gameplay screenshots / GIFs here)*<img width="202" height="442" alt="game_play2_panel" src="https://github.com/user-attachments/assets/24ec8511-5ab8-4232-90b6-449ea3908093" />
<img width="196" height="441" alt="win_panel" src="https://github.com/user-attachments/assets/10b6a534-8abd-4fb4-aae3-ef61c045bc64" />


---

## 👤 Author

Built by **Rumeysa Sevben** as a portfolio project demonstrating mobile game development, game feel, and clean architecture in Unity.

---

*BlockBoost is a personal/portfolio project. Art assets courtesy of [Kenney.nl](https://kenney.nl).*
