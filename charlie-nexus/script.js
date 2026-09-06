/**
 * CHARLIE // NEXUS — HIGH-COMPLEXITY SCI-FI COMMAND INTERFACE
 * Master Controller & Holographic Engine
 *
 * Capabilities:
 * - Procedural Three.js 3D Holographic Core (Multi-domain visualization modes)
 * - Peripheral Particle Background System
 * - Dynamic 2D Canvas Module Graphics (Revenue, Operations, Security, Strategy)
 * - Segmented Radial SVG Progress Gauges with Load Animation
 * - Working Tactical Navigation & Mode Switching
 * - Live Event Stream Generator
 * - Web Audio API Procedural SFX Synthesizer
 * - Realtime Telemetry Clock
 */

(function () {
  'use strict';

  // =========================================================================
  // 1. SOUND EFFECT SYNTHESIZER (Web Audio API)
  // =========================================================================

  class SoundEngine {
    constructor() {
      this.ctx = null;
      this.enabled = true;
      this.initOnFirstGesture();
    }

    init() {
      if (!this.ctx) {
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (AudioCtx) {
          this.ctx = new AudioCtx();
        }
      }
    }

    initOnFirstGesture() {
      const unlock = () => {
        this.init();
        if (this.ctx && this.ctx.state === 'suspended') {
          this.ctx.resume();
        }
        window.removeEventListener('click', unlock);
        window.removeEventListener('keydown', unlock);
      };
      window.addEventListener('click', unlock);
      window.addEventListener('keydown', unlock);
    }

    playTone(freq, type = 'sine', duration = 0.08, gain = 0.08) {
      if (!this.enabled || !this.ctx) return;
      try {
        const osc = this.ctx.createOscillator();
        const g = this.ctx.createGain();
        osc.type = type;
        osc.frequency.setValueAtTime(freq, this.ctx.currentTime);
        g.gain.setValueAtTime(gain, this.ctx.currentTime);
        g.gain.exponentialRampToValueAtTime(0.0001, this.ctx.currentTime + duration);

        osc.connect(g);
        g.connect(this.ctx.destination);
        osc.start();
        osc.stop(this.ctx.currentTime + duration);
      } catch (e) {
        // Audio policy or context error ignored
      }
    }

    playBlip() {
      this.playTone(880, 'triangle', 0.06, 0.06);
    }

    playChirp() {
      if (!this.enabled || !this.ctx) return;
      try {
        const osc = this.ctx.createOscillator();
        const g = this.ctx.createGain();
        osc.type = 'sine';
        osc.frequency.setValueAtTime(540, this.ctx.currentTime);
        osc.frequency.exponentialRampToValueAtTime(1240, this.ctx.currentTime + 0.1);
        g.gain.setValueAtTime(0.07, this.ctx.currentTime);
        g.gain.exponentialRampToValueAtTime(0.0001, this.ctx.currentTime + 0.1);

        osc.connect(g);
        g.connect(this.ctx.destination);
        osc.start();
        osc.stop(this.ctx.currentTime + 0.1);
      } catch (e) {}
    }

    playQuantumPulse() {
      if (!this.enabled || !this.ctx) return;
      try {
        const osc = this.ctx.createOscillator();
        const g = this.ctx.createGain();
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(160, this.ctx.currentTime);
        osc.frequency.exponentialRampToValueAtTime(80, this.ctx.currentTime + 0.35);
        g.gain.setValueAtTime(0.12, this.ctx.currentTime);
        g.gain.exponentialRampToValueAtTime(0.0001, this.ctx.currentTime + 0.35);

        osc.connect(g);
        g.connect(this.ctx.destination);
        osc.start();
        osc.stop(this.ctx.currentTime + 0.35);
      } catch (e) {}
    }
  }

  const sfx = new SoundEngine();

  // SFX Toggle button binding
  const sfxBtn = document.getElementById('sfx-toggle');
  if (sfxBtn) {
    sfxBtn.addEventListener('click', () => {
      sfx.enabled = !sfx.enabled;
      sfxBtn.classList.toggle('sfx-muted', !sfx.enabled);
      sfxBtn.querySelector('.sfx-icon').textContent = sfx.enabled ? '🔊' : '🔇';
      sfxBtn.querySelector('.sfx-label').textContent = sfx.enabled ? 'SFX: ACTIVE' : 'SFX: MUTED';
      if (sfx.enabled) sfx.playBlip();
    });
  }

  // =========================================================================
  // 2. REALTIME TELEMETRY CLOCK
  // =========================================================================

  function initClock() {
    const clockEl = document.getElementById('system-clock');
    if (!clockEl) return;
    const timeEl = clockEl.querySelector('.clock-time');

    function update() {
      const now = new Date();
      const h = String(now.getHours()).padStart(2, '0');
      const m = String(now.getMinutes()).padStart(2, '0');
      const s = String(now.getSeconds()).padStart(2, '0');
      timeEl.textContent = `${h}:${m}:${s}`;
    }
    update();
    setInterval(update, 1000);
  }

  // =========================================================================
  // 3. PERIPHERAL PARTICLE SYSTEM CANVAS
  // =========================================================================

  function initPeripheralParticles() {
    const canvas = document.getElementById('peripheral-particles');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');

    let width = (canvas.width = window.innerWidth);
    let height = (canvas.height = window.innerHeight);

    window.addEventListener('resize', () => {
      width = canvas.width = window.innerWidth;
      height = canvas.height = window.innerHeight;
    });

    const particles = [];
    const count = Math.min(Math.floor((width * height) / 25000), 55);

    for (let i = 0; i < count; i++) {
      particles.push({
        x: Math.random() * width,
        y: Math.random() * height,
        vx: (Math.random() - 0.5) * 0.35,
        vy: (Math.random() - 0.5) * 0.35,
        radius: Math.random() * 1.8 + 0.8,
        color: Math.random() > 0.4 ? 'rgba(0, 255, 255, ' : 'rgba(188, 0, 255, ',
        alpha: Math.random() * 0.5 + 0.2,
        pulseSpeed: Math.random() * 0.02 + 0.005,
      });
    }

    let mouseX = width / 2;
    let mouseY = height / 2;
    window.addEventListener('mousemove', (e) => {
      mouseX = e.clientX;
      mouseY = e.clientY;
    });

    function render() {
      ctx.clearRect(0, 0, width, height);

      for (let i = 0; i < particles.length; i++) {
        const p = particles[i];
        p.x += p.vx;
        p.y += p.vy;

        // Wrap around bounds
        if (p.x < 0) p.x = width;
        if (p.x > width) p.x = 0;
        if (p.y < 0) p.y = height;
        if (p.y > height) p.y = 0;

        // Subtle mouse repulsion
        const dx = p.x - mouseX;
        const dy = p.y - mouseY;
        const dist = Math.sqrt(dx * dx + dy * dy);
        if (dist < 120) {
          p.x += (dx / dist) * 0.8;
          p.y += (dy / dist) * 0.8;
        }

        // Alpha breathing
        p.alpha += Math.sin(Date.now() * p.pulseSpeed) * 0.01;
        const boundedAlpha = Math.max(0.1, Math.min(0.65, p.alpha));

        ctx.beginPath();
        ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
        ctx.fillStyle = p.color + boundedAlpha + ')';
        ctx.shadowBlur = 8;
        ctx.shadowColor = p.color + '0.6)';
        ctx.fill();
        ctx.shadowBlur = 0;
      }

      requestAnimationFrame(render);
    }
    render();
  }

  // =========================================================================
  // 4. PROCEDURAL 3D HOLOGRAPHIC CORE (Three.js WebGL)
  // =========================================================================

  class HolographicCore {
    constructor() {
      this.container = document.getElementById('hologram-viewport');
      this.canvas = document.getElementById('core-webgl-canvas');
      this.activeDomain = 'strategy';
      this.targetRotationSpeed = 0.004;

      if (!this.canvas || typeof THREE === 'undefined') {
        console.warn('Three.js not loaded or canvas missing. Falling back to CSS hologram.');
        return;
      }

      this.initScene();
      this.initGeometry();
      this.initEvents();
      this.animate();
    }

    initScene() {
      this.scene = new THREE.Scene();

      const width = this.container.clientWidth;
      const height = this.container.clientHeight;

      this.camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
      this.camera.position.z = 8.5;

      this.renderer = new THREE.WebGLRenderer({
        canvas: this.canvas,
        alpha: true,
        antialias: true,
        powerPreference: 'high-performance',
      });
      this.renderer.setSize(width, height);
      this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

      // Root rotation group
      this.coreGroup = new THREE.Group();
      this.scene.add(this.coreGroup);

      // Light sources
      const ambientLight = new THREE.AmbientLight(0xffffff, 0.4);
      this.scene.add(ambientLight);

      this.pointLightCyan = new THREE.PointLight(0x00ffff, 2.5, 50);
      this.pointLightCyan.position.set(5, 5, 5);
      this.scene.add(this.pointLightCyan);

      this.pointLightMagenta = new THREE.PointLight(0xbc00ff, 2.8, 50);
      this.pointLightMagenta.position.set(-5, -5, -3);
      this.scene.add(this.pointLightMagenta);
    }

    initGeometry() {
      // 1. Outer Geodesic Wireframe Cage
      const icosaGeo = new THREE.IcosahedronGeometry(2.3, 1);
      this.outerCageMat = new THREE.MeshBasicMaterial({
        color: 0x00ffff,
        wireframe: true,
        transparent: true,
        opacity: 0.35,
      });
      this.outerCage = new THREE.Mesh(icosaGeo, this.outerCageMat);
      this.coreGroup.add(this.outerCage);

      // 2. Concentric Tilted Orbital Rings
      this.orbitalRings = [];
      const ringSpecs = [
        { radius: 2.8, color: 0x00ffff, tiltX: 0.8, tiltY: 0.3, speed: 0.008 },
        { radius: 3.2, color: 0xbc00ff, tiltX: -0.6, tiltY: 0.9, speed: -0.006 },
        { radius: 3.6, color: 0x39ff14, tiltX: 1.2, tiltY: -0.4, speed: 0.005 },
      ];

      ringSpecs.forEach((spec) => {
        const ringGeo = new THREE.BufferGeometry();
        const segments = 64;
        const positions = [];
        for (let i = 0; i <= segments; i++) {
          const theta = (i / segments) * Math.PI * 2;
          positions.push(Math.cos(theta) * spec.radius, Math.sin(theta) * spec.radius, 0);
        }
        ringGeo.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));

        const ringMat = new THREE.LineBasicMaterial({
          color: spec.color,
          transparent: true,
          opacity: 0.55,
        });

        const ringMesh = new THREE.Line(ringGeo, ringMat);
        ringMesh.rotation.x = spec.tiltX;
        ringMesh.rotation.y = spec.tiltY;
        ringMesh.userData = { speed: spec.speed, axis: spec.tiltX > 0 ? 'z' : 'y' };

        this.coreGroup.add(ringMesh);
        this.orbitalRings.push(ringMesh);
      });

      // 3. Central Digital Twin Fractal Core
      const innerGeo = new THREE.OctahedronGeometry(1.2, 2);
      this.innerCoreMat = new THREE.MeshStandardMaterial({
        color: 0x071526,
        emissive: 0xbc00ff,
        emissiveIntensity: 0.65,
        roughness: 0.2,
        metalness: 0.85,
        wireframe: false,
        transparent: true,
        opacity: 0.85,
      });
      this.innerCore = new THREE.Mesh(innerGeo, this.innerCoreMat);
      this.coreGroup.add(this.innerCore);

      // Inner wireframe overlay
      const innerWireGeo = new THREE.OctahedronGeometry(1.22, 1);
      const innerWireMat = new THREE.MeshBasicMaterial({
        color: 0x00ffff,
        wireframe: true,
        transparent: true,
        opacity: 0.75,
      });
      this.innerWire = new THREE.Mesh(innerWireGeo, innerWireMat);
      this.innerCore.add(this.innerWire);

      // 4. Swarm of Orbital Data Particles
      const particleCount = 180;
      const particleGeo = new THREE.BufferGeometry();
      const posArray = new Float32Array(particleCount * 3);
      const colorArray = new Float32Array(particleCount * 3);

      for (let i = 0; i < particleCount * 3; i += 3) {
        const r = 2.0 + Math.random() * 1.8;
        const theta = Math.random() * Math.PI * 2;
        const phi = Math.acos(Math.random() * 2 - 1);

        posArray[i] = r * Math.sin(phi) * Math.cos(theta);
        posArray[i + 1] = r * Math.sin(phi) * Math.sin(theta);
        posArray[i + 2] = r * Math.cos(phi);

        // Gradient between Cyan (0, 1, 1) and Magenta (0.74, 0, 1)
        const isCyan = Math.random() > 0.5;
        colorArray[i] = isCyan ? 0.0 : 0.74;
        colorArray[i + 1] = isCyan ? 1.0 : 0.0;
        colorArray[i + 2] = 1.0;
      }

      particleGeo.setAttribute('position', new THREE.BufferAttribute(posArray, 3));
      particleGeo.setAttribute('color', new THREE.BufferAttribute(colorArray, 3));

      const pMaterial = new THREE.PointsMaterial({
        size: 0.065,
        vertexColors: true,
        transparent: true,
        opacity: 0.85,
        blending: THREE.AdditiveBlending,
      });

      this.particles = new THREE.Points(particleGeo, pMaterial);
      this.coreGroup.add(this.particles);
    }

    initEvents() {
      window.addEventListener('resize', () => {
        if (!this.container || !this.renderer || !this.camera) return;
        const w = this.container.clientWidth;
        const h = this.container.clientHeight;
        this.camera.aspect = w / h;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(w, h);
      });

      // Mouse Parallax
      this.targetParallaxX = 0;
      this.targetParallaxY = 0;
      this.currentParallaxX = 0;
      this.currentParallaxY = 0;

      window.addEventListener('mousemove', (e) => {
        const x = (e.clientX / window.innerWidth) * 2 - 1;
        const y = -(e.clientY / window.innerHeight) * 2 + 1;
        this.targetParallaxX = x * 0.45;
        this.targetParallaxY = y * 0.35;
      });

      // Interactive Core Pulse Trigger
      const trigger = document.getElementById('core-trigger');
      if (trigger) {
        trigger.addEventListener('click', () => {
          this.pulseDiagnosticWave();
        });
      }
    }

    setMode(domain) {
      this.activeDomain = domain;
      sfx.playChirp();

      // Update geometry & colors based on selected domain
      switch (domain) {
        case 'revenue':
          this.outerCageMat.color.setHex(0x39ff14);
          this.innerCoreMat.emissive.setHex(0x00ffff);
          this.pointLightCyan.color.setHex(0x39ff14);
          this.pointLightMagenta.color.setHex(0x00ffff);
          break;
        case 'operations':
          this.outerCageMat.color.setHex(0x00ffff);
          this.innerCoreMat.emissive.setHex(0x39ff14);
          this.pointLightCyan.color.setHex(0x00ffff);
          this.pointLightMagenta.color.setHex(0x39ff14);
          break;
        case 'security':
          this.outerCageMat.color.setHex(0xbc00ff);
          this.innerCoreMat.emissive.setHex(0x39ff14);
          this.pointLightCyan.color.setHex(0xbc00ff);
          this.pointLightMagenta.color.setHex(0x39ff14);
          break;
        case 'strategy':
          this.outerCageMat.color.setHex(0xbc00ff);
          this.innerCoreMat.emissive.setHex(0xbc00ff);
          this.pointLightCyan.color.setHex(0x00ffff);
          this.pointLightMagenta.color.setHex(0xbc00ff);
          break;
        default: // core
          this.outerCageMat.color.setHex(0x00ffff);
          this.innerCoreMat.emissive.setHex(0xbc00ff);
          this.pointLightCyan.color.setHex(0x00ffff);
          this.pointLightMagenta.color.setHex(0xbc00ff);
      }
    }

    pulseDiagnosticWave() {
      sfx.playQuantumPulse();

      // Temporary acceleration and scale pop
      const originalScale = this.coreGroup.scale.x;
      this.coreGroup.scale.set(1.18, 1.18, 1.18);
      this.targetRotationSpeed = 0.035;

      setTimeout(() => {
        this.coreGroup.scale.set(originalScale, originalScale, originalScale);
        this.targetRotationSpeed = 0.004;
      }, 400);

      // Trigger event stream log
      if (window.appendCharlieLog) {
        window.appendCharlieLog('DIAGNOSTIC', 'Digital Twin quantum diagnostic pulse complete (Latent drift: 0.002%)');
      }
    }

    animate() {
      requestAnimationFrame(() => this.animate());

      const time = Date.now() * 0.001;

      // Parallax easing
      this.currentParallaxX += (this.targetParallaxX - this.currentParallaxX) * 0.05;
      this.currentParallaxY += (this.targetParallaxY - this.currentParallaxY) * 0.05;

      this.coreGroup.position.x = this.currentParallaxX;
      this.coreGroup.position.y = this.currentParallaxY;

      // Primary Rotations
      this.coreGroup.rotation.y += this.targetRotationSpeed;
      this.outerCage.rotation.x = Math.sin(time * 0.5) * 0.2;
      this.outerCage.rotation.z = Math.cos(time * 0.3) * 0.2;

      // Inner Core Counter-Rotation
      this.innerCore.rotation.y -= 0.008;
      this.innerCore.rotation.x += 0.006;

      // Orbital Rings Rotation
      this.orbitalRings.forEach((ring) => {
        if (ring.userData.axis === 'z') {
          ring.rotation.z += ring.userData.speed;
        } else {
          ring.rotation.y += ring.userData.speed;
        }
      });

      // Data Particle Swarm Rotation
      this.particles.rotation.y += 0.003;
      this.particles.rotation.x = Math.sin(time * 0.2) * 0.15;

      this.renderer.render(this.scene, this.camera);
    }
  }

  // =========================================================================
  // 5. DYNAMIC 2D CANVAS MODULE GRAPHICS (4 MODULE ICONS)
  // =========================================================================

  function initModuleCanvasIcons() {
    // Helper to setup responsive 2D canvas with DPI scaling
    function setupCanvas(id) {
      const c = document.getElementById(id);
      if (!c) return null;
      const ctx = c.getContext('2d');
      const dpr = window.devicePixelRatio || 1;
      const rect = c.getBoundingClientRect();
      c.width = rect.width * dpr;
      c.height = rect.height * dpr;
      ctx.scale(dpr, dpr);
      return { canvas: c, ctx, width: rect.width, height: rect.height };
    }

    const rev = setupCanvas('icon-canvas-revenue');
    const ops = setupCanvas('icon-canvas-operations');
    const sec = setupCanvas('icon-canvas-security');
    const str = setupCanvas('icon-canvas-strategy');

    let angle = 0;

    function renderModuleIcons() {
      angle += 0.025;

      // 1. REVENUE ICON: Animated Financial Funnel Mesh
      if (rev) {
        const { ctx, width: w, height: h } = rev;
        ctx.clearRect(0, 0, w, h);
        const cx = w / 2;
        const cy = h / 2;

        ctx.strokeStyle = 'rgba(57, 255, 20, 0.4)';
        ctx.lineWidth = 1.2;

        // Draw nodal mesh
        const nodes = [
          { x: cx - 22, y: cy - 14 },
          { x: cx + 22, y: cy - 14 },
          { x: cx, y: cy + 18 },
          { x: cx, y: cy - 4 },
        ];

        ctx.beginPath();
        ctx.moveTo(nodes[0].x, nodes[0].y);
        ctx.lineTo(nodes[1].x, nodes[1].y);
        ctx.lineTo(nodes[2].x, nodes[2].y);
        ctx.closePath();
        ctx.stroke();

        ctx.beginPath();
        ctx.moveTo(nodes[3].x, nodes[3].y);
        ctx.lineTo(nodes[2].x, nodes[2].y);
        ctx.stroke();

        // Pulsing financial nodes
        nodes.forEach((n, i) => {
          const pulse = Math.sin(angle + i) * 1.5 + 2.5;
          ctx.beginPath();
          ctx.arc(n.x, n.y, pulse, 0, Math.PI * 2);
          ctx.fillStyle = i === 2 ? '#39ff14' : '#00ffff';
          ctx.shadowBlur = 6;
          ctx.shadowColor = '#39ff14';
          ctx.fill();
          ctx.shadowBlur = 0;
        });
      }

      // 2. OPERATIONS ICON: Rotating Concentric Scanner Loop
      if (ops) {
        const { ctx, width: w, height: h } = ops;
        ctx.clearRect(0, 0, w, h);
        const cx = w / 2;
        const cy = h / 2;

        // Outer Ring
        ctx.beginPath();
        ctx.arc(cx, cy, 20, angle, angle + Math.PI * 1.4);
        ctx.strokeStyle = '#00ffff';
        ctx.lineWidth = 2;
        ctx.shadowBlur = 8;
        ctx.shadowColor = '#00ffff';
        ctx.stroke();
        ctx.shadowBlur = 0;

        // Inner Counter-Ring
        ctx.beginPath();
        ctx.arc(cx, cy, 12, -angle * 1.3, -angle * 1.3 + Math.PI);
        ctx.strokeStyle = '#39ff14';
        ctx.lineWidth = 1.5;
        ctx.stroke();

        // Center reticle dot
        ctx.beginPath();
        ctx.arc(cx, cy, 2.5, 0, Math.PI * 2);
        ctx.fillStyle = '#00ffff';
        ctx.fill();
      }

      // 3. SECURITY ICON: Futuristic Vault / Cryptographic Shield
      if (sec) {
        const { ctx, width: w, height: h } = sec;
        ctx.clearRect(0, 0, w, h);
        const cx = w / 2;
        const cy = h / 2;

        // Hexagonal Vault Frame
        ctx.beginPath();
        for (let i = 0; i < 6; i++) {
          const a = (i / 6) * Math.PI * 2 + angle * 0.4;
          const x = cx + Math.cos(a) * 20;
          const y = cy + Math.sin(a) * 20;
          if (i === 0) ctx.moveTo(x, y);
          else ctx.lineTo(x, y);
        }
        ctx.closePath();
        ctx.strokeStyle = 'rgba(188, 0, 255, 0.6)';
        ctx.lineWidth = 1.5;
        ctx.stroke();

        // Mechanical Lock Reticle
        ctx.beginPath();
        ctx.arc(cx, cy, 8, -angle, -angle + Math.PI * 1.2);
        ctx.strokeStyle = '#39ff14';
        ctx.lineWidth = 2;
        ctx.stroke();

        ctx.beginPath();
        ctx.arc(cx, cy, 3, 0, Math.PI * 2);
        ctx.fillStyle = '#39ff14';
        ctx.shadowBlur = 6;
        ctx.shadowColor = '#39ff14';
        ctx.fill();
        ctx.shadowBlur = 0;
      }

      // 4. STRATEGY ICON: Tactical 3D Data Vector Horizon
      if (str) {
        const { ctx, width: w, height: h } = str;
        ctx.clearRect(0, 0, w, h);
        const cx = w / 2;
        const cy = h / 2;

        // Fluctuating Strategy Vectors
        ctx.beginPath();
        ctx.moveTo(cx - 24, cy + 12);
        ctx.lineTo(cx - 10, cy - 8 + Math.sin(angle) * 4);
        ctx.lineTo(cx + 6, cy + 4 + Math.cos(angle) * 4);
        ctx.lineTo(cx + 24, cy - 14 + Math.sin(angle * 1.5) * 5);
        ctx.strokeStyle = '#bc00ff';
        ctx.lineWidth = 2;
        ctx.shadowBlur = 8;
        ctx.shadowColor = '#bc00ff';
        ctx.stroke();
        ctx.shadowBlur = 0;

        // Vector Horizon Points
        const points = [
          { x: cx - 10, y: cy - 8 + Math.sin(angle) * 4 },
          { x: cx + 6, y: cy + 4 + Math.cos(angle) * 4 },
          { x: cx + 24, y: cy - 14 + Math.sin(angle * 1.5) * 5 },
        ];
        points.forEach((p) => {
          ctx.beginPath();
          ctx.arc(p.x, p.y, 2.8, 0, Math.PI * 2);
          ctx.fillStyle = '#00ffff';
          ctx.shadowBlur = 6;
          ctx.shadowColor = '#00ffff';
          ctx.fill();
          ctx.shadowBlur = 0;
        });
      }

      requestAnimationFrame(renderModuleIcons);
    }
    renderModuleIcons();
  }

  // =========================================================================
  // 6. SEGMENTED RADIAL 3D PROGRESS GAUGES WITH LOAD ANIMATION
  // =========================================================================

  function initRadialGauges() {
    const gauges = [
      { id: 'health', target: 87.4, el: document.getElementById('gauge-bar-health'), num: document.getElementById('num-health') },
      { id: 'tactical', target: 78.6, el: document.getElementById('gauge-bar-tactical'), num: document.getElementById('num-tactical') },
      { id: 'missions', target: 94.2, el: document.getElementById('gauge-bar-missions'), num: document.getElementById('num-missions') },
      { id: 'security', target: 98.7, el: document.getElementById('gauge-bar-security'), num: document.getElementById('num-security') },
    ];

    const circumference = 2 * Math.PI * 48; // r=48 => ~301.59

    gauges.forEach((g) => {
      if (!g.el || !g.num) return;

      // Animate percentage text and stroke-dashoffset
      let currentVal = 0;
      const duration = 1600;
      const startTime = performance.now();

      function animateGauge(now) {
        const elapsed = now - startTime;
        const progress = Math.min(elapsed / duration, 1);
        // Easing out cubic
        const ease = 1 - Math.pow(1 - progress, 3);
        currentVal = g.target * ease;

        g.num.textContent = currentVal.toFixed(1) + '%';
        const offset = circumference - (currentVal / 100) * circumference;
        g.el.style.strokeDashoffset = offset;

        if (progress < 1) {
          requestAnimationFrame(animateGauge);
        } else {
          g.num.textContent = g.target.toFixed(1) + '%';
        }
      }
      requestAnimationFrame(animateGauge);
    });
  }

  // =========================================================================
  // 7. WORKING TACTICAL NAVIGATION & DYNAMIC STAGE SWITCHER
  // =========================================================================

  function initTacticalNavigation(coreInstance) {
    const navButtons = document.querySelectorAll('.nav-btn');
    const modules = document.querySelectorAll('.tactical-module');
    const modeLabel = document.getElementById('core-mode-label');
    const stateIndicator = document.getElementById('core-state-indicator');
    const regimeBadge = document.getElementById('header-regime-badge');

    // Holographic surrounding badges
    const badgeTL = document.getElementById('holo-metric-tl');
    const badgeTR = document.getElementById('holo-metric-tr');
    const badgeBL = document.getElementById('holo-metric-bl');
    const badgeBR = document.getElementById('holo-metric-br');

    navButtons.forEach((btn) => {
      btn.addEventListener('click', () => {
        const target = btn.getAttribute('data-target');
        if (!target) return;

        sfx.playBlip();

        // 1. Update Active Navigation State
        navButtons.forEach((b) => b.classList.remove('active-nav'));
        btn.classList.add('active-nav');
        document.body.setAttribute('data-active-domain', target);

        // 2. Synchronize 2x2 Module Dominance
        modules.forEach((mod) => {
          mod.classList.remove('active-dominant');
          if (mod.getAttribute('data-domain') === target) {
            mod.classList.add('active-dominant');
          }
        });

        // 3. Update Central Hologram Mode
        if (coreInstance) {
          coreInstance.setMode(target);
        }

        // 4. Update Core Labels & Telemetry Badges
        switch (target) {
          case 'revenue':
            if (modeLabel) modeLabel.textContent = 'REVENUE COMMAND';
            if (stateIndicator) stateIndicator.textContent = '● CASHFLOW FLOWING';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'REVENUE VELOCITY'; badgeTL.querySelector('.badge-val').textContent = '+12.8%'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'PIPELINE VALUE'; badgeTR.querySelector('.badge-val').textContent = '₹4.82 Cr'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'REVENUE GAP'; badgeBL.querySelector('.badge-val').textContent = '₹38.4 L'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'FORECAST CONFIDENCE'; badgeBR.querySelector('.badge-val').textContent = '91.0%'; }
            break;
          case 'operations':
            if (modeLabel) modeLabel.textContent = 'MISSION RUNTIME';
            if (stateIndicator) stateIndicator.textContent = '● DAG ORCHESTRATING';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'ACTIVE MISSIONS'; badgeTL.querySelector('.badge-val').textContent = '18 ACTIVE'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'WORKER UTILIZATION'; badgeTR.querySelector('.badge-val').textContent = '81.7%'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'SUCCESS ADMISSION'; badgeBL.querySelector('.badge-val').textContent = '94.2%'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'BOTTLENECKS'; badgeBR.querySelector('.badge-val').textContent = '03 DETECTED'; }
            break;
          case 'security':
            if (modeLabel) modeLabel.textContent = 'FIREWALL CORE';
            if (stateIndicator) stateIndicator.textContent = '● STRIX SOVEREIGN';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'SECURITY POSTURE'; badgeTL.querySelector('.badge-val').textContent = '98.7%'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'EXECUTION FIREWALL'; badgeTR.querySelector('.badge-val').textContent = 'LOCKED'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'POLICY COMPLIANCE'; badgeBL.querySelector('.badge-val').textContent = '100% AUDITED'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'THREAT LEVEL'; badgeBR.querySelector('.badge-val').textContent = 'ZERO EXPOSURE'; }
            break;
          case 'strategy':
            if (modeLabel) modeLabel.textContent = 'STRATEGY SIMULATOR';
            if (stateIndicator) stateIndicator.textContent = '● PRICE WAR REGIME';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'BUSINESS HEALTH'; badgeTL.querySelector('.badge-val').textContent = '87.4%'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'STRATEGIC ALIGNMENT'; badgeTR.querySelector('.badge-val').textContent = '91.2%'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'TACTICAL SCORE'; badgeBL.querySelector('.badge-val').textContent = '78.6%'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'OPPORTUNITIES'; badgeBR.querySelector('.badge-val').textContent = '07 ADMISSIBLE'; }
            break;
          default: // core
            if (modeLabel) modeLabel.textContent = 'BUSINESS CORE';
            if (stateIndicator) stateIndicator.textContent = '● LIVE TWIN SYNC';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'BUSINESS HEALTH'; badgeTL.querySelector('.badge-val').textContent = '87.4%'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'STRATEGIC ALIGNMENT'; badgeTR.querySelector('.badge-val').textContent = '91.2%'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'REVENUE VELOCITY'; badgeBL.querySelector('.badge-val').textContent = '+12.8%'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'WORKER UTILIZATION'; badgeBR.querySelector('.badge-val').textContent = '81.7%'; }
        }

        // 5. Append event to live log
        window.appendCharlieLog('NAV', `Command stage switched to [${target.toUpperCase()}] domain view`);
      });
    });

    // Also link module card clicks to activate that navigation tab
    modules.forEach((mod) => {
      mod.addEventListener('click', () => {
        const domain = mod.getAttribute('data-domain');
        const correspondingNav = document.getElementById(`nav-${domain}`);
        if (correspondingNav) correspondingNav.click();
      });
    });
  }

  // =========================================================================
  // 8. CHARLIE EVENT STREAM (LIVE MISSION FEED GENERATOR)
  // =========================================================================

  function initLiveEventFeed() {
    const feedList = document.getElementById('event-feed-list');
    if (!feedList) return;

    window.appendCharlieLog = function (cat, message) {
      const now = new Date();
      const h = String(now.getHours()).padStart(2, '0');
      const m = String(now.getMinutes()).padStart(2, '0');
      const s = String(now.getSeconds()).padStart(2, '0');
      const timeStr = `[${h}:${m}:${s}]`;

      const entry = document.createElement('div');
      entry.className = 'feed-entry';

      let catClass = 'cat-core';
      if (cat === 'NAV') catClass = 'cat-sim';
      else if (cat === 'DIAGNOSTIC') catClass = 'cat-alert';
      else if (cat === 'I15-GATE') catClass = 'cat-constraint';
      else if (cat === 'STRATEGY') catClass = 'cat-strategy';
      else if (cat === 'SECURITY') catClass = 'cat-security';

      entry.innerHTML = `
        <span class="entry-time">${timeStr}</span>
        <span class="entry-cat ${catClass}">${cat}</span>
        <span class="entry-msg">${message}</span>
      `;

      feedList.appendChild(entry);
      feedList.scrollTop = feedList.scrollHeight;

      // Keep maximum 25 entries
      if (feedList.children.length > 25) {
        feedList.removeChild(feedList.children[0]);
      }
    };

    // Automated periodic mission events (every 9-14 seconds)
    const mockEvents = [
      { cat: 'I15-GATE', msg: 'Invariant I15-A audit verified: Telemetry freshness SLA satisfied (< 5m).' },
      { cat: 'METROLOGY', msg: 'Worker reputation updated: Calibration variance 0.042 (Bayesian shrinkage applied).' },
      { cat: 'FLEET', msg: 'Monotonic fence token #8843 verified for Attempt #2 on Campaign Node.' },
      { cat: 'RESERVE', msg: 'Resource reservation #res-4109 committed: ₹82,400 consumed (Zero double-dip).' },
      { cat: 'SIMULATE', msg: 'Digital Twin state projected: Liquidity buffer ₹11.25L exceeds sovereign floor.' },
      { cat: 'ARBITRATE', msg: 'Lexicographic arbitration: Candidate Mission B selected under PriceWar regime.' },
      { cat: 'FIREWALL', msg: 'Batch 6 Execution Firewall status confirmed: Sovereign read-only lock armed.' },
    ];

    let eventIdx = 0;
    setInterval(() => {
      const ev = mockEvents[eventIdx % mockEvents.length];
      eventIdx++;
      window.appendCharlieLog(ev.cat, ev.msg);
    }, 11000);
  }

  // =========================================================================
  // 9. BOOTSTRAP MASTER RUNTIME
  // =========================================================================

  document.addEventListener('DOMContentLoaded', () => {
    initClock();
    initPeripheralParticles();
    const core = new HolographicCore();
    initModuleCanvasIcons();
    initRadialGauges();
    initTacticalNavigation(core);
    initLiveEventFeed();

    // Initial boot chirp
    setTimeout(() => {
      sfx.playChirp();
    }, 600);
  });
})();
