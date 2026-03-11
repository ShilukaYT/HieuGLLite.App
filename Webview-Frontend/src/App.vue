<template>
  <v-app>
    <v-fade-transition>

      <div v-if="isLoading" class="loading-screen" :class="isDark ? 'dark' : 'light'">
        <div class="loading-content">

          <div class="video-container mb-6">
            <video v-if="isDark" autoplay muted loop playsinline class="loading-video">
              <source :src=videoPathDark type="video/mp4">
            </video>
            <video v-else autoplay muted loop playsinline class="loading-video">
              <source :src=videoPathLight type="video/mp4">
            </video>

          </div>

          <div class="loading-text font-weight-black mt-4">

          </div>
        </div>
      </div>
    </v-fade-transition>
    <div id="app-container" :style="bgStyle"
      :class="{ 'is-system-locked': isAppLocked || isMaintenance || isNotInWebView }">
      <div class="overlay"></div>
      <MaintenanceModal v-if="isMaintenance" @close="isMaintenance = false; manifest[0].isMaintenance = false" />
      <WarningModal v-else-if="isNotInWebView" @close="isNotInWebView = false" />



      <div v-if="!isAppLocked">


        <Sidebar :apps="apps" :active-id="selectedApp?.id" @change-app="selectedApp = $event"
          @open-settings="showSettings = true" />

        <v-main class="h-100 w-100">
          <GamePage v-if="selectedApp && !isMaintenance && !isNotInWebView" :app="selectedApp"
            :downloading-apps="downloadingApps" :stage-config="stageConfig" @play="handlePlayApp(selectedApp)"
            @open-install="showModal = true" @kill-app="handleKillApp(selectedApp)" @confirm-cancel="confirmCancel"
            @toggle-pause="togglePause" @extra-action="handleExtraAction" />
        </v-main>

        <div class="download-notifier-container">
          <v-fade-transition group>
            <v-card v-for="(info, appId) in downloadingApps" :key="appId" v-show="appId !== selectedApp?.id"
              class="mb-3 download-mini-card" elevation="8" :theme="isDark ? 'dark' : 'light'"
              @click="selectedApp = apps.find(a => a.id === appId)">
              <v-card-text class="pa-3">
                <div class="d-flex align-center">
                  <v-avatar size="32" rounded="sm" class="mr-3">
                    <v-img :src="apps.find(a => a.id === appId)?.icon"></v-img>
                  </v-avatar>

                  <div class="flex-grow-1">
                    <div class="d-flex justify-space-between align-center mb-1">
                      <span class="text-caption font-weight-black text-truncate" style="max-width: 120px;">
                        {{apps.find(a => a.id === appId)?.name}}
                      </span>
                      <span class="text-caption text-grey" style="font-size: 10px !important;">
                        {{ info.status?.includes('DOWNLOADING') ? info.speed : '' }}
                      </span>
                    </div>

                    <v-progress-linear :model-value="info.percent" :color="stageConfig[info.status]?.color" height="6"
                      rounded :indeterminate="stageConfig[info.status]?.loading"></v-progress-linear>

                    <div class="d-flex justify-space-between mt-1" style="font-size: 9px; opacity: 0.7;">
                      <span>{{ stageConfig[info.status]?.text }}</span>
                      <span v-if="info.status?.includes('DOWNLOADING')">{{ info.downloaded }}</span>
                    </div>
                  </div>
                </div>
              </v-card-text>
            </v-card>
          </v-fade-transition>
        </div>

        <InstallModal v-if="showModal && selectedApp" :app="selectedApp" @close="showModal = false"
          @confirm="handleInstall" />
        <SettingsModal v-if="showSettings" @close="showSettings = false" @check-update="handleManualUpdateCheck"
          @get-apps="handleGetApps" @clear-cache="handleClearCache" @uninstall="handleUninstall" :app="manifest" />

        <v-snackbar v-model="showSnackbar" :timeout="2000" :color="isDark ? '#1e1e1e' : 'white'" location="top"
          rounded="pill" class="mt-4">
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
// Thay vì tự gán tay, chúng ta cho nó tự động nội suy
const isAppLocked = computed(() => {
  return isNotInWebView.value ||
    isMaintenance.value ||
    (showUpdateModal.value && updateData.value.isForceUpdate);
});
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

const apps = ref([]);

//json chứa thông tin phiên bản app
const isMaintenance = ref(false);
const manifest = ref([
  {
    FE_version: '26.3.9',
    FE_versioncode: '260309',
    BE_version: null,
    BE_versioncode: null,
    BE_version_latest: '26.3.9',
    BE_versioncode_latest: '260309',
    release_date: '2026-03-11',
    changelog: 'Yêu cầu phiên bản Client: 26.3.9\n\nThêm tab "Ứng dụng" trong cài đặt hệ thống\n - Cho phép xóa bộ nhớ đệm ứng dụng\n - Giờ đây có thể gỡ cài đặt ứng dụng\nSửa lỗi khi cài đặt giả lập trùng lặp\nSửa lỗi đồng bộ trạng thái ứng dụng trên sidebar',
    isMaintenance: false,
    isForceUpdate: false
  }
])

const isLoading = ref(true);
const videoPathDark = ref('http://hieugllite.app/videos/LoadingScreen_dark.mp4');
const videoPathLight = ref('http://hieugllite.app/videos/LoadingScreen_light.mp4');

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
    videoPathDark.value = './assets/videos/LoadingScreen_dark.mp4';
    videoPathLight.value = './assets/videos/LoadingScreen_light.mp4';
    console.log("Video sử dụng: " + videoPathDark)
    // Đếm ngược 5 giây tắt Loading
    setTimeout(() => {
      isLoading.value = false; // 1. Tắt lớp video che màn hình đi
    }, 5000);
  } else {
    window.chrome.webview.postMessage({ type: "GET_CLIENT_VERSION" });
    window.chrome.webview.postMessage({ type: "GET_APPS" });
    window.chrome.webview.addEventListener('message', (event) => {
      const response = event.data;
      if (response.type === 'CLIENT_VERSION') {
        // Cập nhật vào mảng manifest (phần tử 0)
        // Lưu ý: Trong thực tế nên dùng emit, nhưng với cấu trúc hiện tại của bạn thì viết như sau:
        manifest.value[0].BE_version = response.version;
        manifest.value[0].BE_versioncode = response.versioncode;
      }
      // HỨNG DANH SÁCH GAME (MỚI)

      if (response.type === 'APPS_DATA') {
        apps.value = response.data;
        // Dòng này sẽ reset lựa chọn của người dùng về App số 0
        if (apps.value.length > 0) {
          selectedApp.value = apps.value[0];
        }
        snackbarText.value = "Danh sách ứng dụng đã được cập nhật!";
        showSnackbar.value = true;



        window.chrome.webview.postMessage({
          type: "PUSH_FE_VERSION",
          version: manifest.value[0].FE_version
        });

        console.log("Đã tải xong danh sách Game từ C#!", apps.value);
      }
      // Hứng lệnh đổi trạng thái nút Play từ C#
      if (response.type === 'APP_STATE_CHANGED') {
        const targetApp = apps.value.find(a => a.id === response.appId);
        if (targetApp) {
          // Bơm thêm biến isRunning vào thẳng app hiện tại
          targetApp.isRunning = response.isRunning;
        }
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

    window.chrome.webview.postMessage({ type: 'SYNC_DOWNLOAD_STATUS' });


    // Đếm ngược 5 giây tắt Loading
    setTimeout(() => {
      isLoading.value = false; // 1. Tắt lớp video che màn hình đi

      // 2. NGAY SAU KHI TẮT LOADING, MỚI GỌI HÀM KIỂM TRA
      if (!isMaintenance.value) {
        console.log("Đã tắt Loading -> Bắt đầu auto-check cập nhật!"); // Báo log để F12 dễ nhìn
        checkForUpdates(false);
      }
    }, 2000);


  }

  isMaintenance.value = manifest.value[0].isMaintenance;



});



const selectedApp = ref(apps.value[0]);

const bgStyle = computed(() => {
  // 1. NẾU APP ĐANG BỊ KHÓA (Bảo trì, Bắt buộc cập nhật, hoặc Web ngoài)
  if (isAppLocked.value || isMaintenance.value || isNotInWebView.value) {
    return {
      // Bạn có thể thay link này bằng một ảnh nền "System Warning/Hacker" tùy thích
      backgroundImage: `url('https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?q=80&w=1920&auto=format')`
      // Nếu dùng ảnh trong source: backgroundImage: `url('/src/assets/images/locked-bg.jpg')`
    };
  }

  // 2. NẾU BÌNH THƯỜNG: Trả về ảnh nền của Game/App đang được chọn
  return {
    backgroundImage: `url(${selectedApp.value?.background})`
  };
});

// Gửi lệnh CHƠI xuống WinForms C#
const handlePlayApp = (app) => {
  if (app && app.id) {
    console.log(`Đang yêu cầu C# khởi động ứng dụng: ${app.name} (${app.id})`);

    // Bắn lệnh "PLAY" và ID của app xuống cho C#
    window.chrome.webview.postMessage({
      type: "PLAY",
      appId: app.id
    });
  } else {
    alert("Lỗi: Không xác định được ứng dụng cần mở!");
  }
};

//gửi lệnh EXTRA ACTION xuống WinForms C#
const handleExtraAction = (payload) => {
  if (window.chrome?.webview) {

    window.chrome.webview.postMessage({
      type: payload.type, // Sẽ là OPEN_MULTI, UNINSTALL, v.v.
      appId: payload.id
    });
    console.log(`Đang gửi lệnh ${payload.type} cho app ID: ${payload.id}`);
  }
};

// Gửi lệnh ĐÓNG ỨNG DỤNG xuống WinForms C#
const handleKillApp = (app) => {
  if (app && app.id) {
    console.log(`Đang yêu cầu C# đóng ứng dụng: ${app.name} (${app.id})`);

    // Bắn lệnh "KILL_APP" và ID của app xuống cho C#
    window.chrome.webview.postMessage({
      type: "KILL_APP",
      appId: app.id
    });
  } else {
    alert("Lỗi: Không xác định được ứng dụng cần đóng!");
  }
};

// Gửi lệnh CÀI ĐẶT (cùng với Version & OS đã chọn từ Modal) xuống WinForms C#
// App.vue - Tìm đến hàm handleInstall và thay bằng đoạn này:
const handleInstall = (selection) => {
  showModal.value = false;

  // Log để bạn kiểm tra dữ liệu trước khi bắn xuống C#
  console.log("Bắt đầu quy trình INSTALL AIO cho:", selectedApp.value.id);

  //load status waiting
  downloadingApps.value[selectedApp.value.id] = {
    percent: 0,
    status: 'WAITING',
    speed: '',
    downloaded: ''
  };

  if (window.chrome?.webview) {
    // Trích xuất dữ liệu từ các Object mà InstallModal gửi lên
    const v = selection.versionObj;
    const a = selection.androidObj;

    window.chrome.webview.postMessage({
      type: "INSTALL",
      appId: selectedApp.value.id,

      // Thông tin bộ cài (.exe)
      exeUrl: v.downloadURL,
      exeName: v.fileName,
      exeHash: v.SHA256,

      // Thông tin Android Image (.bin)
      androidUrl: a.downloadURL,
      androidName: a.fileName,
      androidHash: a.SHA256,
      androidCode: a.code, // Ví dụ: Nougat32

      // Đường dẫn cài đặt người dùng chọn
      installPath: selection.path
    });
  }
};

// App.vue
const togglePause = (appId) => {
  const app = downloadingApps.value[appId];
  // Nếu status là PAUSED thì gửi lệnh RESUME, ngược lại gửi PAUSE
  const isPaused = app.status === 'PAUSED';

  window.chrome.webview.postMessage({
    type: isPaused ? "RESUME_DOWNLOAD" : "PAUSE_DOWNLOAD",
    appId: appId
  });
};

const confirmCancel = (appId) => {

  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({
      type: "CANCEL_DOWNLOAD", //
      appId: appId
    });
  }

  //listen for message from C# to confirm cancellation
  window.chrome.webview.addEventListener('message', (event) => {
    const res = event.data;
    if (res.type === 'DOWNLOAD_CANCELLED' && res.appId === appId) {
      console.log(`Đã xác nhận hủy tải: ${appId}`);
      delete downloadingApps.value[appId]; // Xóa ngay khỏi UI
    }
  });

}

//==================== Cập nhật =====================
// --- BIẾN TRẠNG THÁI CẬP NHẬT ---
const showUpdateModal = ref(false);
const showSnackbar = ref(false);
const snackbarText = ref("");
const updateData = ref({
  versionName: "",
  changelog: "",
  isForceUpdate: ""
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
  } else if (isManual) {
    // Nếu bấm bằng tay mà KHÔNG có bản mới -> Hiện thông báo
    snackbarText.value = `Bạn đang sử dụng phiên bản mới nhất (${appInfo.BE_version_latest})!`;
    showSnackbar.value = true;
  }
};

const handleDownloadUpdate = (url) => {
  // Chỉ thực hiện khi người dùng thực sự nhấn nút "Tải về" trên Modal cập nhật
  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "OPEN_URL", url: url });
  }
  showUpdateModal.value = false;
};

// --- HÀM HỨNG SỰ KIỆN TỪ SETTINGS ---
const handleManualUpdateCheck = () => {
  checkForUpdates(true); // Truyền true vì đây là thao tác thủ công
};

//=================================================================

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

//==============Cài đặt==================

// App.vue
const downloadingApps = ref({});

const stageConfig = {
  'WAITING': { text: 'Đang chờ tải xuống...', color: 'amber', loading: true },
  'DOWNLOADING_EXE': { text: 'Đang tải xuống 1/2...', color: 'blue', loading: false },
  'DOWNLOADING_ANDROID': { text: 'Đang tải xuống 2/2...', color: 'cyan', loading: false },
  'MERGING': { text: 'Đang hoàn tất...', color: 'deep-purple', loading: true },
  'VERIFYING': { text: 'Đang kiểm tra tính toàn vẹn...', color: 'teal', loading: true },
  'INSTALLING': { text: 'Đang cài đặt...', color: 'orange', loading: true },
  'CLEANINGUP': { text: 'Đang dọn dẹp...', color: 'grey', loading: true },
  'BACKUP_RESTORE': { text: 'Đang khởi tạo trình sao lưu và khôi phục...', color: 'indigo', loading: true },
  'UNINSTALLING': { text: 'Đang gỡ cài đặt...', color: 'red', loading: true },
};


const handleGetApps = () => {
  console.log("Yêu cầu tải lại danh sách Game từ C#!");


  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "GET_APPS" });
    console.log("Đã gửi yêu cầu GET_APPS lên C#");
  } else {
    console.error("Không tìm thấy môi trường WebView2!");
  }
};

window.chrome.webview.addEventListener('message', (event) => {
  const res = event.data; // Thống nhất dùng tên biến 'res'

  switch (res.type) {
    // 1. CẬP NHẬT TIẾN ĐỘ & TRẠNG THÁI

    case 'DOWNLOAD_PROGRESS':
    case 'DOWNLOAD_STATUS':
      downloadingApps.value[res.appId] = {
        ...downloadingApps.value[res.appId],
        percent: res.percent ?? downloadingApps.value[res.appId]?.percent ?? 0,
        status: res.status,
        speed: res.speed || '',
        downloaded: res.downloaded || ''
      };
      break;

    // 2. KHI CÀI ĐẶT HOÀN TẤT
    case 'DOWNLOAD_COMPLETED':
      // Bước A: Tìm App trong danh sách hiển thị để đổi nút
      const targetApp = apps.value.find(a => a.id === res.appId);
      if (targetApp) {
        targetApp.isInstalled = true; // Lật công tắc Đã cài đặt
        if (res.savedPath) targetApp.programPath = res.savedPath; // Lưu path thực tế
        console.log(`Đã kích nổ thành công: ${targetApp.name}`);
      }

      // Bước B: Giữ thông báo 3 giây rồi tự biến mất
      setTimeout(() => {
        delete downloadingApps.value[res.appId];
      }, 3000);
      break;

    // 3. KHI CÓ LỖI XẢY RA
    case 'DOWNLOAD_FAILED':
      console.error("Lỗi cài đặt:", res.error);
      // Giữ thông báo lỗi để người dùng kịp đọc
      setTimeout(() => {
        delete downloadingApps.value[res.appId];
      }, 5000);
      break;

    // 4. ĐỒNG BỘ KHI F5
    // App.vue
    case 'DOWNLOAD_SYNC_DATA':
      res.downloads.forEach(download => {
        const app = this.apps.find(a => a.id === download.appId);
        if (app) {
          app.status = download.status; // Lưu trạng thái vào Vue
          app.percent = download.percent;

          // Nếu status là VERIFYING hoặc INSTALLING -> Hiện hiệu ứng loading chung
          if (download.status === 'VERIFYING' || download.status === 'INSTALLING') {
            app.loading = true; // Bật Spinner hoặc Progress Indeterminate
          }
        }
      });
      break;


    // 5. CẬP NHẬT TRẠNG THÁI APP ĐANG CHẠY (Nút Play/Kill)
    case 'APP_STATE_CHANGED':
      const app = apps.value.find(a => a.id === res.appId);
      if (app) app.isRunning = res.isRunning;
      break;
    case 'DOWNLOAD_CANCELLED':
      console.log(`Đã hủy tải: ${res.appId}`);
      delete downloadingApps.value[res.appId];
      break;

    case 'APP_UNINSTALLED':
      const uninstalledApp = apps.value.find(a => a.id === res.appId);
      if (uninstalledApp) {
        // 1. Chuyển trạng thái để hiện lại nút "CÀI ĐẶT NGAY"
        uninstalledApp.isInstalled = false;
        uninstalledApp.programPath = null;

        // 2. QUAN TRỌNG: Xóa khỏi danh sách đang xử lý để ẩn Progress Bar
        delete downloadingApps.value[res.appId];

        // 3. (Tùy chọn) Hiện thông báo thành công cho người dùng
        snackbarText.value = `Đã gỡ cài đặt thành công ${uninstalledApp.name}`;
        showSnackbar.value = true;

        console.log(`Đã gỡ cài đặt: ${uninstalledApp.name}`);
      }
      break;
    case 'CLEANUP_COMPLETED':

      snackbarText.value = res.message || "Đã xóa bộ nhớ đệm thành công!";
      showSnackbar.value = true;

      console.log("Dọn dẹp hoàn tất!");
      break;
  }
}
);

// Hàm xử lý khi người dùng bấm nút Xóa Cache
const handleClearCache = () => {
  console.log("Đang yêu cầu C# dọn dẹp bộ nhớ đệm...");

  if (window.chrome?.webview) {
    window.chrome.webview.postMessage({ type: "CLEAR_CACHE" });
  } else {
    // Chữa cháy nếu chạy trên trình duyệt web thường để test
    setTimeout(() => {
      isCleaningCache.value = false;
      snackbarText.value = "Chỉ hoạt động trên App thực tế!";
      showSnackbar.value = true;
    }, 1000);
  }
};

const handleUninstall = () =>
{
  console.log("Đang yêu cầu gỡ cài đặt");
  if (window.chrome?.webview)
{
  window.chrome.webview.postMessage({type : "UNINSTALL_APP"})
}
}
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

.is-system-locked .overlay {
  background: rgba(0, 0, 0, 0.75) !important;
  backdrop-filter: blur(8px) grayscale(80%);
  /* Làm nhòe và chuyển ảnh nền sang xám */
  mix-blend-mode: normal !important;
  z-index: 10;
  /* Đẩy lớp phủ lên trên cùng, chỉ chừa lại các Modal */
}

/* App.vue <style> */
.download-notifier-container {
  position: fixed;
  top: 70px;
  /* Cách thanh tiêu đề một khoảng vừa đủ đẹp */
  right: 20px;
  width: 300px;
  z-index: 99;
  /* Cao hơn v-main nhưng thấp hơn các Modal hệ thống */
  pointer-events: none;
}

.download-mini-card {
  pointer-events: auto;
  /* Cho phép tương tác click */
  background: v-bind('isDark ? "rgba(30, 30, 30, 0.9)" : "rgba(255, 255, 255, 0.9)"') !important;
  backdrop-filter: blur(10px);
  border-right: 4px solid #2196F3;
  /* Màu xanh đặc trưng của Download */

  /* App.vue <style> */
  .download-mini-card {
    pointer-events: auto;
    background: v-bind('isDark ? "rgba(30, 30, 30, 0.9)" : "rgba(255, 255, 255, 0.9)"') !important;
    backdrop-filter: blur(10px);
    border-right: 4px solid #2196F3;
    /* Hiệu ứng trượt nhẹ khi xuất hiện */
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  }

  .download-mini-card:hover {
    transform: translateX(-5px);
    /* Nhích nhẹ sang trái khi di chuột vào */
    cursor: pointer;
  }
}

/* Ẩn thanh cuộn cho toàn bộ trang */
html,
body {
  overflow: hidden !important;
  /* Ngăn chặn cuộn */
  height: 100%;
  margin: 0;
}

/* Ẩn thanh cuộn nhưng vẫn cho phép cuộn bằng chuột (nếu cần) */
::-webkit-scrollbar {
  display: none;
}
</style>