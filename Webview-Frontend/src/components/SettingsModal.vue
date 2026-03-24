<template>
  <v-dialog v-model="isOpen" max-width="800" height="600" transition="dialog-bottom-transition" scrollable persistent
    @update:model-value="(val) => !val && closeModal()">
    <v-card class="rounded-xl overflow-hidden" :color="isDark ? '#121212' : '#FFFFFF'" :style="{
      backdropFilter: 'blur(20px)',
      border: isDark ? '1px solid rgba(255,255,255,0.1)' : '1px solid rgba(0,0,0,0.05)'
    }">

      <v-toolbar color="transparent" flat class="px-4 mt-2">
        <v-toolbar-title class="text-h5 font-weight-bold"><v-icon icon="mdi-cog-outline"></v-icon> Cài đặt hệ
          thống</v-toolbar-title>
        <v-spacer></v-spacer>
        <v-btn icon="mdi-close" variant="text" @click="closeModal"></v-btn>
      </v-toolbar>

      <div class="d-flex flex-row h-100" style="min-height: 400px;">

        <v-tabs v-model="tab" direction="vertical" color="warning" class="px-2" style="width: 180px;">
          <v-tab value="general" class="justify-start rounded-lg mb-1"><v-icon start icon="mdi-cog-outline"></v-icon>
            Chung</v-tab>
          <v-tab value="update" class="justify-start rounded-lg"><v-icon start icon="mdi-update"></v-icon>Cập
            nhật</v-tab>
          <v-tab value="application" class="justify-start round-lg"><v-icon start icon="mdi-application"></v-icon>Ứng
            dụng</v-tab>
          <v-tab value="about" class="justify-start rounded-lg"><v-icon start icon="mdi-information-outline"></v-icon>
            Giới thiệu</v-tab>
        </v-tabs>

        <div class="border-end h-100 mx-1" :class="isDark ? 'border-white-50' : 'border-black-25'"></div>

        <v-window v-model="tab" class="flex-grow-1 pa-6">
          <v-window-item value="general">

            <div class="text-h6 font-weight-bold mb-4"><v-icon icon="mdi-palette-outline"></v-icon> Giao diện</div>
           <v-list bg-transparent class="pa-0">
  <v-list-item class="px-0">
    <v-list-item-title class="text-medium-emphasis">Chế độ hiển thị</v-list-item-title>
    <v-list-item-subtitle>Thay đổi giữa giao diện Sáng và Tối</v-list-item-subtitle>
    
    <template v-slot:append>
      <v-btn-toggle 
        v-model="themePreference" 
        mandatory 
        color="warning" 
        variant="tonal" 
        rounded="pill"
        :disabled="!isWin11"
        @update:model-value="applyThemePreference"
      >
        <v-btn value="light" size="small">
          <v-icon icon="mdi-white-balance-sunny" class="mr-1"></v-icon> Sáng
        </v-btn>
        <v-btn value="dark" size="small">
          <v-icon icon="mdi-moon-waning-crescent" class="mr-1"></v-icon> Tối
        </v-btn>
        <v-btn value="system" size="small">
          <v-icon icon="mdi-monitor" class="mr-1"></v-icon> Hệ thống
        </v-btn>
      </v-btn-toggle>
    </template>
  </v-list-item>

  <v-expand-transition>
    <div v-if="!isWin11" class="text-caption text-error mt-2 px-0 font-italic">
      * Tính năng tùy chỉnh thủ công chỉ hỗ trợ trên Windows 11. Hệ thống đang tự động đồng bộ màu theo Windows của bạn.
    </div>
  </v-expand-transition>
</v-list>

            <v-divider class="my-6 border-opacity-25"></v-divider>

            <div class="text-h6 font-weight-bold mb-4"><v-icon icon="mdi-window-close"></v-icon> Hệ thống</div>
            <v-list bg-transparent class="pa-0">
              <v-list-item class="px-0">
                <v-list-item-title class="text-medium-emphasis">Hành vi đóng cửa sổ</v-list-item-title>
                <v-list-item-subtitle>Khi nhấn nút X ở góc phải màn hình</v-list-item-subtitle>
                <template v-slot:append>
                  <v-btn-toggle v-model="closeBehavior" mandatory color="primary" variant="tonal" rounded="pill"
                    @update:model-value="applyCloseBehavior">
                    <v-btn value="tray" size="small"><v-icon icon="mdi-tray-arrow-down" class="mr-1"></v-icon> Khay hệ
                      thống</v-btn>
                    <v-btn value="exit" size="small"><v-icon icon="mdi-power" class="mr-1"></v-icon> Thoát hẳn</v-btn>
                  </v-btn-toggle>
                </template>
              </v-list-item>
            </v-list>

          </v-window-item>

          <v-window-item value="update">
            <div class="mt-4 px-2">
              <div class="d-flex align-center justify-space-between mb-6 px-2">
                <div>
                  <div class="text-subtitle-2 text-grey mb-1">
                    Phiên bản UI/UX: <span class="font-weight-bold ml-1"
                      :class="isDark ? 'text-white' : 'text-black'">{{
                        props.app?.FE_version }}</span>
                  </div>
                  <div class="text-subtitle-2 text-grey">
                    Phiên bản Client: <span class="font-weight-bold ml-1"
                      :class="isDark ? 'text-white' : 'text-black'">{{
                        props.app?.BE_version || 'Không xác định' }}</span>
                  </div>
                </div>
                <v-btn color="primary" variant="tonal" class="rounded-lg update-btn px-5" height="48"
                  @click="$emit('check-update')">
                  <v-icon start icon="mdi-refresh" size="24" class="update-icon"></v-icon>Kiểm tra cập nhật
                </v-btn>
              </div>
              <v-divider class="mb-5 opacity-20"></v-divider>
              <div class="d-flex align-center justify-space-between mb-6">
                <div>
                  <div class="text-subtitle-1 font-weight-bold mb-0">Cập nhật danh sách ứng dụng</div>
                  <div class="text-caption text-medium-emphasis">Tải lại dữ liệu ứng dụng sau khi mở rộng quyền truy cập
                  </div>
                </div>
                <v-btn 
                  color="primary" 
                  variant="tonal" 
                  class="rounded-lg px-5" 
                  :class="{ 'update-btn': !isCooldown }"
                  height="48" 
                  :disabled="isCooldown"
                  @click="handleGetApps"
                >
                  <v-icon start icon="mdi-refresh" size="24" :class="{ 'update-icon': !isCooldown }"></v-icon>
                  {{ isCooldown ? `Thử lại sau (${cooldownTime}s)` : 'Cập nhật ngay' }}
                </v-btn>
              </div>
              <v-divider class="mb-5 opacity-20"></v-divider>
              <div class="px-2 mb-4">
                <div class="text-body-1 font-weight-bold mb-3 d-flex align-center">
                  <v-icon icon="mdi-text-box-outline" start color="primary" size="small"></v-icon>
                  Chi tiết bản cập nhật (Phiên bản: {{ props.app?.FE_version }})
                </div>
                <v-card variant="flat" :color="isDark ? 'rgba(255,255,255,0.03)' : 'rgba(0,0,0,0.03)'"
                  :class="isDark ? 'pa-4 rounded-lg text-body-2 text-grey-lighten-1' : 'pa-4 rounded-lg text-body-2 text-grey-darken-2'"
                  style="white-space: pre-line; max-height: 220px; overflow-y: auto; border: 1px solid rgba(255,255,255,0.05);">
                  {{ props.app?.changelog || 'Không có thông tin thay đổi nào được ghi nhận.' }}
                </v-card>
              </div>
            </div>
          </v-window-item>

          <v-window-item value="application">
            <div class="mt-4 px-2">
              <div class="px-2 pb-4">
                <div class="text-h6 font-weight-bold mb-4"><v-icon icon="mdi-wrench-outline" start color="primary"
                    size="small"></v-icon> Quản lý ứng dụng</div>
                <v-row>
                  <v-col cols="12" class="py-2">
                    <div
                      class="d-flex align-center justify-space-between bg-surface-variant-light pa-3 rounded-lg border">
                      <div>
                        <div class="text-body-2 font-weight-bold">Dọn dẹp bộ nhớ đệm</div>
                        <div class="text-caption text-medium-emphasis">Giải phóng dữ liệu tạm (Sẽ khởi động chậm hơn vào
                          lần sau)
                        </div>
                      </div>
                      <v-btn color="success" variant="tonal" class="rounded-lg px-4" @click="$emit('clear-cache')">
                        <v-icon start icon="mdi-broom" size="18"></v-icon>DỌN DẸP
                      </v-btn>
                    </div>
                  </v-col>
                  <v-col cols="12" class="py-2">
                    <div
                      class="d-flex align-center justify-space-between bg-surface-variant-light pa-3 rounded-lg border">
                      <div>
                        <div class="text-body-2 font-weight-bold text-error">Gỡ cài đặt</div>
                        <div class="text-caption text-medium-emphasis">Xóa toàn bộ ứng dụng và dữ liệu liên quan</div>
                      </div>
                      <v-btn color="error" variant="tonal" class="rounded-lg px-4" @click="$emit('uninstall')">
                        <v-icon start icon="mdi-delete-outline" size="18"></v-icon>GỠ BỎ
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
                <div class="text-body-2 mb-1">Ứng dụng được phát triển bởi <strong>Hiếu GL Lite</strong> cùng với
                  chatbot
                  <strong>Gemini</strong>
                </div>
                <div class="text-body-2 text-medium-emphasis">Dựa trên nền tảng .NET 8, Vue.js và Vuetify 3. Tối ưu hóa
                  trải
                  nghiệm ứng dụng một cách tốt nhất.</div>
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

// Nhận biến 'settings' từ App.vue gửi sang
const props = defineProps(['app', 'settings', 'isWin11']);
const emit = defineEmits(['close', 'check-update', 'get-apps', 'clear-cache', 'uninstall']);
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
    window.chrome.webview.postMessage({ type: "CHANGE_TITLE", title: "Cài đặt" });
  }
});

const closeModal = () => {
  isOpen.value = false;
  clearInterval(cooldownTimer); // <--- THÊM DÒNG NÀY ĐỂ HỦY ĐỒNG HỒ
  
  setTimeout(() => emit('close'), 300);
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "CHANGE_TITLE" })
  }
};
// ĐÃ KHAI TỬ LOCALSTORAGE - LẤY DATA JSON TỪ PROPS
const themePreference = ref(props.settings?.theme || 'system');
const closeBehavior = ref(props.settings?.minimizeToTray ? 'tray' : 'exit');

const applyThemePreference = (val) => {
  let targetTheme = val;
  if (val === 'system') {
    targetTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
  theme.global.name.value = targetTheme;

  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "THEME_CHANGED", mode: val });
  }
};

const applyCloseBehavior = (val) => {
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({
      type: "SET_CLOSE_BEHAVIOR",
      minimizeToTray: val === 'tray'
    });
  }
};

// --- BIẾN CHO TÍNH NĂNG COOLDOWN ---
const isCooldown = ref(false); // Trạng thái có đang bị khóa hay không
const cooldownTime = ref(0);   // Số giây đếm ngược
let cooldownTimer = null;      // Bộ đếm thời gian

const handleGetApps = () => {
  if (isCooldown.value) return; // Chặn mọi click nếu đang khóa

  // 1. Phát sự kiện cập nhật lên C#
  emit('get-apps');

  // 2. Bắt đầu khóa nút và đặt thời gian chờ (Ví dụ: 15 giây)
  isCooldown.value = true;
  cooldownTime.value = 15;

  // 3. Cho đồng hồ chạy lùi mỗi giây
  cooldownTimer = setInterval(() => {
    cooldownTime.value--;
    
    // Khi đếm về 0 thì mở khóa nút và dọn dẹp đồng hồ
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