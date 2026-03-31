<template>
  <v-dialog v-model="isOpen" max-width="800" height="600" transition="dialog-bottom-transition" scrollable persistent
    @update:model-value="(val) => !val && closeModal()">
    <v-card class="rounded-xl overflow-hidden" :color="isDark ? '#121212' : '#FFFFFF'" :style="{
      backdropFilter: 'blur(20px)',
      border: isDark ? '1px solid rgba(255,255,255,0.1)' : '1px solid rgba(0,0,0,0.05)'
    }">

      <v-toolbar color="transparent" flat class="px-4 mt-2">
        <v-toolbar-title class="text-h5 font-weight-bold">
          <div class="d-flex align-center">
            <v-icon icon="mdi-cog-outline" class="mr-2"></v-icon>
            <span>{{ $t('settings.title') }}</span>
          </div>
        </v-toolbar-title>
        <v-spacer></v-spacer>
        <v-btn icon="mdi-close" variant="text" @click="closeModal"></v-btn>
      </v-toolbar>

      <div class="d-flex flex-row h-100" style="min-height: 400px;">

        <v-tabs v-model="tab" direction="vertical" color="warning" class="px-2" style="width: 180px;">
          <v-tab value="general" class="justify-start rounded-lg mb-1">
            <v-icon start icon="mdi-cog-outline"></v-icon>{{ $t('settings.general') }}
          </v-tab>
          <v-tab value="update" class="justify-start rounded-lg">
            <v-icon start icon="mdi-update"></v-icon>{{ $t('settings.update') }}
          </v-tab>
          <v-tab value="application" class="justify-start round-lg">
            <v-icon start icon="mdi-application"></v-icon>{{ $t('settings.apps') }}
          </v-tab>
          <v-tab value="about" class="justify-start rounded-lg">
            <v-icon start icon="mdi-information-outline"></v-icon>{{ $t('settings.about') }}
          </v-tab>
        </v-tabs>

        <div class="border-end h-100 mx-1" :class="isDark ? 'border-white-50' : 'border-black-25'"></div>

        <v-window v-model="tab" class="flex-grow-1 pa-6">
          <v-window-item value="general">

            <div class="d-flex align-center text-h6 font-weight-bold mb-4">
              <v-icon icon="mdi-palette-outline" class="mr-2"></v-icon> {{ $t('settings.theme') }}
            </div>
            <v-list bg-transparent class="pa-0">
              <v-list-item class="px-0">
                <v-list-item-title class="text-medium-emphasis">{{ $t('settings.display_mode') }}</v-list-item-title>
                <v-list-item-subtitle>{{ $t('settings.theme_desc') }}</v-list-item-subtitle>

                <template v-slot:append>
                  <v-btn-toggle v-model="themePreference" mandatory color="warning" variant="tonal" rounded="pill"
                    :disabled="!isWin11" @update:model-value="applyThemePreference">
                    <v-btn value="light" size="small">
                      <v-icon icon="mdi-white-balance-sunny" class="mr-1"></v-icon> {{ $t('settings.light') }}
                    </v-btn>
                    <v-btn value="dark" size="small">
                      <v-icon icon="mdi-moon-waning-crescent" class="mr-1"></v-icon> {{ $t('settings.dark') }}
                    </v-btn>
                    <v-btn value="system" size="small">
                      <v-icon icon="mdi-monitor" class="mr-1"></v-icon> {{ $t('settings.system_sync') }}
                    </v-btn>
                  </v-btn-toggle>
                </template>
              </v-list-item>

              <v-expand-transition>
                <div v-if="!isWin11" class="text-caption text-error mt-2 px-0 font-italic">
                  {{ $t('settings.win11_warning') }}
                </div>
              </v-expand-transition>
            </v-list>

            <v-divider class="my-6 border-opacity-25"></v-divider>

            <div class="d-flex align-center text-h6 font-weight-bold mb-4">
              <v-icon icon="mdi-window-close" class="mr-2"></v-icon> {{ $t('settings.system') }}
            </div>
            <v-list bg-transparent class="pa-0">
              <v-list-item class="px-0">
                <v-list-item-title class="text-medium-emphasis">{{ $t('settings.close_behavior') }}</v-list-item-title>
                <v-list-item-subtitle>{{ $t('settings.close_behavior_desc') }}</v-list-item-subtitle>
                <template v-slot:append>
                  <v-btn-toggle v-model="closeBehavior" mandatory color="primary" variant="tonal" rounded="pill"
                    @update:model-value="applyCloseBehavior">
                    <v-btn value="tray" size="small"><v-icon icon="mdi-tray-arrow-down" class="mr-1"></v-icon> {{
                      $t('settings.tray') }}</v-btn>
                    <v-btn value="exit" size="small"><v-icon icon="mdi-power" class="mr-1"></v-icon> {{
                      $t('settings.exit') }}</v-btn>
                  </v-btn-toggle>
                </template>
              </v-list-item>
            </v-list>
            <template v-if="!isUnsupportedLanguage">
              <v-divider class="my-6 border-opacity-25"></v-divider>

              <div class="d-flex align-center text-h6 font-weight-bold mb-4">
                <v-icon icon="mdi-earth" class="mr-2"></v-icon> {{ $t('settings.language_block') }}
              </div> 
              <v-list bg-transparent class="pa-0">
                <v-list-item class="px-0">
                  <v-list-item-title class="text-medium-emphasis">{{ $t('settings.language') }}</v-list-item-title>
                  <v-list-item-subtitle>{{ $t('settings.language_desc') }}</v-list-item-subtitle>

                  <template v-slot:append>
                    <div style="width: 160px;">
                      <v-select v-model="languagePreference" :items="[
                        { title: 'Tiếng Việt', value: 'vi-VN' },
                        { title: 'English', value: 'en-US' }
                      ]" item-title="title" item-value="value" variant="solo-filled" flat rounded="pill"
                        density="compact" hide-details color="primary" class="text-center" bg-color="#2a2a2a"
                        menu-props="{ rounded: 'xl', elevation: 4 }" @update:model-value="applyLanguagePreference">
                      </v-select>
                    </div>
                  </template>

                </v-list-item>
              </v-list>
            </template>

          </v-window-item>

          <v-window-item value="update">
            <div class="mt-4 px-2">
              <div class="d-flex align-center justify-space-between mb-6 px-2">
                <div>
                  <div class="text-subtitle-2 text-grey mb-1">
                    {{ $t('settings.ui_ux_version') }} <span class="font-weight-bold ml-1"
                      :class="isDark ? 'text-white' : 'text-black'">{{ props.app?.FE_version }}</span>
                  </div>
                  <div class="text-subtitle-2 text-grey">
                    {{ $t('settings.client_version') }} <span class="font-weight-bold ml-1"
                      :class="isDark ? 'text-white' : 'text-black'">{{ props.app?.BE_version || $t('settings.unknown')
                      }}</span>
                  </div>
                </div>
                <v-btn color="primary" variant="tonal" class="rounded-lg update-btn px-5" height="48"
                  @click="$emit('check-update')">
                  <v-icon start icon="mdi-refresh" size="24" class="update-icon"></v-icon>{{ $t('settings.check_update')
                  }}
                </v-btn>
              </div>
              <v-divider class="mb-5 opacity-20"></v-divider>
              <div class="d-flex align-center justify-space-between mb-6">
                <div>
                  <div class="text-subtitle-1 font-weight-bold mb-0">{{ $t('settings.update_app_list') }}</div>
                  <div class="text-caption text-medium-emphasis">{{ $t('settings.update_app_list_desc') }}</div>
                </div>
                <v-btn color="primary" variant="tonal" class="rounded-lg px-5" :class="{ 'update-btn': !isCooldown }"
                  height="48" :disabled="isCooldown" @click="handleGetApps">
                  <v-icon start icon="mdi-refresh" size="24" :class="{ 'update-icon': !isCooldown }"></v-icon>
                  {{ isCooldown ? $t('settings.try_again_later', { time: cooldownTime }) : $t('settings.update_now') }}
                </v-btn>
              </div>
              <v-divider class="mb-5 opacity-20"></v-divider>
              <div class="px-2 mb-4">
                <div class="text-body-1 font-weight-bold mb-3 d-flex align-center">
                  <v-icon icon="mdi-text-box-outline" start color="primary" size="small"></v-icon>
                  {{ $t('settings.update_details', { version: props.app?.BE_version_latest }) }}
                </div>
                <v-card variant="flat" :color="isDark ? 'rgba(255,255,255,0.03)' : 'rgba(0,0,0,0.03)'"
                  :class="isDark ? 'pa-4 rounded-lg text-body-2 text-grey-lighten-1' : 'pa-4 rounded-lg text-body-2 text-grey-darken-2'"
                  style="white-space: pre-line; max-height: 220px; overflow-y: auto; border: 1px solid rgba(255,255,255,0.05);">
                  {{ props.app?.changelog || $t('settings.no_changelog') }}
                </v-card>
              </div>
            </div>
          </v-window-item>

          <v-window-item value="application">
            <div class="mt-4 px-2">
              <div class="px-2 pb-4">
                <div class="text-h6 font-weight-bold mb-4"><v-icon icon="mdi-wrench-outline" start color="primary"
                    size="small"></v-icon> {{ $t('settings.app_management') }}</div>
                <v-row>
                  <v-col cols="12" class="py-2">
                    <div
                      class="d-flex align-center justify-space-between bg-surface-variant-light pa-3 rounded-lg border">
                      <div>
                        <div class="text-body-2 font-weight-bold">{{ $t('settings.clear_cache') }}</div>
                        <div class="text-caption text-medium-emphasis">{{ $t('settings.clear_cache_desc') }}</div>
                      </div>
                      <v-btn color="success" variant="tonal" class="rounded-lg px-4" @click="$emit('clear-cache')">
                        <v-icon start icon="mdi-broom" size="18"></v-icon>{{ $t('settings.btn_clear') }}
                      </v-btn>
                    </div>
                  </v-col>
                  <v-col cols="12" class="py-2">
                    <div
                      class="d-flex align-center justify-space-between bg-surface-variant-light pa-3 rounded-lg border">
                      <div>
                        <div class="text-body-2 font-weight-bold text-error">{{ $t('settings.uninstall') }}</div>
                        <div class="text-caption text-medium-emphasis">{{ $t('settings.uninstall_desc') }}</div>
                      </div>
                      <v-btn color="error" variant="tonal" class="rounded-lg px-4" @click="$emit('uninstall')">
                        <v-icon start icon="mdi-delete-outline" size="18"></v-icon>{{ $t('settings.btn_uninstall') }}
                      </v-btn>
                    </div>
                  </v-col>
                </v-row>
              </div>
            </div>
          </v-window-item>

          <v-window-item value="about">
            <div class="text-center mt-4">
              <v-avatar size="100" variant="tonal" class="mb-4">
                <img src="../assets/images/logo.png" style="width: 100%; height: 100%; object-fit: contain;">
              </v-avatar>
              <div class="text-h4 font-weight-black mb-2">Hieu GL Lite</div>
              <v-card variant="tonal" :color="isDark ? 'grey' : 'primary'" class="mt-8 pa-4 rounded-lg text-left">
                <div class="text-body-2 mb-1" v-html="$t('settings.developed_by')"></div>
                <div class="text-body-2 text-medium-emphasis">{{ $t('settings.based_on') }}</div>
              </v-card>
            </div>
          </v-window-item>

        </v-window>
      </div>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useTheme } from 'vuetify';
import { useI18n } from 'vue-i18n'; // Khai báo thư viện dịch thuật

const { t } = useI18n(); // Gọi biến t để xài trong JS

const props = defineProps(['app', 'settings', 'isWin11']);
const emit = defineEmits(['close', 'check-update', 'get-apps', 'clear-cache', 'uninstall', 'update-theme', 'update-language']);
const theme = useTheme();

const isDark = computed(() => theme.global.current.value.dark);
const isOpen = ref(true);
const tab = ref('general');

const GetClientVersion = () => {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "GET_CLIENT_VERSION" });
  }
};

onMounted(() => {
  GetClientVersion();
  if (window.chrome?.webview) {
    // Dùng biến t() để dịch chữ Cài đặt báo lên cho thanh tiêu đề C#
    window.chrome.webview.postMessage({ type: "CHANGE_TITLE", title: t('settings.settings_title') });
  }
});
const isUnsupportedLanguage = computed(() => {
  return parseInt(props.app?.BE_versioncode || '0') <= 260314;
});

const closeModal = () => {
  isOpen.value = false;
  clearInterval(cooldownTimer);

  setTimeout(() => emit('close'), 300);
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "CHANGE_TITLE" })
  }
};

const themePreference = ref(props.settings?.theme || 'system');
const closeBehavior = ref(props.settings?.minimizeToTray ? 'tray' : 'exit');
const languagePreference = ref(props.settings?.language || 'vi-VN');

const applyThemePreference = (val) => {
  let targetTheme = val;
  if (val === 'system') {
    targetTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
  theme.global.name.value = targetTheme;

  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "THEME_CHANGED", mode: val });
  }
  emit('update-theme', val);
};

const applyCloseBehavior = (val) => {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({
      type: "SET_CLOSE_BEHAVIOR",
      minimizeToTray: val === 'tray'
    });
  }
};

const applyLanguagePreference = (val) => {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "CHANGE_LANGUAGE", lang: val });
  }
  emit('update-language', val);
};

// --- BIẾN CHO TÍNH NĂNG COOLDOWN ---
const isCooldown = ref(false);
const cooldownTime = ref(0);
let cooldownTimer = null;

const handleGetApps = () => {
  if (isCooldown.value) return;

  emit('get-apps');

  isCooldown.value = true;
  cooldownTime.value = 15;

  cooldownTimer = setInterval(() => {
    cooldownTime.value--;
    if (cooldownTime.value <= 0) {
      clearInterval(cooldownTimer);
      isCooldown.value = false;
    }
  }, 1000);
};
</script>

<style scoped>
.update-icon {
  transition: transform 0.6s cubic-bezier(0.4, 0, 0.2, 1);
}

.update-btn:hover .update-icon {
  transform: rotate(180deg);
}

::-webkit-scrollbar {
  width: 6px;
}

::-webkit-scrollbar-thumb {
  background: rgba(150, 150, 150, 0.3);
  border-radius: 10px;
}

::-webkit-scrollbar-thumb:hover {
  background: rgba(150, 150, 150, 0.5);
}
</style>