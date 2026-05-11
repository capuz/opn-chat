import { useTranslation } from '../i18n';

interface RewardModalProps {
  type: 'room' | 'nickname' | 'boost';
  isDark: boolean;
  onWatchAd: () => void;
  onUpgrade?: () => void;
  onClose: () => void;
  isWatchingAd: boolean;
}

export function RewardModal({ type, isDark: _isDark, onWatchAd, onUpgrade, onClose, isWatchingAd }: RewardModalProps) {
  const { t } = useTranslation();

  const subtitle =
    type === 'room'     ? t('monetize.usedFreeRoom') :
    type === 'nickname' ? t('monetize.usedFreeNickChange') :
                          t('monetize.usedFreeBoost');

  return (
    <div style={{
      position: 'fixed', inset: 0,
      background: 'rgba(0,0,0,0.6)',
      backdropFilter: 'blur(6px)',
      zIndex: 60,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
    }} onClick={onClose}>
      <div style={{
        width: 380, borderRadius: 12, padding: 28,
        background: 'var(--ch-modal-bg)',
        border: '1px solid var(--ch-border-2)',
        display: 'flex', flexDirection: 'column', gap: 16,
        animation: 'fadeSlideDown 0.18s ease-out',
      }} onClick={e => e.stopPropagation()}>

        {/* icon */}
        <div style={{ textAlign: 'center', fontSize: 32, lineHeight: 1 }}>🔒</div>

        {/* title */}
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: 15, fontWeight: 700, color: 'var(--ch-text)', marginBottom: 6 }}>
            {t('monetize.limitReached')}
          </div>
          <div style={{ fontSize: 12, color: 'var(--ch-text-2)', lineHeight: 1.5 }}>
            {subtitle}
          </div>
        </div>

        {/* buttons */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>

          {/* Watch Ad */}
          <button
            onClick={isWatchingAd ? undefined : onWatchAd}
            disabled={isWatchingAd}
            style={{
              padding: '10px 16px', borderRadius: 7, border: 'none', cursor: isWatchingAd ? 'default' : 'pointer',
              background: 'var(--ch-btn-active)', color: 'var(--ch-btn-text)',
              fontSize: 13, fontWeight: 600, fontFamily: 'inherit',
              opacity: isWatchingAd ? 0.7 : 1, transition: 'opacity 0.15s',
            }}
          >
            {isWatchingAd ? t('monetize.watchingAd') : `📺 ${t('monetize.watchAd')}`}
          </button>

          {/* Mock ad card */}
          {isWatchingAd && (
            <div style={{
              borderRadius: 8, overflow: 'hidden',
              border: '1px solid var(--ch-border)',
              background: 'var(--ch-bg-3)',
            }}>
              <div style={{
                padding: '4px 10px',
                background: 'var(--ch-border)',
                fontSize: 10, color: 'var(--ch-text-3)',
                letterSpacing: '0.06em',
              }}>
                📢 PUBLICIDAD · opn-chat sponsors
              </div>
              <div style={{ padding: '10px 12px', display: 'flex', gap: 12, alignItems: 'center' }}>
                <div style={{
                  width: 40, height: 40, borderRadius: 8, flexShrink: 0,
                  background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontSize: 20,
                }}>
                  🛡️
                </div>
       <div align="center">

      <script async src="https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-1234567890123456" crossorigin="anonymous"></script>

<ins class="adsbygoogle"
style="display:inline-block;width:728px;height:90px"
data-ad-client="ca-pub-1234567890123456"
data-ad-slot="1234567890"></ins>
<script>
(adsbygoogle = window.adsbygoogle || []).push({});
</script>

      </div> 
              </div>
            </div>
          )}

          {/* Ad progress bar */}
          {isWatchingAd && (
            <div style={{ height: 3, borderRadius: 2, background: 'var(--ch-border-2)', overflow: 'hidden' }}>
              <div style={{
                height: '100%', borderRadius: 2,
                background: 'var(--ch-accent)',
                animation: 'ad-progress 2s linear forwards',
              }} />
            </div>
          )}

          {/* Upgrade Premium (room / boost only) */}
          {(type === 'room' || type === 'boost') && onUpgrade && (
            <button
              onClick={onUpgrade}
              style={{
                padding: '10px 16px', borderRadius: 7, cursor: 'pointer',
                background: 'transparent',
                border: '1px solid var(--ch-border-2)',
                color: 'var(--ch-text)', fontSize: 13, fontWeight: 500,
                fontFamily: 'inherit', transition: 'border-color 0.15s',
              }}
            >
              ⭐ {t('monetize.upgradePremium')}
            </button>
          )}

          {/* Cancel */}
          <button
            onClick={onClose}
            style={{
              padding: '8px 16px', borderRadius: 7, cursor: 'pointer',
              background: 'transparent', border: 'none',
              color: 'var(--ch-text-2)', fontSize: 12,
              fontFamily: 'inherit',
            }}
          >
            {t('common.cancel')}
          </button>
        </div>
      </div>
    </div>
  );
}
