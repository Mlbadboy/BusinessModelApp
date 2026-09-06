# CHARLIE // NEXUS — Autonomous Business Command Interface

**Subsystem:** Futuristic Tactical Frontend for Charlie Business Operating System  
**Version:** 3.5.8 (Batch 3.5 Business Constraint Sovereignty Certified)  
**Stack:** HTML5, Vanilla CSS3 (Custom Design System), Vanilla JavaScript (ES6+), Three.js (Procedural WebGL)

---

## 1. Quick Start / Running Locally

No npm dependencies, node server, or build steps are required. The application is completely self-contained and immediately runnable.

### Option A: Direct Browser Launch
Double click or open `index.html` directly in any modern browser (Chrome, Edge, Firefox, Safari):
```bash
start "e:/Business model app/charlie-nexus/index.html"
```

### Option B: Local HTTP Server (Optional)
```bash
cd "e:/Business model app/charlie-nexus"
npx serve .
# or
python -m http.server 8080
```
Then navigate to `http://localhost:8080`.

---

## 2. Core Visual Language & Aesthetic Features

- **Palette:** 
  - Void Slate Background: `#040508`
  - Electric Cyan: `#00FFFF` (Glow: `rgba(0, 255, 255, 0.45)`)
  - Radioactive Green: `#39FF14` (Glow: `rgba(57, 255, 20, 0.4)`)
  - Deep Magenta: `#BC00FF` (Glow: `rgba(188, 0, 255, 0.5)`)
  - Alert Amber: `#FFB800`
- **Atmosphere:**
  - Dynamic procedural Three.js 3D Hologram at stage center with mouse parallax.
  - Multi-layer CRT scanline overlay and subtle chromatic vignette.
  - Floating peripheral canvas particle field responding to mouse movements.
  - Angular technical clipped corners (`clip-path: polygon(...)`) with neon gradient borders.
  - Procedural Web Audio API sound synthesizer for interactive tactile feedback (no audio assets required).

---

## 3. Major UI Components

### 1. Top Command Header
- **Left:** `CHARLIE // NEXUS` branding with spinning cybernetic crest and `PROCOL-OS // VER 3.5.8` subtitle.
- **Center:** Realtime telemetry beacon (`SYSTEM ONLINE // LEVEL 3 AUTONOMOUS`) and active strategic regime badge (`MARKET DEFENSE: PRICE WAR`).
- **Right:** Quantum-sync latency readout (`12ms`), Batch 6 Execution Firewall status (`LOCKED`), live precision clock, and Root Architect authorization avatar.

### 2. Central 3D Holographic Core
- Built with procedural Three.js WebGL:
  - Outer Geodesic Icosahedron wireframe cage.
  - Concentric counter-rotating orbital rings on tilted axes.
  - Internal fractal octahedron Digital Twin core with emissive lighting.
  - Swarm of orbital data particles drifting in elliptical trajectories.
  - Interactive Central Trigger: Clicking triggers a diagnostic quantum pulse with camera bloom, sound wave, and log event.
  - Surrounding HUD telemetry badges (`BUSINESS HEALTH: 87.4%`, `STRATEGIC ALIGNMENT: 91.2%`, `REVENUE VELOCITY: +12.8%`, `WORKER UTILIZATION: 81.7%`).

### 3. Segmented Radial 3D Progress Gauges
- Four mechanical SVG segmented circular gauges with tick marks and target percentage animations on boot:
  - `BUSINESS HEALTH: 87.4%`
  - `STRATEGY SCORE: 78.6%` (Active dominant magenta ring)
  - `MISSION SUCCESS: 94.2%`
  - `SECURITY POSTURE: 98.7%`

### 4. 2x2 Modular Capability Grid
- **Module 01 — REVENUE COMMAND:** Realtime velocity `+12.8%`, Pipeline `₹4.82 Cr`, Gap `₹38.4 L`, animated 2D canvas financial nodal mesh.
- **Module 02 — OPERATIONS & FLEET:** Active missions `18`, Worker utilization `81.7%`, Success `94.2%`, animated rotating concentric scanner loop.
- **Module 03 — SECURITY & FIREWALL:** Security posture `98.7%`, Firewall `LOCKED`, Threats `02 MITIGATED`, animated mechanical vault locking mechanism.
- **Module 04 — STRATEGY COMMAND (Active/Dominant):** Tactical score `78.6%`, Strategic alignment `91.2%`, Active regime `PRICE WAR`, animated 3D-styled vector horizon.

### 5. Tactical Business Constraint Manifold (Batch 3.5 HUD)
- Realtime sovereign constraint indicators:
  - `LIQUIDITY RESERVE (₹10L FLOOR)`: SAFE (`₹12L`)
  - `GROSS MARGIN (25% FLOOR)`: SAFE (`28.4%`)
  - `CAC CEILING (₹2,500)`: WARNING (`₹2,380`)
  - `HUMAN APPROVAL CAPACITY`: CONSTRAINED (`9/10`)
  - `AUTONOMOUS MISSION FLEET`: SAFE (`18/20`)
- Enforces Invariant I15-D notice: *"Hard constraints cannot be optimized away by reputation or revenue utility."*

### 6. WHY CHARLIE ACTED (Epistemic HUD Panel)
- Explains live autonomous causality:
  - `TRIGGER`: Revenue velocity drop (-11.4%) detected.
  - `EVIDENCE`: 7 verified Digital Twin signals (Ledger TX #9941).
  - `RESPONSIBILITY`: Investigate enterprise churn & compress CAC.
  - `CONSTRAINT STATUS`: Passed (Liquidity SAFE, Margin SAFE).
  - `STRATEGIC REGIME`: MarketDefense_PriceWar.
  - `DECISION`: Autonomous Recovery Mission Admitted (₹75K Allocated).

### 7. Charlie Event Stream (Live Mission Feed)
- Real-time timestamped mission log.
- Automatically generates periodic Charlie OS events (constraint verification, fence token issuance, Bayesian shrinkage adjustments, pre-flight simulations, and firewall arming).

### 8. Futuristic Angular Bottom Navigation
- Five clipped angular tabs: `[ BUSINESS CORE ]`, `[ REVENUE COMMAND ]`, `[ OPERATIONS & FLEET ]`, `[ STRATEGY COMMAND ]`, `[ SECURITY & FIREWALL ]`.
- Fully interactive: clicking switches the 3D Hologram visualization, updates surrounding HUD badges, activates the corresponding module card, and logs to the Event Stream.

---

## 4. List of Interactions Implemented

1. **Mouse Parallax on 3D Core:** Hologram rotates and shifts with mouse position.
2. **Core Diagnostic Click:** Clicking the center core triggers an expanding scale pulse, audio burst, and diagnostic log.
3. **Domain Mode Switching:** Clicking bottom navigation buttons or module cards transitions the 3D hologram colors, rotation speeds, and active telemetry.
4. **Interactive 2D Canvas Graphics:** Four micro-canvases animate continuously with custom vector and particle geometry.
5. **Radial Gauge Startup Animation:** Circular progress strokes and text smoothly interpolate from 0 to target values on load.
6. **SFX Synthesizer & Audio Toggle:** Procedural Web Audio API sound feedback on clicks and transitions, with a mute toggle button in the top-right.
7. **Module Hover & Scanline Effects:** Modules illuminate, lift slightly, and display animated vertical scanlines.
8. **Live Event Stream Auto-Scroll:** New events append with categories, pulse animations, and automatic downward scrolling.
9. **Responsive Mobile Layout:** Adapts smoothly to mobile screens with touch-friendly navigation, stacked cards, and scaled WebGL canvas.
