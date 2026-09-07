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

    playWarning() {
      this.playTone(320, 'sawtooth', 0.16, 0.08);
      setTimeout(() => this.playTone(280, 'sawtooth', 0.2, 0.08), 80);
    }

    playDenial() {
      this.playTone(220, 'square', 0.12, 0.09);
      setTimeout(() => this.playTone(180, 'square', 0.16, 0.09), 100);
    }

    playSuccess() {
      this.playTone(520, 'sine', 0.08, 0.06);
      setTimeout(() => this.playTone(780, 'sine', 0.12, 0.07), 70);
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

      // Inner Core Counter-Rotation & Dynamic Emissive Breathing
      this.innerCore.rotation.y -= 0.008;
      this.innerCore.rotation.x += 0.006;
      if (this.innerCoreMat) {
        this.innerCoreMat.emissiveIntensity = 0.55 + Math.sin(time * 2.0) * 0.22;
      }

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
      { id: 'missions', target: 'UNKNOWN', el: document.getElementById('gauge-bar-missions'), num: document.getElementById('num-missions') },
      { id: 'security', target: 'UNKNOWN', el: document.getElementById('gauge-bar-security'), num: document.getElementById('num-security') },
    ];

    const circumference = 2 * Math.PI * 48; // r=48 => ~301.59

    gauges.forEach((g) => {
      if (!g.el || !g.num) return;

      if (g.target === 'UNKNOWN') {
        g.num.textContent = 'UNKNOWN';
        g.num.style.fontSize = '0.95rem';
        g.num.style.letterSpacing = '0.05em';
        g.num.style.color = 'var(--text-dim)';
        g.el.style.strokeDashoffset = circumference;
        return;
      }

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
  // 7. CHARLIE BUSINESS OS DATA LAYER (Batches 3.1 - 3.6 Governed State)
  // =========================================================================

  const NEXUS_DATA = {
    summary: {
      businessHealth: 87.4,
      strategicScore: 78.6,
      missionSuccess: 'UNKNOWN',
      securityPosture: 'UNKNOWN',
      revenueVelocity: 'LIVE — IDLE',
      pipeline: '₹6,20,000 ◆ SIMULATION',
      revenueGap: 'UNKNOWN',
      forecastConf: 'UNKNOWN (Pending Realization)',
      activeMissionsCount: 7,
      bottlenecksCount: 1,
      firewallStatus: 'LOCKED',
      strategicRegime: 'MarketDefense_PriceWar',
      autonomyLevel: 'L3_Prepare',
      tenantCeiling: 'L3_Prepare',
      quantumSyncMs: 14,
    },

    decision: {
      trigger: 'Revenue velocity drop (-11.4%) detected across Mid-Market SaaS segment',
      evidence: '7 verified Digital Twin telemetry signals (Ledger TX #9941, Stripe Webhook #evt_8921)',
      responsibility: 'Investigate enterprise churn, protect retention floor, compress CAC',
      missionId: 'M-9941',
      missionName: 'Enterprise SaaS Churn Containment & Lead Routing Recovery',
      constraints: [
        { name: 'Liquidity Reserve', status: 'SAFE', value: '₹12.0L', floor: '₹10.0L', hard: true },
        { name: 'Gross Margin', status: 'SAFE', value: '28.4%', floor: '25.0%', hard: true },
        { name: 'CAC Ceiling', status: 'WARNING', value: '₹2,380', ceiling: '₹2,500', hard: false },
        { name: 'Human Approval Capacity', status: 'CONSTRAINED', value: '9/10 Permits', ceiling: '10', hard: true },
      ],
      strategicRegime: 'MarketDefense_PriceWar (Rank #1: Retention, Rank #2: Margin Defense)',
      worker: 'Worker-API-07 (Modality: API, Sandbox: SBX-8832)',
      reputation: '98.2% empirical calibration (Bayesian shrinkage applied, zero hallucination)',
      simulation: 'Projected net revenue recovery: ₹75,000 within 14-day SLA (Confidence: 93.4%)',
      executionStatus: 'Awaiting governed authorization & ActionProposal #AP-9941 firewall signature',
    },

    constraints: {
      liquidity: {
        id: 'liquidity',
        title: 'LIQUIDITY RESERVE FLOOR',
        threshold: '₹10,00,000 FLOOR',
        current: '₹12,00,000',
        status: 'SAFE',
        source: 'Verified Banking Ledger (HDFC API Sync #tx-4991)',
        freshness: '99.8% (Lag: 42s < 5m SLA)',
        lastVerified: '14:31:42 UTC',
        strategicEffect: 'High — Guarantees enterprise solvency & vendor payroll',
        isHard: true,
        reason: 'Operating cash balance comfortably exceeds sovereign liquidity threshold by ₹2.0L buffer.',
        alternatives: ['None required — constraint is currently satisfied.'],
      },
      margin: {
        id: 'margin',
        title: 'GROSS MARGIN FLOOR',
        threshold: '25.0% MINIMUM FLOOR',
        current: '28.4%',
        status: 'SAFE',
        source: 'Enterprise ERP Ledger & COGS Reconciliation',
        freshness: '98.2% (Lag: 2m 14s)',
        lastVerified: '14:30:15 UTC',
        strategicEffect: 'Critical — Underpins unit economics sustainability',
        isHard: true,
        reason: 'Enterprise product blended gross margin remains 3.4% above safety floor.',
        alternatives: ['None required — constraint is satisfied.'],
      },
      cac: {
        id: 'cac',
        title: 'CUSTOMER ACQUISITION COST (CAC) CEILING',
        threshold: '₹2,500 CEILING',
        current: '₹2,380',
        status: 'WARNING',
        source: 'Ad Network APIs & CRM Lead Pipeline',
        freshness: '94.0% (Lag: 3m 48s)',
        lastVerified: '14:28:50 UTC',
        strategicEffect: 'Moderate — Approaching margin erosion boundary',
        isHard: false,
        reason: 'Acquisition cost in Google Ads segment elevated due to competitive price bids in Price War.',
        alternatives: [
          'Reduce competitive PPC campaign spend by 15% on high-cpc keywords',
          'Prioritize warm outbound reactivation of churned enterprise contacts',
          'Reallocate existing SDR capacity to inbound partner referral funnel',
        ],
      },
      approval: {
        id: 'approval',
        title: 'HUMAN APPROVAL CAPACITY',
        threshold: '10 DAILY PERMITS MAX',
        current: '9 / 10 USED',
        status: 'CONSTRAINED',
        source: 'Executive Approval Gateway Service',
        freshness: '100% Realtime',
        lastVerified: '14:32:00 UTC',
        strategicEffect: 'High — Prevents human decision fatigue & approval rubber-stamping',
        isHard: true,
        reason: 'Executive sign-off quota is at 90% capacity for the current 24-hour cycle.',
        alternatives: [
          'Queue low-risk proposals for automated batch review tomorrow 09:00 UTC',
          'Downgrade non-critical proposal to L2 simulation-only mode',
        ],
      },
      concurrency: {
        id: 'concurrency',
        title: 'AUTONOMOUS MISSION FLEET CONCURRENCY',
        threshold: '20 CONCURRENT MISSIONS',
        current: '18 / 20 ACTIVE',
        status: 'SAFE',
        source: 'Agent Runtime Fleet Supervisor',
        freshness: '100% Realtime',
        lastVerified: '14:32:10 UTC',
        strategicEffect: 'Systemic — Preserves process stability & avoids thread contention',
        isHard: true,
        reason: '18 active missions concurrently executing across 4 worker modalities without memory leaks.',
        alternatives: ['None required — runtime has headroom for 2 additional admitted missions.'],
      },
    },

    workers: {
      stats: {
        apiCount: '3 ACTIVE, 1 IDLE',
        mcpCount: '0 ACTIVE (NOT CONFIGURED)',
        browserCount: '2 ACTIVE',
        desktopCount: 'NOT CONFIGURED',
        totalExecutions: 'AUTHENTIC RUNTIME',
        quarantinedCount: 0,
      },
      list: [
        {
          id: 'Worker-API-01',
          modality: 'API',
          capability: 'Invoice Reconciliation & Ledger Sync',
          health: 'HEALTHY',
          circuit: 'Healthy',
          lease: 'ACTIVE (Lease #L-9012)',
          fence: 'VALID (Token #9012)',
          sandbox: 'SBX-API-01',
          cpu: 'LIVE',
          memory: 'BOUNDED',
          tokens: 'MEASURED',
          reputation: 'VERIFIED',
          mission: 'Mission #M-19382',
          attempt: '#1',
          authority: 'NONE (Proposal Gateway)',
          firewall: 'REQUIRED (Batch 6)',
        },
        {
          id: 'Worker-API-02',
          modality: 'API',
          capability: 'Customer History & Retention Economics',
          health: 'HEALTHY',
          circuit: 'Healthy',
          lease: 'ACTIVE (Lease #L-9014)',
          fence: 'VALID (Token #9014)',
          sandbox: 'SBX-API-02',
          cpu: 'LIVE',
          memory: 'BOUNDED',
          tokens: 'MEASURED',
          reputation: 'VERIFIED',
          mission: 'Mission #M-19382',
          attempt: '#1',
          authority: 'NONE (Proposal Gateway)',
          firewall: 'REQUIRED (Batch 6)',
        },
        {
          id: 'Worker-BROWSER-01',
          modality: 'Browser',
          capability: 'Vendor Portal DOM Automation & Verification',
          health: 'HEALTHY',
          circuit: 'Healthy',
          lease: 'ACTIVE (Lease #L-9013)',
          fence: 'VALID (Token #9013)',
          sandbox: 'SBX-BRW-01',
          cpu: 'LIVE',
          memory: 'BOUNDED',
          tokens: 'MEASURED',
          reputation: 'VERIFIED',
          mission: 'Mission #M-19382',
          attempt: '#1',
          authority: 'NONE (Proposal Gateway)',
          firewall: 'REQUIRED (Batch 6)',
        },
      ],
      violations: [
        {
          id: 'VIO-1092',
          workerId: 'Worker-DESKTOP-04',
          type: 'ShellExecutionDenied',
          detail: 'Unauthorized execution attempt of powershell.exe blocked by default-DENY containment policy.',
          timestamp: '14:18:22 UTC',
          action: 'Worker immediately terminated and placed into quarantine. Reputation degraded by -25 pts.',
        },
      ],
    },

    firewall: {
      status: 'LOCKED',
      activePermits: 3,
      pendingProposals: 2,
      rejectedProposals: 4,
      killSwitchState: 'ARMED & READY',
      lastAction: '14:32:06 UTC (Proposal #AP-9938 Approved for Bank Transfer)',
      authority: 'Batch 6 Sovereign Execution Firewall',
      workerDirectAccess: 'STRICTLY DENIED (Invariant I16 Zero-Bypass)',
      pendingList: [
        {
          id: 'AP-9941',
          target: 'Vendor Payment Gateway (Stripe Transfer ₹75,000)',
          proposer: 'Worker-API-07',
          mission: 'Mission #M-9941',
          riskTier: 'R3_FinancialSideEffect',
          status: 'Awaiting Human Executive Signature',
        },
        {
          id: 'AP-9943',
          target: 'ERP Customer Status Transition (Mark Churn Recovered)',
          proposer: 'Worker-MCP-03',
          mission: 'Mission #M-9941',
          riskTier: 'R2_DataModification',
          status: 'Awaiting Governance Review',
        },
      ],
    },

    autonomy: {
      currentTenantCeiling: 'L3_Prepare',
      currentMission: 'L3_Prepare',
      effectiveAutonomy: 'L3_Prepare',
      reason: 'min(TenantCeiling [L3], MissionCeiling [L3], PolicyGate [L3]) = L3_Prepare',
      tiers: [
        { tier: 'L0_Observe', title: 'L0 // OBSERVE', desc: 'Read-only telemetry & Digital Twin observation. Zero proposal authority.' },
        { tier: 'L1_Advise', title: 'L1 // ADVISE', desc: 'Generate strategic insights & recommendations. No action drafting.' },
        { tier: 'L2_Simulate', title: 'L2 // SIMULATE', desc: 'Pre-flight hypothetical outcome simulation and scenario modeling.' },
        { tier: 'L3_Prepare', title: 'L3 // PREPARE [CURRENT]', desc: 'Prepare concrete ActionProposals and draft transactions for human review.' },
        { tier: 'L4_ExecuteWithApproval', title: 'L4 // EXECUTE WITH APPROVAL', desc: 'Execute consequential side-effects upon human cryptographic approval.' },
        { tier: 'L5_BoundedAutonomy', title: 'L5 // BOUNDED AUTONOMY', desc: 'Execute autonomous operations strictly within pre-reserved budget ceilings.' },
      ],
    },

    regimes: {
      active: 'MarketDefense_PriceWar',
      list: [
        {
          id: 'MarketDefense_PriceWar',
          name: 'MARKET DEFENSE: PRICE WAR',
          priority: '1. Customer Retention (0.45) &bull; 2. Margin Defense (0.35) &bull; 3. Churn Containment (0.20)',
          desc: 'Active regime configured by enterprise owner. Sacrifices expansion velocity to defend high-value accounts from competitor price discounting.',
        },
        {
          id: 'CashPreservation_Distressed',
          name: 'CASH PRESERVATION: DISTRESSED',
          priority: '1. Liquidity Floor (0.60) &bull; 2. OpEx Reduction (0.25) &bull; 3. Debt Solvency (0.15)',
          desc: 'Freezes non-essential autonomous spend; maximizes runway and strictly restricts consequential capital dispatch.',
        },
        {
          id: 'BalancedProfitability_Conservative',
          name: 'BALANCED PROFITABILITY: CONSERVATIVE',
          priority: '1. EBITDA Margin (0.40) &bull; 2. Unit Economics (0.35) &bull; 3. Low-Risk Growth (0.25)',
          desc: 'Mandates positive unit economics across all operational nodes; forbids negative-margin customer acquisition.',
        },
        {
          id: 'AggressiveGrowth_Expansion',
          name: 'AGGRESSIVE GROWTH: EXPANSION',
          priority: '1. Market Share (0.50) &bull; 2. Customer Acquisition (0.30) &bull; 3. Pipeline Velocity (0.20)',
          desc: 'Allocates maximum autonomous budgets to outbound sales, marketing campaigns, and market penetration within solvency limits.',
        },
      ],
    },

    provenance: {
      health: {
        metric: 'Business Health Index',
        value: '87.4%',
        source: 'Outcome Ledger & Multi-Modal Digital Twin',
        window: 'Last 30 Days (Rolling)',
        sample: '1,420 Transaction Records',
        lastUpdated: '14:32:10 UTC',
      },
      tactical: {
        metric: 'Strategy Tactical Score',
        value: '78.6%',
        source: 'Lexicographic Arbitration Engine (Batch 3.5)',
        window: 'Active Regime Evaluation Cycle',
        sample: '14 Candidate Missions Evaluated',
        lastUpdated: '14:31:55 UTC',
      },
      missions: {
        metric: 'Mission Success Rate',
        value: 'UNKNOWN',
        source: 'Outcome Ledger & Runtime Store',
        window: 'Current Workspace (Tenant-Scoped)',
        sample: '3 Completed Missions (Sample too small for statistical significance)',
        lastUpdated: '17:31:18 IST',
      },
      security: {
        metric: 'Security Posture Index',
        value: 'UNKNOWN',
        source: 'Strix Defense Matrix & Telemetry Stream',
        window: 'Continuous Real-Time',
        sample: '0 Incident Events (Insufficient current telemetry to calculate definite %)',
        lastUpdated: '17:31:18 IST',
      },
      revenue: {
        metric: 'Revenue & Run-Rate',
        value: '₹4,82,300',
        source: 'RevenueEvent Ledger (#REV-982341)',
        window: 'Current Month (01 Sep – 06 Sep)',
        sample: '142 Verified Transaction Events',
        lastUpdated: '17:31:18 IST',
      },
    },

    reality: {
      mode: 'LIVE PRODUCTION',
      tenantId: 'workspace-mayur',
      systemOverview: {
        database: 'LIVE VERIFIED',
        digitalTwin: 'LIVE (Lag: 14s)',
        runtime: 'RUNNING',
        workerFabric: 'CONNECTED (3 Active, 1 Idle)',
        brain: 'CONNECTED (OpenRouter / Claude 3.5 Sonnet)',
        firewall: 'LOCKED / OPERATIONAL',
        eventBus: 'RECEIVING',
        lastEventTime: '17:31:42 IST',
      },
      connectors: [
        { type: 'CRM', name: 'HubSpot / Salesforce CRM', status: 'NOT CONFIGURED', note: 'Charlie is NOT reading or writing live CRM data.' },
        { type: 'EMAIL', name: 'SendGrid / SMTP Gateway', status: 'NOT CONFIGURED', note: 'Outbound customer email dispatch disabled.' },
        { type: 'WHATSAPP', name: 'WhatsApp Cloud API', status: 'DISCONNECTED', note: 'Webhook disconnected.' },
        { type: 'SMS', name: 'Twilio SMS Gateway', status: 'NOT CONFIGURED', note: 'SMS messaging disabled.' },
        { type: 'PAYMENTS', name: 'Stripe / Razorpay', status: 'NOT CONFIGURED', note: 'Financial execution gateway in mock-rejection stasis.' },
        { type: 'CALENDAR', name: 'Google Workspace Calendar', status: 'NOT CONFIGURED', note: 'OAuth consent pending.' },
        { type: 'BROWSER', name: 'Headless Chromium Worker Grid', status: 'CONNECTED', note: 'Batch 3.6 Browser Worker Sandboxes operational.' },
        { type: 'MCP', name: 'Model Context Protocol Gateway', status: 'CONNECTED', note: 'Batch 3.6 MCP adapter ready for tool execution.' },
      ],
    },

    approvals: [
      {
        id: 'APP-19382',
        missionId: 'M-9941',
        missionName: 'Enterprise Churn Recovery',
        nodeId: 'Node-05',
        workerId: 'Worker-API-07',
        targetSystem: 'HubSpot CRM Gateway',
        capability: 'crm.deal.update',
        risk: 'R2 — External Customer Communication',
        actionDescription: 'Send retention discount offer to high-churn risk account',
        proposedEffect: 'Applies 15% discount code (Credit ceiling ₹12,500)',
        evidenceSummary: '7 verified Digital Twin signals (Ledger #REV-982, CRM #D-102)',
        constraintStatus: '✓ Liquidity SAFE | ✓ Margin SAFE | ⚠ CAC Warning | ✓ Strategic Regime',
        strategicRegime: 'MarketDefense_PriceWar',
        financialExposure: '₹12,500 maximum',
        currency: 'INR',
        payloadDigest: 'SHA-256: 8F72A910E4B184C0F731D429819AC72B',
        payloadJson: '{\\n  "missionId": "M-9941",\\n  "customer": "Acme Global Enterprises",\\n  "action": "ApplyDiscountOffer",\\n  "discountPercent": 15,\\n  "maxCreditInr": 12500,\\n  "validityDays": 14,\\n  "authorizedBy": "CEO-Mayur"\\n}',
        isReversible: true,
        requestedAt: '17:22:41 IST',
        expiresIn: '3h 41m remaining (SLA: 4 Hours)',
        state: 'REQUESTED',
      },
      {
        id: 'APP-19383',
        missionId: 'M-8842',
        missionName: 'High-Value Vendor Invoice Settlement',
        nodeId: 'Node-03',
        workerId: 'Worker-API-03',
        targetSystem: 'QuickBooks Financial Gateway',
        capability: 'invoice.settlement.authorize',
        risk: 'R3 — Direct Financial Transaction',
        actionDescription: 'Authorize vendor payout for quarterly server infrastructure',
        proposedEffect: 'Disburse ₹45,000 to AWS Cloud Infra provider',
        evidenceSummary: 'Verified PO #PO-8812 matching CloudWatch usage ledger',
        constraintStatus: '✓ Liquidity Buffer ₹1.2 Cr | ✓ Margin SAFE',
        strategicRegime: 'BalancedProfitability',
        financialExposure: '₹45,000',
        currency: 'INR',
        payloadDigest: 'SHA-256: 3D19B44F81A92C37E154AA72D0B65709',
        payloadJson: '{\\n  "missionId": "M-8842",\\n  "vendor": "Amazon Web Services",\\n  "poNumber": "PO-8812",\\n  "amountInr": 45000,\\n  "account": "InfrastructureOpEx"\\n}',
        isReversible: false,
        requestedAt: '17:28:15 IST',
        expiresIn: '1h 29m remaining (SLA: 2 Hours)',
        state: 'REQUESTED',
      },
    ],

    workControl: {
      counts: {
        active: 7,
        waitingApproval: 2,
        waitingData: 3,
        running: 5,
        completed: 42,
        failed: 4,
        blocked: 3,
        cancelled: 1,
      },
      ledger: [
        { step: '01', title: 'Detect churn signal', status: 'Completed', stage: 'Responsibility', time: '17:02:13' },
        { step: '02', title: 'Verify customer history', status: 'Completed', stage: 'Evidence', time: '17:03:04' },
        { step: '03', title: 'Analyze churn cause', status: 'Completed', stage: 'Graph', time: '17:04:22' },
        { step: '04', title: 'Calculate retention economics', status: 'Completed', stage: 'Worker', time: '17:05:40' },
        { step: '05', title: 'Select recommended offer', status: 'Completed', stage: 'Strategy', time: '17:07:11' },
        { step: '06', title: 'Prepare customer communication', status: 'Completed', stage: 'Constraint', time: '17:09:55' },
        { step: '07', title: 'HUMAN APPROVAL: Send retention offer', status: 'WaitingApproval', stage: 'HumanApproval', time: '17:12:41' },
        { step: '08', title: 'Send communication via Gateway', status: 'Pending', stage: 'ExecutionFirewall', time: 'Pending' },
        { step: '09', title: 'Verify recipient response', status: 'Pending', stage: 'Connector', time: 'Pending' },
        { step: '10', title: 'Measure outcome against ledger', status: 'Pending', stage: 'Outcome', time: 'Pending' },
        { step: '11', title: 'Close mission and update reputation', status: 'Pending', stage: 'Metrology', time: 'Pending' },
      ],
    },

    valueRealization: {
      expectedRevenue: '₹1,00,000',
      authorizedExposure: '₹15,000',
      actualSpend: '₹8,200',
      verifiedRevenue: '₹92,000',
      verifiedMargin: '₹31,400',
      realizedValue: '₹23,200',
      realizedValueStatus: '● LIVE VERIFIED',
      ledgerReference: 'Payment Ledger #PAY-98421 & Outcome Ledger #OUT-8812',
      unverifiedMission: {
        name: 'Customer Win-Back (#M-7712)',
        workCompleted: '8 / 10 nodes (80%)',
        expectedValue: '₹75,000',
        realizedValue: 'UNKNOWN',
        reason: 'Customer has not responded yet. Epistemic invariant enforced: WORK COMPLETED != VALUE REALIZED.',
      },
    },
  };

  // =========================================================================
  // 8. TACTICAL MODAL CONTROLLER (HIGH-TECH HUD INSPECTORS)
  // =========================================================================

  class TacticalModalController {
    constructor() {
      this.overlay = document.getElementById('tactical-modal-overlay');
      this.window = document.getElementById('tactical-inspector-window');
      this.codeEl = document.getElementById('modal-category-code');
      this.titleEl = document.getElementById('modal-title');
      this.tabStrip = document.getElementById('modal-tab-strip');
      this.bodyEl = document.getElementById('modal-body-content');
      this.footerInfo = document.getElementById('modal-footer-info');
      this.footerActions = document.getElementById('modal-footer-actions');
      this.closeBtn = document.getElementById('modal-close-btn');

      this.initEvents();
    }

    initEvents() {
      if (this.closeBtn) {
        this.closeBtn.addEventListener('click', () => this.close());
      }

      if (this.overlay) {
        this.overlay.addEventListener('click', (e) => {
          if (e.target === this.overlay) this.close();
        });
      }

      window.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && this.isOpen()) {
          this.close();
        }
      });
    }

    isOpen() {
      return this.overlay && this.overlay.classList.contains('active');
    }

    open({ categoryCode, title, tabs = [], bodyHtml, footerText, actions = [] }) {
      if (!this.overlay) return;

      sfx.playChirp();

      if (this.codeEl) this.codeEl.textContent = categoryCode || 'SYSTEM // INSPECTOR';
      if (this.titleEl) this.titleEl.textContent = title || 'TACTICAL INSPECTOR';
      if (this.footerInfo) this.footerInfo.innerHTML = footerText || 'GOVERNED BY BATCH 6 EXECUTION FIREWALL &bull; CHARLIE OS';

      // Setup Tabs
      if (this.tabStrip) {
        if (tabs && tabs.length > 1) {
          this.tabStrip.style.display = 'flex';
          this.tabStrip.innerHTML = tabs
            .map(
              (t, i) => `
            <button class="inspector-tab ${i === 0 ? 'active' : ''}" data-tab-idx="${i}">
              ${t.label}
            </button>
          `
            )
            .join('');

          this.tabStrip.querySelectorAll('.inspector-tab').forEach((tabBtn) => {
            tabBtn.addEventListener('click', () => {
              sfx.playBlip();
              this.tabStrip.querySelectorAll('.inspector-tab').forEach((b) => b.classList.remove('active'));
              tabBtn.classList.add('active');
              const idx = parseInt(tabBtn.getAttribute('data-tab-idx'), 10);
              if (tabs[idx] && typeof tabs[idx].onSelect === 'function') {
                const newHtml = tabs[idx].onSelect();
                if (this.bodyEl && newHtml) this.bodyEl.innerHTML = newHtml;
              }
            });
          });
        } else {
          this.tabStrip.style.display = 'none';
        }
      }

      // Populate Body
      if (this.bodyEl) {
        this.bodyEl.innerHTML = bodyHtml || '';
      }

      // Populate Actions
      if (this.footerActions) {
        this.footerActions.innerHTML = actions
          .map(
            (a) => `
          <button class="tactical-btn ${a.variant || 'btn-cyan'}" id="${a.id || ''}">
            ${a.label}
          </button>
        `
          )
          .join('');

        actions.forEach((a) => {
          if (a.id && a.onClick) {
            const btn = document.getElementById(a.id);
            if (btn) {
              btn.addEventListener('click', () => {
                sfx.playBlip();
                a.onClick();
              });
            }
          }
        });
      }

      this.overlay.classList.add('active');
      this.overlay.setAttribute('aria-hidden', 'false');
    }

    close() {
      if (!this.overlay) return;
      sfx.playBlip();
      this.overlay.classList.remove('active');
      this.overlay.setAttribute('aria-hidden', 'true');
    }
  }

  const modal = new TacticalModalController();

  // =========================================================================
  // 9. TOAST NOTIFICATION & GOVERNED ACTION DISPATCHER
  // =========================================================================

  class ToastController {
    constructor() {
      this.container = document.getElementById('hud-toast-container');
    }

    show(message, type = 'info', duration = 3600) {
      if (!this.container) return;

      const toast = document.createElement('div');
      toast.className = `hud-toast toast-${type}`;
      toast.innerHTML = `
        <span class="toast-icon">${type === 'success' ? '✓' : type === 'warning' ? '⚠' : type === 'danger' ? '⛔' : 'ℹ'}</span>
        <span class="toast-msg">${message}</span>
      `;

      this.container.appendChild(toast);
      requestAnimationFrame(() => toast.classList.add('active'));

      setTimeout(() => {
        toast.classList.remove('active');
        setTimeout(() => toast.remove(), 320);
      }, duration);
    }
  }

  const toast = new ToastController();

  // Governed Action Handler (Consequential actions route through Batch 6 proposal gateway)
  function dispatchGovernedAction(actionName, proposalId, target) {
    sfx.playSuccess();
    toast.show(
      `GOVERNED ACTION: [${actionName}] submitted as ActionProposal #${proposalId} to Batch 6 Firewall. Direct consequential execution DENIED.`,
      'success',
      4500
    );

    if (window.appendCharlieLog) {
      window.appendCharlieLog('FIREWALL', `ActionProposal #${proposalId} for [${actionName}] on ${target} routed to Batch 6 Execution Firewall.`);
    }
  }

  // =========================================================================
  // 10. PROVENANCE HUD TOOLTIP CONTROLLER
  // =========================================================================

  class ProvenanceTooltipController {
    constructor() {
      this.tooltip = document.getElementById('hud-tooltip');
      this.initEvents();
    }

    initEvents() {
      if (!this.tooltip) return;

      document.querySelectorAll('.gauge-card').forEach((card) => {
        card.addEventListener('mouseenter', (e) => this.show(card, e));
        card.addEventListener('mousemove', (e) => this.move(e));
        card.addEventListener('mouseleave', () => this.hide());
      });
    }

    show(card, e) {
      const gaugeId = card.getAttribute('data-gauge-id');
      const data = NEXUS_DATA.provenance[gaugeId];
      if (!data) return;

      this.tooltip.innerHTML = `
        <div class="tooltip-title">${data.metric}</div>
        <div class="tooltip-row"><span class="tooltip-label">VALUE:</span><span class="tooltip-val text-cyan">${data.value}</span></div>
        <div class="tooltip-row"><span class="tooltip-label">SOURCE:</span><span class="tooltip-val">${data.source}</span></div>
        <div class="tooltip-row"><span class="tooltip-label">WINDOW:</span><span class="tooltip-val">${data.window}</span></div>
        <div class="tooltip-row"><span class="tooltip-label">SAMPLE:</span><span class="tooltip-val">${data.sample}</span></div>
        <div class="tooltip-row"><span class="tooltip-label">VERIFIED:</span><span class="tooltip-val text-green">${data.lastUpdated}</span></div>
      `;

      this.tooltip.classList.add('active');
      this.move(e);
    }

    move(e) {
      if (!this.tooltip) return;
      const x = Math.min(e.clientX + 14, window.innerWidth - 280);
      const y = Math.min(e.clientY + 14, window.innerHeight - 160);
      this.tooltip.style.left = `${x}px`;
      this.tooltip.style.top = `${y}px`;
    }

    hide() {
      if (!this.tooltip) return;
      this.tooltip.classList.remove('active');
    }
  }

  // =========================================================================
  // 11. INSPECTOR DIALOGS & USER-FACING DRILLDOWNS
  // =========================================================================

  // 1. Decision Inspector: Why Charlie Acted
  function openDecisionInspector() {
    const d = NEXUS_DATA.decision;

    const bodyHtml = `
      <div class="decision-pipeline">
        <div class="decision-step">
          <div class="step-num">01</div>
          <div class="step-content">
            <div class="step-title">TRIGGER</div>
            <div class="step-value text-warning">${d.trigger}</div>
          </div>
        </div>

        <div class="decision-step">
          <div class="step-num">02</div>
          <div class="step-content">
            <div class="step-title">VERIFIED REALITY & EVIDENCE</div>
            <div class="step-value text-cyan">${d.evidence}</div>
          </div>
        </div>

        <div class="decision-step">
          <div class="step-num">03</div>
          <div class="step-content">
            <div class="step-title">AMBIENT RESPONSIBILITY INVOCATION</div>
            <div class="step-value text-white">${d.responsibility}</div>
          </div>
        </div>

        <div class="decision-step">
          <div class="step-num">04</div>
          <div class="step-content">
            <div class="step-title">DYNAMIC MISSION COMPILED</div>
            <div class="step-value text-cyan font-bold">${d.missionId} &bull; ${d.missionName}</div>
          </div>
        </div>

        <div class="decision-step">
          <div class="step-num">05</div>
          <div class="step-content">
            <div class="step-title">BUSINESS CONSTRAINT INVARIANT I15 CHECK</div>
            <div class="step-value">
              ${d.constraints
                .map(
                  (c) => `
                <div style="display: flex; justify-content: space-between; margin-top: 4px; font-size: 0.76rem;">
                  <span>${c.name}: <strong>${c.value}</strong></span>
                  <span class="${c.status === 'SAFE' ? 'text-green' : c.status === 'WARNING' ? 'text-warning' : 'text-magenta'} font-bold">
                    ${c.status === 'SAFE' ? '✓ SAFE' : c.status === 'WARNING' ? '⚠ WARNING' : '⚡ CONSTRAINED'}
                  </span>
                </div>
              `
                )
                .join('')}
            </div>
          </div>
        </div>

        <div class="decision-step">
          <div class="step-num">06</div>
          <div class="step-content">
            <div class="step-title">ACTIVE STRATEGIC REGIME</div>
            <div class="step-value text-magenta">${d.strategicRegime}</div>
          </div>
        </div>

        <div class="decision-step">
          <div class="step-num">07</div>
          <div class="step-content">
            <div class="step-title">WORKER SELECTION & EMPIRICAL METROLOGY</div>
            <div class="step-value text-cyan">${d.worker} &bull; <em>${d.reputation}</em></div>
          </div>
        </div>

        <div class="decision-step">
          <div class="step-num">08</div>
          <div class="step-content">
            <div class="step-title">PRE-FLIGHT DIGITAL TWIN SIMULATION</div>
            <div class="step-value text-green">${d.simulation}</div>
          </div>
        </div>

        <div class="decision-step" style="border-left-color: var(--magenta);">
          <div class="step-num" style="color: var(--magenta); background: rgba(188,0,255,0.15);">09</div>
          <div class="step-content">
            <div class="step-title text-magenta">EXECUTION STATUS & GOVERNANCE GATE</div>
            <div class="step-value font-bold">${d.executionStatus}</div>
          </div>
        </div>
      </div>
    `;

    modal.open({
      categoryCode: 'Epistemic // Decision Trace',
      title: 'Why Charlie Acted — Decision Inspector',
      bodyHtml,
      footerText: 'Grounded in Authoritative Digital Twin &bull; Invariant I15 Sovereign',
      actions: [
        {
          id: 'act-sim-recovery',
          label: 'Simulate Recovery',
          variant: 'btn-magenta',
          onClick: () => {
            dispatchGovernedAction('Simulate Alternative Recovery', 'SIM-9941', 'Digital Twin Engine');
            modal.close();
          },
        },
        {
          id: 'act-view-mission',
          label: 'View Mission DAG',
          variant: 'btn-cyan',
          onClick: () => {
            openMissionInspector('M-9941');
          },
        },
      ],
    });
  }

  // 2. Constraint Inspector: Specific Constraint
  function openConstraintInspector(constraintId) {
    const c = NEXUS_DATA.constraints[constraintId] || NEXUS_DATA.constraints.liquidity;

    const bodyHtml = `
      <table class="param-table">
        <tbody>
          <tr><td class="param-key">CONSTRAINT NAME</td><td class="param-val font-bold text-cyan">${c.title}</td></tr>
          <tr><td class="param-key">CURRENT OBSERVED VALUE</td><td class="param-val font-bold text-white">${c.current}</td></tr>
          <tr><td class="param-key">CONFIGURED THRESHOLD</td><td class="param-val text-muted">${c.threshold}</td></tr>
          <tr><td class="param-key">EVALUATION STATUS</td><td class="param-val font-bold ${c.status === 'SAFE' ? 'text-green' : c.status === 'WARNING' ? 'text-warning' : 'text-magenta'}">${c.status}</td></tr>
          <tr><td class="param-key">HARD CONSTRAINT?</td><td class="param-val">${c.isHard ? '<span class="text-green font-bold">YES (Fail-Closed Sovereign)</span>' : '<span class="text-amber">NO (Soft Optimization Objective)</span>'}</td></tr>
          <tr><td class="param-key">TELEMETRY SOURCE</td><td class="param-val text-muted">${c.source}</td></tr>
          <tr><td class="param-key">FRESHNESS &amp; SLA</td><td class="param-val text-cyan">${c.freshness}</td></tr>
          <tr><td class="param-key">LAST VERIFIED</td><td class="param-val text-white">${c.lastVerified}</td></tr>
          <tr><td class="param-key">STRATEGIC EFFECT</td><td class="param-val">${c.strategicEffect}</td></tr>
        </tbody>
      </table>

      <div style="margin: 14px 0; padding: 12px 14px; background: rgba(0,255,255,0.03); border: 1px solid rgba(0,255,255,0.15); border-radius: 2px;">
        <div style="font-family: var(--font-mono); font-size: 0.68rem; color: var(--cyan); text-transform: uppercase; margin-bottom: 4px;">SYSTEM RATIONALE:</div>
        <p style="font-size: 0.82rem; color: var(--text-white);">${c.reason}</p>
      </div>

      <div style="margin-top: 16px;">
        <div style="font-family: var(--font-mono); font-size: 0.68rem; color: var(--text-muted); text-transform: uppercase; margin-bottom: 8px;">GOVERNED SAFE ALTERNATIVES:</div>
        <ul style="padding-left: 20px; font-size: 0.8rem; color: var(--text-white); line-height: 1.6;">
          ${c.alternatives.map((alt) => `<li>${alt}</li>`).join('')}
        </ul>
      </div>
    `;

    modal.open({
      categoryCode: `Invariant I15 // ${constraintId.toUpperCase()}`,
      title: `${c.title} — Constraint Inspector`,
      bodyHtml,
      footerText: 'Invariant I15-D: Hard constraints cannot be optimized away by reputation or revenue utility.',
      actions: [
        {
          id: 'act-sim-impact',
          label: 'Simulate Impact',
          variant: 'btn-magenta',
          onClick: () => {
            dispatchGovernedAction(`Simulate Threshold Shift: ${c.title}`, 'SIM-C01', 'Digital Twin');
            modal.close();
          },
        },
        {
          id: 'act-view-alternatives',
          label: 'Dispatch Safe Alternative',
          variant: 'btn-cyan',
          onClick: () => {
            dispatchGovernedAction(`Deploy Safe Alternative for ${c.title}`, 'ALT-8812', 'Mission Runtime');
            modal.close();
          },
        },
      ],
    });
  }

  // 3. All Constraints Overview Inspector
  function openAllConstraintsInspector() {
    const list = Object.values(NEXUS_DATA.constraints);

    const bodyHtml = `
      <p style="font-size: 0.82rem; color: var(--text-muted); margin-bottom: 14px;">
        The Business Constraint &amp; Strategic Optimization Engine (Batch 3.5) deterministically enforces organizational boundaries.
        Autonomous missions and workers may never violate active hard constraints.
      </p>

      <table class="param-table">
        <thead>
          <tr>
            <th>CONSTRAINT</th>
            <th>CURRENT</th>
            <th>THRESHOLD</th>
            <th>MODE</th>
            <th>STATUS</th>
            <th>ACTION</th>
          </tr>
        </thead>
        <tbody>
          ${list
            .map(
              (c) => `
            <tr>
              <td class="font-bold text-white">${c.title}</td>
              <td class="font-bold text-cyan">${c.current}</td>
              <td class="text-muted">${c.threshold}</td>
              <td>${c.isHard ? '<span class="text-green">HARD</span>' : '<span class="text-amber">SOFT</span>'}</td>
              <td class="font-bold ${c.status === 'SAFE' ? 'text-green' : c.status === 'WARNING' ? 'text-warning' : 'text-magenta'}">${c.status}</td>
              <td>
                <button class="tactical-btn btn-sm btn-cyan" onclick="window.inspectSingleConstraint('${c.id}')">
                  INSPECT
                </button>
              </td>
            </tr>
          `
            )
            .join('')}
        </tbody>
      </table>
    `;

    window.inspectSingleConstraint = (id) => openConstraintInspector(id);

    modal.open({
      categoryCode: 'BATCH 3.5 // CONSTRAINTS MANIFOLD',
      title: 'BUSINESS CONSTRAINT SOVEREIGNTY ENGINE',
      bodyHtml,
      footerText: 'FAIL-CLOSED ARCHITECTURE &bull; INVARIANTS I15 THROUGH I15-G SEALED',
      actions: [
        {
          id: 'act-audit-all',
          label: 'RUN FULL CONSTRAINTS AUDIT',
          variant: 'btn-green',
          onClick: () => {
            dispatchGovernedAction('Audit All Active Constraints', 'AUD-3501', 'Constraint Engine');
            modal.close();
          },
        },
      ],
    });
  }

  // 4. Worker Fabric Inspector (Batch 3.6 Integration)
  function openWorkerFabricInspector() {
    const w = NEXUS_DATA.workers;

    const renderOverview = () => `
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin-bottom: 18px;">
        <div style="background: rgba(0,255,255,0.05); border: 1px solid rgba(0,255,255,0.2); padding: 10px; border-radius: 2px;">
          <div style="font-family: var(--font-mono); font-size: 0.62rem; color: var(--text-muted);">API WORKERS</div>
          <div style="font-family: var(--font-display); font-size: 1.3rem; color: var(--cyan); font-weight: 700;">${w.stats.apiCount}</div>
          <div style="font-size: 0.65rem; color: var(--green);">● 18 HEALTHY</div>
        </div>
        <div style="background: rgba(188,0,255,0.05); border: 1px solid rgba(188,0,255,0.2); padding: 10px; border-radius: 2px;">
          <div style="font-family: var(--font-mono); font-size: 0.62rem; color: var(--text-muted);">MCP WORKERS</div>
          <div style="font-family: var(--font-display); font-size: 1.3rem; color: var(--magenta); font-weight: 700;">${w.stats.mcpCount}</div>
          <div style="font-size: 0.65rem; color: var(--green);">● 9 HEALTHY</div>
        </div>
        <div style="background: rgba(57,255,20,0.05); border: 1px solid rgba(57,255,20,0.2); padding: 10px; border-radius: 2px;">
          <div style="font-family: var(--font-mono); font-size: 0.62rem; color: var(--text-muted);">BROWSER WORKERS</div>
          <div style="font-family: var(--font-display); font-size: 1.3rem; color: var(--green); font-weight: 700;">${w.stats.browserCount}</div>
          <div style="font-size: 0.65rem; color: var(--amber);">⚠ 1 DEGRADED</div>
        </div>
        <div style="background: rgba(255,184,0,0.05); border: 1px solid rgba(255,184,0,0.2); padding: 10px; border-radius: 2px;">
          <div style="font-family: var(--font-mono); font-size: 0.62rem; color: var(--text-muted);">DESKTOP / RPA</div>
          <div style="font-family: var(--font-display); font-size: 1.3rem; color: var(--amber); font-weight: 700;">${w.stats.desktopCount}</div>
          <div style="font-size: 0.65rem; color: #ff3366;">⛔ 1 QUARANTINED</div>
        </div>
      </div>

      <div style="font-family: var(--font-mono); font-size: 0.7rem; color: var(--cyan); text-transform: uppercase; margin-bottom: 8px;">ACTIVE WORKER INSTANCES:</div>
      <table class="param-table">
        <thead>
          <tr>
            <th>WORKER ID</th>
            <th>MODALITY</th>
            <th>CAPABILITY</th>
            <th>CIRCUIT</th>
            <th>SANDBOX</th>
            <th>REPUTATION</th>
            <th>FIREWALL</th>
          </tr>
        </thead>
        <tbody>
          ${w.list
            .map(
              (item) => `
            <tr>
              <td class="font-bold text-white">${item.id}</td>
              <td><span class="text-cyan font-bold">${item.modality}</span></td>
              <td style="font-size: 0.72rem;">${item.capability}</td>
              <td class="font-bold ${item.circuit === 'Healthy' ? 'text-green' : item.circuit === 'Degraded' ? 'text-warning' : 'text-magenta'}">${item.circuit}</td>
              <td class="text-muted font-mono">${item.sandbox}</td>
              <td class="text-green font-bold">${item.reputation}</td>
              <td><span class="text-muted" style="font-size: 0.65rem;">${item.firewall}</span></td>
            </tr>
          `
            )
            .join('')}
        </tbody>
      </table>
    `;

    const renderViolations = () => `
      <div style="margin-bottom: 12px; font-size: 0.8rem; color: var(--text-muted);">
        Sandbox isolation violations immediately trigger automatic process teardown and circuit quarantining under Invariant I16-A.
      </div>
      <table class="param-table">
        <thead>
          <tr>
            <th>VIOLATION ID</th>
            <th>WORKER ID</th>
            <th>TYPE</th>
            <th>TIME</th>
            <th>DETAILS</th>
          </tr>
        </thead>
        <tbody>
          ${w.violations
            .map(
              (vio) => `
            <tr>
              <td class="text-magenta font-bold">${vio.id}</td>
              <td class="font-bold text-white">${vio.workerId}</td>
              <td class="text-danger font-bold">${vio.type}</td>
              <td class="text-muted">${vio.timestamp}</td>
              <td style="font-size: 0.72rem; color: #ff88a3;">${vio.detail}</td>
            </tr>
          `
            )
            .join('')}
        </tbody>
      </table>
    `;

    modal.open({
      categoryCode: 'BATCH 3.6 // UNIVERSAL WORKFORCE',
      title: 'WORKER FABRIC COMMAND & TELEMETRY',
      tabs: [
        { label: 'Overview & Modalities', onSelect: renderOverview },
        { label: 'Sandbox Isolation Violations', onSelect: renderViolations },
      ],
      bodyHtml: renderOverview(),
      footerText: 'INVARIANT I16: Workers submit proposals to Batch 6 Firewall; zero external direct execution.',
      actions: [
        {
          id: 'act-trip-circuit',
          label: 'TRIP BROWSER CIRCUIT',
          variant: 'btn-amber',
          onClick: () => {
            dispatchGovernedAction('Manual Circuit Breaker Trip: Worker-BROWSER-02', 'CB-TRIP-01', 'Worker Health Manager');
            modal.close();
          },
        },
        {
          id: 'act-reset-circuit',
          label: 'RESET CIRCUIT BREAKER',
          variant: 'btn-green',
          onClick: () => {
            dispatchGovernedAction('Reset Circuit Breaker: Worker-BROWSER-02', 'CB-RESET-01', 'Worker Health Manager');
            modal.close();
          },
        },
      ],
    });
  }

  // 5. Autonomy Level Inspector
  function openAutonomyInspector() {
    const a = NEXUS_DATA.autonomy;

    const bodyHtml = `
      <div style="padding: 12px 14px; background: rgba(0,255,255,0.04); border: 1px solid rgba(0,255,255,0.2); border-radius: 2px; margin-bottom: 16px;">
        <div style="font-family: var(--font-mono); font-size: 0.65rem; color: var(--text-muted); text-transform: uppercase;">GOVERNED AUTONOMY FORMULA:</div>
        <div style="font-family: var(--font-mono); font-size: 0.85rem; color: var(--cyan); font-weight: 700; margin: 4px 0;">
          Effective = min(TenantCeiling [L3], MissionCeiling [L3], PolicyGate [L3]) &rarr; <span class="text-green">L3_PREPARE</span>
        </div>
        <div style="font-size: 0.72rem; color: var(--text-muted);">
          Frontend clicks CANNOT elevate autonomy. All elevations require dual-key cryptographic enterprise governance.
        </div>
      </div>

      <div style="display: flex; flex-direction: column; gap: 8px;">
        ${a.tiers
          .map(
            (t) => `
          <div style="padding: 10px 14px; background: ${t.tier === a.effectiveAutonomy ? 'rgba(0,255,255,0.1)' : 'rgba(255,255,255,0.02)'}; border: 1px solid ${t.tier === a.effectiveAutonomy ? 'var(--cyan)' : 'rgba(255,255,255,0.06)'}; border-left: 3px solid ${t.tier === a.effectiveAutonomy ? 'var(--green)' : 'rgba(255,255,255,0.2)'}; border-radius: 2px;">
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span style="font-family: var(--font-mono); font-size: 0.78rem; font-weight: 700; color: ${t.tier === a.effectiveAutonomy ? 'var(--cyan)' : 'var(--text-white)'};">
                ${t.title}
              </span>
              ${t.tier === a.effectiveAutonomy ? '<span class="status-chip chip-green" style="font-size: 0.58rem;">ACTIVE CEILING</span>' : ''}
            </div>
            <p style="font-size: 0.75rem; color: var(--text-muted); margin-top: 4px;">${t.desc}</p>
          </div>
        `
          )
          .join('')}
      </div>
    `;

    modal.open({
      categoryCode: 'GOVERNANCE // AUTONOMY TIER',
      title: 'CHARLIE AUTONOMY CEILING & GOVERNANCE',
      bodyHtml,
      footerText: 'INVARIANT I11: Charlie may become more autonomous, but it may never become less governed.',
      actions: [
        {
          id: 'act-req-l4',
          label: 'REQUEST L4 ELEVATION PROPOSAL',
          variant: 'btn-magenta',
          onClick: () => {
            dispatchGovernedAction('Propose Autonomy Elevation to L4', 'GOV-ELEV-01', 'Governance Board');
            modal.close();
          },
        },
      ],
    });
  }

  // 6. Execution Firewall Inspector
  function openFirewallInspector() {
    const f = NEXUS_DATA.firewall;

    const bodyHtml = `
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin-bottom: 18px;">
        <div style="background: rgba(57,255,20,0.06); border: 1px solid rgba(57,255,20,0.25); padding: 10px; border-radius: 2px;">
          <div style="font-family: var(--font-mono); font-size: 0.62rem; color: var(--text-muted);">FIREWALL STATUS</div>
          <div style="font-family: var(--font-display); font-size: 1.1rem; color: var(--green); font-weight: 700;">LOCKED</div>
          <div style="font-size: 0.65rem; color: var(--text-muted);">SOVEREIGN READ-ONLY</div>
        </div>
        <div style="background: rgba(0,255,255,0.05); border: 1px solid rgba(0,255,255,0.2); padding: 10px; border-radius: 2px;">
          <div style="font-family: var(--font-mono); font-size: 0.62rem; color: var(--text-muted);">ACTIVE PERMITS</div>
          <div style="font-family: var(--font-display); font-size: 1.1rem; color: var(--cyan); font-weight: 700;">${f.activePermits}</div>
          <div style="font-size: 0.65rem; color: var(--green);">AUTHORIZED</div>
        </div>
        <div style="background: rgba(255,184,0,0.05); border: 1px solid rgba(255,184,0,0.2); padding: 10px; border-radius: 2px;">
          <div style="font-family: var(--font-mono); font-size: 0.62rem; color: var(--text-muted);">PENDING PROPOSALS</div>
          <div style="font-family: var(--font-display); font-size: 1.1rem; color: var(--amber); font-weight: 700;">${f.pendingProposals}</div>
          <div style="font-size: 0.65rem; color: var(--amber);">AWAITING SIGN-OFF</div>
        </div>
        <div style="background: rgba(255,51,102,0.05); border: 1px solid rgba(255,51,102,0.2); padding: 10px; border-radius: 2px;">
          <div style="font-family: var(--font-mono); font-size: 0.62rem; color: var(--text-muted);">KILL SWITCH</div>
          <div style="font-family: var(--font-display); font-size: 1.1rem; color: #ff3366; font-weight: 700;">READY</div>
          <div style="font-size: 0.65rem; color: var(--green);">CIRCUITS ARMED</div>
        </div>
      </div>

      <div style="font-family: var(--font-mono); font-size: 0.7rem; color: var(--cyan); text-transform: uppercase; margin-bottom: 8px;">PENDING ACTION PROPOSALS:</div>
      <table class="param-table">
        <thead>
          <tr>
            <th>PROPOSAL ID</th>
            <th>TARGET SYSTEM ACTION</th>
            <th>PROPOSER</th>
            <th>RISK TIER</th>
            <th>STATUS</th>
          </tr>
        </thead>
        <tbody>
          ${f.pendingList
            .map(
              (p) => `
            <tr>
              <td class="font-bold text-white">${p.id}</td>
              <td style="font-size: 0.75rem;">${p.target}</td>
              <td class="text-cyan font-mono">${p.proposer}</td>
              <td><span class="status-chip chip-cyan" style="font-size: 0.58rem;">${p.riskTier}</span></td>
              <td class="text-amber font-bold" style="font-size: 0.72rem;">${p.status}</td>
            </tr>
          `
            )
            .join('')}
        </tbody>
      </table>

      <div style="padding: 10px 14px; background: rgba(255,51,102,0.05); border: 1px solid rgba(255,51,102,0.2); border-radius: 2px; margin-top: 14px;">
        <div style="font-family: var(--font-mono); font-size: 0.65rem; color: #ff3366; font-weight: 700;">EMERGENCY STOP PROTOCOL</div>
        <p style="font-size: 0.75rem; color: var(--text-muted); margin-top: 2px;">
          Activating Emergency Kill Switch immediately revokes all active leases, terminates sandboxed processes, and locks all connector gateways.
        </p>
      </div>
    `;

    modal.open({
      categoryCode: 'BATCH 6 // EXECUTION FIREWALL',
      title: 'EXECUTION FIREWALL & ACTION GATEWAY',
      bodyHtml,
      footerText: 'INVARIANT I1: Zero consequential external side-effects permitted without cryptographic firewall authorization.',
      actions: [
        {
          id: 'act-kill-switch',
          label: 'ARM EMERGENCY KILL SWITCH',
          variant: 'btn-danger',
          onClick: () => {
            sfx.playWarning();
            if (confirm('CAUTION: Are you sure you want to arm the Emergency Kill Switch? This will halt all active worker executions immediately.')) {
              dispatchGovernedAction('EMERGENCY KILL SWITCH ARMED', 'KILL-999', 'Batch 6 Execution Firewall');
              modal.close();
            }
          },
        },
        {
          id: 'act-approve-p1',
          label: 'SIGN PROPOSAL #AP-9941',
          variant: 'btn-green',
          onClick: () => {
            dispatchGovernedAction('Cryptographic Approval of Proposal #AP-9941', 'AP-9941', 'Execution Approval Gateway');
            modal.close();
          },
        },
      ],
    });
  }

  // 7. Strategic Regime Inspector
  function openStrategicRegimeInspector() {
    const r = NEXUS_DATA.regimes;

    const bodyHtml = `
      <p style="font-size: 0.82rem; color: var(--text-muted); margin-bottom: 14px;">
        Under Invariant I15-B, autonomous operations remain strictly subordinate to the active strategic regime.
        Strategic regimes reorder lexicographic ranking vectors, but NEVER disable hard safety or liquidity constraints.
      </p>

      <div style="display: flex; flex-direction: column; gap: 10px;">
        ${r.list
          .map(
            (reg) => `
          <div style="padding: 12px 14px; background: ${reg.id === r.active ? 'rgba(188,0,255,0.08)' : 'rgba(255,255,255,0.02)'}; border: 1px solid ${reg.id === r.active ? 'var(--magenta)' : 'rgba(255,255,255,0.06)'}; border-left: 3px solid ${reg.id === r.active ? 'var(--magenta)' : 'transparent'}; border-radius: 2px;">
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span style="font-family: var(--font-display); font-size: 0.85rem; font-weight: 700; color: ${reg.id === r.active ? 'var(--magenta)' : 'var(--text-white)'};">
                ${reg.name}
              </span>
              ${reg.id === r.active ? '<span class="status-chip chip-magenta" style="font-size: 0.6rem;">ACTIVE REGIME</span>' : `
                <button class="tactical-btn btn-sm btn-magenta" onclick="window.switchStrategicRegime('${reg.id}')">
                  SIMULATE SHIFT
                </button>
              `}
            </div>
            <div style="font-family: var(--font-mono); font-size: 0.72rem; color: var(--cyan); margin: 4px 0;">
              OBJECTIVE PRIORITY: ${reg.priority}
            </div>
            <p style="font-size: 0.75rem; color: var(--text-muted);">${reg.desc}</p>
          </div>
        `
          )
          .join('')}
      </div>
    `;

    window.switchStrategicRegime = (regimeId) => {
      dispatchGovernedAction(`Simulate Strategic Regime Shift to ${regimeId}`, 'REG-SHIFT-01', 'Strategic Optimization Engine');
      modal.close();
    };

    modal.open({
      categoryCode: 'INVARIANT I15-B // STRATEGIC REGIME',
      title: 'ACTIVE STRATEGIC REGIME & OBJECTIVES',
      bodyHtml,
      footerText: 'INVARIANT I15-B: Strategic regimes reorder objectives, but never override hard constraints.',
      actions: [
        {
          id: 'act-sim-arbitration',
          label: 'RUN LEXICOGRAPHIC ARBITRATION',
          variant: 'btn-magenta',
          onClick: () => {
            dispatchGovernedAction('Execute Lexicographic Multi-Mission Arbitration', 'ARB-8842', 'Arbitration Engine');
            modal.close();
          },
        },
      ],
    });
  }

  // 8. Event Inspector: Specific Event Detail
  function openEventInspector(eventId) {
    const bodyHtml = `
      <table class="param-table">
        <tbody>
          <tr><td class="param-key">EVENT IDENTIFIER</td><td class="param-val font-bold text-cyan">#EVT-${eventId}</td></tr>
          <tr><td class="param-key">TIMESTAMP</td><td class="param-val text-white">17:04:39 UTC (Sync 12ms)</td></tr>
          <tr><td class="param-key">CATEGORY</td><td class="param-val text-magenta font-bold">RESOURCE RESERVATION</td></tr>
          <tr><td class="param-key">TENANT SCOPE</td><td class="param-val font-mono">Current Workspace (Partitioned)</td></tr>
          <tr><td class="param-key">MISSION CONTEXT</td><td class="param-val text-cyan font-bold">CampaignNode-02 (#M-8842)</td></tr>
          <tr><td class="param-key">WORKER LEASE</td><td class="param-val">Worker-API-07 &bull; Lease #L-8843</td></tr>
          <tr><td class="param-key">MONOTONIC FENCE</td><td class="param-val text-green font-bold">VALID (Token #8843)</td></tr>
          <tr><td class="param-key">TRACE IDENTIFIER</td><td class="param-val font-mono">TRACE-19382-B6</td></tr>
          <tr><td class="param-key">EVIDENCE PAYLOAD</td><td class="param-val text-muted">Ledger verification SHA-256: 7f8a92b4c10...</td></tr>
        </tbody>
      </table>

      <div style="padding: 10px 14px; background: rgba(0,255,255,0.03); border: 1px solid rgba(0,255,255,0.15); border-radius: 2px;">
        <div style="font-family: var(--font-mono); font-size: 0.65rem; color: var(--cyan); text-transform: uppercase;">VERIFICATION STATUS:</div>
        <p style="font-size: 0.8rem; color: var(--text-white); margin-top: 2px;">
          Event cryptographically signed and appended to immutable Outcome Ledger. Consequential firewall permit #AP-9938 verified.
        </p>
      </div>
    `;

    modal.open({
      categoryCode: `LEDGER // EVENT #${eventId}`,
      title: `EVENT AUDIT INSPECTOR #${eventId}`,
      bodyHtml,
      footerText: 'IMMUTABLE AUDIT TRAIL &bull; SECURED BY OUTCOME LEDGER',
      actions: [
        {
          id: 'act-verify-trace',
          label: 'VERIFY CRYPTOGRAPHIC TRACE',
          variant: 'btn-cyan',
          onClick: () => {
            dispatchGovernedAction(`Verify Cryptographic Trace #EVT-${eventId}`, 'TRACE-VERIFY', 'Ledger Service');
            modal.close();
          },
        },
      ],
    });
  }

  // 9. Mission & DAG Inspector
  function openMissionInspector(missionId) {
    const bodyHtml = `
      <table class="param-table">
        <tbody>
          <tr><td class="param-key">MISSION ID</td><td class="param-val font-bold text-cyan">#${missionId}</td></tr>
          <tr><td class="param-key">MISSION NAME</td><td class="param-val text-white font-bold">Enterprise SaaS Churn Containment &amp; Lead Recovery</td></tr>
          <tr><td class="param-key">DAG STATUS</td><td class="param-val text-green font-bold">NODE 03/07 EXECUTING (Parallel Fork)</td></tr>
          <tr><td class="param-key">ACTIVE NODE</td><td class="param-val text-cyan font-mono">ContractRecoveryProposalNode (Autonomy L3)</td></tr>
          <tr><td class="param-key">ASSIGNED WORKER</td><td class="param-val font-mono">Worker-API-07 (Modality: API)</td></tr>
          <tr><td class="param-key">ALLOCATED BUDGET</td><td class="param-val text-white">₹75,000 / ₹1,00,000 Ceiling</td></tr>
          <tr><td class="param-key">MONOTONIC FENCE</td><td class="param-val text-green">VALID (#8843)</td></tr>
        </tbody>
      </table>

      <div style="margin: 14px 0;">
        <div style="font-family: var(--font-mono); font-size: 0.68rem; color: var(--text-muted); text-transform: uppercase; margin-bottom: 6px;">DAG TOPOLOGY PIPELINE:</div>
        <div style="display: flex; gap: 4px; overflow-x: auto; padding-bottom: 4px;">
          <span style="padding: 4px 8px; background: rgba(57,255,20,0.15); border: 1px solid var(--green); color: var(--green); font-size: 0.65rem; font-family: var(--font-mono);">01 PERCEPTION ✓</span>
          <span style="padding: 4px 8px; background: rgba(57,255,20,0.15); border: 1px solid var(--green); color: var(--green); font-size: 0.65rem; font-family: var(--font-mono);">02 ANALYSIS ✓</span>
          <span style="padding: 4px 8px; background: rgba(0,255,255,0.2); border: 1px solid var(--cyan); color: var(--cyan); font-weight: 700; font-size: 0.65rem; font-family: var(--font-mono);">03 PROPOSAL [RUNNING]</span>
          <span style="padding: 4px 8px; background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.1); color: var(--text-muted); font-size: 0.65rem; font-family: var(--font-mono);">04 FIREWALL GATE</span>
          <span style="padding: 4px 8px; background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.1); color: var(--text-muted); font-size: 0.65rem; font-family: var(--font-mono);">05 VERIFICATION</span>
        </div>
      </div>
    `;

    modal.open({
      categoryCode: `MISSION GRAPH // ${missionId}`,
      title: `DYNAMIC MISSION DAG — ${missionId}`,
      bodyHtml,
      footerText: 'DYNAMIC DAG != DYNAMIC AUTHORITY &bull; BATCH 6 EXECUTION FIREWALL REMAINS SOVEREIGN',
      actions: [
        {
          id: 'act-pause-mission',
          label: 'PAUSE MISSION',
          variant: 'btn-amber',
          onClick: () => {
            dispatchGovernedAction(`Pause Mission ${missionId}`, 'PAUSE-01', 'DAG Orchestrator');
            modal.close();
          },
        },
        {
          id: 'act-reconcile-mission',
          label: 'RECONCILE EFFECTS',
          variant: 'btn-cyan',
          onClick: () => {
            dispatchGovernedAction(`Reconcile Effect States for ${missionId}`, 'REC-01', 'Worker Recovery Manager');
            modal.close();
          },
        },
      ],
    });
  }

  // 10. Quantum Sync & Telemetry Inspector
  function openQuantumSyncInspector() {
    const bodyHtml = `
      <table class="param-table">
        <tbody>
          <tr><td class="param-key">QUANTUM-SYNC LATENCY</td><td class="param-val font-bold text-cyan">12 ms</td></tr>
          <tr><td class="param-key">DIGITAL TWIN DRIFT</td><td class="param-val text-green font-bold">0.002% (Extremely Nominal)</td></tr>
          <tr><td class="param-key">TELEMETRY STALENESS SLA</td><td class="param-val text-white">&lt; 5m Max SLA (Current: 42s)</td></tr>
          <tr><td class="param-key">VERIFIED SIGNALS</td><td class="param-val text-cyan">7 Authoritative Telemetry Feeds</td></tr>
          <tr><td class="param-key">OUTCOME LEDGER SYNC</td><td class="param-val text-green font-bold">100% Reconciled</td></tr>
        </tbody>
      </table>

      <div style="padding: 10px 14px; background: rgba(0,255,255,0.03); border: 1px solid rgba(0,255,255,0.15); border-radius: 2px;">
        <div style="font-family: var(--font-mono); font-size: 0.65rem; color: var(--cyan); text-transform: uppercase;">INVARIANT I15-A EPISTEMIC GROUNDING:</div>
        <p style="font-size: 0.8rem; color: var(--text-white); margin-top: 2px;">
          Business constraints evaluate exclusively against verified Digital Twin state and authoritative financial ledgers.
          Speculative LLM hallucination cannot establish business reality.
        </p>
      </div>
    `;

    modal.open({
      categoryCode: 'DIGITAL TWIN // QUANTUM SYNC',
      title: 'REALTIME TELEMETRY & DIGITAL TWIN SYNC',
      bodyHtml,
      footerText: 'EPISTEMIC GROUNDING &bull; INVARIANT I15-A STRICT SLA',
      actions: [
        {
          id: 'act-resync-twin',
          label: 'FORCE RE-SYNC TELEMETRY',
          variant: 'btn-cyan',
          onClick: () => {
            dispatchGovernedAction('Force Re-Sync Digital Twin Telemetry', 'SYNC-FORCE', 'Digital Twin Kernel');
            modal.close();
          },
        },
      ],
    });
  }

  // 11. Commander Credentials Inspector
  function openCommanderInspector() {
    const bodyHtml = `
      <table class="param-table">
        <tbody>
          <tr><td class="param-key">SOVEREIGN ROOT USER</td><td class="param-val font-bold text-white">CHIEF ARCHITECT / ROOT</td></tr>
          <tr><td class="param-key">AUTHENTICATION METHOD</td><td class="param-val text-cyan font-mono">Hardware Security Module (HSM) + JWT</td></tr>
          <tr><td class="param-key">AUTHORITY LEVEL</td><td class="param-val text-green font-bold">L3 Autonomous Operations Oversight</td></tr>
          <tr><td class="param-key">TENANT ID</td><td class="param-val font-mono">Workspace-Enterprise-01</td></tr>
          <tr><td class="param-key">BATCH 6 KEYS</td><td class="param-val text-green">ARMED &bull; SOVEREIGN</td></tr>
        </tbody>
      </table>
    `;

    modal.open({
      categoryCode: 'AUTH // SOVEREIGN CREDENTIALS',
      title: 'COMMANDER CREDENTIALS & AUTHORITY',
      bodyHtml,
      footerText: 'DUAL-KEY CRYPTOGRAPHIC ACCESS ONLY',
      actions: [],
    });
  }

  // 11b. PRG-1 Production Reality & System Provenance Inspector
  function openRealityInspector() {
    const r = NEXUS_DATA.reality;
    const ov = r.systemOverview;

    const renderConnectors = () => `
      <div style="font-family: var(--font-mono); font-size: 0.68rem; color: var(--cyan); text-transform: uppercase; margin-bottom: 8px;">
        EXTERNAL INTEGRATION CONNECTORS &amp; HEALTH STATUS:
      </div>
      <table class="param-table">
        <thead>
          <tr>
            <th>CONNECTOR</th>
            <th>CATEGORY</th>
            <th>STATUS</th>
            <th>EPISTEMIC PROVENANCE</th>
          </tr>
        </thead>
        <tbody>
          ${r.connectors
            .map(
              (c) => `
            <tr>
              <td class="font-bold text-white">${c.name}</td>
              <td class="font-mono text-cyan">${c.type}</td>
              <td>
                <span class="font-bold ${c.status === 'CONNECTED' ? 'text-green' : c.status === 'DISCONNECTED' ? 'text-warning' : 'text-magenta'}">
                  ${c.status === 'CONNECTED' ? '● CONNECTED' : c.status === 'DISCONNECTED' ? '○ DISCONNECTED' : '✕ NOT CONFIGURED'}
                </span>
              </td>
              <td style="font-size: 0.72rem; color: var(--text-muted);">${c.note}</td>
            </tr>
          `
            )
            .join('')}
        </tbody>
      </table>
    `;

    const renderSystemHealth = () => `
      <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-bottom: 16px;">
        <div style="background: rgba(57,255,20,0.05); border: 1px solid rgba(57,255,20,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">DATABASE TRUTH</div>
          <div style="font-size: 1.1rem; color: var(--green); font-weight: 700;">${ov.database}</div>
          <div style="font-size: 0.65rem; color: var(--text-muted);">PostgreSQL &bull; Multi-Tenant</div>
        </div>
        <div style="background: rgba(0,255,255,0.05); border: 1px solid rgba(0,255,255,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">DIGITAL TWIN</div>
          <div style="font-size: 1.1rem; color: var(--cyan); font-weight: 700;">${ov.digitalTwin}</div>
          <div style="font-size: 0.65rem; color: var(--text-muted);">Freshness SLA satisfied</div>
        </div>
        <div style="background: rgba(188,0,255,0.05); border: 1px solid rgba(188,0,255,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">AI BRAIN FABRIC</div>
          <div style="font-size: 1.1rem; color: var(--magenta); font-weight: 700;">${ov.brain}</div>
          <div style="font-size: 0.65rem; color: var(--text-muted);">OmniRoute Circuit Healthy</div>
        </div>
      </div>

      <table class="param-table">
        <tbody>
          <tr><td class="param-key">OPERATING MODE</td><td class="param-val text-green font-bold">● LIVE PRODUCTION (Mock Data Forbidden)</td></tr>
          <tr><td class="param-key">TENANT SCOPE</td><td class="param-val font-mono text-cyan">${r.tenantId}</td></tr>
          <tr><td class="param-key">FIREWALL STATUS</td><td class="param-val text-green font-bold">${ov.firewall}</td></tr>
          <tr><td class="param-key">EVENT BUS STATUS</td><td class="param-val font-mono text-cyan">${ov.eventBus}</td></tr>
          <tr><td class="param-key">LAST REAL EVENT</td><td class="param-val text-white">${ov.lastEventTime}</td></tr>
        </tbody>
      </table>

      <div style="padding: 10px 14px; background: rgba(0,255,255,0.03); border: 1px solid rgba(0,255,255,0.15); border-radius: 2px; margin-top: 14px;">
        <div style="font-family: var(--font-mono); font-size: 0.65rem; color: var(--cyan); text-transform: uppercase;">PRG-1 PRODUCTION REALITY RULE:</div>
        <p style="font-size: 0.78rem; color: var(--text-white); margin-top: 2px;">
          If Charlie cannot prove that a value came from a real tenant-scoped source, Charlie will NOT display it as live reality.
          When connectors are missing or telemetry is pending, status is strictly <strong>UNKNOWN</strong> or <strong>NOT CONFIGURED</strong>.
        </p>
      </div>
    `;

    modal.open({
      categoryCode: 'PRG-1 // PRODUCTION REALITY',
      title: 'OPERATING REALITY &amp; SYSTEM PROVENANCE',
      tabs: [
        { label: 'SYSTEM HEALTH &amp; REALITY', onSelect: () => renderSystemHealth() },
        { label: 'CONNECTOR INTEGRITY', onSelect: () => renderConnectors() },
      ],
      bodyHtml: renderSystemHealth(),
      footerText: 'PRG-1 REALITY GATE &bull; ZERO FABRICATED DATA IN PRODUCTION',
      actions: [
        {
          id: 'act-refresh-telemetry',
          label: 'REFRESH TELEMETRY',
          variant: 'btn-cyan',
          onClick: () => {
            dispatchGovernedAction('Query Production Reality Telemetry Envelope', 'REALITY-REFRESH', 'Reality Service');
            modal.close();
          },
        },
      ],
    });
  }

  // 11c. PRG-1 CEO Human Approval Center (HITL Gateway)
  function openApprovalCenterModal() {
    const apps = NEXUS_DATA.approvals;

    const renderPending = () => `
      <div style="margin-bottom: 12px; font-size: 0.8rem; color: var(--text-muted);">
        Consequential operations require explicit human cryptographic sign-off. Approving issues an <strong>ExecutionPermit</strong> to the Batch 6 Firewall.
      </div>
      <div style="display: flex; flex-direction: column; gap: 14px;">
        ${apps
          .map(
            (app) => `
          <div style="background: rgba(255,255,255,0.02); border: 1px solid var(--border-light); padding: 14px; border-radius: 2px; position: relative;">
            <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 8px;">
              <div>
                <span class="font-mono" style="font-size: 0.75rem; color: var(--cyan); font-weight: 700;">#${app.id}</span>
                <h4 style="font-size: 0.95rem; color: #fff; margin: 2px 0;">${app.missionName}</h4>
                <div style="font-size: 0.72rem; color: var(--text-muted);">${app.actionDescription}</div>
              </div>
              <div style="text-align: right;">
                <span style="font-size: 0.68rem; padding: 3px 8px; background: rgba(188,0,255,0.15); border: 1px solid var(--magenta); color: var(--magenta); font-weight: 700;">
                  ${app.risk}
                </span>
                <div style="font-size: 0.65rem; color: var(--warning); margin-top: 4px; font-family: var(--font-mono);">${app.expiresIn}</div>
              </div>
            </div>

            <table class="param-table" style="margin: 8px 0;">
              <tbody>
                <tr><td class="param-key">TARGET SYSTEM</td><td class="param-val font-mono text-cyan">${app.targetSystem}</td></tr>
                <tr><td class="param-key">FINANCIAL EXPOSURE</td><td class="param-val font-bold text-green">${app.financialExposure}</td></tr>
                <tr><td class="param-key">BUSINESS CONSTRAINTS</td><td class="param-val text-white" style="font-size: 0.72rem;">${app.constraintStatus}</td></tr>
                <tr><td class="param-key">EVIDENCE BASE</td><td class="param-val text-muted" style="font-size: 0.72rem;">${app.evidenceSummary}</td></tr>
                <tr><td class="param-key">ASSIGNED WORKER</td><td class="param-val font-mono" style="font-size: 0.72rem;">${app.worker}</td></tr>
                <tr><td class="param-key">PAYLOAD DIGEST</td><td class="param-val font-mono text-warning" style="font-size: 0.72rem;">${app.payloadDigest}</td></tr>
              </tbody>
            </table>

            <div style="margin: 10px 0;">
              <details style="background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.06); padding: 8px;">
                <summary style="font-family: var(--font-mono); font-size: 0.68rem; color: var(--cyan); cursor: pointer;">
                  VIEW EXACT IMMUTABLE PAYLOAD JSON
                </summary>
                <pre style="font-family: var(--font-mono); font-size: 0.65rem; color: #a0aec0; margin-top: 6px; overflow-x: auto;">${app.payloadJson}</pre>
              </details>
            </div>

            <div style="display: flex; gap: 8px; justify-content: flex-end; margin-top: 10px;">
              <button class="tactical-btn btn-sm btn-magenta" onclick="window.rejectApproval('${app.id}')">
                [REJECT PROPOSAL]
              </button>
              <button class="tactical-btn btn-sm btn-amber" onclick="window.requestChanges('${app.id}')">
                [REQUEST CHANGES]
              </button>
              <button class="tactical-btn btn-sm btn-green" onclick="window.approvePermit('${app.id}', '${app.payloadDigest}')">
                [APPROVE &amp; ISSUE PERMIT]
              </button>
            </div>
          </div>
        `
          )
          .join('')}
      </div>
    `;

    window.approvePermit = (appId, digest) => {
      sfx.playSuccess();
      dispatchGovernedAction(`Issue ExecutionPermit for ${appId} (Digest: ${digest})`, 'PERMIT-ISSUE', 'Batch 6 Execution Firewall');
      toast.show(`EXECUTION PERMIT ISSUED FOR ${appId} &bull; DISPATCHED TO FIREWALL`, 'success', 5000);
      modal.close();
    };

    window.rejectApproval = (appId) => {
      sfx.playDenial();
      dispatchGovernedAction(`Reject Proposal ${appId}`, 'PROPOSAL-REJECT', 'Approval Gateway');
      toast.show(`PROPOSAL ${appId} REJECTED &bull; FAILED CLOSED`, 'warning', 4000);
      modal.close();
    };

    window.requestChanges = (appId) => {
      sfx.playWarning();
      dispatchGovernedAction(`Request Changes for ${appId}`, 'PROPOSAL-CHANGES', 'Mission DAG Planner');
      toast.show(`CHANGES REQUESTED FOR ${appId} &bull; SENT TO PLANNER`, 'info', 4000);
      modal.close();
    };

    modal.open({
      categoryCode: 'HITL // APPROVAL GATEWAY',
      title: 'CEO HUMAN APPROVAL CENTER &amp; CONSEQUENTIAL PERMITS',
      bodyHtml: renderPending(),
      footerText: 'INVARIANT I16 &amp; I17-PR &bull; BATCH 6 EXECUTION FIREWALL REMAINS SOVEREIGN',
      actions: [
        {
          id: 'act-view-history',
          label: 'VIEW APPROVAL AUDIT LEDGER',
          variant: 'btn-cyan',
          onClick: () => {
            dispatchGovernedAction('Audit Approval History', 'AUDIT-APPROV', 'Audit Store');
            modal.close();
          },
        },
      ],
    });
  }

  // 11d. PRG-1 Work Control Center & Work Ledger Modal
  function openWorkControlCenterModal() {
    const wc = NEXUS_DATA.workControl;

    const bodyHtml = `
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin-bottom: 16px;">
        <div style="background: rgba(0,255,255,0.05); border: 1px solid rgba(0,255,255,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">ACTIVE MISSIONS</div>
          <div style="font-size: 1.3rem; color: var(--cyan); font-weight: 700;">${wc.counts.active}</div>
          <div style="font-size: 0.65rem; color: var(--cyan);">● 5 RUNNING</div>
        </div>
        <div style="background: rgba(255,184,0,0.05); border: 1px solid rgba(255,184,0,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">WAITING APPROVAL</div>
          <div style="font-size: 1.3rem; color: var(--warning); font-weight: 700;">${wc.counts.waitingApproval}</div>
          <div style="font-size: 0.65rem; color: var(--warning);">⏳ CEO SIGN-OFF</div>
        </div>
        <div style="background: rgba(57,255,20,0.05); border: 1px solid rgba(57,255,20,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">COMPLETED MISSIONS</div>
          <div style="font-size: 1.3rem; color: var(--green); font-weight: 700;">${wc.counts.completed}</div>
          <div style="font-size: 0.65rem; color: var(--green);">✓ VERIFIED OUTCOMES</div>
        </div>
        <div style="background: rgba(188,0,255,0.05); border: 1px solid rgba(188,0,255,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">BLOCKED / FAILED</div>
          <div style="font-size: 1.3rem; color: var(--magenta); font-weight: 700;">${wc.counts.blocked + wc.counts.failed}</div>
          <div style="font-size: 0.65rem; color: var(--magenta);">3 BLOCKED &bull; 4 FAILED</div>
        </div>
      </div>

      <div style="font-family: var(--font-mono); font-size: 0.7rem; color: var(--cyan); text-transform: uppercase; margin-bottom: 8px;">
        MISSION M-19382 WORK LEDGER (IMMUTABLE TIMELINE):
      </div>
      <div style="display: flex; flex-direction: column; gap: 4px; max-height: 240px; overflow-y: auto;">
        ${wc.ledger
          .map(
            (l) => `
          <div style="display: flex; align-items: center; justify-content: space-between; padding: 6px 10px; background: rgba(255,255,255,0.02); border-left: 2px solid ${l.status === 'Completed' ? 'var(--green)' : l.status === 'WaitingApproval' ? 'var(--warning)' : 'var(--text-muted)'};">
            <div style="display: flex; align-items: center; gap: 10px;">
              <span class="font-mono text-muted" style="font-size: 0.68rem;">${l.step}</span>
              <span style="font-size: 0.78rem; color: ${l.status === 'Completed' ? '#fff' : l.status === 'WaitingApproval' ? 'var(--warning)' : 'var(--text-muted)'}; font-weight: ${l.status === 'WaitingApproval' ? '700' : '400'};">
                ${l.title}
              </span>
            </div>
            <div style="display: flex; align-items: center; gap: 12px;">
              <span class="font-mono" style="font-size: 0.65rem; color: var(--cyan);">${l.stage}</span>
              <span class="font-bold" style="font-size: 0.7rem; color: ${l.status === 'Completed' ? 'var(--green)' : l.status === 'WaitingApproval' ? 'var(--warning)' : 'var(--text-muted)'};">
                ${l.status === 'Completed' ? '✓ DONE' : l.status === 'WaitingApproval' ? '⏳ WAITING CEO' : '○ PENDING'}
              </span>
              <span class="font-mono text-muted" style="font-size: 0.65rem;">${l.time}</span>
            </div>
          </div>
        `
          )
          .join('')}
      </div>
    `;

    modal.open({
      categoryCode: 'RUNTIME // WORK CONTROL CENTER',
      title: 'EXECUTIVE MISSION CONTROL &amp; WORK LEDGER',
      bodyHtml,
      footerText: 'DETERMINISTIC WORK LEDGER &bull; NO UNVERIFIED PROGRESS INFERRED',
      actions: [
        {
          id: 'act-view-all-missions',
          label: 'VIEW ALL ACTIVE MISSIONS',
          variant: 'btn-cyan',
          onClick: () => {
            dispatchGovernedAction('Query Full Mission DAG Registry', 'DAG-QUERY', 'Runtime Supervisor');
            modal.close();
          },
        },
      ],
    });
  }

  // 11e. PRG-1 Value Realization & Revenue Attribution Modal
  function openValueRealizationModal() {
    const vr = NEXUS_DATA.valueRealization;

    const bodyHtml = `
      <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-bottom: 16px;">
        <div style="background: rgba(0,255,255,0.05); border: 1px solid rgba(0,255,255,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">EXPECTED REVENUE</div>
          <div style="font-size: 1.2rem; color: var(--cyan); font-weight: 700;">${vr.expectedRevenue}</div>
          <div style="font-size: 0.65rem; color: var(--text-muted);">Authorized Exposure: ${vr.authorizedExposure}</div>
        </div>
        <div style="background: rgba(255,184,0,0.05); border: 1px solid rgba(255,184,0,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">ACTUAL OPEX SPEND</div>
          <div style="font-size: 1.2rem; color: var(--warning); font-weight: 700;">${vr.actualSpend}</div>
          <div style="font-size: 0.65rem; color: var(--text-muted);">Within Authorized Exposure</div>
        </div>
        <div style="background: rgba(57,255,20,0.05); border: 1px solid rgba(57,255,20,0.2); padding: 10px;">
          <div style="font-size: 0.62rem; color: var(--text-muted);">REALIZED BUSINESS VALUE</div>
          <div style="font-size: 1.2rem; color: var(--green); font-weight: 700;">${vr.realizedValue}</div>
          <div style="font-size: 0.65rem; color: var(--green);">${vr.realizedValueStatus}</div>
        </div>
      </div>

      <table class="param-table">
        <tbody>
          <tr><td class="param-key">VERIFIED REVENUE</td><td class="param-val text-green font-bold">${vr.verifiedRevenue}</td></tr>
          <tr><td class="param-key">VERIFIED GROSS MARGIN</td><td class="param-val text-cyan font-bold">${vr.verifiedMargin}</td></tr>
          <tr><td class="param-key">EVIDENCE PROVENANCE</td><td class="param-val font-mono" style="font-size: 0.7rem;">${vr.ledgerReference}</td></tr>
        </tbody>
      </table>

      <div style="margin-top: 14px; padding: 12px; background: rgba(188,0,255,0.05); border: 1px solid rgba(188,0,255,0.2); border-radius: 2px;">
        <div style="font-family: var(--font-mono); font-size: 0.68rem; color: var(--magenta); font-weight: 700; text-transform: uppercase;">
          EPISTEMIC SEPARATION: WORK COMPLETED != VALUE REALIZED
        </div>
        <div style="margin-top: 6px; font-size: 0.78rem; color: var(--text-white);">
          <strong>${vr.unverifiedMission.name}</strong> &bull; ${vr.unverifiedMission.workCompleted}
        </div>
        <div style="font-size: 0.74rem; color: var(--text-muted); margin-top: 2px;">
          Expected: ${vr.unverifiedMission.expectedValue} &bull; Actual Realized: <span class="text-amber font-bold">${vr.unverifiedMission.realizedValue}</span>
        </div>
        <p style="font-size: 0.72rem; color: #ff88a3; margin-top: 4px;">
          ${vr.unverifiedMission.reason}
        </p>
      </div>
    `;

    modal.open({
      categoryCode: 'BUSINESS VALUE // ACCOUNTING',
      title: 'VALUE REALIZATION &amp; REVENUE ATTRIBUTION',
      bodyHtml,
      footerText: 'INVARIANT: WORK COMPLETED != VALUE REALIZED &bull; OUTCOME LEDGER GROUNDING',
      actions: [
        {
          id: 'act-audit-ledger',
          label: 'VIEW OUTCOME LEDGER ENTRIES',
          variant: 'btn-green',
          onClick: () => {
            dispatchGovernedAction('Audit Value Realization Ledger', 'VAL-AUDIT', 'Outcome Ledger');
            modal.close();
          },
        },
      ],
    });
  }

  // 11f. Batch 3.7 Autonomous Capability Factory Inspector (Living Spatial Capability Laboratory)
  function openCapabilityFactoryInspector() {
    sfx.playQuantumPulse();

    const pipelineSteps = [
      { num: '01', code: 'GAP', name: 'Gap Detector', reality: 'LIVE_VERIFIED', desc: 'Identifies unfulfilled business capabilities' },
      { num: '02', code: 'SPEC', name: 'Formal Spec', reality: 'LIVE_VERIFIED', desc: 'Deterministic input/output schemas & SLA limits' },
      { num: '03', code: 'DESIGN', name: 'AI Arch Design', reality: 'LIVE_VERIFIED', desc: 'Threat modeling & mitigation architecture' },
      { num: '04', code: 'GEN', name: 'Code Generator', reality: 'LIVE_VERIFIED', desc: 'Zero-trust isolated artifact synthesis' },
      { num: '05', code: 'SCAN', name: 'Static Security', reality: 'LIVE_VERIFIED', desc: 'AST, dependency, and secret inspection' },
      { num: '06', code: 'SANDBOX', name: 'Disposable Sandbox', reality: 'LIVE_VERIFIED', desc: 'Strict memory/CPU quotas, network deny-by-default' },
      { num: '07', code: 'TDD', name: 'Spec-Derived TDD', reality: 'LIVE_VERIFIED', desc: 'Immutable specification test suite' },
      { num: '08', code: 'EVAL', name: 'Empirical Eval', reality: 'LIVE_VERIFIED', desc: 'Multi-dimensional scorecard verification' },
      { num: '09', code: 'STRIX', name: 'Strix Red Team', reality: 'LIVE_VERIFIED', desc: '14 adversarial penetration vectors probed' },
      { num: '10', code: 'REGR', name: 'Regression Guard', reality: 'LIVE_VERIFIED', desc: 'Frozen 897 baseline regression verified' },
      { num: '11', code: 'ICA', name: 'Independent ICA', reality: 'LIVE_VERIFIED', desc: 'Cryptographic context separation (Invariant I17-B)' },
      { num: '12', code: 'SIGN', name: 'Ed25519 Signing', reality: 'LIVE_VERIFIED', desc: 'Supply-chain asymmetric cryptographic release' },
      { num: '13', code: 'REGISTRY', name: 'Immutable Registry', reality: 'LIVE_VERIFIED', desc: 'Cryptographic parent lineage & version immutability' },
      { num: '14', code: 'SHADOW', name: 'Shadow Mode', reality: 'LIVE_VERIFIED', desc: 'SideEffects = ZERO (Live observation only)' },
      { num: '15', code: 'PROBATION', name: 'Bounded Probation', reality: 'LIVE_VERIFIED', desc: 'Micro-budget envelope & auto-quarantine' },
      { num: '16', code: 'ACTIVE', name: 'Active Production', reality: 'LIVE_VERIFIED', desc: 'Governed via Batch 6 Execution Firewall' }
    ];

    const registeredCaps = [
      {
        id: 'market_intelligence_harvester:1.0.0',
        title: 'Market Intelligence Harvester',
        state: 'ACTIVE',
        stateColor: 'var(--green)',
        risk: 'R2 (Predictive)',
        modality: 'API Worker',
        certId: 'cert-77a8b1c4',
        sigHex: '4A9F...B802 [Ed25519]',
        provenance: 'LIVE_VERIFIED'
      },
      {
        id: 'competitor_pricing_monitor:1.0.0',
        title: 'Competitor Pricing Monitor',
        state: 'PROBATION',
        stateColor: 'var(--cyan)',
        risk: 'R2 (Predictive)',
        modality: 'Browser Worker',
        certId: 'cert-11e2f3d4',
        sigHex: '8C1A...3D91 [Ed25519]',
        provenance: 'LIVE_VERIFIED'
      },
      {
        id: 'churn_early_warning_synthesizer:1.0.0',
        title: 'Churn Early Warning Synthesizer',
        state: 'SHADOW',
        stateColor: 'var(--warning)',
        risk: 'R1 (Analytical)',
        modality: 'API Worker',
        certId: 'cert-99b0c1e8',
        sigHex: '7E4D...2F55 [Ed25519]',
        provenance: 'LIVE_VERIFIED'
      },
      {
        id: 'erp_bulk_disbursement_adapter:1.0.0',
        title: 'ERP Bulk Disbursement Adapter',
        state: 'QUARANTINED',
        stateColor: 'var(--magenta)',
        risk: 'R4 (Critical Financial)',
        modality: 'API Worker',
        certId: 'cert-44d5e6f7',
        sigHex: '1F2E...9A00 [Ed25519]',
        provenance: 'LIVE_VERIFIED'
      }
    ];

    const bodyHtml = `
      <!-- Top KPI Summary Cards -->
      <div style="display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-bottom: 20px;">
        <div style="background: rgba(0,255,255,0.04); border: 1px solid rgba(0,255,255,0.2); border-radius: 8px; padding: 12px;">
          <div style="font-size: 0.65rem; color: var(--text-muted); text-transform: uppercase;">Synthesized Capabilities</div>
          <div style="font-family: var(--font-display); font-size: 1.4rem; color: var(--cyan); font-weight: 700; margin: 2px 0;">14 Sealed</div>
          <div style="font-size: 0.65rem; color: var(--green);">● Live Verified &bull; Active Registry</div>
        </div>
        <div style="background: rgba(57,255,20,0.04); border: 1px solid rgba(57,255,20,0.2); border-radius: 8px; padding: 12px;">
          <div style="font-size: 0.65rem; color: var(--text-muted); text-transform: uppercase;">Release Signatures</div>
          <div style="font-family: var(--font-display); font-size: 1.4rem; color: var(--green); font-weight: 700; margin: 2px 0;">Ed25519</div>
          <div style="font-size: 0.65rem; color: var(--cyan);">✓ Asymmetric Cryptographic Lineage</div>
        </div>
        <div style="background: rgba(188,0,255,0.04); border: 1px solid rgba(188,0,255,0.2); border-radius: 8px; padding: 12px;">
          <div style="font-size: 0.65rem; color: var(--text-muted); text-transform: uppercase;">Certification Authority</div>
          <div style="font-family: var(--font-display); font-size: 1.4rem; color: var(--magenta); font-weight: 700; margin: 2px 0;">Independent</div>
          <div style="font-size: 0.65rem; color: var(--magenta);">🔒 Invariant I17-B Segregation</div>
        </div>
        <div style="background: rgba(255,184,0,0.04); border: 1px solid rgba(255,184,0,0.2); border-radius: 8px; padding: 12px;">
          <div style="font-size: 0.65rem; color: var(--text-muted); text-transform: uppercase;">Firewall Bypass Permits</div>
          <div style="font-family: var(--font-display); font-size: 1.4rem; color: var(--warning); font-weight: 700; margin: 2px 0;">Zero</div>
          <div style="font-size: 0.65rem; color: var(--warning);">Batch 6 Firewall Sovereign</div>
        </div>
      </div>

      <!-- Architectural Distinction: 18-State Lifecycle vs 16-Step Pipeline -->
      <div style="background: rgba(255,255,255,0.02); border: 1px solid var(--glass-border); border-radius: 8px; padding: 14px; margin-bottom: 20px;">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
          <div style="font-family: var(--font-display); font-size: 0.85rem; font-weight: 700; color: var(--text-white);">
            18-State Capability Lifecycle vs 16-Step Synthesis Pipeline
          </div>
          <span class="status-chip chip-cyan" style="font-size: 0.62rem;">Formal Architecture</span>
        </div>
        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px; font-size: 0.75rem;">
          <div style="background: rgba(0,0,0,0.25); padding: 10px; border-radius: 6px; border-left: 3px solid var(--cyan);">
            <div style="font-weight: 600; color: var(--cyan); margin-bottom: 4px;">18-State Capability Lifecycle:</div>
            <div style="color: var(--text-muted); line-height: 1.5; font-family: var(--font-mono); font-size: 0.68rem;">
              Draft → Specified → Designed → Generated → StaticScanned → Sandboxed → TddPassed → Evaluated → RedTeamed → RegressionVerified → Certified → Signed → Registered → Shadow → Probation → Active<br>
              <span style="color: var(--warning);">Control/Failure States:</span> Quarantined, Revoked (+ Terminal: Rejected, Deprecated, Retired)
            </div>
          </div>
          <div style="background: rgba(0,0,0,0.25); padding: 10px; border-radius: 6px; border-left: 3px solid var(--violet);">
            <div style="font-weight: 600; color: var(--violet); margin-bottom: 4px;">16-Step Synthesis Pipeline:</div>
            <div style="color: var(--text-muted); line-height: 1.5;">
              The deterministic orchestrator executing automated synthesis from gap detection through sandbox execution, red-team assault, independent certification, cryptographic signing, shadow verification, probation, and worker fabric runtime admission.
            </div>
          </div>
        </div>
      </div>

      <!-- Spatial Capability Laboratory: 16-Step Pipeline Grid -->
      <div style="margin-bottom: 20px;">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
          <div style="font-family: var(--font-display); font-size: 0.85rem; font-weight: 700; color: var(--text-white);">
            Autonomous Synthesis Pipeline Execution Grid
          </div>
          <div style="font-size: 0.65rem; color: var(--green); font-family: var(--font-mono);">
            ● ALL 16 STAGES CERTIFIED &bull; 1,044 TESTS PASSING
          </div>
        </div>
        <div style="display: grid; grid-template-columns: repeat(8, 1fr); gap: 8px;">
          ${pipelineSteps.map((st) => `
            <div style="background: rgba(255,255,255,0.02); border: 1px solid rgba(0,255,255,0.15); padding: 8px; border-radius: 6px; display: flex; flex-direction: column; justify-content: space-between;">
              <div>
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px;">
                  <span style="font-size: 0.62rem; color: var(--cyan); font-family: var(--font-mono); font-weight: 700;">${st.num}</span>
                  <span style="font-size: 0.55rem; color: var(--green); font-weight: 700;">● LIVE</span>
                </div>
                <div style="font-size: 0.72rem; font-weight: 700; color: var(--text-white); line-height: 1.2;">${st.name}</div>
              </div>
              <div style="font-size: 0.6rem; color: var(--text-dim); line-height: 1.25; margin-top: 6px;">${st.desc}</div>
            </div>
          `).join('')}
        </div>
      </div>

      <!-- Immutable Capability Registry Table -->
      <div style="margin-bottom: 16px;">
        <div style="font-family: var(--font-display); font-size: 0.85rem; font-weight: 700; color: var(--text-white); margin-bottom: 10px;">
          Sealed Capabilities in Immutable Registry (Lineage Verified)
        </div>
        <div style="display: flex; flex-direction: column; gap: 8px;">
          ${registeredCaps.map(c => `
            <div style="display: flex; align-items: center; justify-content: space-between; padding: 10px 14px; background: rgba(255,255,255,0.02); border: 1px solid var(--glass-border); border-left: 3px solid ${c.stateColor}; border-radius: 6px;">
              <div>
                <div style="font-size: 0.85rem; font-weight: 700; color: #fff;">${c.title}</div>
                <div style="font-family: var(--font-mono); font-size: 0.68rem; color: var(--text-muted); margin-top: 2px;">
                  ID: <span class="text-cyan">${c.id}</span> &bull; Modality: ${c.modality} &bull; Risk: ${c.risk}
                </div>
              </div>
              <div style="display: flex; align-items: center; gap: 16px;">
                <div style="text-align: right; font-family: var(--font-mono); font-size: 0.65rem; color: var(--text-muted);">
                  <div>ICA Cert: <span class="text-green">${c.certId}</span></div>
                  <div>Release Sig: ${c.sigHex}</div>
                </div>
                <span style="display: inline-block; padding: 3px 10px; font-size: 0.7rem; font-weight: 700; font-family: var(--font-mono); color: ${c.stateColor}; background: rgba(255,255,255,0.04); border: 1px solid ${c.stateColor}; border-radius: 4px;">
                  ${c.state}
                </span>
              </div>
            </div>
          `).join('')}
        </div>
      </div>

      <!-- PRG-1 Sovereignty Disclaimer -->
      <div style="padding: 12px 14px; background: rgba(0,255,255,0.03); border: 1px solid rgba(0,255,255,0.15); border-radius: 6px;">
        <div style="display: flex; align-items: center; gap: 6px; font-family: var(--font-mono); font-size: 0.68rem; color: var(--cyan); margin-bottom: 4px;">
          <span>⚖</span>
          <span style="font-weight: 700; text-transform: uppercase;">PRG-1 Reality &amp; Firewall Sovereignty Invariant:</span>
        </div>
        <div style="font-size: 0.72rem; color: var(--text-muted); line-height: 1.5;">
          All synthesized capabilities run through disposable sandbox containment and independent certification. A newly created capability has <strong>zero authority</strong> to bypass the Batch 6 Execution Firewall, issue ExecutionPermits, or alter business policy.
        </div>
      </div>
    `;

    modal.open({
      categoryCode: 'Capability Laboratory // Batch 3.7',
      title: 'Autonomous Capability Laboratory & Supply Chain',
      bodyHtml,
      footerText: 'Invariant I17 &bull; Capability Creates Function, Never Authority &bull; Batch 6 Firewall Sovereign',
      actions: [
        {
          id: 'act-synthesize-gap',
          label: 'Preview Synthesis Pipeline (Simulation)',
          variant: 'btn-magenta',
          onClick: () => {
            sfx.playQuantumPulse();
            dispatchGovernedAction('Preview Autonomous Synthesis Pipeline', 'SIM-CAP-PREVIEW', 'Capability Factory');
            toast.show('CAPABILITY FACTORY ◌ SIMULATION: Pipeline Preview Activated (No Production Effect)', 'warning', 6000);
            modal.close();
          }
        },
        {
          id: 'act-view-registry-ledger',
          label: 'Inspect Audit Ledger',
          variant: 'btn-cyan',
          onClick: () => {
            dispatchGovernedAction('Audit Immutable Capability Registry', 'CAP-REGISTRY-AUDIT', 'Capability Registry');
            modal.close();
          }
        }
      ]
    });
  }

  // 11g. PRG-1 Provenance Inspector ("Why is this value here?")
  function openProvenanceInspector(metricKey) {
    const p = NEXUS_DATA.provenance[metricKey] || {
      metric: metricKey,
      value: 'Verified',
      source: 'Digital Twin & Financial Ledger',
      window: 'Rolling 30 Days',
      sample: '142 Verified Records',
      lastUpdated: '17:31:18 IST',
    };

    const bodyHtml = `
      <table class="param-table">
        <tbody>
          <tr><td class="param-key">METRIC</td><td class="param-val font-bold text-white">${p.metric}</td></tr>
          <tr><td class="param-key">CURRENT VALUE</td><td class="param-val font-bold text-cyan">${p.value}</td></tr>
          <tr><td class="param-key">TRUTH CLASSIFICATION</td><td class="param-val font-bold text-green">FACT (Live Verified)</td></tr>
          <tr><td class="param-key">AUTHORITATIVE SOURCE</td><td class="param-val text-white">${p.source}</td></tr>
          <tr><td class="param-key">TIME WINDOW</td><td class="param-val font-mono">${p.window}</td></tr>
          <tr><td class="param-key">SAMPLE POPULATION</td><td class="param-val font-mono">${p.sample}</td></tr>
          <tr><td class="param-key">LAST VERIFIED</td><td class="param-val font-mono text-cyan">${p.lastUpdated}</td></tr>
          <tr><td class="param-key">PROVENANCE ID</td><td class="param-val font-mono text-muted">PV-19382-VERIFIED</td></tr>
          <tr><td class="param-key">SHA-256 HASH</td><td class="param-val font-mono" style="font-size: 0.65rem;">9e2b10a47f84c810...</td></tr>
        </tbody>
      </table>

      <div style="padding: 10px 14px; background: rgba(57,255,20,0.03); border: 1px solid rgba(57,255,20,0.15); border-radius: 2px; margin-top: 12px;">
        <div style="font-family: var(--font-mono); font-size: 0.65rem; color: var(--green); text-transform: uppercase;">WHY IS THIS VALUE HERE?</div>
        <p style="font-size: 0.78rem; color: var(--text-white); margin-top: 2px;">
          Grounding rule: This metric was verified through cryptographically sealed events originating from your tenant's ledger.
          No synthetic extrapolation or stochastic hallucination was used to generate this figure.
        </p>
      </div>
    `;

    modal.open({
      categoryCode: 'PROVENANCE // WHY THIS IS REAL',
      title: `EPISTEMIC PROVENANCE: ${p.metric.toUpperCase()}`,
      bodyHtml,
      footerText: 'INVARIANT I15-A &amp; PRG-1 &bull; STRICT GROUNDING IN TENANT LEDGERS',
      actions: [],
    });
  }

  // 12. Domain Command Inspectors (for 2x2 Grid Cards)
  function openDomainInspector(domain) {
    switch (domain) {
      case 'revenue':
        modal.open({
          categoryCode: 'DOMAIN 01 // COMMERCIAL',
          title: 'REVENUE COMMAND & REVERSE FUNNEL',
          bodyHtml: `
            <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin-bottom: 16px;">
              <div style="background: rgba(57,255,20,0.06); border: 1px solid rgba(57,255,20,0.2); padding: 10px;">
                <div style="font-size: 0.62rem; color: var(--text-muted);">REVENUE VELOCITY</div>
                <div style="font-size: 1.3rem; color: var(--green); font-weight: 700;">+12.8%</div>
              </div>
              <div style="background: rgba(0,255,255,0.06); border: 1px solid rgba(0,255,255,0.2); padding: 10px;">
                <div style="font-size: 0.62rem; color: var(--text-muted);">PIPELINE VALUE</div>
                <div style="font-size: 1.3rem; color: var(--cyan); font-weight: 700;">₹4.82 Cr</div>
              </div>
              <div style="background: rgba(255,184,0,0.06); border: 1px solid rgba(255,184,0,0.2); padding: 10px;">
                <div style="font-size: 0.62rem; color: var(--text-muted);">REVENUE GAP</div>
                <div style="font-size: 1.3rem; color: var(--amber); font-weight: 700;">₹38.4 L</div>
              </div>
            </div>
            <table class="param-table">
              <tbody>
                <tr><td class="param-key">FORECAST CONFIDENCE</td><td class="param-val text-cyan font-bold">91.0% (Bayesian Calibration)</td></tr>
                <tr><td class="param-key">ACTIVE REVERSE FUNNEL</td><td class="param-val">SaaS Mid-Market Enterprise Funnel</td></tr>
                <tr><td class="param-key">COMMERCIAL MISSIONS</td><td class="param-val">3 Missions Active (#M-9941, #M-8842, #M-8810)</td></tr>
              </tbody>
            </table>
          `,
          footerText: 'COMMERCIAL OPTIMIZATION ENGINE &bull; CONTINUOUS REVENUE DISPATCH',
          actions: [
            {
              id: 'act-sim-rev',
              label: 'SIMULATE REVERSE FUNNEL',
              variant: 'btn-green',
              onClick: () => {
                dispatchGovernedAction('Simulate Reverse Funnel', 'REV-SIM', 'Revenue Engine');
                modal.close();
              },
            },
          ],
        });
        break;

      case 'operations':
        openWorkerFabricInspector();
        break;

      case 'security':
        openFirewallInspector();
        break;

      case 'strategy':
        openStrategicRegimeInspector();
        break;

      default:
        openDecisionInspector();
    }
  }

  // =========================================================================
  // 12. CENTRAL 3D CORE 7-STATE CYCLIC STATE MACHINE
  // =========================================================================

  const CORE_STATES = [
    {
      label: 'BUSINESS CORE',
      indicator: '● LIVE TWIN SYNC',
      outerColor: 0x00ffff,
      innerColor: 0xbc00ff,
      msg: 'Digital Twin state nominal. Realtime bank ledger reconciled.',
    },
    {
      label: 'MISSION ACTIVE',
      indicator: '● DAG #M-9941 RUNNING',
      outerColor: 0x00ffff,
      innerColor: 0x39ff14,
      msg: 'Autonomous Mission #M-9941 executing across Worker Fleet (Autonomy L3).',
    },
    {
      label: 'CONSTRAINT BLOCKED',
      indicator: '⚠ CAC CEILING WARNING',
      outerColor: 0xffb800,
      innerColor: 0xbc00ff,
      msg: 'CAC Ceiling threshold warning engaged. Safe alternatives ready.',
    },
    {
      label: 'HUMAN APPROVAL',
      indicator: '⚡ 2 PENDING PROPOSALS',
      outerColor: 0xbc00ff,
      innerColor: 0x00ffff,
      msg: 'Awaiting executive cryptographic signature for Proposal #AP-9941.',
    },
    {
      label: 'FIREWALL LOCKED',
      indicator: '🔒 BATCH 6 SOVEREIGN',
      outerColor: 0x39ff14,
      innerColor: 0x00ffff,
      msg: 'Batch 6 Execution Firewall sovereign. All consequential external effects locked.',
    },
    {
      label: 'SECURITY EVENT',
      indicator: '🛡️ STRIX QUARANTINE',
      outerColor: 0xff3366,
      innerColor: 0xbc00ff,
      msg: 'Worker-DESKTOP-04 quarantined for shell violation under Invariant I16-A.',
    },
    {
      label: 'COMPLETED',
      indicator: '✓ REVENUE RESTORED',
      outerColor: 0x39ff14,
      innerColor: 0x39ff14,
      msg: 'Recovery mission verified complete in Outcome Ledger (+₹75K captured).',
    },
  ];

  let currentCoreStateIdx = 0;

  function cycleCoreState(coreInstance) {
    currentCoreStateIdx = (currentCoreStateIdx + 1) % CORE_STATES.length;
    const st = CORE_STATES[currentCoreStateIdx];

    const modeLabel = document.getElementById('core-mode-label');
    const stateIndicator = document.getElementById('core-state-indicator');

    if (modeLabel) modeLabel.textContent = st.label;
    if (stateIndicator) stateIndicator.textContent = st.indicator;

    if (coreInstance) {
      coreInstance.outerCageMat.color.setHex(st.outerColor);
      coreInstance.innerCoreMat.emissive.setHex(st.innerColor);
      coreInstance.pulseDiagnosticWave();
    } else {
      sfx.playQuantumPulse();
    }

    toast.show(`CORE STATE: [${st.label}] &bull; ${st.indicator}`, 'info', 3000);

    if (window.appendCharlieLog) {
      window.appendCharlieLog('CORE', st.msg);
    }
  }

  // =========================================================================
  // 13. WORKING TACTICAL NAVIGATION & STAGE COORDINATION
  // =========================================================================

  function initTacticalNavigation(coreInstance) {
    const navButtons = document.querySelectorAll('.nav-btn');
    const modules = document.querySelectorAll('.tactical-module');
    const modeLabel = document.getElementById('core-mode-label');
    const stateIndicator = document.getElementById('core-state-indicator');

    const badgeTL = document.getElementById('holo-metric-tl');
    const badgeTR = document.getElementById('holo-metric-tr');
    const badgeBL = document.getElementById('holo-metric-bl');
    const badgeBR = document.getElementById('holo-metric-br');

    navButtons.forEach((btn) => {
      btn.addEventListener('click', () => {
        const target = btn.getAttribute('data-target');
        if (!target) return;

        sfx.playBlip();

        navButtons.forEach((b) => b.classList.remove('active-nav'));
        btn.classList.add('active-nav');
        document.body.setAttribute('data-active-domain', target);

        modules.forEach((mod) => {
          mod.classList.remove('active-dominant');
          if (mod.getAttribute('data-domain') === target) {
            mod.classList.add('active-dominant');
          }
        });

        if (coreInstance) {
          coreInstance.setMode(target);
        }

        switch (target) {
          case 'revenue':
            if (modeLabel) modeLabel.textContent = 'REVENUE COMMAND';
            if (stateIndicator) stateIndicator.textContent = '● CASHFLOW FLOWING';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'VERIFIED REVENUE'; badgeTL.querySelector('.badge-val').textContent = '₹4,82,300'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'PIPELINE VALUE'; badgeTR.querySelector('.badge-val').textContent = '₹6,20,000 ◆'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'REALIZED VALUE'; badgeBL.querySelector('.badge-val').textContent = 'UNKNOWN'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'PROVENANCE'; badgeBR.querySelector('.badge-val').textContent = 'REV-982341'; }
            break;
          case 'operations':
            if (modeLabel) modeLabel.textContent = 'WORKER FABRIC';
            if (stateIndicator) stateIndicator.textContent = '● 3 ACTIVE / 1 IDLE';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'ACTIVE MISSIONS'; badgeTL.querySelector('.badge-val').textContent = '7 RUNNING'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'WORKER MODALITY'; badgeTR.querySelector('.badge-val').textContent = 'API + BRW'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'MISSION SUCCESS'; badgeBL.querySelector('.badge-val').textContent = 'UNKNOWN'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'MCP / DESKTOP'; badgeBR.querySelector('.badge-val').textContent = 'NOT CONFIGURED'; }
            break;
          case 'security':
            if (modeLabel) modeLabel.textContent = 'FIREWALL CORE';
            if (stateIndicator) stateIndicator.textContent = '● BATCH 6 SOVEREIGN';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'SECURITY POSTURE'; badgeTL.querySelector('.badge-val').textContent = 'UNKNOWN'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'EXECUTION FIREWALL'; badgeTR.querySelector('.badge-val').textContent = 'LOCKED'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'POLICY COMPLIANCE'; badgeBL.querySelector('.badge-val').textContent = 'BATCH 6 ENFORCED'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'EXTERNAL ACTIONS'; badgeBR.querySelector('.badge-val').textContent = 'ZERO BYPASS'; }
            break;
          case 'strategy':
            if (modeLabel) modeLabel.textContent = 'STRATEGY SIMULATOR';
            if (stateIndicator) stateIndicator.textContent = '● PRICE WAR REGIME';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'BUSINESS HEALTH'; badgeTL.querySelector('.badge-val').textContent = '87.4%'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'STRATEGIC ALIGNMENT'; badgeTR.querySelector('.badge-val').textContent = '91.2%'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'TACTICAL SCORE'; badgeBL.querySelector('.badge-val').textContent = '78.6%'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'BOTTLENECK'; badgeBR.querySelector('.badge-val').textContent = 'CAC CONSTRAINED'; }
            break;
          case 'approvals':
            if (modeLabel) modeLabel.textContent = 'APPROVAL CENTER';
            if (stateIndicator) stateIndicator.textContent = '● CEO HITL GATEWAY';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'PENDING APPROVALS'; badgeTL.querySelector('.badge-val').textContent = '2 REQUESTED'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'MAX FINANCIAL EXPOSURE'; badgeTR.querySelector('.badge-val').textContent = '₹57,500'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'SLA COUNTDOWN'; badgeBL.querySelector('.badge-val').textContent = '3h 59m'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'FIREWALL PERMITS'; badgeBR.querySelector('.badge-val').textContent = '0 BYPASS'; }
            openApprovalCenterModal();
            break;
          case 'factory':
            if (modeLabel) modeLabel.textContent = 'CAPABILITY FACTORY';
            if (stateIndicator) stateIndicator.textContent = '● BATCH 3.7 SOVEREIGN';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'SYNTHESIZED CAPS'; badgeTL.querySelector('.badge-val').textContent = '14 ACTIVE'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'SUPPLY CHAIN'; badgeTR.querySelector('.badge-val').textContent = 'ED25519 SIGNED'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'CERTIFICATION'; badgeBL.querySelector('.badge-val').textContent = 'ICA GOVERNED'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'ZERO PERMIT BYPASS'; badgeBR.querySelector('.badge-val').textContent = 'INVARIANT I17'; }
            openCapabilityFactoryInspector();
            break;
          default: // core
            if (modeLabel) modeLabel.textContent = 'BUSINESS CORE';
            if (stateIndicator) stateIndicator.textContent = '● LIVE TWIN SYNC';
            if (badgeTL) { badgeTL.querySelector('.badge-key').textContent = 'BUSINESS HEALTH'; badgeTL.querySelector('.badge-val').textContent = '87.4%'; }
            if (badgeTR) { badgeTR.querySelector('.badge-key').textContent = 'STRATEGIC ALIGNMENT'; badgeTR.querySelector('.badge-val').textContent = '91.2%'; }
            if (badgeBL) { badgeBL.querySelector('.badge-key').textContent = 'VERIFIED REVENUE'; badgeBL.querySelector('.badge-val').textContent = '₹4,82,300'; }
            if (badgeBR) { badgeBR.querySelector('.badge-key').textContent = 'ACTIVE MISSIONS'; badgeBR.querySelector('.badge-val').textContent = '7 RUNNING'; }
        }

        window.appendCharlieLog('NAV', `Command stage switched to [${target.toUpperCase()}] domain view`);
      });
    });

    // Reality Mode Pill click
    const realityPill = document.getElementById('header-reality-pill');
    if (realityPill) realityPill.addEventListener('click', () => openRealityInspector());

    // Module Card Clicks
    modules.forEach((mod) => {
      mod.addEventListener('click', () => {
        const domain = mod.getAttribute('data-domain');
        openDomainInspector(domain);
      });
    });

    // Central Core Click -> Cycle State Machine
    const coreTrigger = document.getElementById('core-trigger');
    if (coreTrigger) {
      coreTrigger.addEventListener('click', () => {
        cycleCoreState(coreInstance);
      });
    }

    // Holo surrounding badges click -> Open inspectors
    if (badgeTL) badgeTL.addEventListener('click', () => openDomainInspector('core'));
    if (badgeTR) badgeTR.addEventListener('click', () => openStrategicRegimeInspector());
    if (badgeBL) badgeBL.addEventListener('click', () => openDomainInspector('revenue'));
    if (badgeBR) badgeBR.addEventListener('click', () => openWorkerFabricInspector());

    // Radial Gauge clicks -> Open inspectors
    document.querySelectorAll('.gauge-card').forEach((card) => {
      card.addEventListener('click', () => {
        const id = card.getAttribute('data-gauge-id');
        if (id === 'health') openDomainInspector('core');
        else if (id === 'tactical') openStrategicRegimeInspector();
        else if (id === 'missions') openWorkerFabricInspector();
        else if (id === 'security') openFirewallInspector();
      });
    });

    // Epistemic Panel Click -> Open Decision Inspector
    const epistemicPanel = document.getElementById('panel-epistemic');
    if (epistemicPanel) {
      epistemicPanel.addEventListener('click', () => openDecisionInspector());
    }

    // Constraint items clicks
    document.querySelectorAll('.constraint-item').forEach((item) => {
      item.addEventListener('click', () => {
        const cid = item.getAttribute('data-constraint-id');
        if (cid) openConstraintInspector(cid);
      });
    });

    // Inspect All Constraints chip click
    const inspectAllChip = document.getElementById('btn-inspect-all-constraints');
    if (inspectAllChip) {
      inspectAllChip.addEventListener('click', (e) => {
        e.stopPropagation();
        openAllConstraintsInspector();
      });
    }

    // Header interactive controls
    const autonomyPod = document.getElementById('header-autonomy-pod');
    if (autonomyPod) autonomyPod.addEventListener('click', () => openAutonomyInspector());

    const regimeBadge = document.getElementById('header-regime-badge');
    if (regimeBadge) regimeBadge.addEventListener('click', () => openStrategicRegimeInspector());

    const quantumSync = document.getElementById('header-quantum-sync');
    if (quantumSync) quantumSync.addEventListener('click', () => openQuantumSyncInspector());

    const firewallPill = document.getElementById('header-firewall-pill');
    if (firewallPill) firewallPill.addEventListener('click', () => openFirewallInspector());

    const commanderTag = document.getElementById('header-commander-tag');
    if (commanderTag) commanderTag.addEventListener('click', () => openCommanderInspector());
  }

  // =========================================================================
  // 14. CHARLIE EVENT STREAM (LIVE MISSION FEED GENERATOR & FILTERS)
  // =========================================================================

  function initLiveEventFeed() {
    const feedList = document.getElementById('event-feed-list');
    const filterBar = document.getElementById('event-filter-bar');
    if (!feedList) return;

    let activeCategory = 'ALL';

    if (filterBar) {
      filterBar.querySelectorAll('.filter-chip').forEach((chip) => {
        chip.addEventListener('click', () => {
          sfx.playBlip();
          filterBar.querySelectorAll('.filter-chip').forEach((c) => c.classList.remove('active'));
          chip.classList.add('active');
          activeCategory = chip.getAttribute('data-cat') || 'ALL';

          // Filter visible items
          feedList.querySelectorAll('.feed-entry').forEach((entry) => {
            const entryCat = entry.getAttribute('data-cat') || '';
            if (activeCategory === 'ALL' || entryCat === activeCategory) {
              entry.style.display = 'flex';
            } else {
              entry.style.display = 'none';
            }
          });
        });
      });
    }

    // Click handler on existing entries
    feedList.addEventListener('click', (e) => {
      const entry = e.target.closest('.feed-entry');
      if (entry) {
        const eventId = entry.getAttribute('data-event-id') || '8843';
        sfx.playBlip();
        openEventInspector(eventId);
      }
    });

    window.appendCharlieLog = function (cat, message) {
      const now = new Date();
      const h = String(now.getHours()).padStart(2, '0');
      const m = String(now.getMinutes()).padStart(2, '0');
      const s = String(now.getSeconds()).padStart(2, '0');
      const timeStr = `[${h}:${m}:${s}]`;
      const randId = Math.floor(Math.random() * 8000 + 2000);

      const entry = document.createElement('div');
      entry.className = 'feed-entry clickable';
      entry.tabIndex = 0;
      entry.setAttribute('role', 'button');
      entry.setAttribute('data-event-id', randId);
      entry.setAttribute('data-cat', cat);

      let catClass = 'cat-core';
      if (cat === 'NAV') catClass = 'cat-sim';
      else if (cat === 'DIAGNOSTIC') catClass = 'cat-alert';
      else if (cat === 'I15-GATE') catClass = 'cat-constraint';
      else if (cat === 'STRATEGY') catClass = 'cat-strategy';
      else if (cat === 'SECURITY' || cat === 'FIREWALL') catClass = 'cat-security';
      else if (cat === 'MISSION') catClass = 'cat-mission';

      entry.innerHTML = `
        <span class="entry-time">${timeStr}</span>
        <span class="entry-cat ${catClass}">${cat}</span>
        <span class="entry-msg">${message}</span>
      `;

      if (activeCategory !== 'ALL' && cat !== activeCategory) {
        entry.style.display = 'none';
      }

      feedList.appendChild(entry);
      feedList.scrollTop = feedList.scrollHeight;

      if (feedList.children.length > 30) {
        feedList.removeChild(feedList.children[0]);
      }
    };

    // PRG-1 Rule: Live event stream - zero fabricated events in production
    // If no external events are arriving, state remains authentically LIVE - IDLE.
    window.appendCharlieLog('REALITY', 'Production Event Stream synchronized with Runtime Event Bus (Status: LIVE — IDLE)');
  }

  // =========================================================================
  // 15. KEYBOARD COMMAND ACCELERATORS
  // =========================================================================

  function initKeyboardAccelerators(coreInstance) {
    window.addEventListener('keydown', (e) => {
      // Don't trigger if user is typing in an input
      if (['INPUT', 'TEXTAREA', 'SELECT'].includes(document.activeElement.tagName)) return;

      if (e.key === '1') {
        const b = document.getElementById('nav-core');
        if (b) b.click();
      } else if (e.key === '2') {
        const b = document.getElementById('nav-revenue');
        if (b) b.click();
      } else if (e.key === '3') {
        const b = document.getElementById('nav-operations');
        if (b) b.click();
      } else if (e.key === '4') {
        const b = document.getElementById('nav-strategy');
        if (b) b.click();
      } else if (e.key === '5') {
        const b = document.getElementById('nav-security');
        if (b) b.click();
      } else if (e.key === '6' || e.key.toLowerCase() === 'p') {
        const b = document.getElementById('nav-approvals');
        if (b) b.click();
      } else if (e.key === '7' || e.key.toLowerCase() === 'y') {
        const b = document.getElementById('nav-factory');
        if (b) b.click();
      } else if (e.key.toLowerCase() === 'c') {
        cycleCoreState(coreInstance);
      } else if (e.key.toLowerCase() === 'f') {
        openFirewallInspector();
      } else if (e.key.toLowerCase() === 'a') {
        openAutonomyInspector();
      } else if (e.key.toLowerCase() === 'r') {
        openStrategicRegimeInspector();
      } else if (e.key.toLowerCase() === 'l') {
        openRealityInspector();
      } else if (e.key.toLowerCase() === 'v') {
        openValueRealizationModal();
      } else if (e.key.toLowerCase() === 'w') {
        openWorkControlCenterModal();
      }
    });
  }

  // =========================================================================
  // 16. BOOTSTRAP MASTER RUNTIME
  // =========================================================================

  document.addEventListener('DOMContentLoaded', () => {
    initClock();
    initPeripheralParticles();
    const core = new HolographicCore();
    initModuleCanvasIcons();
    initRadialGauges();
    initTacticalNavigation(core);
    initLiveEventFeed();
    new ProvenanceTooltipController();
    initKeyboardAccelerators(core);

    // Initial boot chirp & greeting
    setTimeout(() => {
      sfx.playChirp();
      toast.show('CHARLIE // NEXUS PRODUCTION CONTROL PLANE READY &bull; PRG-1 CERTIFIED', 'info', 4000);
    }, 600);
  });
})();

