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
        <v-avatar size="48" class="mr-4 rounded-lg elevation-4" color="transparent">
          <v-img :src="app.icon"></v-img>
        </v-avatar>
        <span class="text-h5 font-weight-bold">Cài đặt {{ app.name }}</span>
        <v-spacer></v-spacer>
        <v-btn icon="mdi-close" variant="text" size="small" @click="closeModal"></v-btn>
      </v-card-title>

      <v-card-text class="px-6 py-4">
        <div class="text-subtitle-1 font-weight-medium text-medium-emphasis mb-2">Cấu hình phiên bản:</div>
        <v-row class="mb-2">
          <v-col cols="6">
            <v-select
              v-model="selectedVerObj"
              :items="app.versions"
              item-title="ver"
              return-object
              label="Phiên bản"
              variant="solo-filled"
              :bg-color="isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.05)'"
              density="comfortable"
              hide-details
              prepend-inner-icon="mdi-application-cog"
              class="rounded-lg"
            ></v-select>
          </v-col>
          
          <v-col cols="6">
            <v-select
              v-model="selectedAndroidObj"
              :items="availableAndroids"
              item-title="name"
              return-object
              label="Hệ điều hành"
              variant="solo-filled"
              :bg-color="isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.05)'"
              density="comfortable"
              hide-details
              prepend-inner-icon="mdi-android"
              class="rounded-lg"
            ></v-select>
          </v-col>
        </v-row>

        <div class="text-subtitle-1 font-weight-medium text-medium-emphasis mb-2 mt-4">Thư mục lưu trữ:</div>
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
            <v-btn color="blue" variant="tonal" size="small" class="rounded-lg font-weight-bold px-4" @click="browseFolder">
              DUYỆT
            </v-btn>
          </template>
        </v-text-field>
      </v-card-text>

      <v-card-actions class="px-6 pb-6 pt-0">
        <v-spacer></v-spacer>
        <v-btn variant="text" class="px-6 font-weight-bold" @click="closeModal">HỦY BỎ</v-btn>
        <v-btn 
          color="blue" 
          variant="elevated" 
          class="px-8 font-weight-bold" 
          :class="isDark ? 'text-black' : 'text-white'"
          rounded="pill" 
          elevation="6"
          @click="confirmInstall"
        >
          <v-icon icon="mdi-rocket-launch" class="mr-2"></v-icon> CÀI ĐẶT NGAY
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import { useTheme } from 'vuetify'; // Import useTheme

const props = defineProps(['app']);
const emit = defineEmits(['close', 'confirm']);

// --- THEME LOGIC ---
const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

const isOpen = ref(true);
const emulatorPath = ref('');

// 1. Chọn Version mặc định là bản đầu tiên
const selectedVerObj = ref(props.app?.versions?.[0] || null);

// 2. Danh sách Android tự động cập nhật khi đổi Version
const availableAndroids = computed(() => selectedVerObj.value?.androids || []);

// 3. Chọn Android mặc định là bản đầu tiên của Version đó
const selectedAndroidObj = ref(availableAndroids.value[0] || null);

// Theo dõi nếu đổi Version thì reset Android về bản đầu tiên của Version mới
watch(availableAndroids, (newVal) => {
  selectedAndroidObj.value = newVal[0] || null;
});

// Logic Path ban đầu
const oem = computed(() => props.app?.oem || 'unknown');
if (props.app.oem === 'BlueStacks_nxt'){
  emulatorPath.value = `C:\\ProgramData\\${props.app?.oem.replace(/\s/g, '')}`;
}
else {
  emulatorPath.value = `C:\\${props.app?.oem}`;
}

const closeModal = () => {
  isOpen.value = false;
  setTimeout(() => emit('close'), 300);
};

const confirmInstall = () => {
  emit('confirm', {
    version: selectedVerObj.value?.ver,
    codename: selectedAndroidObj.value?.code,
    path: emulatorPath.value
  });
};

const handleMessage = (event) => {
  if (event.data?.type === "FOLDER_SELECTED") {
    emulatorPath.value = event.data.path + `\\${oem.value}`;
  }
};

const browseFolder = () => {
  if (window.chrome?.webview) window.chrome.webview.postMessage({ type: "SELECT_FOLDER" });
};

onMounted(() => window.chrome?.webview?.addEventListener('message', handleMessage));
onUnmounted(() => window.chrome?.webview?.removeEventListener('message', handleMessage));
</script>