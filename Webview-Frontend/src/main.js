import { createApp } from 'vue'
import App from './App.vue'

// ==========================================
// 1. CẤU HÌNH VUETIFY (Giao diện)
// ==========================================
import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import '@mdi/font/css/materialdesignicons.css' 

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'dark', // Game Launcher thì xài Dark mode là chân ái
  },
})

// ==========================================
// 2. CẤU HÌNH i18n (Đa ngôn ngữ)
// ==========================================
import { createI18n } from 'vue-i18n'
// Đảm bảo bạn đã tạo 2 file JSON này trong thư mục src/locales/ nhé!
import vi from './locales/vi-VN.json'
import en from './locales/en-US.json'

const i18n = createI18n({
  legacy: false,           // Bắt buộc false để xài chung với Vue 3 <script setup>
  locale: 'vi-VN',         // Ngôn ngữ mặc định
  fallbackLocale: 'en-US', // Tiếng Anh làm phương án dự phòng nếu thiếu từ
  messages: {
    'vi-VN': vi,
    'en-US': en
  }
})

// ==========================================
// 3. KHỞI TẠO VÀ GẮN ĐỒ CHƠI VÀO APP
// ==========================================
const app = createApp(App)

app.use(vuetify) // Gắn giao diện
app.use(i18n)    // Gắn từ điển

app.mount('#app') // Bật app lên!