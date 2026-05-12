import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { authService } from '../services/auth.service';
import { apiService } from '../services/api.service';
import { useTranslation } from '../i18n/I18nContext';
import type { SupportedLanguage } from '../i18n/I18nContext';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5091';
const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID || '';

// ─── Brand ────────────────────────────────────────────────────────────────────

const BrandMark = ({ size = 36 }: { size?: number }) => (
  <svg width={size} height={size} viewBox="0 0 36 36" fill="none">
    <rect width="36" height="36" rx="10" fill="#6366f1" />
    <circle cx="18" cy="24" r="4" fill="white" />
    <circle cx="11" cy="24" r="2.8" fill="white" fillOpacity="0.5" />
    <circle cx="25" cy="24" r="2.8" fill="white" fillOpacity="0.5" />
    <path d="M10 19c0-4.418 3.582-8 8-8s8 3.582 8 8" stroke="white" strokeWidth="2.5" strokeLinecap="round" />
  </svg>
);

// ─── Icons ────────────────────────────────────────────────────────────────────

const GoogleIcon = () => (
  <svg width="17" height="17" viewBox="0 0 18 18" fill="none">
    <path d="M17.64 9.205c0-.639-.057-1.252-.164-1.841H9v3.481h4.844a4.14 4.14 0 01-1.796 2.716v2.259h2.908C16.658 14.253 17.64 11.945 17.64 9.205z" fill="#4285F4" />
    <path d="M9 18c2.43 0 4.467-.806 5.956-2.18l-2.908-2.259c-.806.54-1.837.86-3.048.86-2.344 0-4.328-1.584-5.036-3.711H.957v2.332A8.997 8.997 0 009 18z" fill="#34A853" />
    <path d="M3.964 10.71A5.41 5.41 0 013.682 9c0-.593.102-1.17.282-1.71V4.958H.957A8.996 8.996 0 000 9c0 1.452.348 2.827.957 4.042l3.007-2.332z" fill="#FBBC05" />
    <path d="M9 3.58c1.321 0 2.508.454 3.44 1.345l2.582-2.58C13.463.891 11.426 0 9 0A8.997 8.997 0 00.957 4.958L3.964 7.29C4.672 5.163 6.656 3.58 9 3.58z" fill="#EA4335" />
  </svg>
);

const EyeOnIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
    <circle cx="12" cy="12" r="3" />
  </svg>
);

const EyeOffIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24" />
    <line x1="1" y1="1" x2="23" y2="23" />
  </svg>
);

// ─── Left panel decoration ────────────────────────────────────────────────────

const PanelArt = () => (
  <svg width="480" height="480" viewBox="0 0 480 480" fill="none" style={{ position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -52%)', pointerEvents: 'none' }}>
    {[220, 175, 130, 88, 48].map((r, i) => (
      <circle key={r} cx="240" cy="240" r={r} stroke="#6366f1" strokeWidth="0.6" strokeOpacity={0.18 + i * 0.04} />
    ))}
    {[
      [52, 108], [428, 148], [72, 352], [412, 332],
      [240, 42], [240, 438], [145, 78], [335, 78],
      [145, 402], [335, 402],
    ].map(([cx, cy], i) => (
      <circle key={i} cx={cx} cy={cy} r="2.5" fill="#6366f1" fillOpacity="0.35" />
    ))}
    <line x1="240" y1="20" x2="240" y2="460" stroke="#6366f1" strokeWidth="0.4" strokeOpacity="0.1" />
    <line x1="20" y1="240" x2="460" y2="240" stroke="#6366f1" strokeWidth="0.4" strokeOpacity="0.1" />
  </svg>
);

// ─── Language selector ────────────────────────────────────────────────────────

const LANG_OPTIONS: { value: SupportedLanguage | 'auto'; label: string }[] = [
  { value: 'auto',  label: '🌐 Auto' },
  { value: 'es',    label: 'ES' },
  { value: 'en',    label: 'EN' },
  { value: 'pt-BR', label: 'PT' },
];

// ─── Component ────────────────────────────────────────────────────────────────

const LoginPage = () => {
  const navigate = useNavigate();
  const { refreshAuth } = useAuth();
  const { t, language, autoDetect, setLanguage } = useTranslation();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  useEffect(() => {
    if (authService.isAuthenticated()) navigate('/chat');
  }, [navigate]);

  useEffect(() => {
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => {
      const google = (window as any).google;
      if (google && GOOGLE_CLIENT_ID) {
        google.accounts.id.initialize({
          client_id: GOOGLE_CLIENT_ID,
          callback: handleGoogleCallback,
        });
        google.accounts.id.renderButton(
          document.getElementById('google-signin-button'),
          { theme: 'outline', size: 'large', width: '100%', text: 'continue_with', shape: 'rectangular' }
        );
      }
    };
    document.body.appendChild(script);
    return () => { if (document.body.contains(script)) document.body.removeChild(script); };
  }, []);

  const handleGoogleCallback = async (response: any) => {
    if (!response.credential) { setError(t('login.googleError')); return; }
    try {
      setIsLoading(true);
      setError('');
      const api = apiService.getAxiosInstance();
      const res = await api.post(`${API_URL}/api/auth/google`, { googleToken: response.credential });
      const { accessToken, refreshToken, user } = res.data;
      localStorage.setItem('accessToken', accessToken);
      localStorage.setItem('refreshToken', refreshToken);
      localStorage.setItem('user', JSON.stringify(user));
      refreshAuth();
      navigate('/chat');
    } catch (err: any) {
      setError(err.response?.data?.message || t('login.googleError'));
      setIsLoading(false);
    }
  };

  const handleEmailSignIn = (e: React.FormEvent) => {
    e.preventDefault();
    setError(t('login.emailComingSoon'));
  };

  const handleDevLogin = () => {
    const mockUser = { id: '123', email: 'user@opn-chat.com', nickname: 'TestUser', avatarUrl: '', bio: '', status: 'Online', lastSeen: new Date().toISOString() };
    localStorage.setItem('accessToken', 'mock-access-token');
    localStorage.setItem('refreshToken', 'mock-refresh-token');
    localStorage.setItem('user', JSON.stringify(mockUser));
    refreshAuth();
    navigate('/chat');
  };

  return (
    <div style={{ minHeight: '100vh', display: 'flex', fontFamily: '-apple-system, BlinkMacSystemFont, "Inter", "Segoe UI", sans-serif' }}>

      {/* ── Left panel ── */}
      <div
        className="hidden lg:flex"
        style={{
          width: '44%', flexDirection: 'column', justifyContent: 'space-between',
          padding: '44px 52px', background: '#09090b', position: 'relative', overflow: 'hidden',
        }}
      >
        <div style={{
          position: 'absolute', top: '42%', left: '50%',
          transform: 'translate(-50%, -50%)',
          width: 380, height: 380,
          background: 'radial-gradient(circle, rgba(99,102,241,0.22) 0%, transparent 68%)',
          borderRadius: '50%', pointerEvents: 'none',
        }} />

        <PanelArt />

        <div style={{ display: 'flex', alignItems: 'center', gap: 10, position: 'relative', zIndex: 1 }}>
          <BrandMark size={34} />
          <span style={{ color: '#fafafa', fontSize: 17, fontWeight: 600, letterSpacing: '-0.02em' }}>opnchat</span>
        </div>

        <div style={{ position: 'relative', zIndex: 1 }}>
          <p style={{ color: '#52525b', fontSize: 12, marginBottom: 14, letterSpacing: '0.06em', textTransform: 'uppercase', fontWeight: 500 }}>
            {t('login.tagline1')}
          </p>
          <h2 style={{ color: '#fafafa', fontSize: 30, fontWeight: 600, lineHeight: 1.25, letterSpacing: '-0.03em', margin: 0, whiteSpace: 'pre-line' }}>
            {t('login.tagline2')}
          </h2>
          <p style={{ color: '#71717a', fontSize: 14, marginTop: 14, lineHeight: 1.65, whiteSpace: 'pre-line' }}>
            {t('login.tagline3')}
          </p>
        </div>
      </div>

      {/* ── Right panel ── */}
      <div style={{
        flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center',
        background: '#ffffff', padding: '40px 24px',
      }}>
        <div style={{ width: '100%', maxWidth: 368 }}>

          {/* Mobile brand */}
          <div className="flex lg:hidden" style={{ justifyContent: 'center', alignItems: 'center', gap: 10, marginBottom: 36 }}>
            <BrandMark size={30} />
            <span style={{ fontSize: 17, fontWeight: 600, color: '#111', letterSpacing: '-0.02em' }}>opnchat</span>
          </div>

          {/* Heading */}
          <div style={{ marginBottom: 28 }}>
            <h1 style={{ fontSize: 22, fontWeight: 600, color: '#111827', margin: '0 0 5px', letterSpacing: '-0.025em' }}>
              {t('login.welcome')}
            </h1>
            <p style={{ fontSize: 14, color: '#9ca3af', margin: 0, lineHeight: 1.5 }}>
              {t('login.subtitle')}
            </p>
          </div>

          {/* Email + password form */}
          <form onSubmit={handleEmailSignIn} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
              <label style={{ fontSize: 13, fontWeight: 500, color: '#374151' }}>{t('common.email')}</label>
              <input
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder="you@company.com"
                autoComplete="email"
                style={inputStyle}
                onFocus={e => applyFocus(e.currentTarget)}
                onBlur={e => removeFocus(e.currentTarget)}
              />
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <label style={{ fontSize: 13, fontWeight: 500, color: '#374151' }}>{t('common.password')}</label>
                <a
                  href="#"
                  style={{ fontSize: 12, color: '#6366f1', textDecoration: 'none', fontWeight: 500 }}
                  onMouseEnter={e => (e.currentTarget.style.textDecoration = 'underline')}
                  onMouseLeave={e => (e.currentTarget.style.textDecoration = 'none')}
                >
                  {t('login.forgotPassword')}
                </a>
              </div>
              <div style={{ position: 'relative' }}>
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  placeholder="••••••••"
                  autoComplete="current-password"
                  style={{ ...inputStyle, paddingRight: 40 }}
                  onFocus={e => applyFocus(e.currentTarget)}
                  onBlur={e => removeFocus(e.currentTarget)}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(v => !v)}
                  style={{
                    position: 'absolute', right: 11, top: '50%', transform: 'translateY(-50%)',
                    background: 'none', border: 'none', cursor: 'pointer',
                    color: '#9ca3af', padding: 0, display: 'flex', alignItems: 'center',
                    transition: 'color 0.15s',
                  }}
                  onMouseEnter={e => (e.currentTarget.style.color = '#6b7280')}
                  onMouseLeave={e => (e.currentTarget.style.color = '#9ca3af')}
                >
                  {showPassword ? <EyeOffIcon /> : <EyeOnIcon />}
                </button>
              </div>
            </div>

            {error && (
              <div style={{
                padding: '10px 13px', borderRadius: 8,
                background: '#fef2f2', border: '1px solid #fecaca',
                fontSize: 13, color: '#dc2626', lineHeight: 1.4,
              }}>
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={isLoading}
              style={{
                background: isLoading ? '#a5b4fc' : '#6366f1',
                color: 'white', border: 'none', borderRadius: 8,
                padding: '10px 18px', fontSize: 14, fontWeight: 600,
                cursor: isLoading ? 'not-allowed' : 'pointer',
                width: '100%', letterSpacing: '-0.01em',
                transition: 'background 0.15s, box-shadow 0.15s',
                boxShadow: '0 1px 3px rgba(99,102,241,0.25)',
              }}
              onMouseEnter={e => { if (!isLoading) { e.currentTarget.style.background = '#4f46e5'; e.currentTarget.style.boxShadow = '0 4px 12px rgba(99,102,241,0.35)'; } }}
              onMouseLeave={e => { if (!isLoading) { e.currentTarget.style.background = '#6366f1'; e.currentTarget.style.boxShadow = '0 1px 3px rgba(99,102,241,0.25)'; } }}
            >
              {isLoading ? t('login.signingIn') : t('login.signIn')}
            </button>
          </form>

          {/* Divider */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, margin: '18px 0' }}>
            <div style={{ flex: 1, height: 1, background: '#f3f4f6' }} />
            <span style={{ fontSize: 12, color: '#d1d5db', fontWeight: 500, letterSpacing: '0.02em' }}>OR</span>
            <div style={{ flex: 1, height: 1, background: '#f3f4f6' }} />
          </div>

          <div id="google-signin-button" style={{ width: '100%', minHeight: 44 }} />

          <p style={{ textAlign: 'center', fontSize: 13, color: '#9ca3af', marginTop: 22 }}>
            {t('login.noAccount')}{' '}
            <a
              href="#"
              style={{ color: '#6366f1', fontWeight: 500, textDecoration: 'none' }}
              onMouseEnter={e => (e.currentTarget.style.textDecoration = 'underline')}
              onMouseLeave={e => (e.currentTarget.style.textDecoration = 'none')}
            >
              {t('login.signUp')}
            </a>
          </p>

          {/* Dev mode */}
          <div style={{ marginTop: 28, paddingTop: 18, borderTop: '1px solid #f9fafb' }}>
            <p style={{ textAlign: 'center', fontSize: 11, color: '#e5e7eb', marginBottom: 8, letterSpacing: '0.05em', textTransform: 'uppercase' }}>
              {t('login.devMode')}
            </p>
            <button
              onClick={handleDevLogin}
              style={{
                width: '100%', background: '#f9fafb',
                border: '1px dashed #e5e7eb', borderRadius: 8,
                padding: '8px 16px', fontSize: 12, color: '#9ca3af',
                cursor: 'pointer', fontWeight: 500,
                transition: 'border-color 0.15s, color 0.15s',
              }}
              onMouseEnter={e => { e.currentTarget.style.borderColor = '#d1d5db'; e.currentTarget.style.color = '#6b7280'; }}
              onMouseLeave={e => { e.currentTarget.style.borderColor = '#e5e7eb'; e.currentTarget.style.color = '#9ca3af'; }}
            >
              {t('login.continueAsTest')}
            </button>
          </div>

          {/* Language selector */}
          <div style={{ marginTop: 20, display: 'flex', gap: 6, justifyContent: 'center', flexWrap: 'wrap' }}>
            {LANG_OPTIONS.map(opt => {
              const isActive = opt.value === 'auto' ? autoDetect : (!autoDetect && language === opt.value);
              return (
                <button
                  key={opt.value}
                  onClick={() => setLanguage(opt.value as SupportedLanguage | 'auto')}
                  style={{
                    padding: '3px 11px', borderRadius: 999, fontSize: 11, cursor: 'pointer',
                    fontWeight: isActive ? 600 : 400,
                    background: isActive ? '#6366f1' : 'transparent',
                    color: isActive ? '#fff' : '#9ca3af',
                    border: `1px solid ${isActive ? '#6366f1' : '#e5e7eb'}`,
                    transition: 'all 0.15s',
                  }}
                >
                  {opt.label}
                </button>
              );
            })}
          </div>

        </div>
      </div>

    </div>
  );
};

// ─── Helpers ──────────────────────────────────────────────────────────────────

const inputStyle: React.CSSProperties = {
  width: '100%', boxSizing: 'border-box',
  border: '1px solid #e5e7eb', borderRadius: 8,
  padding: '10px 13px', fontSize: 14, color: '#111827',
  outline: 'none', background: 'white',
  transition: 'border-color 0.15s, box-shadow 0.15s',
};

const applyFocus = (el: HTMLInputElement) => {
  el.style.borderColor = '#6366f1';
  el.style.boxShadow = '0 0 0 3px rgba(99,102,241,0.12)';
};

const removeFocus = (el: HTMLInputElement) => {
  el.style.borderColor = '#e5e7eb';
  el.style.boxShadow = 'none';
};

export default LoginPage;
