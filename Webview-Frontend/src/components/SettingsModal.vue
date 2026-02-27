<template>
  <v-dialog v-model="isOpen" max-width="800" height="600" transition="dialog-bottom-transition"
    @update:model-value="(val) => !val && closeModal()">
    <v-card class="rounded-xl overflow-hidden" :color="isDark ? '#121212' : '#FFFFFF'" :style="{
      backdropFilter: 'blur(20px)',
      border: isDark ? '1px solid rgba(255,255,255,0.1)' : '1px solid rgba(0,0,0,0.05)'
    }">

      <v-toolbar color="transparent" flat class="px-4 mt-2">
        <v-toolbar-title class="text-h5 font-weight-bold">Cài đặt hệ thống</v-toolbar-title>
        <v-spacer></v-spacer>
        <v-btn icon="mdi-close" variant="text" @click="closeModal"></v-btn>
      </v-toolbar>

      <div class="d-flex flex-row h-100" style="min-height: 400px;">

        <v-tabs v-model="tab" direction="vertical" color="warning" class="px-2" style="width: 180px;">
          <v-tab value="general" class="justify-start rounded-lg mb-1">
            <v-icon start icon="mdi-cog-outline"></v-icon> Chung
          </v-tab>
          <v-tab value="update" class="justify-start rounded-lg">
            <v-icon start icon="mdi-update"></v-icon>Cập nhật
          </v-tab>
          <v-tab value="about" class="justify-start rounded-lg">
            <v-icon start icon="mdi-information-outline"></v-icon> Giới thiệu
          </v-tab>
        </v-tabs>

        <div class="border-end h-100 mx-1" :class="isDark ? 'border-white-50' : 'border-black-25'"></div>



        <v-window v-model="tab" class="flex-grow-1 pa-6">
          <!-- Tab General -->
          <v-window-item value="general">
            <div class="text-h6 font-weight-bold mb-4">Giao diện</div>

            <v-list bg-transparent class="pa-0">
              <v-list-item class="px-0">
                <v-list-item-title class="text-medium-emphasis">Chế độ hiển thị</v-list-item-title>
                <v-list-item-subtitle>Thay đổi giữa giao diện Sáng và Tối</v-list-item-subtitle>

                <template v-slot:append>
                  <v-btn-toggle v-model="themePreference" mandatory color="warning" variant="tonal" rounded="pill"
                    @update:model-value="applyThemePreference">
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
            </v-list>

            <v-divider class="my-6 border-opacity-25"></v-divider>

            <div class="text-h6 font-weight-bold mb-4">Khởi động</div>
            <v-switch label="Tự động chạy cùng Windows" color="warning" hide-details inset></v-switch>
            <br>
            <div class="text-h6 font-weight-bold mb-4">Cài đặt ứng dụng</div>
            <v-switch label="Giữ lại tệp sau khi cài đặt (chỉ áp dụng cho giả lập công khai)" color="warning"
              hide-details inset></v-switch>
          </v-window-item>

          <!-- Tab update -->
          <v-window-item value="update">
            <div class="mt-4 px-2">

              <div class="d-flex align-center justify-space-between mb-6 px-2">

                <div>
                  <div class="text-subtitle-2 text-grey mb-1">
                    Phiên bản UI/UX:
                    <span class="font-weight-bold ml-1" :class="isDark ? 'text-white' : 'text-black'">
                      {{ props.app[0]?.FE_version }}
                    </span>
                  </div>
                  <div class="text-subtitle-2 text-grey">
                    Phiên bản Client:
                    <span class="font-weight-bold ml-1" :class="isDark ? 'text-white' : 'text-black'">
                      {{ props.app[0]?.BE_version || 'Không xác định' }}
                    </span>
                  </div>
                </div>

                <v-btn color="primary" variant="tonal" class="rounded-lg update-btn px-5" height="48"
                  @click="$emit('check-update')">
                  <v-icon start icon="mdi-refresh" size="24" class="update-icon"></v-icon>
                  Kiểm tra cập nhật
                </v-btn>

              </div>

              <v-divider class="mb-5 opacity-20"></v-divider>

              <div class="px-2">
                <div class="text-body-1 font-weight-bold mb-3 d-flex align-center">
                  <v-icon icon="mdi-text-box-outline" start color="primary" size="small"></v-icon>
                  Chi tiết bản cập nhật mới nhất (Phiên bản: {{ props.app[0]?.FE_version }})
                </div>

                <v-card variant="flat" :color="isDark ? 'rgba(255,255,255,0.03)' : 'rgba(0,0,0,0.03)'"
                  class="pa-4 rounded-lg text-body-2 text-grey-lighten-1"
                  style="white-space: pre-line; max-height: 220px; overflow-y: auto; border: 1px solid rgba(255,255,255,0.05);">
                  {{ props.app[0]?.changelog || 'Không có thông tin thay đổi nào được ghi nhận.' }}
                </v-card>
              </div>

            </div>
          </v-window-item>


          <!-- Tab About -->
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

              <div class="mt-8">
                <v-btn variant="text" color="warning" prepend-icon="mdi-github">Source Code</v-btn>
                <v-btn variant="text" color="info" prepend-icon="mdi-web">Trang chủ</v-btn>
              </div>
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

const props = defineProps(['app']);
const emit = defineEmits(['close', 'check-update']);
const theme = useTheme();

const isDark = computed(() => theme.global.current.value.dark);
const isOpen = ref(true);
const tab = ref('general');
const themeName = ref(theme.global.name.value);

// Hàm gửi yêu cầu lấy version
const GetClientVersion = () => {
  if (window.chrome?.webview) {
    // 1. Gửi yêu cầu lên C#
    window.chrome.webview.postMessage({ type: "GET_CLIENT_VERSION" });
  }
};

onMounted(() => {
  GetClientVersion();

  // 2. Chỉ đăng ký lắng nghe tin nhắn MỘT LẦN khi mount component
  if (window.chrome?.webview) {
    window.chrome.webview.addEventListener('message', (event) => {
      const response = event.data;
      if (response.type === 'CLIENT_VERSION') {
        // Cập nhật vào mảng manifest (phần tử 0)
        // Lưu ý: Trong thực tế nên dùng emit, nhưng với cấu trúc hiện tại của bạn thì viết như sau:
        props.app[0].BE_version = response.version;
        props.app[0].BE_versioncode = response.versioncode;
      }
    });
    window.chrome.webview.postMessage({ type: "CHANGE_TITLE", title: "Cài đặt" });
  }

});

const closeModal = () => {
  isOpen.value = false;
  setTimeout(() => emit('close'), 300);
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "CHANGE_TITLE" })
  }
};



const themePreference = ref(localStorage.getItem('theme-preference') || 'system');

const applyThemePreference = (val) => {
  // 1. Lưu tùy chọn vào localStorage
  localStorage.setItem('theme-preference', val);

  let targetTheme = val;

  // 2. Nếu là 'system', kiểm tra màu của Windows
  if (val === 'system') {
    targetTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  // 3. Cập nhật Vuetify Theme
  theme.global.name.value = targetTheme;

  // 4. Báo cho C# để đổi màu thanh Title
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({
      type: "THEME_CHANGED",
      mode: targetTheme
    });
  }
};
</script>
<style scoped>
/* Hiệu ứng xoay mượt mà cho Icon khi lia chuột vào nút */
.update-icon {
  transition: transform 0.6s cubic-bezier(0.4, 0, 0.2, 1);
}

.update-btn:hover .update-icon {
  transform: rotate(180deg);
}

/* Tùy chỉnh thanh cuộn cho khu vực Changelog nhìn xịn hơn */
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