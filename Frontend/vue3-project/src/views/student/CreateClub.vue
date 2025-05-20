<template>
  <div class="create-club-page">
    <div class="page-header">
      <h1>Yeni Topluluk Oluştur</h1>
    </div>

    <div class="form-container">
      <form @submit.prevent="submitForm" class="create-club-form">
        <div class="form-group">
          <label for="clubName">Topluluk Adı <span class="required">*</span></label>
          <input
            type="text"
            id="clubName"
            v-model="clubForm.name"
            required
            placeholder="Topluluğun adını girin"
          />
        </div>

        <div class="form-group">
          <label for="description">Açıklama <span class="required">*</span></label>
          <textarea
            id="description"
            v-model="clubForm.description"
            rows="4"
            required
            placeholder="Topluluğun amacı ve faaliyetleri hakkında bilgi verin"
          ></textarea>
        </div>

        <div class="form-group">
          <label for="category">Kategori <span class="required">*</span></label>
          <select id="category" v-model="clubForm.categoryId" required>
            <option value="" disabled>Kategori seçin</option>
            <option v-for="category in categories" :key="category.id" :value="category.id">
              {{ category.name }}
            </option>
          </select>
        </div>

        <div class="form-group">
          <label for="advisor">Danışman <span class="required">*</span></label>
          <select id="advisor" v-model="clubForm.advisorId" required>
            <option value="" disabled>Danışman seçin</option>
            <option v-for="advisor in advisors" :key="advisor.advisorId" :value="advisor.advisorId">
              {{ advisor.fullName }}
            </option>
          </select>
        </div>

        <div class="form-group">
          <label for="clubImage">Topluluk Logosu</label>
          <div class="file-upload">
            <input
              type="file"
              id="clubImage"
              @change="handleImageUpload"
              accept="image/*"
            />
            <div class="file-preview" v-if="imagePreview">
              <img :src="imagePreview" alt="Logo Önizleme" />
              <button type="button" class="remove-btn" @click="removeImage">
                <i class="fas fa-times"></i>
              </button>
            </div>
            <div class="upload-placeholder" v-else>
              <i class="fas fa-cloud-upload-alt"></i>
              <span>Görsel yüklemek için tıklayın veya sürükleyin</span>
            </div>
          </div>
        </div>

        <div class="form-group">
          <label for="clubDocument">Topluluk Belgesi (PDF)</label>
          <div class="file-upload">
            <input
              type="file"
              id="clubDocument"
              @change="handleDocumentUpload"
              accept="application/pdf"
            />
            <div class="file-preview document" v-if="documentName">
              <i class="fas fa-file-pdf document-icon"></i>
              <span>{{ documentName }}</span>
              <button type="button" class="remove-btn" @click="removeDocument">
                <i class="fas fa-times"></i>
              </button>
            </div>
            <div class="upload-placeholder" v-else>
              <i class="fas fa-file-upload"></i>
              <span>Topluluk tüzüğü veya diğer belgeleri yükleyin</span>
            </div>
          </div>
        </div>

        <div class="form-actions">
          <button type="button" class="btn btn-secondary" @click="$router.push('/student/communities')">
            İptal
          </button>
          <button type="submit" class="btn btn-primary" :disabled="loading">
            {{ loading ? 'Gönderiliyor...' : 'Topluluk Oluştur' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script>
import { clubService, categoryService } from '@/services';
import { authService } from '@/services';

export default {
  name: 'CreateClub',
  data() {
    return {
      clubForm: {
        name: '',
        description: '',
        categoryId: '',
        advisorId: '',
        image: null,
        document: null
      },
      categories: [],
      advisors: [],
      imagePreview: null,
      documentName: null,
      loading: false,
      error: null
    };
  },
  methods: {
    async fetchCategories() {
      try {
        const response = await categoryService.getAllCategories();
        this.categories = response.data;
      } catch (error) {
        console.error('Kategorileri getirme hatası:', error);
        this.error = 'Kategoriler yüklenirken bir hata oluştu.';
      }
    },
    async fetchAdvisors() {
      try {
        const response = await clubService.getAdvisors();
        console.log('Advisors API response:', response.data);
        this.advisors = response.data.advisors;
      } catch (error) {
        console.error('Danışmanları getirme hatası:', error);
        this.error = 'Danışmanlar yüklenirken bir hata oluştu.';
      }
    },
    handleImageUpload(event) {
      const file = event.target.files[0];
      if (!file) return;

      if (!file.type.includes('image/')) {
        alert('Lütfen geçerli bir görsel dosyası yükleyin.');
        return;
      }

      this.clubForm.image = file;
      const reader = new FileReader();
      reader.onload = e => {
        this.imagePreview = e.target.result;
      };
      reader.readAsDataURL(file);
    },
    handleDocumentUpload(event) {
      const file = event.target.files[0];
      if (!file) return;

      if (file.type !== 'application/pdf') {
        alert('Lütfen PDF formatında bir belge yükleyin.');
        return;
      }

      this.clubForm.document = file;
      this.documentName = file.name;
    },
    removeImage() {
      this.clubForm.image = null;
      this.imagePreview = null;
      document.getElementById('clubImage').value = '';
    },
    removeDocument() {
      this.clubForm.document = null;
      this.documentName = null;
      document.getElementById('clubDocument').value = '';
    },
    async submitForm() {
      this.loading = true;
      this.error = null;

      try {
        const formData = new FormData();
        formData.append('name', this.clubForm.name);
        formData.append('description', this.clubForm.description);
        formData.append('categoryId', Number(this.clubForm.categoryId));
        formData.append('advisorId', Number(this.clubForm.advisorId));

        console.log('Form içeriği:', {
          name: this.clubForm.name,
          description: this.clubForm.description,
          categoryId: Number(this.clubForm.categoryId),
          advisorId: Number(this.clubForm.advisorId),
          formData: [...formData.entries()]
        });

        if (this.clubForm.image) {
          formData.append('image', this.clubForm.image);
        }

        if (this.clubForm.document) {
          formData.append('document', this.clubForm.document);
        }

        const user = authService.getUser();
        formData.append('userId', user.id);
        
        const response = await clubService.createClub(formData);
        console.log('Topluluk oluşturma başarılı:', response.data);

        this.$router.push({
          path: '/student/communities',
          query: { created: 'success' }
        });
      } catch (error) {
        console.error('Topluluk oluşturma hatası:', error);
        this.error = error.response?.data?.message || 'Topluluk oluşturulurken bir hata oluştu.';
        alert(this.error);
      } finally {
        this.loading = false;
      }
    }
  },
  mounted() {
    this.fetchCategories();
    this.fetchAdvisors();
  }
};
</script>

<style scoped>
.create-club-page {
  padding: 2rem;
}

.page-header {
  margin-bottom: 2rem;
}

.form-container {
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  padding: 2rem;
  max-width: 800px;
  margin: 0 auto;
}

.create-club-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

label {
  font-weight: 500;
  color: #2c3e50;
}

.required {
  color: #e74c3c;
}

input, textarea, select {
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
  transition: border-color 0.3s;
}

input:focus, textarea:focus, select:focus {
  border-color: #3498db;
  outline: none;
}

textarea {
  resize: vertical;
  min-height: 100px;
}

.file-upload {
  position: relative;
  border: 2px dashed #ddd;
  border-radius: 4px;
  padding: 1.5rem;
  text-align: center;
  transition: border-color 0.3s;
  cursor: pointer;
}

.file-upload:hover {
  border-color: #3498db;
}

.file-upload input[type="file"] {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  opacity: 0;
  cursor: pointer;
}

.upload-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  color: #7f8c8d;
}

.upload-placeholder i {
  font-size: 2rem;
}

.file-preview {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.file-preview img {
  max-width: 100px;
  max-height: 100px;
  border-radius: 4px;
}

.file-preview.document {
  justify-content: center;
}

.document-icon {
  font-size: 2rem;
  color: #e74c3c;
}

.remove-btn {
  background: #e74c3c;
  color: white;
  border: none;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 1rem;
}

.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  cursor: pointer;
  transition: background-color 0.3s;
}

.btn-primary {
  background-color: #3498db;
  color: white;
}

.btn-primary:hover {
  background-color: #2980b9;
}

.btn-primary:disabled {
  background-color: #95a5a6;
  cursor: not-allowed;
}

.btn-secondary {
  background-color: #ecf0f1;
  color: #2c3e50;
}

.btn-secondary:hover {
  background-color: #bdc3c7;
}
</style> 