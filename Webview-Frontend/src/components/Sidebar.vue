<template>
  <v-navigation-drawer permanent rail rail-width="100"
    :color="isDark ? 'rgba(18, 18, 18, 0.8)' : 'rgba(255, 255, 255, 0.8)'" class="border-0"
    style="backdrop-filter: blur(20px); border-right: 1px solid rgba(255,255,255,0.05) !important;">
    <div class="d-flex flex-column h-100 py-4">

      <div class="d-flex justify-center mb-6">
        <v-menu location="right center" transition="slide-x-transition" open-on-hover :close-delay="500">
          <template v-slot:activator="{ props }">
            <v-avatar size="60" variant="" v-bind="props" class="cursor-pointer logo-btn"
              style="transition: transform 0.3s ease;">
              <img src="../assets/images/logo.png" style="width: 100%; height: 100%; object-fit: contain;">
            </v-avatar>
          </template>

          <v-list :bg-color="isDark ? '#1e1e1e' : 'white'" class="rounded-xl ml-4 pa-2" elevation="12" min-width="180">
            <v-list-subheader class="text-uppercase font-weight-bold text-caption">{{ $t('sidebar.connect')
            }}</v-list-subheader>

            <v-list-item @click="openExternal('https://facebook.com/hieushiluka')" rounded="lg" class="mb-1">
              <template v-slot:prepend>
                <v-icon icon="mdi-facebook" :color="isDark ? 'white' : 'black'"></v-icon>
              </template>
              <v-list-item-title>Facebook</v-list-item-title>
            </v-list-item>

            <v-list-item @click="openExternal('http://youtube.com/channel/UCLd32LcLg-Tx6mbGPOqEbvQ?sub_confirmation=1')"
              rounded="lg" class="mb-1">
              <template v-slot:prepend>
                <v-icon icon="mdi-youtube" :color="isDark ? 'white' : 'black'"></v-icon>
              </template>
              <v-list-item-title>Youtube</v-list-item-title>
            </v-list-item>

            <v-list-item @click="openExternal('http://tiktok.com/@hieushiluka')" rounded="lg" class="mb-1">
              <template v-slot:prepend>
                <v-icon :color="isDark ? 'white' : 'black'">
                  <font-awesome-icon :icon="['fab', 'tiktok']" />
                </v-icon>
              </template>
              <v-list-item-title>TikTok</v-list-item-title>
            </v-list-item>
            <v-list-item @click="openExternal('https://discord.gg/GnmKM9bqEf')" rounded="lg" class="mb-1">
              <template v-slot:prepend>
                <v-icon :color="isDark ? 'white' : 'black'">
                  <font-awesome-icon icon="fa-brands fa-discord" />
                </v-icon>
              </template>
              <v-list-item-title>Discord</v-list-item-title>
            </v-list-item>

            <v-list-item @click="openExternal('https://github.com/ShilukaYT')" rounded="lg" class="mb-1">
              <template v-slot:prepend>
                <v-icon icon="mdi-github" :color="isDark ? 'white' : 'black'"></v-icon>
              </template>
              <v-list-item-title>GitHub</v-list-item-title>
            </v-list-item>

            <v-divider class="my-2"></v-divider>

            <v-list-subheader class="text-uppercase font-weight-bold text-caption">{{ $t('sidebar.support_me')
            }}</v-list-subheader>

            <v-list-item @click="openPopup('https://img.vietqr.io/image/MB-0967420947-compact.jpg', 540, 540)"
              rounded="lg" base-color="warning">
              <template v-slot:prepend>
                <v-icon :color="isDark ? 'white' : 'black'">
                  <Icon icon="arcticons:mb-bank" />
                </v-icon>
              </template>
              <v-list-item-title class="font-weight-bold">MBBank</v-list-item-title>
            </v-list-item>

            <v-list-item @click="openPopup('https://img.vietqr.io/image/Momo-0967420947-compact.jpg', 540, 540)"
              rounded="lg" base-color="warning">
              <template v-slot:prepend>
                <v-icon :color="isDark ? 'white' : 'black'">
                  <Icon icon="arcticons:momo" />
                </v-icon>
              </template>
              <v-list-item-title class="font-weight-bold">Momo</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
      </div>

      <v-list density="compact" class="flex-grow-1 bg-transparent px-2 overflow-y-auto hide-scrollbar" nav>
        <v-list-item v-for="app in apps" :key="app.id" :value="app.id" class="mb-3 rounded-xl py-2 custom-list-item"
          @click="$emit('change-app', app)" @contextmenu.prevent="onContextMenu(app)" :active="activeId === app.id">

          <template v-slot:default>

            <div :id="'context-anchor-' + app.id"
              style="position: absolute; right: 0; top: 50%; width: 1px; height: 1px; pointer-events: none;"></div>

            <div class="d-flex justify-center w-100 position-relative" style="overflow: visible !important;">
              <div class="position-relative" style="overflow: visible !important;">
                <v-avatar size="52" class="pa-1 elevation-2" rounded="lg">
                  <v-img :src="app.icon"></v-img>
                </v-avatar>
                <img v-if="app.badge" :src="app.badge" class="app-badge-icon" />
              </div>
            </div>

          </template>
        </v-list-item>
      </v-list>

      <v-menu v-model="showContextMenu"
        :activator="selectedContextApp ? '#context-anchor-' + selectedContextApp.id : null" location="right center"
        transition="slide-x-transition">

        <v-list :bg-color="isDark ? '#1a1a1a' : 'white'" class="rounded-xl ml-4 pa-2 menu-context-box" elevation="12"
          min-width="170">
          <template v-if="selectedContextApp">
            <div class="d-flex align-center px-4 pt-2 pb-3 mb-3" style="border-bottom: 1px solid rgba(255, 255, 255, 0.08);">
                
                <div class="position-relative mr-3" style="overflow: visible !important;">
                  <v-avatar size="44" class="pa-1 elevation-2" rounded="lg">
                    <v-img :src="selectedContextApp.icon"></v-img>
                  </v-avatar>
                  
                  <img v-if="selectedContextApp.badge" :src="selectedContextApp.badge" class="app-badge-icon" style="width: 20px; height: 20px; bottom: -4px; right: -4px;" />
                </div>

                <div class="d-flex flex-column">
                  <span class="font-weight-black text-subtitle-2 text-uppercase" style="letter-spacing: 0.5px; line-height: 1.2;">{{ selectedContextApp.name }}</span>
                  </div>
              </div>

            <v-list-item v-if="!selectedContextApp.isInstalled" @click="handleContextInstall"
              :disabled="isAnyAppDownloading" class="custom-context-item rounded-lg mb-1" ripple="false">
              <div class="d-flex align-center justify-start w-100 pa-1">
                <v-icon :icon="isThisAppDownloading ? 'mdi-loading mdi-spin' : 'mdi-download'" color="info" size="24"
                  class="mr-4"></v-icon>

                <span class="font-weight-bold text-body-2 text-uppercase" style="letter-spacing: 0.5px;">
                  {{ isThisAppDownloading ? ($t('app.stages.installing') || 'ĐANG CÀI ĐẶT') :
                    ($t('game_page.install_now') || 'CÀI ĐẶT') }}
                </span>
              </div>
            </v-list-item>

            <template v-else>

              

              <v-list-item @click="handleContextPlay" class="custom-context-item rounded-lg mb-2" ripple="false">
                <div class="d-flex align-center justify-start w-100 pa-1">
                  <v-icon icon="mdi-play" color="success" size="28" class="mr-3" style="margin-left: -2px;"></v-icon>
                  <span class="font-weight-black text-body-2 text-uppercase" style="letter-spacing: 0.5px;">{{
                    $t('game_page.open_app') || 'CHƠI' }}</span>
                </div>
              </v-list-item>

              <v-divider class="my-1 border-opacity-25"></v-divider>

              <v-list-item @click="handleExtra('OPEN_MULTI')" class="custom-context-item rounded-lg mb-1"
                ripple="false">
                <div class="d-flex align-center justify-start w-100 pa-1">
                  <v-icon icon="mdi-layers-triple" size="20" class="mr-4 text-medium-emphasis"></v-icon>
                  <span class="font-weight-bold text-body-2">{{ $t('game_page.multi_instance_manager') || 'Trình quản lý đa phiên bản' }}</span>
                </div>
              </v-list-item>

              <v-list-item @click="handleExtra('CLEANUP')" class="custom-context-item rounded-lg mb-1" ripple="false">
                <div class="d-flex align-center justify-start w-100 pa-1">
                  <v-icon icon="mdi-broom" size="20" class="mr-4 text-medium-emphasis"></v-icon>
                  <span class="font-weight-bold text-body-2">{{ $t('game_page.clear_memory') || 'Giải phóng dung lượng'
                    }}</span>
                </div>
              </v-list-item>

              <v-list-item @click="handleExtra('BACKUP')" class="custom-context-item rounded-lg mb-1" ripple="false">
                <div class="d-flex align-center justify-start w-100 pa-1">
                  <v-icon icon="mdi-cloud-upload" size="20" class="mr-4 text-medium-emphasis"></v-icon>
                  <span class="font-weight-bold text-body-2">{{ $t('game_page.backup') || 'Sao lưu' }}</span>
                </div>
              </v-list-item>

              <v-list-item @click="handleExtra('RESTORE')" class="custom-context-item rounded-lg mb-1" ripple="false">
                <div class="d-flex align-center justify-start w-100 pa-1">
                  <v-icon icon="mdi-cloud-download" size="20" class="mr-4 text-medium-emphasis"></v-icon>
                  <span class="font-weight-bold text-body-2">{{ $t('game_page.restore') || 'Khôi phục' }}</span>
                </div>
              </v-list-item>

              <v-divider class="my-1 border-opacity-25"></v-divider>

              <v-list-item @click="handleExtra('CHANGE_VERSION')" class="custom-context-item rounded-lg mb-1"
                ripple="false">
                <div class="d-flex align-center justify-start w-100 pa-1">
                  <v-icon icon="mdi-source-branch" color="info" size="20" class="mr-4"></v-icon>
                  <span class="font-weight-bold text-body-2 text-info">{{ $t('game_page.change_version') || 'Thay đổi phiên bản' }}</span>
                </div>
              </v-list-item>

              <v-list-item @click="handleExtra('UNINSTALL')" class="custom-context-item-error rounded-lg"
                ripple="false">
                <div class="d-flex align-center justify-start w-100 pa-1">
                  <v-icon icon="mdi-trash-can-outline" color="#ff5252" size="20" class="mr-4"></v-icon>
                  <span class="font-weight-bold text-body-2" style="color: #ff5252;">{{ $t('game_page.uninstall') || 'Gỡ cài đặt' }}</span>
                </div>
              </v-list-item>
            </template>

          </template>
        </v-list>
      </v-menu>

      <div class="d-flex flex-column align-center mt-auto">
        <v-btn icon="mdi-cog" variant="text" :color="isDark ? 'grey-lighten-1' : 'grey-darken-1'" size="large"
          class="mb-4" @click="$emit('open-settings')"></v-btn>

        <div class="d-flex flex-column align-center mt-auto pb-4">

          <v-menu location="right bottom" transition="slide-x-transition" open-on-hover :close-delay="500"
            :close-on-content-click="false">

            <template v-slot:activator="{ props }">

              <v-badge :dot="!user" :color="user ? 'success' : 'grey'" location="bottom right" offset-x="3"
                offset-y="3">
                <v-avatar v-bind="props" size="44" class="elevation-4 cursor-pointer d-flex align-center justify-center"
                  :color="!user ? (isDark ? 'grey-darken-3' : 'grey-lighten-2') : 'transparent'"
                  :style="{ border: user ? '2px solid #5865F2' : '2px solid #444' }">

                  <Icon v-if="!user" icon="solar:user-bold" width="24" height="24"
                    :class="isDark ? 'text-grey-lighten-1' : 'text-grey-darken-1'" />

                  <v-img v-else :src="userAvatarUrl" alt="Avatar" draggable="false"></v-img>

                </v-avatar>
              </v-badge>

            </template>

            <v-card :color="isDark ? '#1e1e1e' : 'white'" min-width="250" class="rounded-xl ml-4">

              <template v-if="user">
                <v-list bg-transparent>

                  <v-list-item>

                    <template v-slot:prepend>
                      <v-avatar size="64" :image="userAvatarUrl" class="mr-2"></v-avatar>
                    </template>

                    <template v-slot:title>
                      <div class="font-weight-bold text-body-1">{{ user.name }}</div>
                    </template>

                    <template v-slot:subtitle>
                      <div class="text-caption d-flex align-center mt-1">
                        <span style="opacity: 1;">@{{ user.username }}</span>
                      </div>
                      <div class="text-caption text-truncate mt-1" style="opacity: 1;">
                        {{ user.email }}
                      </div>
                    </template>

                    <template v-slot:append>
                      <v-icon icon="mdi-discord" color="#5865F2" size="large"></v-icon>
                    </template>

                  </v-list-item>

                </v-list>

                <v-divider class="mx-4 my-2"></v-divider>

                <div class="px-2 pb-2">
                  <v-list-item @click="handleLogout" color="error" rounded="lg" class="px-4 py-1">
                    <div class="d-flex align-center text-error">
                      <v-icon icon="mdi-logout" class="mr-3" size="22"></v-icon>
                      <span class="text-body-1 font-weight-medium">{{ $t('sidebar.logout') }}</span>
                    </div>
                  </v-list-item>
                </div>
              </template>

              <template v-else>
                <div class="pa-6 text-center">
                  <v-icon icon="mdi-account-circle-outline" size="48" class="mb-2 opacity-20"></v-icon>
                  <div class="text-body-2 mb-4">{{ $t('sidebar.login_prompt') }}</div>

                  <v-btn block color="#5865F2" prepend-icon="mdi-discord" class="rounded-lg" @click="handleLogin">
                    {{ $t('sidebar.login_discord') }}
                  </v-btn>
                </div>
              </template>

            </v-card>
          </v-menu>
        </div>
      </div>

    </div>
  </v-navigation-drawer>
</template>

<script setup>
const props = defineProps(['apps', 'activeId', 'user', 'downloadingApps']);
const emit = defineEmits(['change-app', 'open-settings', 'open-install', 'extra-action']);
import { ref, computed, onMounted } from 'vue';
import { useTheme } from 'vuetify';
import { Icon } from '@iconify/vue';

import { library } from '@fortawesome/fontawesome-svg-core'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { faFacebook, faYoutube, faTiktok, faGithub, faDiscord } from '@fortawesome/free-brands-svg-icons'

library.add(faFacebook, faYoutube, faTiktok, faGithub, faDiscord)

import defaultAvt from '@/assets/images/avtDefault.jpg';
const updateAppsList = computed(() => {
  return props.apps.map(app => {
    if (!app.icon) {
      return { ...app, icon: defaultAvt };
    }
    return app;
  });
});

const userAvatarUrl = computed(() => {
  if (!props.user || !props.user.id) return '';

  const avatar = props.user.avatar;
  if (avatar && avatar !== 'null' && avatar !== '' && !avatar.endsWith('/.png')) {
    return avatar;
  }

  try {
    const id = props.user.id;
    const desc = props.user.discriminator;

    if (desc === '0' || !desc) {
      const index = Number(BigInt(id) >> 22n) % 6;
      return `https://cdn.discordapp.com/embed/avatars/${index}.png`;
    } else {
      return `https://cdn.discordapp.com/embed/avatars/${parseInt(desc) % 5}.png`;
    }
  } catch (e) {
    return 'https://cdn.discordapp.com/embed/avatars/0.png';
  }
});

const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

const openExternal = (url) => {
  if (window.chrome?.webview) {
    const features = 'width=500,height=700,left=100,top=100,menubar=no,status=no';
    window.chrome.webview.postMessage({
      type: "OPEN_URL",
      url: url
    });
  } else {
    window.open(url, '_blank');
  }
};

const openPopup = (url, width, height) => {
  const left = (window.screen.width / 2) - (width / 2);
  const top = (window.screen.height / 2) - (height / 2);

  const features = `width=${width},height=${height},left=${left},top=${top},menubar=no,toolbar=no,location=no,status=no,resizable=no`;

  window.open(url, 'HieuGLLitePopup', features);
};

const handleLogin = () => {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "LOGIN_DISCORD" });
  }
};

const handleLogout = () => {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "LOGOUT_DISCORD" });
  }
};

// ==========================================
// LOGIC CHO MENU CHUỘT PHẢI (ĐÃ TỐI ƯU)
// ==========================================
const showContextMenu = ref(false);
const selectedContextApp = ref(null);

const onContextMenu = (app) => {
  showContextMenu.value = false;
  selectedContextApp.value = app;

  setTimeout(() => {
    showContextMenu.value = true;
  }, 50);
};

const handleContextInstall = () => {
  emit('change-app', selectedContextApp.value);
  setTimeout(() => { emit('open-install'); }, 100);
};

const handleContextPlay = () => {
  if (window.chrome?.webview) window.chrome.webview.postMessage({ type: "PLAY", appId: selectedContextApp.value.id });
};

const handleContextUninstall = () => {
  if (window.chrome?.webview) window.chrome.webview.postMessage({ type: "UNINSTALL", appId: selectedContextApp.value.id });
};

// 1. Kiểm tra xem CÓ BẤT KỲ app nào đang tải không (Khóa tổng thể)
const isAnyAppDownloading = computed(() => {
  return props.downloadingApps && Object.keys(props.downloadingApps).length > 0;
});

// 2. Kiểm tra xem app ĐANG CLICK CHUỘT PHẢI có phải là app đang tải không
const isThisAppDownloading = computed(() => {
  if (!selectedContextApp.value || !props.downloadingApps) return false;
  return !!props.downloadingApps[selectedContextApp.value.id];
});
// Hàm xử lý các tác vụ quản lý nâng cao (Đồng bộ với GamePage)
const handleExtra = (actionType) => {
  showContextMenu.value = false; // Tắt menu

  // Tùy chọn: Chuyển màn hình sang game đó luôn để người dùng dễ nhìn
  emit('change-app', selectedContextApp.value);

  // Gửi lệnh ra ngoài App.vue để nó tự xử lý (Mở Modal hoặc gọi C#)
  setTimeout(() => {
    emit('extra-action', { type: actionType, id: selectedContextApp.value.id });
  }, 50);
};
</script>

<style scoped>
.logo-btn:hover {
  transform: scale(1.1);
  filter: brightness(1.2);
}

.cursor-pointer {
  cursor: pointer;
}
</style>
<style scoped>
:deep(.v-list-item__content),
:deep(.v-list-item__spacer),
.custom-list-item {
  overflow: visible !important;
}

.app-badge-icon {
  position: absolute;
  width: 26px;
  height: 26px;
  bottom: -6px;
  right: -6px;
  z-index: 100;
  object-fit: contain;
  filter: drop-shadow(0px 2px 4px rgba(0, 0, 0, 0.8));
  pointer-events: none;
}

/* Tút lại viền và bóng đổ cho Box Menu */
.menu-context-box {
  border: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5) !important;
}

/* Ép chiều cao tối thiểu và hiệu ứng mượt */
.custom-context-item,
.custom-context-item-error {
  min-height: 48px !important;
  padding: 0 12px !important;
  transition: all 0.2s ease;
}

/* Nền xám nhạt mờ khi Hover vào nút CHƠI / CÀI ĐẶT */
.custom-context-item:hover {
  background-color: rgba(255, 255, 255, 0.06) !important;
}

/* --- ĐẶC BIỆT CHO NÚT GỠ CÀI ĐẶT --- */
/* Màu nền xám/đỏ nhạt mặc định giống y chang trong ảnh của bạn */

/* Khi hover vào sẽ ửng đỏ lên một chút cho nguy hiểm */
.custom-context-item-error:hover {
  background-color: rgba(255, 82, 82, 0.12) !important;
}
</style>