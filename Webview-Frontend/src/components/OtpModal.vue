  <template>
  <v-dialog :model-value="modelValue" persistent max-width="450" transition="dialog-bottom-transition">
    <v-card class="rounded-xl pa-6 text-center border position-relative" :theme="isDark ? 'dark' : 'light'" elevation="24">
      
      <v-btn
        icon="mdi-close"
        variant="text"
        color="grey"
        size="small"
        class="position-absolute"
        style="top: 12px; right: 12px; z-index: 2;"
        @click="closeModal"
        :disabled="isLoading"
      ></v-btn>

      <v-icon icon="mdi-shield-lock-outline" color="success" size="70" class="mx-auto mb-2 mt-2"></v-icon>
      
      <v-card-title class="text-h5 font-weight-bold text-success pb-0">
        Xác Minh Bảo Mật
      </v-card-title>
      
      <v-card-text class="text-body-1 mt-3 pa-2">
        Ứng dụng này yêu cầu xác minh danh tính.<br>
        Mã OTP gồm 6 chữ số đã được gửi tới email:<br>
       <b class="text-primary">{{ maskedEmail }}</b>
        
        <div class="mt-6 mb-2">
          <v-otp-input
            v-model="otpCode"
            :length="6"
            :disabled="isLoading"
            focus-all
            @finish="submitOtp"
          ></v-otp-input>
        </div>
        
        <div class="text-caption text-error font-weight-bold" v-if="errorMessage" style="min-height: 20px;">
          {{ errorMessage }}
        </div>
        
        <div class="mt-2 pb-2">
          <span class="text-body-2 text-grey">Chưa nhận được mã? </span>
          <v-btn
            variant="text"
            color="primary"
            size="small"
            class="font-weight-bold"
            :disabled="countdown > 0 || isLoading"
            @click="resendOtp"
          >
            GỬI LẠI {{ countdown > 0 ? `(${countdown}s)` : '' }}
          </v-btn>
        </div>
      </v-card-text>
      
      </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { useTheme } from 'vuetify';

const props = defineProps({
  modelValue: Boolean,
  email: String,
  isLoading: Boolean,
  errorMessage: String
});

const emit = defineEmits(['update:modelValue', 'submit', 'resend', 'cancel']);

const theme = useTheme();
const isDark = computed(() => theme.global.current.value.dark);

const otpCode = ref('');
const countdown = ref(60);
let timer = null;

// Tự động đếm ngược khi Modal mở
watch(() => props.modelValue, (isOpen) => {
  if (isOpen) {
    otpCode.value = '';
    startCountdown();
  } else {
    clearInterval(timer);
  }
});

const startCountdown = () => {
  countdown.value = 60;
  clearInterval(timer);
  timer = setInterval(() => {
    if (countdown.value > 0) countdown.value--;
    else clearInterval(timer);
  }, 1000);
};

const resendOtp = () => {
  otpCode.value = '';
  startCountdown();
  emit('resend');
};

const submitOtp = () => {
  if (otpCode.value.length === 6) {
    emit('submit', otpCode.value);
  }
};

const closeModal = () => {
  otpCode.value = '';
  emit('cancel');
  emit('update:modelValue', false);
};

// Hàm tự động che email
const maskedEmail = computed(() => {
  // Nếu không có email hoặc email không đúng định dạng thì trả về rỗng
  if (!props.email || !props.email.includes('@')) return props.email;

  // Tách username và domain (ví dụ: hieugllite và gmail.com)
  const [username, domain] = props.email.split('@');
  
  // 1. Xử lý Username: Lấy chữ đầu tiên, phần còn lại thay bằng dấu *
  // Nếu tên quá ngắn (1 ký tự), tự cho 4 dấu sao
  const hiddenUser = username.charAt(0) + '*'.repeat(username.length > 1 ? username.length - 1 : 4);

  // 2. Xử lý Domain: Tách tên miền và đuôi (ví dụ: gmail và com)
  const domainParts = domain.split('.');
  const domainName = domainParts[0];
  const extension = domainParts.slice(1).join('.'); // Lấy phần đuôi (.com, .vn, .edu.vn...)

  // Lấy chữ đầu của domain, còn lại thay bằng *
  const hiddenDomain = domainName.charAt(0) + '*'.repeat(domainName.length > 1 ? domainName.length - 1 : 3);

  // Ghép lại thành phẩm
  return `${hiddenUser}@${hiddenDomain}.${extension}`;
});
</script>