export type Locale = 'en' | 'es'

export const messages = {
  en: {
    navFeatures: 'Features',
    navWidget: 'Widget',
    navHow: 'How it works',
    navPrivacy: 'Privacy',
    heroTitle: 'One tap. Habit done.',
    heroSubtitle:
      'Track healthy habits with almost zero friction. Complete today\'s habits from a home screen widget — no need to open the app.',
    heroCta: 'Download APK',
    heroRepo: 'View on GitHub',
    featuresTitle: 'Built for daily momentum',
    featureWidget: 'Home screen widget',
    featureWidgetDesc: 'Square grid of today\'s habits. Tap a cell to complete — it disappears instantly.',
    featureOffline: 'Offline-first',
    featureOfflineDesc: 'Firestore cache keeps reads and writes fast, even without signal.',
    featureStreaks: 'Smart streaks',
    featureStreaksDesc: 'Non-scheduled days don\'t break your streak.',
    featureI18n: 'English & Spanish',
    featureI18nDesc: 'Switch language anytime in Settings.',
    widgetTitle: 'The widget is the product',
    widgetDesc:
      'Add OneTap Habits to your home screen. Only incomplete habits for today appear — up to six in a adaptive grid. Finished? They vanish so you see what\'s left.',
    howTitle: 'How it works',
    how1: 'Install the APK from GitHub Releases',
    how2: 'Sign in and create habits (toggle Show in widget)',
    how3: 'Add the widget and tap to complete',
    privacyTitle: 'Privacy',
    privacyBody:
      'Firebase Authentication and Cloud Firestore store your habits under your account. No ads. Open source under MIT.',
    ctaTitle: 'Ready to try it?',
    ctaButton: 'Get the latest APK',
    footer: 'MIT License · Open source'
  },
  es: {
    navFeatures: 'Funciones',
    navWidget: 'Widget',
    navHow: 'Cómo funciona',
    navPrivacy: 'Privacidad',
    heroTitle: 'Un toque. Hábito hecho.',
    heroSubtitle:
      'Registra hábitos con mínima fricción. Completa los de hoy desde un widget en la pantalla de inicio — sin abrir la app.',
    heroCta: 'Descargar APK',
    heroRepo: 'Ver en GitHub',
    featuresTitle: 'Hecho para el día a día',
    featureWidget: 'Widget en inicio',
    featureWidgetDesc: 'Cuadrícula con los hábitos de hoy. Toca una celda para completar — desaparece al instante.',
    featureOffline: 'Offline primero',
    featureOfflineDesc: 'La caché de Firestore funciona sin conexión.',
    featureStreaks: 'Rachas inteligentes',
    featureStreaksDesc: 'Los días no programados no rompen la racha.',
    featureI18n: 'Inglés y español',
    featureI18nDesc: 'Cambia el idioma en Ajustes.',
    widgetTitle: 'El widget es el producto',
    widgetDesc:
      'Añade OneTap Habits al inicio. Solo aparecen hábitos incompletos de hoy — hasta seis en una cuadrícula. ¿Listo? Desaparecen para que veas lo que falta.',
    howTitle: 'Cómo funciona',
    how1: 'Instala el APK desde GitHub Releases',
    how2: 'Inicia sesión y crea hábitos (Mostrar en widget)',
    how3: 'Añade el widget y toca para completar',
    privacyTitle: 'Privacidad',
    privacyBody:
      'Firebase Auth y Firestore guardan tus hábitos en tu cuenta. Sin anuncios. Código abierto MIT.',
    ctaTitle: '¿Listo para probar?',
    ctaButton: 'Obtener el APK',
    footer: 'Licencia MIT · Código abierto'
  }
} as const

export type MessageKey = keyof typeof messages.en
