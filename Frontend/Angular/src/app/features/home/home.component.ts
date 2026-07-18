import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  inject,
  signal,
} from '@angular/core';
import { RouterModule, Router } from '@angular/router';

interface Capability {
  icon: string;
  title: string;
  desc: string;
}

interface GalleryImage {
  src: string;
  alt: string;
  caption: string;
  credit: string;
  fallbackColor: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- NAVBAR -->
    <nav class="home-nav" [class.scrolled]="isScrolled()">
      <div class="nav-container">
        <a class="nav-brand" routerLink="/home">
          <svg class="brand-star" viewBox="0 0 20 20" fill="currentColor" width="18" height="18">
            <path d="M10 1.5l2.4 5 5.5.8-4 3.9.95 5.5L10 14.1l-4.85 2.6.95-5.5-4-3.9 5.5-.8z"/>
          </svg>
          ORDISS
        </a>
        <button class="hamburger" (click)="toggleMenu()" [class.open]="menuOpen()">
          <span></span><span></span><span></span>
        </button>
        <div class="nav-links" [class.open]="menuOpen()">
          <a href="#mission" (click)="closeMenu()">Mission</a>
          <a href="#capabilities" (click)="closeMenu()">Capabilities</a>
          <a href="#gallery" (click)="closeMenu()">Gallery</a>
          <a href="#security" (click)="closeMenu()">Security</a>
          <button class="nav-cta" (click)="goToLogin()">Sign In</button>
        </div>
      </div>
    </nav>

    <!-- HERO -->
    <section id="home" class="hero-section">
      <div class="hero-content">
        <div class="hero-badge">U.S. Military Supply Chain Management</div>
        <h1 class="hero-title">ORDISS</h1>
        <p class="hero-subtitle">Operational Resource &amp; Defense<br/>Inventory Supply System</p>
        <p class="hero-desc">Secure, mission-ready inventory management for the modern warfighter.<br/>Built to DoD standards. Deployed at scale.</p>
        <div class="hero-actions">
          <button class="btn-primary" (click)="goToLogin()">Access System</button>
          <a href="#mission" class="btn-ghost">Learn More ↓</a>
        </div>
      </div>
      <div class="hero-scroll-hint">
        <svg viewBox="0 0 24 24" fill="none" width="24" height="24">
          <path d="M12 5v14M5 12l7 7 7-7" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>
    </section>

    <!-- STATS STRIP -->
    <section class="stats-strip">
      <div class="stats-container">
        @for (stat of stats; track stat.label) {
          <div class="stat-item">
            <span class="stat-number">{{ stat.value }}</span>
            <span class="stat-label">{{ stat.label }}</span>
          </div>
        }
      </div>
    </section>

    <!-- MISSION -->
    <section id="mission" class="mission-section">
      <div class="section-container">
        <div class="mission-grid">
          <div class="mission-text">
            <span class="eyebrow">Our Mission</span>
            <h2 class="section-heading">Modernizing Military Supply Chain Operations</h2>
            <p>ORDISS delivers a unified digital platform for U.S. military inventory management, procurement, and logistics. Built with zero-trust security and real-time visibility across all supply depots.</p>
            <p>From issue and receipt tracking to traffic and security management, every module is engineered for the demanding operational tempo of today's armed forces.</p>
            <ul class="feature-list">
              <li><span class="check">✓</span> DoD-grade zero-trust security architecture</li>
              <li><span class="check">✓</span> Multi-factor auth — Password, CAC &amp; FIDO2</li>
              <li><span class="check">✓</span> Real-time supply chain visibility &amp; reporting</li>
              <li><span class="check">✓</span> Automated procurement &amp; approval workflows</li>
              <li><span class="check">✓</span> Role-based access control across all modules</li>
            </ul>
          </div>
          <div class="mission-image-wrap">
            <div class="mission-img-container">
              <img
                src="https://upload.wikimedia.org/wikipedia/commons/thumb/a/a0/Defense.gov_photo_essay_100908-A-3108M-002.jpg/800px-Defense.gov_photo_essay_100908-A-3108M-002.jpg"
                alt="U.S. Army supply operations — public domain, DoD"
                loading="lazy"
                (error)="onImgError($event)"
              />
            </div>
            <div class="img-credit">Public Domain · U.S. Department of Defense</div>
          </div>
        </div>
      </div>
    </section>

    <!-- SERVICE BRANCHES -->
    <section class="branches-section">
      <div class="section-container">
        <div class="branches-header">
          <span class="eyebrow light">Service Branches Supported</span>
        </div>
        <div class="branches-grid">
          @for (branch of branches; track branch.name) {
            <div class="branch-item">
              <img [src]="branch.seal" [alt]="branch.name + ' seal'" loading="lazy" (error)="onImgError($event)" />
              <span>{{ branch.name }}</span>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- CAPABILITIES -->
    <section id="capabilities" class="capabilities-section">
      <div class="section-container">
        <div class="section-header">
          <span class="eyebrow">System Capabilities</span>
          <h2 class="section-heading">Comprehensive Supply Chain Management</h2>
          <p class="section-sub">Fourteen integrated modules covering every aspect of military logistics and inventory control.</p>
        </div>
        <div class="cap-grid">
          @for (cap of capabilities; track cap.title) {
            <div class="cap-card">
              <div class="cap-icon">{{ cap.icon }}</div>
              <h3>{{ cap.title }}</h3>
              <p>{{ cap.desc }}</p>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- GALLERY -->
    <section id="gallery" class="gallery-section">
      <div class="section-container">
        <div class="section-header">
          <span class="eyebrow light">In the Field</span>
          <h2 class="section-heading light">Supporting the Warfighter</h2>
        </div>
        <div class="gallery-grid">
          @for (img of galleryImages; track img.src) {
            <div class="gallery-item" [style.background]="img.fallbackColor">
              <img [src]="img.src" [alt]="img.alt" loading="lazy" (error)="onImgError($event)" />
              <div class="gallery-overlay">
                <p>{{ img.caption }}</p>
                <small>{{ img.credit }}</small>
              </div>
            </div>
          }
        </div>
        <p class="gallery-note">All images are public domain — U.S. Department of Defense</p>
      </div>
    </section>

    <!-- SECURITY -->
    <section id="security" class="security-section">
      <div class="section-container">
        <div class="security-grid">
          <div class="security-text">
            <span class="eyebrow">Authentication &amp; Security</span>
            <h2 class="section-heading">Mission-Grade Access Control</h2>
            <p class="security-intro">ORDISS supports three secure authentication pathways ensuring only authorized personnel can access the system — from field terminals to secure operations centers.</p>
            <div class="auth-methods">
              <div class="auth-method">
                <div class="auth-icon">
                  <svg viewBox="0 0 24 24" fill="none" width="28" height="28">
                    <rect x="3" y="11" width="18" height="11" rx="2" stroke="currentColor" stroke-width="2"/>
                    <path d="M7 11V7a5 5 0 0110 0v4" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                    <circle cx="12" cy="16" r="1.5" fill="currentColor"/>
                  </svg>
                </div>
                <div>
                  <strong>Password Authentication</strong>
                  <p>Standard credentials with strong validation, account lockout, and secure token management.</p>
                </div>
              </div>
              <div class="auth-method">
                <div class="auth-icon">
                  <svg viewBox="0 0 24 24" fill="none" width="28" height="28">
                    <rect x="2" y="5" width="20" height="14" rx="2" stroke="currentColor" stroke-width="2"/>
                    <path d="M2 10h20" stroke="currentColor" stroke-width="2"/>
                    <circle cx="7" cy="15" r="1.5" fill="currentColor"/>
                  </svg>
                </div>
                <div>
                  <strong>CAC / Smart Card</strong>
                  <p>Common Access Card authentication leveraging DoD PKI infrastructure for certificate-based identity.</p>
                </div>
              </div>
              <div class="auth-method">
                <div class="auth-icon">
                  <svg viewBox="0 0 24 24" fill="none" width="28" height="28">
                    <path d="M21 2H3v8c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V2z" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/>
                    <path d="M9 12l2 2 4-4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </div>
                <div>
                  <strong>FIDO2 / WebAuthn</strong>
                  <p>Hardware security keys and biometric authentication for zero-trust, phishing-resistant environments.</p>
                </div>
              </div>
            </div>
            <button class="btn-primary" style="margin-top:2rem" (click)="goToLogin()">Access the System →</button>
          </div>
          <div class="security-visual">
            <!-- Animated shield SVG -->
            <svg viewBox="0 0 320 340" class="shield-svg" xmlns="http://www.w3.org/2000/svg">
              <defs>
                <linearGradient id="shieldFill" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0%" stop-color="#1a4731"/>
                  <stop offset="100%" stop-color="#2d7a56"/>
                </linearGradient>
                <linearGradient id="shieldGlow" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="#4ade80" stop-opacity="0.4"/>
                  <stop offset="100%" stop-color="#4ade80" stop-opacity="0"/>
                </linearGradient>
                <filter id="glow">
                  <feGaussianBlur stdDeviation="4" result="blur"/>
                  <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
                </filter>
              </defs>
              <!-- Outer shield -->
              <path d="M160 20 L288 70 L288 175 C288 238 228 282 160 305 C92 282 32 238 32 175 L32 70 Z"
                    fill="url(#shieldFill)" stroke="#4ade80" stroke-width="2"/>
              <!-- Inner shield -->
              <path d="M160 40 L272 85 L272 175 C272 228 218 268 160 289 C102 268 48 228 48 175 L48 85 Z"
                    fill="none" stroke="rgba(74,222,128,0.25)" stroke-width="1.5"/>
              <!-- Glow layer -->
              <path d="M160 20 L288 70 L288 175 C288 238 228 282 160 305 C92 282 32 238 32 175 L32 70 Z"
                    fill="url(#shieldGlow)" opacity="0.5"/>
              <!-- Stars row -->
              <text x="160" y="108" text-anchor="middle" fill="rgba(200,169,110,0.9)" font-size="13" letter-spacing="8" font-family="Outfit,sans-serif">★ ★ ★</text>
              <!-- Lock body -->
              <rect x="130" y="158" width="60" height="52" rx="8" fill="#4ade80" filter="url(#glow)"/>
              <!-- Lock shackle -->
              <path d="M143 158 L143 136 Q160 120 177 136 L177 158"
                    fill="none" stroke="#4ade80" stroke-width="9" stroke-linecap="round"/>
              <!-- Keyhole -->
              <circle cx="160" cy="180" r="9" fill="#0D2B1D"/>
              <rect x="156" y="186" width="8" height="14" rx="3" fill="#0D2B1D"/>
              <!-- Pulse rings -->
              <circle cx="160" cy="175" r="85" fill="none" stroke="rgba(74,222,128,0.1)" stroke-width="1" class="pulse-ring r1"/>
              <circle cx="160" cy="175" r="110" fill="none" stroke="rgba(74,222,128,0.06)" stroke-width="1" class="pulse-ring r2"/>
              <circle cx="160" cy="175" r="135" fill="none" stroke="rgba(74,222,128,0.03)" stroke-width="1" class="pulse-ring r3"/>
            </svg>
          </div>
        </div>
      </div>
    </section>

    <!-- CTA BANNER -->
    <section class="cta-section">
      <div class="cta-container">
        <h2>Ready to Get Started?</h2>
        <p>Authorized U.S. military personnel can access the system using their credentials, CAC card, or FIDO2 security key.</p>
        <div class="cta-actions">
          <button class="btn-primary" (click)="goToLogin()">Sign In Now</button>
          <a routerLink="/coming-soon" class="btn-ghost-dark">Coming Features</a>
        </div>
      </div>
    </section>

    <!-- FOOTER -->
    <footer class="home-footer">
      <div class="footer-container">
        <div class="footer-top">
          <div class="footer-brand">
            <svg class="brand-star" viewBox="0 0 20 20" fill="currentColor" width="20" height="20">
              <path d="M10 1.5l2.4 5 5.5.8-4 3.9.95 5.5L10 14.1l-4.85 2.6.95-5.5-4-3.9 5.5-.8z"/>
            </svg>
            ORDISS
          </div>
          <p>Operational Resource &amp; Defense Inventory Supply System</p>
        </div>
        <nav class="footer-links">
          <a href="#home">Home</a>
          <a href="#mission">Mission</a>
          <a href="#capabilities">Capabilities</a>
          <a href="#gallery">Gallery</a>
          <a href="#security">Security</a>
          <a routerLink="/login">Sign In</a>
          <a routerLink="/coming-soon">Coming Soon</a>
        </nav>
        <p class="footer-notice">
          FOR AUTHORIZED U.S. MILITARY PERSONNEL ONLY. Unauthorized access, use, or modification of this system is prohibited and may be subject to civil and criminal penalties. All activities on this system are subject to monitoring and recording.
        </p>
        <p class="footer-copy">© {{ year }} ORDISS · U.S. Military Supply Chain Management System</p>
      </div>
    </footer>
  `,
  styles: [`
    :host { display: block; font-family: 'Outfit', sans-serif; scroll-behavior: smooth; }

    /* ── NAVBAR ──────────────────────────────────────────── */
    .home-nav {
      position: fixed; top: 0; left: 0; right: 0; z-index: 1000;
      padding: 1.1rem 2rem;
      transition: background 0.35s ease, box-shadow 0.35s ease, padding 0.35s ease;
    }
    .home-nav.scrolled {
      background: rgba(10,32,21,0.96);
      backdrop-filter: blur(12px);
      box-shadow: 0 2px 24px rgba(0,0,0,0.4);
      padding: 0.7rem 2rem;
    }
    .nav-container {
      max-width: 1200px; margin: 0 auto;
      display: flex; align-items: center; justify-content: space-between;
    }
    .nav-brand {
      font-size: 1.4rem; font-weight: 700; color: #fff;
      text-decoration: none; letter-spacing: 0.06em;
      display: flex; align-items: center; gap: 0.4rem;
    }
    .brand-star { color: #C8A96E; }
    .nav-links { display: flex; align-items: center; gap: 2rem; }
    .nav-links a {
      color: rgba(255,255,255,0.82); text-decoration: none;
      font-weight: 500; font-size: 0.95rem;
      transition: color 0.2s; position: relative;
    }
    .nav-links a::after {
      content: ''; position: absolute; bottom: -2px; left: 0; width: 0;
      height: 1.5px; background: #C8A96E; transition: width 0.25s;
    }
    .nav-links a:hover { color: #C8A96E; }
    .nav-links a:hover::after { width: 100%; }
    .nav-cta {
      background: #C8A96E; color: #0D2B1D; border: none;
      padding: 0.5rem 1.5rem; border-radius: 6px;
      font-weight: 700; font-size: 0.9rem; cursor: pointer;
      font-family: 'Outfit', sans-serif; transition: background 0.2s, transform 0.15s;
    }
    .nav-cta:hover { background: #ddb96e; transform: translateY(-1px); }
    .hamburger { display: none; }

    @media (max-width: 768px) {
      .hamburger {
        display: flex; flex-direction: column; gap: 5px;
        background: none; border: none; cursor: pointer; padding: 4px;
      }
      .hamburger span {
        display: block; width: 24px; height: 2px; background: white;
        transition: transform 0.3s, opacity 0.3s;
      }
      .hamburger.open span:nth-child(1) { transform: translateY(7px) rotate(45deg); }
      .hamburger.open span:nth-child(2) { opacity: 0; }
      .hamburger.open span:nth-child(3) { transform: translateY(-7px) rotate(-45deg); }
      .nav-links {
        display: none; position: fixed; top: 0; right: 0; bottom: 0; width: 260px;
        flex-direction: column; background: rgba(10,32,21,0.97);
        padding: 5rem 2rem 2rem; gap: 1.5rem; align-items: flex-start;
        transition: transform 0.3s; transform: translateX(100%);
      }
      .nav-links.open { display: flex; transform: translateX(0); }
    }

    /* ── HERO ────────────────────────────────────────────── */
    .hero-section {
      min-height: 100vh; display: flex; align-items: center; justify-content: center;
      position: relative; overflow: hidden; text-align: center; padding: 2rem;
      background:
        linear-gradient(160deg, rgba(13,43,29,0.92) 0%, rgba(26,71,49,0.88) 50%, rgba(13,43,29,0.95) 100%),
        url('https://upload.wikimedia.org/wikipedia/commons/thumb/b/b8/80th_Training_Command_Soldiers_provide_supply_support_120903-A-QH755-001.jpg/1280px-80th_Training_Command_Soldiers_provide_supply_support_120903-A-QH755-001.jpg')
        center/cover no-repeat;
    }
    /* decorative grid */
    .hero-section::before {
      content: ''; position: absolute; inset: 0;
      background-image:
        linear-gradient(rgba(74,222,128,0.04) 1px, transparent 1px),
        linear-gradient(90deg, rgba(74,222,128,0.04) 1px, transparent 1px);
      background-size: 60px 60px;
    }
    .hero-content { position: relative; z-index: 2; max-width: 780px; }
    .hero-badge {
      display: inline-block;
      background: rgba(200,169,110,0.15); border: 1px solid rgba(200,169,110,0.45);
      color: #C8A96E; padding: 0.4rem 1.2rem; border-radius: 100px;
      font-size: 0.8rem; font-weight: 600; letter-spacing: 0.12em;
      text-transform: uppercase; margin-bottom: 1.5rem;
    }
    .hero-title {
      font-size: clamp(4.5rem, 12vw, 9rem); font-weight: 700; color: #fff;
      margin: 0 0 0.4rem; letter-spacing: 0.12em; line-height: 1;
    }
    .hero-subtitle {
      font-size: clamp(1rem, 2.5vw, 1.35rem); color: #C8A96E;
      font-weight: 500; margin: 0 0 1rem; letter-spacing: 0.04em; line-height: 1.5;
    }
    .hero-desc {
      font-size: clamp(0.95rem, 1.5vw, 1.1rem); color: rgba(255,255,255,0.72);
      margin: 0 0 2.5rem; line-height: 1.7;
    }
    .hero-actions { display: flex; gap: 1rem; justify-content: center; flex-wrap: wrap; }
    .btn-primary {
      background: #C8A96E; color: #0D2B1D; border: none;
      padding: 0.9rem 2.4rem; border-radius: 8px; font-weight: 700;
      font-size: 1rem; cursor: pointer; font-family: 'Outfit', sans-serif;
      transition: background 0.2s, transform 0.2s, box-shadow 0.2s;
      letter-spacing: 0.02em;
    }
    .btn-primary:hover {
      background: #ddb96e; transform: translateY(-2px);
      box-shadow: 0 10px 28px rgba(200,169,110,0.35);
    }
    .btn-ghost {
      background: transparent; color: #fff;
      border: 1.5px solid rgba(255,255,255,0.45);
      padding: 0.9rem 2.4rem; border-radius: 8px;
      font-weight: 600; font-size: 1rem; text-decoration: none;
      transition: border-color 0.2s, background 0.2s;
      font-family: 'Outfit', sans-serif;
    }
    .btn-ghost:hover { border-color: #fff; background: rgba(255,255,255,0.08); }
    .hero-scroll-hint {
      position: absolute; bottom: 2rem; left: 50%;
      transform: translateX(-50%); opacity: 0.5;
      animation: bobDown 2.2s ease-in-out infinite;
    }
    @keyframes bobDown {
      0%,100% { transform: translateX(-50%) translateY(0); }
      50% { transform: translateX(-50%) translateY(10px); }
    }

    /* ── STATS STRIP ─────────────────────────────────────── */
    .stats-strip {
      background: #0D2B1D;
      border-top: 1px solid rgba(200,169,110,0.25);
      border-bottom: 1px solid rgba(200,169,110,0.1);
      padding: 2.5rem 2rem;
    }
    .stats-container {
      max-width: 1200px; margin: 0 auto;
      display: grid; grid-template-columns: repeat(4,1fr); gap: 1rem; text-align: center;
    }
    @media (max-width: 640px) { .stats-container { grid-template-columns: repeat(2,1fr); } }
    .stat-item { color: #fff; }
    .stat-number { display: block; font-size: 2rem; font-weight: 700; color: #C8A96E; line-height: 1.1; }
    .stat-label {
      display: block; font-size: 0.8rem; color: rgba(255,255,255,0.55);
      margin-top: 0.3rem; text-transform: uppercase; letter-spacing: 0.1em;
    }

    /* ── LAYOUT HELPERS ──────────────────────────────────── */
    .section-container { max-width: 1200px; margin: 0 auto; padding: 0 2rem; }
    .section-header { text-align: center; margin-bottom: 3.5rem; }
    .eyebrow {
      display: inline-block; color: #2d7a56; font-weight: 700;
      font-size: 0.75rem; letter-spacing: 0.18em;
      text-transform: uppercase; margin-bottom: 0.75rem;
    }
    .eyebrow.light { color: #C8A96E; }
    .section-heading {
      font-size: clamp(1.8rem, 4vw, 2.7rem); font-weight: 700;
      color: #0D2B1D; margin: 0 0 1rem; line-height: 1.2;
    }
    .section-heading.light { color: #fff; }
    .section-sub { color: #6b7280; font-size: 1.05rem; max-width: 580px; margin: 0 auto; line-height: 1.7; }

    /* ── MISSION ─────────────────────────────────────────── */
    .mission-section { padding: 6rem 2rem; background: #f7f9f7; }
    .mission-grid {
      display: grid; grid-template-columns: 1fr 1fr;
      gap: 5rem; align-items: center;
    }
    @media (max-width: 900px) { .mission-grid { grid-template-columns: 1fr; gap: 2.5rem; } }
    .mission-text .section-heading { margin-top: 0.5rem; }
    .mission-text p { color: #555; line-height: 1.8; margin: 0 0 1rem; font-size: 1.05rem; }
    .feature-list { list-style: none; padding: 0; margin: 1.5rem 0 0; display: flex; flex-direction: column; gap: 0.7rem; }
    .feature-list li { display: flex; align-items: center; gap: 0.75rem; font-weight: 500; color: #333; font-size: 0.98rem; }
    .check { color: #2d7a56; font-weight: 700; }
    .mission-img-container {
      border-radius: 14px; overflow: hidden; height: 440px;
      background: linear-gradient(135deg, #1a4731, #0D2B1D);
    }
    .mission-img-container img { width: 100%; height: 100%; object-fit: cover; display: block; }
    .img-credit {
      margin-top: 0.5rem; font-size: 0.72rem; color: #aaa; text-align: right;
    }

    /* ── BRANCHES ────────────────────────────────────────── */
    .branches-section { background: #0D2B1D; padding: 3rem 2rem; }
    .branches-header { text-align: center; margin-bottom: 2rem; }
    .branches-grid {
      max-width: 900px; margin: 0 auto;
      display: flex; justify-content: center; align-items: center;
      flex-wrap: wrap; gap: 3rem;
    }
    .branch-item {
      display: flex; flex-direction: column; align-items: center; gap: 0.6rem;
      color: rgba(255,255,255,0.65); font-size: 0.8rem; font-weight: 500;
      letter-spacing: 0.05em; text-transform: uppercase;
      transition: color 0.2s;
    }
    .branch-item:hover { color: #C8A96E; }
    .branch-item img { width: 64px; height: 64px; object-fit: contain; filter: brightness(0.85) grayscale(0.3); transition: filter 0.2s; }
    .branch-item:hover img { filter: brightness(1) grayscale(0); }

    /* ── CAPABILITIES ────────────────────────────────────── */
    .capabilities-section { padding: 6rem 2rem; background: #fff; }
    .cap-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px,1fr)); gap: 1.5rem; }
    .cap-card {
      background: #f7f9f7; border: 1px solid #e5ebe5; border-radius: 12px;
      padding: 1.75rem; transition: all 0.3s ease;
    }
    .cap-card:hover {
      transform: translateY(-5px);
      box-shadow: 0 16px 48px rgba(13,43,29,0.1);
      border-color: #2d7a56;
      background: #fff;
    }
    .cap-icon { font-size: 2.2rem; margin-bottom: 1rem; line-height: 1; }
    .cap-card h3 { font-size: 1.05rem; font-weight: 700; color: #0D2B1D; margin: 0 0 0.5rem; }
    .cap-card p { color: #6b7280; font-size: 0.9rem; line-height: 1.6; margin: 0; }

    /* ── GALLERY ─────────────────────────────────────────── */
    .gallery-section { padding: 6rem 2rem; background: #0D2B1D; }
    .gallery-grid { display: grid; grid-template-columns: repeat(3,1fr); gap: 1.25rem; }
    @media (max-width: 768px) { .gallery-grid { grid-template-columns: 1fr; } }
    .gallery-item {
      position: relative; border-radius: 10px; overflow: hidden;
      height: 260px; cursor: pointer;
    }
    .gallery-item img {
      width: 100%; height: 100%; object-fit: cover;
      transition: transform 0.5s ease; display: block;
    }
    .gallery-item:hover img { transform: scale(1.07); }
    .gallery-overlay {
      position: absolute; bottom: 0; left: 0; right: 0;
      background: linear-gradient(transparent, rgba(0,0,0,0.78));
      padding: 2rem 1rem 1rem; color: #fff;
      transform: translateY(100%); transition: transform 0.3s ease;
    }
    .gallery-item:hover .gallery-overlay { transform: translateY(0); }
    .gallery-overlay p { font-size: 0.9rem; font-weight: 600; margin: 0 0 0.2rem; }
    .gallery-overlay small { font-size: 0.72rem; opacity: 0.65; }
    .gallery-note {
      text-align: center; margin-top: 1.5rem;
      font-size: 0.75rem; color: rgba(255,255,255,0.35); letter-spacing: 0.05em;
    }

    /* ── SECURITY ────────────────────────────────────────── */
    .security-section { padding: 6rem 2rem; background: #f7f9f7; }
    .security-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 5rem; align-items: center; }
    @media (max-width: 900px) { .security-grid { grid-template-columns: 1fr; gap: 3rem; } }
    .security-intro { color: #555; font-size: 1.05rem; line-height: 1.8; margin: 0 0 0.5rem; }
    .auth-methods { display: flex; flex-direction: column; gap: 1.75rem; margin-top: 2rem; }
    .auth-method { display: flex; gap: 1.25rem; align-items: flex-start; }
    .auth-icon {
      color: #2d7a56; flex-shrink: 0;
      background: rgba(45,122,86,0.1); border-radius: 10px;
      width: 52px; height: 52px; display: flex; align-items: center; justify-content: center;
    }
    .auth-method strong { color: #0D2B1D; font-size: 1rem; display: block; margin-bottom: 0.3rem; }
    .auth-method p { color: #6b7280; font-size: 0.9rem; margin: 0; line-height: 1.6; }
    .security-visual { display: flex; justify-content: center; align-items: center; }
    .shield-svg {
      width: min(280px, 100%);
      filter: drop-shadow(0 0 32px rgba(74,222,128,0.18));
    }
    .pulse-ring { animation: pulsate 3s ease-out infinite; }
    .r2 { animation-delay: 0.6s; }
    .r3 { animation-delay: 1.2s; }
    @keyframes pulsate {
      0% { opacity: 1; r: 80px; }
      100% { opacity: 0; r: 140px; }
    }

    /* ── CTA BANNER ──────────────────────────────────────── */
    .cta-section {
      padding: 5rem 2rem; text-align: center;
      background: linear-gradient(135deg, #1a4731, #0D2B1D);
    }
    .cta-container { max-width: 640px; margin: 0 auto; }
    .cta-container h2 { font-size: 2.4rem; font-weight: 700; color: #fff; margin: 0 0 1rem; }
    .cta-container p { color: rgba(255,255,255,0.7); font-size: 1.05rem; margin: 0 0 2rem; line-height: 1.7; }
    .cta-actions { display: flex; gap: 1rem; justify-content: center; flex-wrap: wrap; }
    .btn-ghost-dark {
      background: transparent; color: rgba(255,255,255,0.85);
      border: 1.5px solid rgba(255,255,255,0.35);
      padding: 0.9rem 2.4rem; border-radius: 8px;
      font-weight: 600; font-size: 1rem; text-decoration: none;
      font-family: 'Outfit', sans-serif; transition: border-color 0.2s, background 0.2s;
    }
    .btn-ghost-dark:hover { border-color: rgba(255,255,255,0.7); background: rgba(255,255,255,0.06); }

    /* ── FOOTER ──────────────────────────────────────────── */
    .home-footer { background: #060d09; padding: 3rem 2rem; }
    .footer-container { max-width: 1200px; margin: 0 auto; text-align: center; }
    .footer-top { margin-bottom: 1.5rem; }
    .footer-brand {
      font-size: 1.6rem; font-weight: 700; color: #fff; margin-bottom: 0.4rem;
      display: flex; align-items: center; justify-content: center; gap: 0.4rem;
    }
    .footer-top p { color: rgba(255,255,255,0.4); font-size: 0.9rem; margin: 0; }
    .footer-links {
      display: flex; justify-content: center; flex-wrap: wrap; gap: 1.75rem; margin-bottom: 1.5rem;
    }
    .footer-links a {
      color: rgba(255,255,255,0.5); text-decoration: none; font-size: 0.88rem; transition: color 0.2s;
    }
    .footer-links a:hover { color: #C8A96E; }
    .footer-notice {
      font-size: 0.75rem; color: rgba(255,255,255,0.25);
      max-width: 720px; margin: 0 auto 1rem; line-height: 1.65;
      text-transform: uppercase; letter-spacing: 0.03em;
    }
    .footer-copy { font-size: 0.78rem; color: rgba(255,255,255,0.2); margin: 0; }
  `],
})
export class HomeComponent {
  private readonly router = inject(Router);

  readonly isScrolled = signal(false);
  readonly menuOpen = signal(false);
  readonly year = new Date().getFullYear();

  @HostListener('window:scroll')
  onScroll(): void {
    this.isScrolled.set(window.scrollY > 40);
  }

  toggleMenu(): void { this.menuOpen.update(v => !v); }
  closeMenu(): void { this.menuOpen.set(false); }
  goToLogin(): void { this.router.navigateByUrl('/login'); }

  onImgError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.style.display = 'none';
  }

  readonly stats = [
    { value: '14+', label: 'Service Modules' },
    { value: '3', label: 'Auth Methods' },
    { value: 'Real-Time', label: 'Supply Tracking' },
    { value: 'Multi-Site', label: 'Deployment Ready' },
  ];

  readonly branches = [
    {
      name: 'Army',
      seal: 'https://upload.wikimedia.org/wikipedia/commons/thumb/9/91/Seal_of_the_United_States_Department_of_the_Army.svg/200px-Seal_of_the_United_States_Department_of_the_Army.svg.png',
    },
    {
      name: 'Navy',
      seal: 'https://upload.wikimedia.org/wikipedia/commons/thumb/5/5c/Seal_of_the_United_States_Department_of_the_Navy.svg/200px-Seal_of_the_United_States_Department_of_the_Navy.svg.png',
    },
    {
      name: 'Air Force',
      seal: 'https://upload.wikimedia.org/wikipedia/commons/thumb/9/96/Seal_of_the_Department_of_the_Air_Force.svg/200px-Seal_of_the_Department_of_the_Air_Force.svg.png',
    },
    {
      name: 'Marine Corps',
      seal: 'https://upload.wikimedia.org/wikipedia/commons/thumb/3/39/United_States_Marine_Corps_Logo.svg/200px-United_States_Marine_Corps_Logo.svg.png',
    },
    {
      name: 'Coast Guard',
      seal: 'https://upload.wikimedia.org/wikipedia/commons/thumb/5/5b/Seal_of_the_United_States_Coast_Guard.svg/200px-Seal_of_the_United_States_Coast_Guard.svg.png',
    },
  ];

  readonly capabilities: Capability[] = [
    { icon: '📦', title: 'Issue & Receipt', desc: 'Track all supply issuances and receipts with real-time audit trails and digital signatures.' },
    { icon: '🏭', title: 'Storehouse Management', desc: 'Full warehouse inventory control, bin management, and capacity planning.' },
    { icon: '🛒', title: 'Procurement', desc: 'Automated purchase requisitions, vendor management, and approval workflows.' },
    { icon: '🔧', title: 'Repair & Maintenance', desc: 'Equipment lifecycle tracking, maintenance scheduling, and work order management.' },
    { icon: '📊', title: 'Budget Planning', desc: 'Budget allocation, expenditure tracking, and financial reporting across all units.' },
    { icon: '📋', title: 'Reporting', desc: 'Comprehensive real-time dashboards and exportable reports for command oversight.' },
    { icon: '🚔', title: 'Traffic & Security', desc: 'Gate access management, vehicle logging, and security checkpoint integration.' },
    { icon: '♻️', title: 'Salvage', desc: 'Manage condemned equipment, disposal processes, and salvage valuation.' },
    { icon: '📄', title: 'Document Share', desc: 'Secure distribution of supply orders, manifests, and technical documentation.' },
    { icon: '📡', title: 'Communication', desc: 'Internal messaging and notification system for supply chain stakeholders.' },
    { icon: '🏛️', title: 'Administration', desc: 'Unit management, organizational hierarchy, and configuration of system settings.' },
    { icon: '👤', title: 'IAM', desc: 'Identity and access management with role-based permissions and audit logging.' },
    { icon: '🔍', title: 'Inspectorate', desc: 'Inspection scheduling, checklist management, and compliance reporting.' },
    { icon: '📈', title: 'Dashboard', desc: 'Executive overview of all supply chain KPIs, alerts, and operational status.' },
  ];

  readonly galleryImages: GalleryImage[] = [
    {
      src: 'https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/USMC-120130-M-AU422-173.jpg/800px-USMC-120130-M-AU422-173.jpg',
      alt: 'U.S. Marines supply operations',
      caption: 'Marine Corps Supply Operations',
      credit: 'Public Domain · U.S. Marine Corps',
      fallbackColor: 'linear-gradient(135deg,#1a3a2a,#0f2318)',
    },
    {
      src: 'https://upload.wikimedia.org/wikipedia/commons/thumb/3/3c/USAF_Aircraft_on_Flightline.jpg/800px-USAF_Aircraft_on_Flightline.jpg',
      alt: 'U.S. Air Force logistics',
      caption: 'Air Force Logistics Support',
      credit: 'Public Domain · U.S. Air Force',
      fallbackColor: 'linear-gradient(135deg,#1a2a3a,#0f1823)',
    },
    {
      src: 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a0/Defense.gov_photo_essay_100908-A-3108M-002.jpg/800px-Defense.gov_photo_essay_100908-A-3108M-002.jpg',
      alt: 'U.S. Army supply depot operations',
      caption: 'Army Supply Depot Operations',
      credit: 'Public Domain · U.S. Army',
      fallbackColor: 'linear-gradient(135deg,#2a2a1a,#18180f)',
    },
  ];
}
