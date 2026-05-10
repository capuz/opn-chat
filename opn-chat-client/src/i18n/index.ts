export type Locale = 'en';

const translations: Record<Locale, Record<string, string>> = {
  en: {
    you:             'You',
    online:          'online',
    signOut:         'sign out',
    global:          'global',
    send:            'Send',
    noMessages:      'No messages yet. Say hello! 👋',
    typeMessage:     'Type a message...',
    messageName:     'Message {name}...',
    noConversations: 'No conversations yet',
    rooms:           'Rooms',
    newRoom:         'New room',
    privates:        'Privates',
    onlineSection:   'Online',
    active:          'Active ({n})',
    away:            'Away ({n})',
    connecting:      'Connecting...',
    noOneOnline:     'No one online',
    createRoom:      'Create Room',
    userSettings:    'User Settings',
    privateLabel:    'private',
    cancel:          'Cancel',
    save:            'Save',
    close:           'Close',
    roomName:        'Room Name',
    description:     'Description',
    optional:        'Optional',
    privateRoom:     'Private room',
    password:        'Password',
    nickname:        'Nickname',
    showFlag:        'Show my country flag',
    soundNotifs:     'Sound notifications',
    soundDesc:       'Plays a sound when you receive a DM',
    detectingLoc:    'Detecting location...',
    awayHint:        'away · {msg} · type /back to return',
    changesLeft:     '{n} change{s} left',
    changesOf3:      '{n} change{s} remaining of 3 allowed per account.',
    deleteForMe:     'Delete for me',
    deleteForAll:    'Delete for everyone',
    thisDeleted:     'This message was deleted.',
    connected:       'Connected',
    adminDashboard:  'Admin Dashboard',
    overview:        'Overview',
    live:            'Live',
    users:           'Users',
    rooms:           'Rooms',
    messages:        'Messages',
    reports:         'Reports',
    audit:           'Audit',
    analytics:       'Analytics',
    settings:        'Settings',
    console:         'Console',
    backToChat:      '← Chat',
    totalUsers:      'Total Users',
    onlineNow:       'Online Now',
    activeRooms:     'Active Rooms',
    messagesToday:   'Messages Today',
    bannedUsers:     'Banned Users',
    pendingReports:  'Pending Reports',
    serverUptime:    'Server Uptime',
    connections:     'Connections',
    search:          'Search...',
    ban:             'Ban',
    tempBan:         'Temp Ban',
    unban:           'Unban',
    kick:            'Kick',
    mute:            'Mute',
    unmute:          'Unmute',
    forceLogout:     'Force Logout',
    resetNickname:   'Reset Nickname',
    toggleAdmin:     'Toggle Admin',
    deactivate:      'Deactivate',
    lock:            'Lock',
    unlock:          'Unlock',
    deleteRoom:      'Delete Room',
    clearMessages:   'Clear Messages',
    deleteMessage:   'Delete Message',
    deleteAll:       'Delete All Messages',
    resolve:         'Resolve',
    sendAnnounce:    'Send Announcement',
    announce:        'Announce',
    maintenanceWarn: 'Warning: enabling maintenance mode will affect all users',
  },
};

let locale: Locale = 'en';

export const setLocale = (l: Locale): void => { locale = l; };
export const getLocale = (): Locale => locale;

/**
 * Returns the translation for `key` in the current locale.
 * Supports {param} interpolation via the `params` argument.
 * Falls back to English, then to the raw key.
 *
 * NOTE: This is a module-level function (not React state). Changing the locale
 * at runtime requires a full re-render (e.g. force-update or page reload).
 * When adding a locale switcher, wrap this in a React context + state.
 */
export const t = (key: string, params?: Record<string, string | number>): string => {
  let text = translations[locale]?.[key] ?? translations.en[key] ?? key;
  if (params) {
    for (const [k, v] of Object.entries(params)) {
      text = text.replaceAll(`{${k}}`, String(v));
    }
  }
  return text;
};
