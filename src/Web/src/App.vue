<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref } from 'vue'
import { messages, type Locale, type MessageKey } from '@/i18n/messages'
import { APK_URL, REPO_URL } from '@/data/links'

const locale = ref<Locale>('en')
const t = (key: MessageKey) => messages[locale.value][key]

const darkMq = window.matchMedia('(prefers-color-scheme: dark)')

function applyTheme(isDark: boolean) {
  document.documentElement.classList.toggle('dark', isDark)
}

function onMqChange(e: MediaQueryListEvent) {
  applyTheme(e.matches)
}

onMounted(() => {
  applyTheme(darkMq.matches)
  darkMq.addEventListener('change', onMqChange)
})

onBeforeUnmount(() => {
  darkMq.removeEventListener('change', onMqChange)
})

const navItems = computed(() => [
  { href: '#features', label: t('navFeatures') },
  { href: '#widget', label: t('navWidget') },
  { href: '#how', label: t('navHow') },
  { href: '#privacy', label: t('navPrivacy') }
])
</script>

<template>
  <header class="sticky top-0 z-50 border-b border-neutral-200/80 bg-neutral-50/90 backdrop-blur dark:border-neutral-800 dark:bg-neutral-950/90">
    <div class="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-4 sm:px-6">
      <a href="#" class="flex items-center gap-2.5 text-lg font-bold tracking-tight">
        <img src="/logo.svg" alt="" width="36" height="36" class="rounded-lg" />
        OneTap Habits
      </a>
      <nav class="hidden gap-6 text-sm md:flex">
        <a v-for="item in navItems" :key="item.href" :href="item.href" class="text-neutral-600 hover:text-neutral-900 dark:text-neutral-400 dark:hover:text-white">{{ item.label }}</a>
      </nav>
      <div class="flex items-center gap-2">
        <button type="button" class="rounded-lg px-2 py-1 text-sm" :class="locale === 'en' ? 'bg-neutral-900 text-white dark:bg-white dark:text-neutral-900' : ''" @click="locale = 'en'">EN</button>
        <button type="button" class="rounded-lg px-2 py-1 text-sm" :class="locale === 'es' ? 'bg-neutral-900 text-white dark:bg-white dark:text-neutral-900' : ''" @click="locale = 'es'">ES</button>
      </div>
    </div>
  </header>

  <main>
    <section class="mx-auto max-w-6xl px-4 py-20 sm:px-6 md:py-28">
      <h1 class="max-w-2xl text-4xl font-extrabold tracking-tight sm:text-5xl">{{ t('heroTitle') }}</h1>
      <p class="mt-6 max-w-xl text-lg text-neutral-600 dark:text-neutral-300">{{ t('heroSubtitle') }}</p>
      <div class="mt-8 flex flex-wrap gap-3">
        <a :href="APK_URL" class="rounded-xl bg-brand-500 px-5 py-3 font-semibold text-white shadow-lg shadow-brand-500/20 hover:bg-brand-600">{{ t('heroCta') }}</a>
        <a :href="REPO_URL" class="rounded-xl border border-neutral-300 px-5 py-3 font-semibold dark:border-neutral-700">{{ t('heroRepo') }}</a>
      </div>
    </section>

    <section id="features" class="border-t border-neutral-200 bg-white py-20 dark:border-neutral-800 dark:bg-neutral-900">
      <div class="mx-auto max-w-6xl px-4 sm:px-6">
        <h2 class="text-3xl font-bold">{{ t('featuresTitle') }}</h2>
        <div class="mt-10 grid gap-8 sm:grid-cols-2">
          <article><h3 class="font-semibold">{{ t('featureWidget') }}</h3><p class="mt-2 text-neutral-600 dark:text-neutral-400">{{ t('featureWidgetDesc') }}</p></article>
          <article><h3 class="font-semibold">{{ t('featureOffline') }}</h3><p class="mt-2 text-neutral-600 dark:text-neutral-400">{{ t('featureOfflineDesc') }}</p></article>
          <article><h3 class="font-semibold">{{ t('featureStreaks') }}</h3><p class="mt-2 text-neutral-600 dark:text-neutral-400">{{ t('featureStreaksDesc') }}</p></article>
          <article><h3 class="font-semibold">{{ t('featureI18n') }}</h3><p class="mt-2 text-neutral-600 dark:text-neutral-400">{{ t('featureI18nDesc') }}</p></article>
        </div>
      </div>
    </section>

    <section id="widget" class="py-20">
      <div class="mx-auto max-w-6xl px-4 sm:px-6 md:grid md:grid-cols-2 md:items-center md:gap-12">
        <div>
          <h2 class="text-3xl font-bold">{{ t('widgetTitle') }}</h2>
          <p class="mt-4 text-neutral-600 dark:text-neutral-400">{{ t('widgetDesc') }}</p>
        </div>
        <div class="mt-10 md:mt-0">
          <div class="mx-auto grid aspect-square max-w-xs grid-cols-2 gap-2 rounded-2xl bg-neutral-900 p-3 shadow-xl">
            <div class="flex items-center justify-center rounded-lg bg-neutral-800 p-2 text-center text-xs text-white">Water</div>
            <div class="flex items-center justify-center rounded-lg bg-neutral-800 p-2 text-center text-xs text-white">Read</div>
            <div class="flex items-center justify-center rounded-lg bg-neutral-800 p-2 text-center text-xs text-white">Walk</div>
            <div class="flex items-center justify-center rounded-lg border border-dashed border-neutral-600 p-2 text-center text-xs text-neutral-500">+ tap</div>
          </div>
        </div>
      </div>
    </section>

    <section id="how" class="border-t border-neutral-200 bg-white py-20 dark:border-neutral-800 dark:bg-neutral-900">
      <div class="mx-auto max-w-6xl px-4 sm:px-6">
        <h2 class="text-3xl font-bold">{{ t('howTitle') }}</h2>
        <ol class="mt-8 list-decimal space-y-3 pl-5 text-neutral-700 dark:text-neutral-300">
          <li>{{ t('how1') }}</li>
          <li>{{ t('how2') }}</li>
          <li>{{ t('how3') }}</li>
        </ol>
      </div>
    </section>

    <section id="privacy" class="py-20">
      <div class="mx-auto max-w-6xl px-4 sm:px-6">
        <h2 class="text-3xl font-bold">{{ t('privacyTitle') }}</h2>
        <p class="mt-4 max-w-2xl text-neutral-600 dark:text-neutral-400">{{ t('privacyBody') }}</p>
      </div>
    </section>

    <section class="border-t border-neutral-200 bg-neutral-900 py-16 text-white dark:border-neutral-800">
      <div class="mx-auto max-w-6xl px-4 text-center sm:px-6">
        <h2 class="text-2xl font-bold">{{ t('ctaTitle') }}</h2>
        <a :href="APK_URL" class="mt-6 inline-block rounded-xl bg-brand-500 px-6 py-3 font-semibold hover:bg-brand-600">{{ t('ctaButton') }}</a>
      </div>
    </section>
  </main>

  <footer class="border-t border-neutral-200 py-8 text-center text-sm text-neutral-500 dark:border-neutral-800">
    <p>{{ t('footer') }} · <a :href="REPO_URL" class="underline">GitHub</a></p>
  </footer>
</template>
