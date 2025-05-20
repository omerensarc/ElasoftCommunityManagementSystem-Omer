<template>
  <div class="register-container">
    <div class="register-box">
      <h2>Kayıt Ol</h2>
      <div v-if="error" class="error-message">
        {{ error }}
      </div>
      <div v-if="success" class="success-message">
        {{ success }}
      </div>
      <form @submit.prevent="handleRegister">
        <div class="form-group">
          <label>Ad:</label>
          <input type="text" v-model="name" required>
        </div>
        <div class="form-group">
          <label>Soyad:</label>
          <input type="text" v-model="surname" required>
        </div>
        <div class="form-group">
          <label>E-posta:</label>
          <input type="email" v-model="email" required>
        </div>
        <div class="form-group">
          <label>Telefon Numarası:</label>
          <input type="tel" v-model="phoneNumber" required>
        </div>
        <div class="form-group">
          <label>Okul Numarası:</label>
          <input type="text" v-model="schoolNumber" required>
        </div>
        <button type="submit" class="btn btn-primary" :disabled="loading">
          {{ loading ? 'Kayıt Yapılıyor...' : 'Kayıt Ol' }}
        </button>
      </form>
      <div class="login-link">
        <p>Zaten hesabınız var mı? <router-link to="/login">Giriş Yap</router-link></p>
      </div>
    </div>
  </div>
</template>

<script>
import { authService } from '@/services';

export default {
  name: 'Register',
  data() {
    return {
      name: '',
      surname: '',
      email: '',
      phoneNumber: '',
      schoolNumber: '',
      loading: false,
      error: null,
      success: null
    }
  },
  methods: {
    async handleRegister() {
      this.loading = true;
      this.error = null;
      this.success = null;

      try {
        const userData = {
          name: this.name,
          surname: this.surname,
          email: this.email,
          phoneNumber: this.phoneNumber,
          schoolNumber: this.schoolNumber,
        };

        console.log('Register Payload:', userData); // Log the payload

        const response = await authService.register(userData);

        if (response.data.success) {
          this.success = 'Kayıt başarılı! E-posta adresinize gönderilen şifre ile giriş yapabilirsiniz.';
          localStorage.setItem('tempUser', JSON.stringify({ email: this.email }));

          setTimeout(() => {
            this.$router.push('/login');
          }, 3000);
        }
      } catch (error) {
        console.error('Kayıt hatası:', error);
        if (error.response) {
          this.error = error.response.data.message || 'Kayıt işlemi başarısız oldu!';
        } else {
          this.error = 'Kayıt yapılırken bir hata oluştu!';
        }
      } finally {
        this.loading = false;
      }
    }
  }
}
</script>

<style scoped>
.register-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background-color: #f5f5f5;
}

.register-box {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 400px;
}

h2 {
  text-align: center;
  color: #2c3e50;
  margin-bottom: 2rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

label {
  display: block;
  margin-bottom: 0.5rem;
  color: #666;
}

input {
  width: 100%;
  padding: 0.8rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
}

.btn {
  width: 100%;
  padding: 0.8rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 1rem;
  transition: background-color 0.2s;
}

.btn-primary {
  background-color: #3498db;
  color: white;
}

.btn-primary:hover {
  background-color: #2980b9;
}

.btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.login-link {
  text-align: center;
  margin-top: 1rem;
}

.login-link p {
  font-size: 0.9rem;
  color: #666;
}

.login-link a {
  color: #3498db;
  text-decoration: none;
}

.login-link a:hover {
  text-decoration: underline;
}

.error-message {
  background-color: #f8d7da;
  color: #721c24;
  padding: 0.75rem;
  border-radius: 4px;
  margin-bottom: 1rem;
  text-align: center;
}
</style>
