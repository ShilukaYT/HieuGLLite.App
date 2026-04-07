<template>
  <v-dialog v-model="isOpen" max-width="600" persistent transition="dialog-bottom-transition">
    <v-card 
      class="rounded-xl pa-2" 
      :color="isDark ? '#121212' : '#FFFFFF'" 
      :style="{
        backdropFilter: 'blur(20px)',
        border: isDark ? '1px solid rgba(255,255,255,0.1)' : '1px solid rgba(0,0,0,0.05)'
      }"
    >
      
      <v-card-title class="d-flex align-center mt-3 px-6">
        <v-avatar size="48" class="mr-4 rounded-lg elevation-4 pa-1" color="transparent">
          <v-img :src="app.icon"></v-img>
        </v-avatar>
        <span class="text-h5 font-weight-bold">
          {{ isMultiInstance ? $t('install_modal.title_create_clone') : $t('install_modal.title_install') }} {{ app.name }}
        </span>
        <v-spacer></v-spacer>
        <v-btn icon="mdi-close" variant="text" size="small" @click="closeModal"></v-btn>
      </v-card-title>

      <v-card-text class="px-6 py-4">
        <div class="text-subtitle-1 font-weight-medium text-medium-emphasis mb-2">
          {{ isMultiInstance ? $t('install_modal.subtitle_clone') : $t('install_modal.subtitle_install') }}
        </div>
        
        <v-row class="mb-2">
          <v-col cols="6" v-if="!isMultiInstance">
            <v-select
              v-model="selectedVerObj"
              :items="app.versions"
              item-title="ver"
              return-object
              :label="$t('install_modal.label_version')"
              variant="solo-filled"
              :bg-color="isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.05)'"
              density="comfortable"
              hide-details
              prepend-inner-icon="mdi-application-cog"
              class="rounded-lg"
              :menu-props="{ maxHeight: 150 }"
            ></v-select>
          </v-col>
          
          <v-col :cols="isMultiInstance ? 12 : 6">
            <v-select
              v-model="selectedAndroidObj"
              :items="availableAndroids"
              item-title="displayName"
              return-object
              :label="$t('install_modal.label_os')"
              variant="solo-filled"
              :bg-color="isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.05)'"
              density="comfortable"
              hide-details
              prepend-inner-icon="mdi-android"
              class="rounded-lg"
              :item-props="item => ({ disabled: item.isDisabled })"
              :menu-props="{ maxHeight: 150 }"
            >
              <template v-slot:no-data>
                <div class="pa-3 text-caption text-center text-grey">
                  {{ $t('install_modal.all_integrated') }}
                </div>
              </template>
            </v-select>
          </v-col>
        </v-row>

        <template v-if="!isMultiInstance">
          <div class="text-subtitle-1 font-weight-medium text-medium-emphasis mb-2 mt-4">
            {{ $t('install_modal.storage_folder') }}
          </div>
          <v-text-field
            v-model="emulatorPath"
            readonly
            variant="solo-filled"
            :bg-color="isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.05)'"
            hide-details
            class="rounded-lg"
            prepend-inner-icon="mdi-folder-open"
          >
            <template v-slot:append-inner>
              <v-btn color="isDark ? 'grey' : 'light-blue'" variant="tonal" size="small" class="rounded-lg font-weight-bold px-4" @click="browseFolder">
                {{ $t('install_modal.btn_browse') }}
              </v-btn>
            </template>
          </v-text-field>
        </template>
      </v-card-text>

      <v-card-actions class="px-6 pb-6 pt-0">
        <v-spacer></v-spacer>
        <v-btn variant="text" class="px-6 font-weight-bold" @click="closeModal">{{ $t('install_modal.btn_cancel') }}</v-btn>
        <v-btn 
  color="blue" 
  variant="elevated" 
  class="px-8 font-weight-bold text-white" 
  rounded="pill" 
  elevation="6"
  prepend-icon="mdi-download" 
  :disabled="!selectedAndroidObj"
  @click="confirmInstall"
>
  {{ isMultiInstance ? $t('install_modal.btn_integrate_now') : $t('install_modal.btn_install_now') }}
</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import { useTheme } from 'vuetify';
import { useI18n } from 'vue-i18n'; // Kéo thư viện dịch thuật vào

const { t } = useI18n(); // Khởi tạo biến t để dùng trong JS

const props = defineProps({
  app: Object,
  isMultiInstance: Boolean 
});
const emit = defineEmits(['close', 'confirm']);

// --- THEME LOGIC ---
const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

const isOpen = ref(true);
const emulatorPath = ref('');

// 1. Chọn Version mặc định là bản đầu tiên
const selectedVerObj = ref(props.app?.versions?.[0] || null);

// 2. Danh sách Android tự động cập nhật và kiểm tra trạng thái "Đã cài"
const availableAndroids = computed(() => {
  const androids = selectedVerObj.value?.androids || [];

  return androids.map(a => {
    // Check xem mã Android (ví dụ: Pie64) đã tồn tại trong danh sách instances chưa
    // Lưu ý: Chỉ áp dụng kiểm tra này nếu đang mở bảng ở chế độ Multi-Instance
    const isAlreadyInstalled = props.isMultiInstance && props.app?.instances?.some(inst => inst.name === a.code);

    return {
      ...a,
      // ĐA NGÔN NGỮ: Dịch chữ (Đã tích hợp)
      displayName: isAlreadyInstalled ? `${a.name} ${t('install_modal.already_integrated')}` : a.name,
      // Gắn cờ khóa
      isDisabled: isAlreadyInstalled 
    };
  });
});

// 3. Chọn Android mặc định là bản đầu tiên KHÔNG bị khóa
const selectedAndroidObj = ref(availableAndroids.value.find(a => !a.isDisabled) || null);

// Theo dõi nếu đổi Version thì reset Android về bản hợp lệ đầu tiên
watch(availableAndroids, (newVal) => {
  selectedAndroidObj.value = newVal.find(a => !a.isDisabled) || null;
});

// Logic Path ban đầu
const oem = computed(() => props.app?.oem || 'unknown');
emulatorPath.value = `C:\\ProgramData\\${props.app?.oem.replace(/\s/g, '')}`;

const closeModal = () => {
  isOpen.value = false;
  setTimeout(() => emit('close'), 300);
};

const confirmInstall = () => {
  emit('confirm', {
    versionObj: selectedVerObj.value,
    androidObj: selectedAndroidObj.value,
    path: emulatorPath.value
  });
};

const handleMessage = (event) => {
  if (event.data?.type === "FOLDER_SELECTED") {
    const baseDir = event.data.path.replace(/[\\/]+$/, ''); 
    emulatorPath.value = `${baseDir}\\${oem.value}`;
  }
};

const browseFolder = () => {
  if (window.chrome?.webview) window.chrome.webview.postMessage({ type: "SELECT_FOLDER" });
};

onMounted(() => window.chrome?.webview?.addEventListener('message', handleMessage));
onUnmounted(() => window.chrome?.webview?.removeEventListener('message', handleMessage));
</script>