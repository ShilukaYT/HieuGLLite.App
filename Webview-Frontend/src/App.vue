<template>
  <v-app>
    <v-fade-transition>

      <div v-if="isLoading" class="loading-screen" :class="isDark ? 'dark' : 'light'">
        <div class="loading-content">

          <div class="video-container mb-6">
            <video v-if="isDark" autoplay muted loop playsinline class="loading-video">
              <source src="./assets/videos/LoadingScreen_dark.mp4" type="video/mp4">
            </video>
            <video v-else autoplay muted loop playsinline class="loading-video">
              <source src="./assets/videos/LoadingScreen_light.mp4" type="video/mp4">
            </video>

          </div>

          <div class="loading-text font-weight-black mt-4">

          </div>
        </div>
      </div>
    </v-fade-transition>
    <div id="app-container" :style="bgStyle">
      <div class="overlay"></div>
      <MaintenanceModal v-if="isMaintenance" @close="manifest[0].isMaintenance = false" />
      <WarningModal v-else-if="isNotInWebView" @close="isNotInWebView = false" />
      
      

      <div v-if="!isAppLocked">


        <Sidebar :apps="apps" :active-id="selectedApp?.id" @change-app="selectedApp = $event"
          @open-settings="showSettings = true" />

        <v-main class="h-100 w-100">
          <GamePage v-if="selectedApp || isMaintenance || isNotInWebView" :app="selectedApp" @play="handlePlay"
            @open-install="showModal = true" />
        </v-main>

        <InstallModal v-if="showModal && selectedApp" :app="selectedApp" @close="showModal = false"
          @confirm="handleInstall" />
        <SettingsModal v-if="showSettings" @close="showSettings = false" @check-update="handleManualUpdateCheck"
          :app="manifest" />
        
        <v-snackbar
          v-model="showSnackbar" :timeout="3000" :color="isDark ? '#1e1e1e' : 'white'" location="top" rounded="pill"
          class="mt-4">
          <div class="text-body-1 font-weight-medium d-flex align-center" :class="isDark ? 'text-white' : 'text-black'">
            <v-icon start icon="mdi-check-circle" color="success"></v-icon>
            {{ snackbarText }}
          </div>
        </v-snackbar>
      </div>
      <UpdateModal v-model="showUpdateModal" :update-data="updateData" @download="handleDownloadUpdate" /> 
    </div>
  </v-app>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import Sidebar from './components/Sidebar.vue';
import GamePage from './components/GamePage.vue';
import InstallModal from './components/InstallModal.vue';
import WarningModal from './components/WarningModal.vue';
import SettingsModal from './components/SettingsModal.vue'; // Thêm dòng này
import MaintenanceModal from './components/MaintenanceModal.vue';
import UpdateModal from './components/UpdateModal.vue';
const showSettings = ref(false); // Thêm dòng này
const isAppLocked = ref(false);

const isNotInWebView = ref(false);

const showModal = ref(false);

import { useTheme } from 'vuetify';
const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

// Overlay sẽ đổi từ đen sang trắng mờ tùy theo theme
const overlayStyle = computed(() => ({
  background: isDark.value
    ? 'linear-gradient(90deg, rgba(0,0,0,0.9) 0%, rgba(0,0,0,0.4) 100%)'
    : 'linear-gradient(90deg, rgba(255,255,255,0.9) 0%, rgba(255,255,255,0.4) 100%)'
}));

// Kiểm tra ngay lúc này
const isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;

// Lắng nghe thay đổi từ hệ thống (không cần C# can thiệp)
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', event => {
  const newColorScheme = event.matches ? "dark" : "light";
  theme.global.name.value = newColorScheme; // Đổi theme Vuetify
  // Sau đó báo ngược lên C# để đổi màu thanh Title như mình đã làm trước đó
});

const apps = ref([
  {
    id: 'bs5', name: 'BlueStacks 5', desc: 'Emulator Android số 1 thế giới. Tối ưu cho PC của bạn.', oem: 'BlueStacks_nxt',
    icon: 'https://cdn-www.bluestacks.com/bs-images/logo-icon.png', background: 'https://support.bluestacks.com/hc/article_attachments/29496373485965',
    isInstalled: false,
    versions: [
      { ver: '5.21.660', androids: [{ name: 'Android 11', code: 'Rvc64' }, { name: 'Android 9', code: 'Pie64' }] },
      { ver: '5.10.0', androids: [{ name: 'Android 7', code: 'Nougat32' }] }
    ]
  },
  {
    id: 'ld9', name: 'LDPlayer 9', desc: 'Trải nghiệm mượt mà không độ trễ.', oem: 'LDPlayer9',
    icon: 'https://img.icons8.com/color/96/ldplayer.png', background: 'https://cellphones.com.vn/sforum/wp-content/uploads/2023/04/tai-ldplayer-2.jpg',
    isInstalled: false,
    versions: [{ ver: '9.0', androids: [{ name: 'Android 9', code: 'P64' }] }]
  }
]);

//json chứa thông tin phiên bản app
const isMaintenance = ref(false);

const manifest = ref([
  {
    FE_version: '26.2.0',
    FE_versioncode: '260200',
    BE_version: null,
    BE_versioncode: null,
    BE_version_latest: '26.3.0',
    BE_versioncode_latest: '260300',
    release_date: '2026-02-01',
    changelog_settings: 'Phiên bản: 26.3.0 (260300)\nCập nhật lớn với nhiều cải tiến về hiệu suất và giao diện người dùng.',
    changelog: 'Cập nhật lớn với nhiều cải tiến về hiệu suất và giao diện người dùng.',
    isMaintenance: false,
    isForceUpdate: false


  }
])

const isLoading = ref(true);

onMounted(() => {
  // 1. Lấy tùy chọn từ localStorage
  const savedPreference = localStorage.getItem('theme-preference') || 'system';
  let targetTheme = savedPreference;

  if (savedPreference === 'system') {
    targetTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  // 2. Cập nhật giao diện Vue
  theme.global.name.value = targetTheme;



  if (!window.chrome || !window.chrome.webview) {
    // Nếu mở bằng Chrome/Edge thường -> Bật cảnh báo
    isNotInWebView.value = true;
    console.warn("Đang chạy trên trình duyệt web thông thường!");
    // Đếm ngược 5 giây tắt Loading
  setTimeout(() => {
    isLoading.value = false; // 1. Tắt lớp video che màn hình đi
  }, 5000);
  } else {
    window.chrome.webview.postMessage({ type: "GET_CLIENT_VERSION" });
      window.chrome.webview.addEventListener('message', (event) => {
      const response = event.data;
      if (response.type === 'CLIENT_VERSION') {
        // Cập nhật vào mảng manifest (phần tử 0)
        // Lưu ý: Trong thực tế nên dùng emit, nhưng với cấu trúc hiện tại của bạn thì viết như sau:
manifest.value[0].BE_version = response.version;
manifest.value[0].BE_versioncode = response.versioncode;
      }
    });
    // Đã nằm trong WinForms
    console.log("Tuyệt vời! Đã kết nối với C# WebView2.");

    // 3. QUAN TRỌNG: Gửi ngay thông báo cho C# để đổi màu Form
    if (window.chrome?.webview) {
      window.chrome.webview.postMessage({
        type: "THEME_CHANGED",
        mode: targetTheme
      });
      

    }

    // Đếm ngược 5 giây tắt Loading
  setTimeout(() => {
    isLoading.value = false; // 1. Tắt lớp video che màn hình đi
    
    // 2. NGAY SAU KHI TẮT LOADING, MỚI GỌI HÀM KIỂM TRA
    if (!isMaintenance.value) {
      console.log("Đã tắt Loading -> Bắt đầu auto-check cập nhật!"); // Báo log để F12 dễ nhìn
      checkForUpdates(false); 
    }
  }, 5000);


  }

  isMaintenance.value = manifest.value[0].isMaintenance;



});



const selectedApp = ref(apps.value[0]);

const bgStyle = computed(() => ({
  backgroundImage: `url(${selectedApp.value?.background})`
}));

// Gửi lệnh CHƠI xuống WinForms C#
const handlePlay = () => {
  console.log("PLAY:", selectedApp.value.id);
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "PLAY", id: selectedApp.value.id });
  }
};

// Gửi lệnh CÀI ĐẶT (cùng với Version & OS đã chọn từ Modal) xuống WinForms C#
const handleInstall = (selection) => {
  showModal.value = false;
  console.log("INSTALL:", selectedApp.value.id, selection);
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({
      type: "INSTALL",
      id: selectedApp.value.id,
      version: selection.version,
      codename: selection.codename
    });
  }
};

//Update app
// --- BIẾN TRẠNG THÁI CẬP NHẬT ---
const showUpdateModal = ref(false);
const showSnackbar = ref(false);
const snackbarText = ref("");
const updateData = ref({
  versionName: "",
  changelog: "",
  isForceUpdate:""
});

// --- HÀM KIỂM TRA CẬP NHẬT (Dùng chung) ---
const checkForUpdates = (isManual = false) => {
  const appInfo = manifest.value[0];

  // Chuyển FE_versioncode và BE_versioncode_latest thành số để so sánh
  const currentCode = parseInt(appInfo.BE_versioncode);
  const latestCode = parseInt(appInfo.BE_versioncode_latest);

  if (latestCode > currentCode) {
    // Nếu có bản mới -> Cập nhật dữ liệu và Bật Modal
    updateData.value = {
      versionName: appInfo.BE_version_latest,
      changelog: appInfo.changelog,
      isForceUpdate: appInfo.isForceUpdate
    };
    showUpdateModal.value = true;
    // NẾU BẢN MỚI NÀY LÀ BẮT BUỘC -> BẬT CÒNG TAY KHÓA APP LUÔN!
    if (appInfo.isForceUpdate) {
      isAppLocked.value = true; 
    }
  } else if (isManual) {
    // Nếu bấm bằng tay mà KHÔNG có bản mới -> Hiện thông báo
    snackbarText.value = `Bạn đang sử dụng phiên bản mới nhất (${appInfo.FE_version})!`;
    showSnackbar.value = true;
  }
};

// --- HÀM HỨNG SỰ KIỆN TỪ SETTINGS ---
const handleManualUpdateCheck = () => {
  checkForUpdates(true); // Truyền true vì đây là thao tác thủ công
};

// // Gửi lệnh khi chuột ấn xuống thanh Title
// const dragWindow = () => {
//   if (window.chrome?.webview) window.chrome.webview.postMessage({ type: "DRAG_WINDOW" });
// };

// // Gửi lệnh thu nhỏ
// const minimizeWindow = () => {
//   if (window.chrome?.webview) window.chrome.webview.postMessage({ type: "MINIMIZE_WINDOW" });
// };

// // Gửi lệnh đóng app
// const closeWindow = () => {
//   if (window.chrome?.webview) window.chrome.webview.postMessage({ type: "CLOSE_WINDOW" });
// };
</script>

<style>
@font-face {
  font-family: 'GoogleSans';
  /* Tên bạn tự đặt */
  src: url('./assets/fonts/GoogleSans-Medium.ttf') format('truetype');
  font-weight: normal;
  font-style: normal;
}

body,
html {
  margin: 0;
  padding: 0;
  width: 100%;
  height: 100%;
  overflow: hidden;
  font-family: 'GoogleSans', sans-serif !important;
}

#app-container {
  display: flex;
  width: 100vw;
  height: 100vh;
  background-size: cover;
  background-position: center;
  transition: background-image 0.4s ease;
  position: relative;
  filter: brightness(1.1)
}

/* Trong App.vue */
.overlay {
  position: absolute;
  inset: 0;
  z-index: 1;
  /* - Giảm 0.9 xuống 0.7 ở điểm bắt đầu (bên trái)
    - Giảm 0.4 xuống 0.1 ở điểm giữa
    - Kết thúc sớm hơn ở 80% thay vì 100% để phần bên phải của banner sáng hoàn toàn
  */
  background: linear-gradient(90deg,
      rgba(0, 0, 0, 0.7) 0%,
      rgba(0, 0, 0, 0.1) 60%,
      transparent 80%);

  /* Thêm hiệu ứng này để làm ảnh trông trong trẻo hơn */
  mix-blend-mode: multiply;
  opacity: 0.8;
  /* Có thể tinh chỉnh thêm độ trong suốt tổng thể ở đây */
  transition: background 0.5s ease;
}

/* --- CSS CHO MÀN HÌNH LOADING --- */
.loading-screen {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  /* Màu nền dựa trên theme để video tệp vào */
  background-color: v-bind('isDark ? "#121212" : "#FFFFFF"');
  transition: background-color 0.3s ease;
}

.loading-content {
  text-align: center;
}

.video-container {
  /* Loại bỏ mọi viền hoặc bóng ở container */
  width: 200px;
  /* Điều chỉnh kích thước tùy video của bạn */
  height: auto;
  margin: 0 auto;
  position: relative;
  border: none !important;
  box-shadow: none !important;
}

.loading-video {
  width: 100%;
  height: auto;
  /* Ngăn khoảng trắng dưới video */

  /* QUAN TRỌNG: Loại bỏ hoàn toàn viền, bo góc và hiệu ứng phát sáng */
  border-radius: 0 !important;
  filter: none !important;
  border: none !important;
  outline: none !important;
}

.loading-text {
  font-family: 'GoogleSans', sans-serif !important;
  letter-spacing: 3px;
  font-size: 0.85rem;
  opacity: 0.6;
  /* Chữ đổi màu theo nền */
  color: v-bind('isDark ? "#FFFFFF" : "#121212"');
}
</style>
<style>
/* Ghi đè biến màu của Vuetify để đồng bộ với màu #121212 của WinForms */
.v-theme--dark {
  --v-theme-surface: 18, 18, 18 !important;
  /* Màu cho các Card, Sheet */
  --v-theme-background: 18, 18, 18 !important;
  /* Màu nền ứng dụng */
}

/* Đảm bảo thẻ v-application luôn dùng màu chuẩn */
.v-application.v-theme--dark {
  background: #121212 !important;
}

/* Sửa lại loading-screen để chắc chắn không bị lệch */
.loading-screen.dark {
  background-color: #121212 !important;
}

* {
  user-select: none;
  /* Ngăn bôi đen */
  -webkit-user-drag: none;
  /* Ngăn kéo thả hình ảnh (dành cho nhân Chromium) */
}

/* Nếu bạn vẫn muốn người dùng bôi đen được văn bản trong các ô nhập liệu (nếu có) */
input,
textarea {
  user-select: text;
}

/* Ngăn kéo ảnh cụ thể cho tất cả thẻ img */
img {
  -webkit-user-drag: none;
  user-select: none;
  pointer-events: none;
  /* Cách này triệt để nhất, nhưng sẽ làm ảnh không click được */
}
</style>