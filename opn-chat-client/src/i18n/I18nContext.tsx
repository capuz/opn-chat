import { createContext, useCallback, useContext, useEffect, useState } from 'react';

// ── Locale files ─────────────────────────────────────────────────────────────
import enCommon    from '../locales/en/common.json';
import enChat      from '../locales/en/chat.json';
import enAdmin     from '../locales/en/admin.json';
import enCommands  from '../locales/en/commands.json';
import enMonetize  from '../locales/en/monetize.json';
import esCommon    from '../locales/es/common.json';
import esChat      from '../locales/es/chat.json';
import esAdmin     from '../locales/es/admin.json';
import esCommands  from '../locales/es/commands.json';
import esMonetize  from '../locales/es/monetize.json';
import ptCommon    from '../locales/pt-BR/common.json';
import ptChat      from '../locales/pt-BR/chat.json';
import ptAdmin     from '../locales/pt-BR/admin.json';
import ptCommands  from '../locales/pt-BR/commands.json';
import ptMonetize  from '../locales/pt-BR/monetize.json';

// ── Types ─────────────────────────────────────────────────────────────────────
export type SupportedLanguage = 'en' | 'es' | 'pt-BR';

const SUPPORTED: SupportedLanguage[] = ['en', 'es', 'pt-BR'];
const STORAGE_KEY = 'opnchat-language';

// ── Translation table ─────────────────────────────────────────────────────────
const TRANSLATIONS: Record<SupportedLanguage, Record<string, string>> = {
  en:     { ...enCommon,  ...enChat,  ...enAdmin,  ...enCommands,  ...enMonetize  },
  es:     { ...esCommon,  ...esChat,  ...esAdmin,  ...esCommands,  ...esMonetize  },
  'pt-BR':{ ...ptCommon,  ...ptChat,  ...ptAdmin,  ...ptCommands,  ...ptMonetize  },
};

// ── Module-level sync (for standalone t() outside React) ──────────────────────
let _currentLang: SupportedLanguage = 'en';
export const setModuleLang = (l: SupportedLanguage): void => { _currentLang = l; };
export const getModuleLang = (): SupportedLanguage => _currentLang;

export function standaloneT(key: string, params?: Record<string, string | number>): string {
  let text = TRANSLATIONS[_currentLang]?.[key] ?? TRANSLATIONS.en[key] ?? key;
  if (params) {
    for (const [k, v] of Object.entries(params))
      text = text.replaceAll(`{${k}}`, String(v));
  }
  return text;
}

// ── Locale detection ──────────────────────────────────────────────────────────
function detectLocale(): { locale: string; language: SupportedLanguage; timezone: string } {
  const locale   = navigator.language || 'en';
  const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  const base     = locale.split('-')[0].toLowerCase();

  let language: SupportedLanguage = 'en';
  if (locale.toLowerCase().startsWith('pt')) language = 'pt-BR';
  else if (base === 'es') language = 'es';

  return { locale, language, timezone };
}

// ── Context shape ─────────────────────────────────────────────────────────────
interface I18nContextType {
  language: SupportedLanguage;
  locale: string;
  timezone: string;
  autoDetect: boolean;
  setLanguage: (lang: SupportedLanguage | 'auto') => void;
  t: (key: string, params?: Record<string, string | number>) => string;
  formatDate: (date: string | Date, opts?: Intl.DateTimeFormatOptions) => string;
}

const I18nContext = createContext<I18nContextType>({
  language: 'en',
  locale: 'en',
  timezone: 'UTC',
  autoDetect: true,
  setLanguage: () => {},
  t: (key) => key,
  formatDate: (date) => String(date),
});

// ── Provider ──────────────────────────────────────────────────────────────────
export function I18nProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<{
    language: SupportedLanguage;
    locale: string;
    timezone: string;
    autoDetect: boolean;
  }>(() => {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored && stored !== 'auto' && SUPPORTED.includes(stored as SupportedLanguage)) {
      return {
        language: stored as SupportedLanguage,
        locale: navigator.language || stored,
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        autoDetect: false,
      };
    }
    const detected = detectLocale();
    return { ...detected, autoDetect: true };
  });

  // Keep module-level in sync so standalone t() works
  useEffect(() => {
    setModuleLang(state.language);
  }, [state.language]);

  const setLanguage = useCallback((lang: SupportedLanguage | 'auto') => {
    localStorage.setItem(STORAGE_KEY, lang);
    if (lang === 'auto') {
      const detected = detectLocale();
      setState({ ...detected, autoDetect: true });
    } else {
      setState(s => ({ ...s, language: lang, autoDetect: false }));
    }
  }, []);

  const t = useCallback((key: string, params?: Record<string, string | number>): string => {
    let text = TRANSLATIONS[state.language]?.[key] ?? TRANSLATIONS.en[key] ?? key;
    if (params) {
      for (const [k, v] of Object.entries(params))
        text = text.replaceAll(`{${k}}`, String(v));
    }
    return text;
  }, [state.language]);

  const formatDate = useCallback(
    (date: string | Date, opts?: Intl.DateTimeFormatOptions) =>
      new Date(date).toLocaleString(state.locale, opts),
    [state.locale],
  );

  return (
    <I18nContext.Provider value={{ ...state, setLanguage, t, formatDate }}>
      {children}
    </I18nContext.Provider>
  );
}

export const useTranslation = (): I18nContextType => useContext(I18nContext);
