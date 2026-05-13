import { useTranslation } from '../i18n';

interface RewardModalProps {
  type: 'room' | 'nickname' | 'boost';
  onWatchAd: () => void;
  onUpgrade?: () => void;
  onClose: () => void;
  isWatchingAd: boolean;
  adFailed?: boolean;
}

const TYPE_CONFIG = {
  room:     { icon: '🏠', showUpgrade: true  },
  nickname: { icon: '✏️', showUpgrade: false },
  boost:    { icon: '⚡', showUpgrade: true  },
} satisfies Record<string, { icon: string; showUpgrade: boolean }>;

export function RewardModal({ type, onWatchAd, onUpgrade, onClose, isWatchingAd, adFailed }: RewardModalProps) {
  const { t } = useTranslation();
  const { icon, showUpgrade } = TYPE_CONFIG[type];

  const subtitle =
    type === 'room'     ? t('monetize.usedFreeRoom') :
    type === 'nickname' ? t('monetize.usedFreeNickChange') :
                          t('monetize.usedFreeBoost');

  return (
    <div
      style={{
        position: 'fixed', inset: 0,
        background: 'rgba(0,0,0,0.55)',
        backdropFilter: 'blur(8px)',
        zIndex: 60,
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        padding: '16px',
      }}
      onClick={onClose}
    >
      <div
        style={{
          width: '100%', maxWidth: 360,
          borderRadius: 16, padding: '28px 24px',
          background: 'var(--ch-modal-bg)',
          border: '1px solid var(--ch-border-2)',
          display: 'flex', flexDirection: 'column', gap: 20,
          animation: 'fadeSlideDown 0.2s ease-out',
          boxShadow: '0 24px 64px rgba(0,0,0,0.35)',
        }}
        onClick={e => e.stopPropagation()}
      >
        {/* Header */}
        <div style={{ textAlign: 'center', display: 'flex', flexDirection: 'column', gap: 8, alignItems: 'center' }}>
          <div style={{
            width: 56, height: 56, borderRadius: 14,
            background: 'var(--ch-accent-dim)',
            border: '1px solid var(--ch-border-2)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 26, lineHeight: 1,
            animation: adFailed ? 'shake 0.4s ease-out' : undefined,
          }}>
            {adFailed ? '⚠️' : icon}
          </div>

          <div>
            <div style={{ fontSize: 15, fontWeight: 700, color: 'var(--ch-text)', marginBottom: 4 }}>
              {t('monetize.limitReached')}
            </div>
            <div style={{ fontSize: 12, color: 'var(--ch-text-2)', lineHeight: 1.6, maxWidth: 280 }}>
              {adFailed ? t('monetize.adNotCompleted') : subtitle}
            </div>
          </div>
        </div>

        {/* Ad action area */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>

          {isWatchingAd ? (
            /* ── Watching state ── */
            <div style={{
              padding: '14px 16px',
              borderRadius: 10,
              background: 'var(--ch-bg-2)',
              border: '1px solid var(--ch-border)',
              display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10,
              animation: 'fadeSlideUp 0.15s ease-out',
            }}>
              {/* Spinner */}
              <div style={{
                width: 22, height: 22,
                borderRadius: '50%',
                border: '2.5px solid var(--ch-border-2)',
                borderTopColor: 'var(--ch-accent)',
                animation: 'spin 0.75s linear infinite',
              }} />
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--ch-text)', marginBottom: 2 }}>
                  {t('monetize.watchingAd')}
                </div>
                <div style={{ fontSize: 11, color: 'var(--ch-text-2)' }}>
                  ~30s
                </div>
              </div>
              {/* Progress bar */}
              <div style={{
                width: '100%', height: 3, borderRadius: 2,
                background: 'var(--ch-border)',
                overflow: 'hidden',
              }}>
                <div style={{
                  height: '100%',
                  background: 'var(--ch-accent)',
                  animation: 'ad-progress 30s linear forwards',
                }} />
              </div>
            </div>
          ) : (
            /* ── Idle / retry state ── */
            <button
              onClick={onWatchAd}
              style={{
                padding: '12px 16px', borderRadius: 10, border: 'none',
                cursor: 'pointer',
                background: 'var(--ch-btn-active)', color: 'var(--ch-btn-text)',
                fontSize: 13, fontWeight: 600, fontFamily: 'inherit',
                display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6,
                transition: 'opacity 0.15s',
              }}
              onMouseEnter={e => (e.currentTarget.style.opacity = '0.88')}
              onMouseLeave={e => (e.currentTarget.style.opacity = '1')}
            >
              {adFailed ? '🔄' : '📺'}
              {adFailed ? t('monetize.watchAdRetry') : t('monetize.watchAd')}
            </button>
          )}

          {/* Upgrade Premium */}
          {showUpgrade && onUpgrade && !isWatchingAd && (
            <button
              onClick={onUpgrade}
              style={{
                padding: '10px 16px', borderRadius: 10, cursor: 'pointer',
                background: 'transparent',
                border: '1px solid var(--ch-border-2)',
                color: 'var(--ch-text)', fontSize: 13, fontWeight: 500,
                fontFamily: 'inherit',
                display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6,
                transition: 'border-color 0.15s',
              }}
              onMouseEnter={e => (e.currentTarget.style.borderColor = 'var(--ch-accent)')}
              onMouseLeave={e => (e.currentTarget.style.borderColor = 'var(--ch-border-2)')}
            >
              ⭐ {t('monetize.upgradePremium')}
            </button>
          )}

          {/* Cancel */}
          <button
            onClick={isWatchingAd ? undefined : onClose}
            disabled={isWatchingAd}
            style={{
              padding: '8px 16px', borderRadius: 8, cursor: isWatchingAd ? 'default' : 'pointer',
              background: 'transparent', border: 'none',
              color: isWatchingAd ? 'var(--ch-text-3)' : 'var(--ch-text-2)',
              fontSize: 12, fontFamily: 'inherit',
              transition: 'color 0.15s',
            }}
          >
            {t('common.cancel')}
          </button>
        </div>
      </div>
    </div>
  );
}
