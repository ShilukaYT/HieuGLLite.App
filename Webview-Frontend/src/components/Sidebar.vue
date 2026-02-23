<template>
  <v-navigation-drawer permanent rail rail-width="100"
    :color="isDark ? 'rgba(18, 18, 18, 0.8)' : 'rgba(255, 255, 255, 0.8)'" class="border-0"
    style="backdrop-filter: blur(20px); border-right: 1px solid rgba(255,255,255,0.05) !important;">
    <div class="d-flex flex-column h-100 py-4">

      <div class="d-flex justify-center mb-6">
        <v-menu location="right center" transition="slide-x-transition">
          <template v-slot:activator="{ props }">
            <v-avatar size="60" variant="tonal" v-bind="props" class="cursor-pointer logo-btn"
              style="transition: transform 0.3s ease;">
              <img src="../assets/images/logo.png" style="width: 100%; height: 100%; object-fit: contain;">
            </v-avatar>
          </template>

          <v-list :bg-color="isDark ? '#1e1e1e' : 'white'" class="rounded-xl ml-4 pa-2" elevation="12" min-width="180">
            <v-list-subheader class="text-uppercase font-weight-bold text-caption">Kết nối</v-list-subheader>

            <v-list-item @click="openExternal('https://facebook.com/hieushiluka')" rounded="lg" class="mb-1">
              <template v-slot:prepend>
                <v-icon icon="mdi-facebook" color="isDark ? 'white':'black'"></v-icon>
              </template>
              <v-list-item-title>Facebook</v-list-item-title>
            </v-list-item>

            <v-list-item @click="openExternal('http://youtube.com/channel/UCLd32LcLg-Tx6mbGPOqEbvQ?sub_confirmation=1')"
              rounded="lg" class="mb-1">
              <template v-slot:prepend>
                <v-icon icon="mdi-youtube" color="isDark ? 'white':'black'"></v-icon>
              </template>
              <v-list-item-title>Youtube</v-list-item-title>
            </v-list-item>

            <v-list-item @click="openExternal('http://tiktok.com/@hieushiluka')" rounded="lg" class="mb-1">
              <template v-slot:prepend>
                <Icon icon="ic:round-tiktok" width="25px" :color="isDark ? 'white' : 'black'" />
              </template>
              <v-list-item-title>TikTok</v-list-item-title>
            </v-list-item>

            <v-list-item @click="openExternal('https://github.com/ShilukaYT')" rounded="lg" class="mb-1">
              <template v-slot:prepend>
                <v-icon icon="mdi-github" :color="isDark ? 'white' : 'black'"></v-icon>
              </template>
              <v-list-item-title>GitHub</v-list-item-title>
            </v-list-item>

            <v-divider class="my-2"></v-divider>

            <v-list-subheader class="text-uppercase font-weight-bold text-caption">Ủng hộ</v-list-subheader>

            <v-list-item @click="openPopup('https://img.vietqr.io/image/MB-0967420947-compact.jpg', 540, 540)"
              rounded="lg" base-color="warning">
              <template v-slot:prepend>
                <v-icon icon="mdi-coffee"></v-icon>
              </template>
              <v-list-item-title class="font-weight-bold">Ủng hộ tôi qua MB</v-list-item-title>
            </v-list-item>
            <v-list-item @click="openPopup('https://img.vietqr.io/image/Momo-0967420947-compact.jpg', 540, 540)"
              rounded="lg" base-color="warning">
              <template v-slot:prepend>
                <v-icon icon="mdi-coffee"></v-icon>
              </template>
              <v-list-item-title class="font-weight-bold">Ủng hộ tôi qua Momo</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
      </div>

      <v-list density="compact" class="flex-grow-1 bg-transparent px-2" nav>
        <v-list-item v-for="app in apps" :key="app.id" :value="app.id" :active="activeId === app.id"
          active-color="yellow" class="mb-3 rounded-xl py-2" @click="$emit('change-app', app)">
          <template v-slot:default>
            <div class="d-flex justify-center w-100">
              <v-avatar size="52" :class="activeId === app.id ? 'elevation-8' : 'elevation-2'" rounded="lg"
                style="transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);">
                <v-img :src="app.icon" :alt="app.name"></v-img>
              </v-avatar>
            </div>
          </template>
        </v-list-item>
      </v-list>

      <div class="d-flex flex-column align-center mt-auto">
        <v-btn icon="mdi-cog" variant="text" :color="isDark ? 'grey-lighten-1' : 'grey-darken-1'" size="large"
          class="mb-4" @click="$emit('open-settings')"></v-btn>

       <div class="d-flex flex-column align-center mt-auto pb-4">
  
  <v-menu location="right bottom" transition="slide-x-transition" :close-on-content-click="false">
    
    <template v-slot:activator="{ props }">
      <v-badge :dot="!user" :color="user ? 'success' : 'grey'" location="bottom right" offset-x="3" offset-y="3">
        <v-avatar 
          v-bind="props" 
          size="44" 
          class="elevation-4 cursor-pointer" 
          :style="{ border: user ? '2px solid #5865F2' : '2px solid #444' }"
        >
          <v-img :src="user ? user.avatar : defaultAvt" draggable="false"></v-img>
        </v-avatar>
      </v-badge>
    </template>

    <v-card :color="isDark ? '#1e1e1e' : 'white'" min-width="250" class="rounded-xl ml-4">
      
      <template v-if="user">
        <v-list bg-transparent>
          <v-list-item :prepend-avatar="user.avatar" :title="user.name" :subtitle="user.email">
            <template v-slot:append>
              <v-icon icon="mdi-discord" color="#5865F2"></v-icon>
            </template>
          </v-list-item>
        </v-list>
        <v-divider class="mx-4"></v-divider>
        <v-list density="compact" nav bg-transparent class="pa-2">
          <v-list-item @click="handleLogout" prepend-icon="mdi-logout" title="Đăng xuất" color="error"></v-list-item>
        </v-list>
      </template>

      <template v-else>
        <div class="pa-6 text-center">
          <v-icon icon="mdi-account-circle-outline" size="48" class="mb-2 opacity-20"></v-icon>
          <div class="text-body-2 mb-4">Đăng nhập để đồng bộ dữ liệu</div>
          
          <v-btn block color="#5865F2" prepend-icon="mdi-discord" class="rounded-lg" @click="handleLogin">
            Đăng nhập với Discord
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
defineProps(['apps', 'activeId']);
defineEmits(['change-app', 'open-settings']);
import { ref, computed, onMounted } from 'vue';
import { useTheme } from 'vuetify';
import { Icon } from '@iconify/vue';

import defaultAvt from '@/assets/images/avtDefault.jpg'; // Ảnh mặc định


const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

const user = ref(null);

const openExternal = (url) => {
  if (window.chrome?.webview) {
    // Gửi yêu cầu mở link ra trình duyệt ngoài lên C#
    const features = 'width=500,height=700,left=100,top=100,menubar=no,status=no';
    window.chrome.webview.postMessage({
      type: "OPEN_URL",
      url: url
    });
  } else {
    // Nếu chạy trên trình duyệt thường thì vẫn mở tab mới
    window.open(url, '_blank');
  }
  
};

onMounted(() => {
  if (window.chrome?.webview) {
    // 1. Vừa bật app lên là hối C# kiểm tra tự động liền!
    window.chrome.webview.postMessage({ type: "CHECK_AUTO_LOGIN" });

    // 2. Lắng nghe C# trả lời
    window.chrome.webview.addEventListener('message', (event) => {
      if (event.data.type === 'USER_LOGGED_IN') {
        user.value = event.data.data;
      }
    });
  }
});
const openPopup = (url, width, height) => {
  // Tính toán để cửa sổ hiện ra chính giữa màn hình
  const left = (window.screen.width / 2) - (width / 2);
  const top = (window.screen.height / 2) - (height / 2);

  // Các thuộc tính để ép trình duyệt mở cửa sổ nhỏ (Popup mode)
  const features = `width=${width},height=${height},left=${left},top=${top},menubar=no,toolbar=no,location=no,status=no,resizable=no`;

  // Lệnh mở cửa sổ
  window.open(url, 'HieuGLLitePopup', features);
};

const handleLogin = () => {
  if (window.chrome?.webview) {
    // Chỉ việc hét lên cho C# biết: "Ê, ĐĂNG NHẬP ĐI!"
    window.chrome.webview.postMessage({ type: "LOGIN_DISCORD" });
  }
};

// Hàm gắn vào nút "Đăng xuất"
const handleLogout = () => {
  user.value = null; // Giao diện về mặc định
  if (window.chrome?.webview) {
    // Kêu C# xóa file token đi
    window.chrome.webview.postMessage({ type: "LOGOUT_DISCORD" });
  }
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