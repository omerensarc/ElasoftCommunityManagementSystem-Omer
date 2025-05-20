import api from './api';

const authService = {
  /**
   * Kullanıcı girişi yapar
   * @param {Object} credentials - Kullanıcı bilgileri {email, password}
   * @returns {Promise} - API yanıtı
   */
  login(credentials) {
    return api.post('/auth/login', credentials);
  },

  /**
   * Kullanıcı kaydı yapar
   * @param {Object} userData - Kullanıcı bilgileri
   * @returns {Promise} - API yanıtı
   */
  register(userData) {
    return api.post('/auth/register', userData);
  },

  /**
   * Kullanıcı çıkışı yapar
   */
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  },

  /**
   * Kullanıcı bilgilerini getirir
   * @returns {Promise} - API yanıtı
   */
  getCurrentUser() {
    return api.get('/auth/whoami');
  },

  /**
   * Kullanıcının token'ını yerel depoya kaydeder
   * @param {string} token - JWT token
   */
  saveToken(token) {
    localStorage.setItem('token', token);
  },

  /**
   * Kullanıcı bilgilerini yerel depoya kaydeder
   * @param {Object} user - Kullanıcı bilgileri
   */
  saveUser(user) {
    localStorage.setItem('user', JSON.stringify(user));
  },

  /**
   * Local storage'dan kullanıcı bilgilerini getirir
   * @returns {Object|null} Kullanıcı bilgileri veya null
   */
  getUser() {
    try {
      const userStr = localStorage.getItem('user');
      // Konsola debug bilgisi ekleyelim
      console.log('User raw data from localStorage:', userStr);
      
      if (!userStr) {
        console.log('User data not found in localStorage');
        return null;
      }
      
      const user = JSON.parse(userStr);
      
      // Kullanıcı bilgilerini doğrulayalım
      if (!user || !user.id) {
        console.warn('Invalid user data in localStorage:', user);
        return null;
      }
      
      return user;
    } catch (error) {
      console.error('Error parsing user data:', error);
      return null;
    }
  },

  /**
   * Kullanıcının giriş yapıp yapmadığını kontrol eder
   * @returns {boolean} - Giriş durumu
   */
  isLoggedIn() {
    return !!localStorage.getItem('token');
  },

  /**
   * 2FA'yı etkinleştirir ve QR kod URL'sini döndürür
   * @returns {Promise} - API yanıtı
   */
  enable2FA() {
    return api.post('/auth/2fa/enable');
  },

  /**
   * 2FA kodunu doğrular
   * @param {string} code - 2FA doğrulama kodu
   * @returns {Promise} - API yanıtı
   */
  verify2FA(code) {
    return api.post('/auth/2fa/verify', { code });
  },

  /**
   * 2FA'yı devre dışı bırakır
   * @returns {Promise} - API yanıtı
   */
  disable2FA() {
    return api.post('/auth/2fa/disable');
  },

  /**
   * 2FA kodunu giriş sırasında doğrular
   * @param {Object} data - Doğrulama bilgileri {userId, code}
   * @returns {Promise} - API yanıtı
   */
  validate2FA(data) {
    return api.post('/auth/2fa/validate', data);
  },

  /**
   * Şifre sıfırlama isteği gönderir
   * @param {string} email - Kullanıcı e-postası
   * @returns {Promise} - API yanıtı
   */
  requestPasswordReset(email) {
    return api.post('/auth/password-reset/request', { email });
  },

  /**
   * Şifreyi sıfırlar
   * @param {Object} data - Sıfırlama bilgileri {email, token, newPassword}
   * @returns {Promise} - API yanıtı
   */
  resetPassword(data) {
    return api.post('/auth/password-reset/reset', data);
  },

  /**
   * Kullanıcı profilini günceller
   * @param {Object} profileData - Güncellenecek profil bilgileri
   * @returns {Promise} - API yanıtı
   */
  updateProfile(profileData) {
    return api.put('/user/profile', profileData);
  },

  /**
   * Kullanıcı şifresini değiştirir
   * @param {Object} passwordData - Şifre değiştirme verileri {currentPassword, newPassword}
   * @returns {Promise} - API yanıtı
   */
  changePassword(passwordData) {
    const user = this.getUser(); // Get user to access user.id and other required fields
    if (!user || !user.id) {
      return Promise.reject('Kullanıcı bilgileri bulunamadı.');
    }

    // Ensure passwordData contains the necessary fields
    if (!passwordData || !passwordData.currentPassword || !passwordData.newPassword) {
       return Promise.reject('Mevcut ve yeni şifre alanları gereklidir.');
    }

    // Construct the payload including all fields required by the backend validation,
    // based on the latest error message.
    const payload = {
      // Fetch existing user details
      FirstName: user.name?.split(' ')[0] || '', // Simple split for FirstName
      LastName: user.name?.split(' ').slice(1).join(' ') || '', // Simple split for LastName
      Phone: user.phone || '',
      StudentId: user.studentId || '',
      Role: user.role || '', // Include Role as it seems required by backend validation
      DepartmentId: user.departmentId || null, // Include DepartmentId just in case
      
      // Include password fields
      CurrentPassword: passwordData.currentPassword, 
      NewPassword: passwordData.newPassword
    };

    console.log('Payload for password change (full DTO) via PUT /users/{id}:', payload);
    
    // Use the user update endpoint with the full payload.
    // If the "cannot change role" error reappears, the backend PUT /users/{id} needs fixing.
    return api.put(`/users/${user.id}`, payload); 
  },

  /**
   * Kullanıcı profil fotoğrafını günceller
   * @param {FormData} formData - Resim dosyası içeren form verisi
   * @returns {Promise} - API yanıtı
   */
  updateAvatar(formData) {
    return api.post('/user/avatar', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
  }
};

export default authService;