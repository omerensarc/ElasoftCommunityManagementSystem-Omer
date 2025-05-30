<template>
  <div class="admin-events">
    <div class="page-header">
      <h1>Etkinlikler</h1>
      <button @click="showAddModal = true" class="add-btn">
        <i class="fas fa-plus"></i>
        Yeni Etkinlik
      </button>
    </div>

    <div class="filters">
      <div class="search-box">
        <i class="fas fa-search"></i>
        <input type="text" v-model="searchQuery" placeholder="Etkinlik ara..." @input="filterEvents">
      </div>
      <div class="filter-group">
        <select v-model="statusFilter" @change="filterEvents">
          <option value="all">Tüm Durumlar</option>
          <option value="upcoming">Yaklaşan</option>
          <option value="approved">Onaylanan</option>
          <option value="ongoing">Devam Eden</option>
          <option value="completed">Tamamlanan</option>
          <option value="cancelled">İptal Edilen</option>

        </select>
        <select v-model="communityFilter" @change="filterEvents">
          <option value="all">Tüm Topluluklar</option>
          <option v-for="community in communities" :key="community.id" :value="community.id">
            {{ community.name }}
          </option>
        </select>
      </div>
    </div>
    <div v-if="notification.show" class="notification" :class="`notification-${notification.type}`">
      <div class="notification-content">
        <div class="notification-icon">
          <i v-if="notification.type === 'success'" class="fas fa-check-circle"></i>
          <i v-else-if="notification.type === 'error'" class="fas fa-exclamation-circle"></i>
          <i v-else-if="notification.type === 'warning'" class="fas fa-exclamation-triangle"></i>
          <i v-else class="fas fa-info-circle"></i>
        </div>
        <span class="notification-message">{{ notification.message }}</span>
        <button class="notification-close" @click="notification.show = false">
          <i class="fas fa-times"></i>
        </button>
      </div>
      <div class="notification-progress" :style="{ animationDuration: notification.duration + 'ms' }"></div>
    </div>

    <div v-if="loading" class="loading-indicator">
      <div class="spinner"></div>
      <p>Etkinlikler yükleniyor...</p>
    </div>

    <div v-if="error" class="error-message">
      <p>{{ error }}</p>
      <div class="text-center">
        <button @click="fetchEvents" class="retry-btn">
          <i class="fas fa-sync"></i> Tekrar Dene
        </button>
      </div>
    </div>

    <div v-if="!loading && !error && events.length === 0" class="empty-state">
      <p>Hiç etkinlik bulunamadı.</p>
      <button @click="showAddModal = true" class="add-btn">
        <i class="fas fa-plus"></i> Yeni Etkinlik Oluştur
      </button>
    </div>

    <div v-if="!loading && events.length > 0" class="events-grid">
      <div v-for="event in filteredEvents" :key="event.id" class="event-card">
        <div class="event-image">
          <img :src="event.image" :alt="event.title">
          <div class="status-badge" :class="event.status">
            {{ getStatusText(event.status) }}
          </div>
        </div>
        <div class="event-content">
          <h3>{{ event.title }}</h3>
          <div class="detail-item community-name">
            <i class="fas fa-users"></i>
            <span>{{ event.clubName || getCommunityName(event.communityId) }}</span>
          </div>
          <div class="event-details">
            <div class="detail-item">
              <i class="fas fa-calendar"></i>
              <span>{{ formatDate(event.startDate) }} - {{ formatDate(event.endDate) }}</span>
            </div>
            <div class="detail-item">
              <i class="fas fa-clock"></i>
              <span>{{ event.startTime }} - {{ event.endTime }}</span>
            </div>
          </div>
          <div class="event-stats">
            <span>
              <i class="fas fa-user-check"></i>
              {{ event.currentParticipants }}/{{ event.maxParticipants }} Katılımcı
            </span>
            <span>
              <i class="fas fa-clock"></i>
              {{
                Math.round(
                  (new Date('1970-01-01T' + event.endTime) - new Date('1970-01-01T' + event.startTime)) / 3600000
                )
              }} saat
            </span>
          </div>
          <p class="description">{{ event.description }}</p>
          <div class="event-actions">
            <button @click="editEvent(event)" class="edit-btn">
              <i class="fas fa-edit"></i>
              Düzenle
            </button>
            <button @click="deleteEvent(event.id)" class="delete-btn">
              <i class="fas fa-trash"></i>
              Sil
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Etkinlik Ekleme/Düzenleme Modalı -->
    <div v-if="showAddModal" class="modal">
      <div class="modal-content">
        <div class="modal-header">
          <h2>{{ editingEvent ? 'Etkinlik Düzenle' : 'Yeni Etkinlik' }}</h2>
          <button @click="closeModal" class="close-btn">
            <i class="fas fa-times"></i>
          </button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="saveEvent">
            <div class="form-group">
              <label>Etkinlik Başlığı</label>
              <input v-model="eventForm.title" type="text" required>
            </div>
            <div class="form-group">
              <label>Açıklama</label>
              <textarea v-model="eventForm.description" rows="4" required></textarea>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Topluluk</label>
                <select v-model="eventForm.communityId" required @change="logCommunityChange">
                  <option :value="null" disabled>Topluluk seçin</option>
                  <option v-for="community in communities" :key="community.id || community.clubId"
                    :value="community.clubId || community.id">
                    {{ community.name }}
                  </option>
                </select>
              </div>

              <div class="form-group">
                <label>Maksimum Katılımcı</label>
                <input v-model="eventForm.maxParticipants" type="number" required>

              </div>

            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Başlangıç Tarihi</label>
                <input v-model="eventForm.startDate" type="date" required>
              </div>
              <div class="form-group">
                <label>Bitiş Tarihi</label>
                <input v-model="eventForm.endDate" type="date" required>
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Başlangıç Saati</label>
                <input v-model="eventForm.startTime" type="time" required>
              </div>
              <div class="form-group">
                <label>Bitiş Saati</label>
                <input v-model="eventForm.endTime" type="time" required>
              </div>
            </div>

            <div class="form-group">
              <label>Etkinlik Görseli</label>
              <div class="image-upload">
                <input type="file" @change="handleImageUpload" accept="image/*">
                <div v-if="eventForm.imagePreview" class="upload-preview">
                  <img :src="eventForm.imagePreview" alt="Event Preview">
                </div>
                <div v-else-if="eventForm.image && typeof eventForm.image === 'string'" class="upload-preview">
                  <img :src="eventForm.image" alt="Event Preview">
                </div>
              </div>
            </div>
            <div class="form-actions">
              <button type="button" @click="closeModal" class="cancel-btn">İptal</button>
              <button type="submit" class="save-btn">Kaydet</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { eventService, clubService } from '@/services';

export default {
  name: 'AdminEvents',
  data() {
    return {
      searchQuery: '',
      statusFilter: 'all',
      communityFilter: 'all',
      showAddModal: false,
      editingEvent: null,
      loading: false,
      error: null,
      communities: [],
      notification: {
        show: false,
        type: 'info',
        message: '',
        timeout: null,
        duration: 3000
      },
      eventForm: {
        title: '',
        communityId: null,
        startDate: '',
        endDate: '',
        startTime: '',
        endTime: '',
        maxParticipants: 0,
        currentParticipants: 0,
        description: '',
        image: null,
        imagePreview: null
      },
      events: []
    }
  },
  created() {
    // Component oluşturulduğunda toplulukları ve etkinlikleri getir
    this.fetchCommunities();
    this.fetchEvents();
  },
  computed: {
    filteredEvents() {
      let filtered = this.events;

      // Arama filtresi
      if (this.searchQuery) {
        const query = this.searchQuery.toLowerCase();
        filtered = filtered.filter(event =>
          event.title.toLowerCase().includes(query) ||
          event.description.toLowerCase().includes(query)
        );
      }

      // Durum filtresi
      if (this.statusFilter !== 'all') {
        filtered = filtered.filter(event => event.status === this.statusFilter);
      }

      // Topluluk filtresi
      if (this.communityFilter !== 'all') {
        const communityId = parseInt(this.communityFilter);
        filtered = filtered.filter(event => event.communityId === communityId);
      }

      return filtered;
    },
    getCommunityName() {
      return (communityId) => {
        // Önce events içindeki clubName alanını kontrol et
        const event = this.events.find(e => e.communityId === communityId);
        if (event && event.clubName) {
          return event.clubName;
        }

        // Yoksa communities array'den bul
        const community = this.communities.find(c =>
          c.id === communityId || c.clubId === communityId
        );

        return community ? community.name : 'Bilinmeyen Topluluk';
      };
    }
  },
  methods: {
    logCommunityChange() {
      console.log('Community selection changed. New eventForm.communityId:', this.eventForm.communityId);
      console.log('All communities:', this.communities);
      console.log('Selected community details:', this.communities.find(c =>
        (c.id === this.eventForm.communityId) || (c.clubId === this.eventForm.communityId)
      ));
    },
    async fetchCommunities() {
      try {
        // Backend API'den topluluk verilerini al
        const response = await clubService.getAllClubs();
        console.log('Raw API response for communities:', response);

        // Check and transform community data if needed
        if (response && response.data) {
          // Create a standardized structure that works with our form
          this.communities = response.data.map(club => {
            return {
              id: club.id || club.clubId, // Support both property names
              clubId: club.id || club.clubId, // Support both property names
              name: club.name || club.clubName || 'İsimsiz Topluluk'
            };
          });
        } else {
          this.communities = [];
        }

        console.log('Processed communities data:', this.communities);
      } catch (error) {
        console.error('Topluluklar alınırken hata:', error);
        // Hata durumunda test verilerini göster (sadece geliştirme aşamasında)
        this.communities = [
          { id: 1, clubId: 1, name: 'Teknoloji Topluluğu' },
          { id: 2, clubId: 2, name: 'Fotoğrafçılık Kulübü' },
          { id: 3, clubId: 3, name: 'Kitap Okuma Kulübü' }
        ];
        console.log('Using fallback communities data:', this.communities);
      }
    },
    async fetchEvents() {
      try {
        this.loading = true;
        this.error = null;

        // Backend API'den etkinlik verilerini al
        const response = await eventService.getAllEvents();
        console.log('Raw events data from API:', response);

        if (response && response.data) {
          this.events = response.data.map(event => {
            // Tarih ve saat işlemleri
            let startDate, endDate, startTime, endTime;

            try {
              // Backend'den gelen ISO tarih formatlarını parse et
              startDate = new Date(event.startDate || event.StartDate);
              endDate = new Date(event.endDate || event.EndDate);

              // Saatleri yerel saate göre formatla
              startTime = startDate.getHours().toString().padStart(2, '0') + ':' +
                startDate.getMinutes().toString().padStart(2, '0');
              endTime = endDate.getHours().toString().padStart(2, '0') + ':' +
                endDate.getMinutes().toString().padStart(2, '0');

              console.log('Extracted times:', {
                startDate: startDate,
                startTime: startTime,
                endDate: endDate,
                endTime: endTime
              });
            } catch (e) {
              console.error('Error parsing date/time:', e);
              startTime = '00:00';
              endTime = '00:00';
            }

            // Tarih string'ini YYYY-MM-DD formatına çevir
            const startDateStr = startDate instanceof Date && !isNaN(startDate) ?
              startDate.toISOString().substring(0, 10) : '';
            const endDateStr = endDate instanceof Date && !isNaN(endDate) ?
              endDate.toISOString().substring(0, 10) : '';

            return {
              id: event.eventId || event.EventId || event.id,
              title: event.name || event.Name,
              communityId: event.clubId || event.ClubId,
              startDate: startDateStr,
              endDate: endDateStr,
              startTime: startTime,
              endTime: endTime,
              maxParticipants: event.maxParticipants || event.MaxParticipants || 0,
              currentParticipants: event.participantCount || event.ParticipantCount || 0,
              description: event.description || event.Description,
              image: event.image ? `data:image/jpeg;base64,${event.image}` : (event.imageUrl || event.ImageUrl || '/images/default-event.png'),
              status: event.status || event.Status || 'upcoming',
              clubName: event.clubName || event.ClubName
            };
          });
        } else {
          // API veri dönmezse boş dizi kullan
          this.events = [];
          console.warn('API\'den etkinlik verisi alınamadı');
        }
      } catch (error) {
        console.error('Etkinlikler yüklenirken hata:', error);
        this.error = 'Etkinlikler yüklenirken bir hata oluştu.';
        // Hata durumunda etkinlikleri temizle
        this.events = [];
      } finally {
        this.loading = false;
      }
    },
    getStatusText(status) {
      const statusMap = {
        upcoming: 'Yaklaşan',
        ongoing: 'Devam Eden',
        completed: 'Tamamlanan',
        cancelled: 'İptal Edilen',
        approved: 'Onaylanan'
      }
      return statusMap[status] || status;
    },
    formatDate(dateString) {
      const options = { year: 'numeric', month: 'long', day: 'numeric' };
      return new Date(dateString).toLocaleDateString('tr-TR', options);
    },
    filterEvents() {
      // Filtreleme işlemi computed property üzerinden yapılıyor
    },
    editEvent(event) {
      console.log('Editing event with ID:', event.id);
      console.log('Full event object:', JSON.stringify(event));

      // Düzenleme sırasında event.id'nin doğru şekilde kaydedildiğinden emin ol
      this.editingEvent = {
        ...event,
        id: event.id // ID'nin doğru olduğundan emin ol
      };

      console.log('Event times for editing:', {
        startDate: event.startDate,
        startTime: event.startTime,
        endDate: event.endDate,
        endTime: event.endTime
      });

      // Form değerlerini ayarla
      this.eventForm = {
        title: event.title,
        communityId: event.communityId,
        startDate: event.startDate,
        endDate: event.endDate,
        startTime: event.startTime,
        endTime: event.endTime,
        maxParticipants: event.maxParticipants,
        currentParticipants: event.currentParticipants || 0,
        description: event.description,
        image: event.image,
        imagePreview: event.image
      };

      console.log('Event form initialized with time values:', {
        startDate: this.eventForm.startDate,
        startTime: this.eventForm.startTime,
        endDate: this.eventForm.endDate,
        endTime: this.eventForm.endTime
      });

      // Modal'ı göster
      this.showAddModal = true;
    },
    async deleteEvent(id) {
      if (confirm('Bu etkinliği silmek istediğinizden emin misiniz?')) {
        try {
          console.log(`Attempting to delete event with ID: ${id}`);

          // Backend API'ye silme isteği gönder
          await eventService.deleteEvent(id);

          // UI güncellemesi
          this.events = this.events.filter(e => e.id !== id);

          // Başarı mesajı
          this.showNotification('success', 'Etkinlik başarıyla silindi.');
        } catch (error) {
          console.error('Etkinlik silinirken hata:', error);
          this.showNotification('error', 'Etkinlik silinirken bir hata oluştu.');
        }
      }
    },
    handleImageUpload(event) {
      const file = event.target.files[0];
      if (file) {
        // Dosya bilgisini sakla
        this.eventForm.image = file;

        // Önizleme için URL oluştur
        const reader = new FileReader();
        reader.onload = e => {
          this.eventForm.imagePreview = e.target.result;
        };
        reader.readAsDataURL(file);
      }
    },
    async saveEvent() {
      try {
        // Validate communityId more specifically
        if (this.eventForm.communityId === null || this.eventForm.communityId === undefined) {
          this.showNotification('error', 'Lütfen bir topluluk seçin.');
          return;
        }

        // Check if user is authenticated
        const token = localStorage.getItem('token');
        if (!token) {
          this.showNotification('error', 'Oturum süreniz dolmuş olabilir. Lütfen tekrar giriş yapın.');
          // Optionally redirect to login page
          return;
        }

        console.log('Form values before sending:', {
          title: this.eventForm.title,
          communityId: this.eventForm.communityId,
          description: this.eventForm.description,
          startDate: this.eventForm.startDate,
          endDate: this.eventForm.endDate,
          startTime: this.eventForm.startTime,
          endTime: this.eventForm.endTime,
          maxParticipants: this.eventForm.maxParticipants
        });

        // Use this test method first to see if a minimal payload works
        if (await this.testSimpleSubmission()) {
          this.showNotification('success', 'Test submission successful!');
          return;
        }

        const formData = new FormData();

        // Map frontend field names to backend field names
        formData.append('Name', this.eventForm.title);
        formData.append('ClubId', String(this.eventForm.communityId));
        formData.append('Description', this.eventForm.description);

        // Combine date and time into ISO strings for backend
        // Timezone'dan bağımsız bir UTC tarih oluştur
        const startDateStr = this.eventForm.startDate;
        const startTimeStr = this.eventForm.startTime;
        console.log('Raw date/time inputs:', { startDateStr, startTimeStr, endDateStr: this.eventForm.endDate, endTimeStr: this.eventForm.endTime });

        // Tarih ve saati uygun formatta birleştir
        // Örnek: 2025-04-14T18:00:00 (UTC belirteci olmadan)
        const startDateTimeStr = `${startDateStr}T${startTimeStr}:00`;
        const endDateTimeStr = `${this.eventForm.endDate}T${this.eventForm.endTime}:00`;

        console.log('Formatted date strings:', { startDateTimeStr, endDateTimeStr });

        // Backend'e gönder
        formData.append('StartDate', startDateTimeStr);
        formData.append('EndDate', endDateTimeStr);
        formData.append('MaxParticipants', String(this.eventForm.maxParticipants));
        formData.append('EventType', 'physical'); // Default event type

        // Append image last (large files)
        if (this.eventForm.image && this.eventForm.image instanceof File) {
          formData.append('ImageFile', this.eventForm.image);
        }

        // Log FormData contents for debugging
        console.log('FormData entries before sending:');
        for (let pair of formData.entries()) {
          console.log(pair[0] + ': ' + pair[1]);
        }

        let response;
        if (this.editingEvent) {
          console.log(`Updating event with ID: ${this.editingEvent.id}`);

          // Düzenleme sırasında doğru eventId'nin kullanıldığından emin ol
          const eventId = this.editingEvent.id;

          // EventId için ayrıca formData'ya ekle (bazı API'ler bunu bekleyebilir)
          formData.append('EventId', eventId);

          // Güncellenecek dosyaların kimlik bilgileri
          console.log('Event being updated:', this.editingEvent);
          console.log('UpdateEvent calling with ID:', eventId);

          try {
            response = await eventService.updateEvent(eventId, formData);
            console.log('Update response:', response);
          } catch (updateError) {
            console.error('Error during update:', updateError);
            throw updateError;  // Hatayı yeniden fırlat
          }
        } else {
          console.log('Creating new event');
          response = await eventService.createEvent(formData);
        }

        console.log('Success response:', response);
        await this.fetchEvents();
        this.closeModal();
      } catch (error) {
        console.error('Etkinlik kaydedilirken hata:', error);

        let errorMessage = 'Etkinlik kaydedilirken bir hata oluştu.';

        if (error.response) {
          console.error('Error response:', error.response.data);

          if (error.response.status === 401) {
            errorMessage = 'Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.';
            // Redirect to login
          } else if (error.response.status === 403) {
            errorMessage = 'Bu işlemi yapmak için yetkiniz bulunmuyor.';
          } else if (error.response.status === 500) {
            errorMessage = 'Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.';
          } else if (error.response.data && error.response.data.Message) {
            errorMessage = error.response.data.Message;
          }

          if (error.response.data && error.response.data.errors) {
            console.error('Validation errors:', error.response.data.errors);
            errorMessage += ' Lütfen form alanlarını kontrol edin.';
          }
        }

        this.showNotification('error', errorMessage);
      }
    },

    // A simple test submission with minimal data to isolate the issue
    async testSimpleSubmission() {
      try {
        const token = localStorage.getItem('token');

        // Create minimal test data with correct backend field names
        const testData = new FormData();
        testData.append('Name', 'Test Event');
        testData.append('ClubId', '2'); // Use a valid ID that exists
        testData.append('Description', 'Test Description');
        testData.append('StartDate', '2025-01-01T00:00:00.000Z'); // ISO format with time
        testData.append('EndDate', '2025-01-02T00:00:00.000Z'); // ISO format with time
        testData.append('MaxParticipants', '10');
        testData.append('EventType', 'physical');

        console.log('Sending test submission with minimal data');

        // Direct fetch call with minimal configuration
        const response = await fetch('https://localhost:7274/api/events/ekle', {
          method: 'POST',
          headers: {
            'Accept': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: testData
        });

        const responseData = await response.json();
        console.log('Test submission response:', response.status, responseData);

        return response.ok;
      } catch (error) {
        console.error('Test submission failed:', error);
        return false;
      }
    },
    showNotification(type, message, duration = 3000) {
      this.notification.type = type;
      this.notification.message = message;
      this.notification.show = true;

      if (this.notification.timeout) {
        clearTimeout(this.notification.timeout);
      }

      this.notification.timeout = setTimeout(() => {
        this.notification.show = false;
      }, duration);
    },
    closeModal() {
      this.showAddModal = false;
      this.editingEvent = null;
      this.eventForm = {
        title: '',
        communityId: null,
        startDate: '',
        endDate: '',
        startTime: '',
        endTime: '',
        maxParticipants: 0,
        currentParticipants: 0,
        description: '',
        image: null,
        imagePreview: null
      }
    }
  }
}
</script>

<style scoped>
.admin-events {
  padding: 1rem;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.add-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  background: #2ecc71;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  transition: background-color 0.3s;
}

.add-btn:hover {
  background: #27ae60;
}

.filters {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;
}

.search-box {
  position: relative;
  flex: 1;
  max-width: 400px;
}

.search-box i {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  color: #7f8c8d;
}

.search-box input {
  width: 100%;
  padding: 0.75rem 1rem 0.75rem 2.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.filter-group {
  display: flex;
  gap: 1rem;
}

.filter-group select {
  padding: 0.75rem 1rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  background-color: white;
}

.events-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 1.5rem;
}

.event-card {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  /* İçeriği dikey olarak yayar */
  background: white;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.event-image {
  position: relative;
  height: 200px;
}

.event-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.status-badge {
  position: absolute;
  top: 1rem;
  right: 1rem;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  color: white;
  font-size: 0.8rem;
}

.status-badge.upcoming {
  background: #3498db;
}

.status-badge.ongoing {
  background: #2ecc71;
}

.status-badge.completed {
  background: #95a5a6;
}

.status-badge.cancelled {
  background: #e74c3c;
}

.status-badge.approved {
  background: #27ae60;
}

.event-content {
  padding: 1.5rem;
}

.event-content h3 {
  margin: 0 0 0.5rem;
  color: #2c3e50;
}

.event-meta {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
  color: #7f8c8d;
  font-size: 0.9rem;
}

.event-meta span {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.event-details {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #34495e;
}

.detail-item i {
  color: #7f8c8d;
  width: 20px;
}

.event-stats {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
  color: #7f8c8d;
  font-size: 0.9rem;
}

.event-stats span {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.description {
  color: #34495e;
  margin-bottom: 1rem;
  line-height: 1.5;
}

.event-actions {
  display: flex;
  gap: 0.5rem;
}

.edit-btn,
.delete-btn {
  flex: 1;
  padding: 0.5rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  transition: background-color 0.3s;
}

.edit-btn {
  background: #3498db;
  color: white;
}

.edit-btn:hover {
  background: #2980b9;
}

.delete-btn {
  background: #e74c3c;
  color: white;
}

.delete-btn:hover {
  background: #c0392b;
}

/* Hata ve Yükleme Durumları */
.loading-indicator {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  min-height: 200px;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid rgba(0, 0, 0, 0.1);
  border-radius: 50%;
  border-top: 4px solid #3498db;
  animation: spin 1s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }

  100% {
    transform: rotate(360deg);
  }
}

.error-message {
  color: #e74c3c;
  text-align: center;
  margin: 2rem 0;
}

.retry-btn {
  background: #3498db;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  cursor: pointer;
  margin-top: 1rem;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.retry-btn:hover {
  background: #2980b9;
}

.text-center {
  text-align: center;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  min-height: 200px;
  text-align: center;
}

.empty-state p {
  color: #7f8c8d;
  margin-bottom: 1rem;
  font-size: 1.1rem;
}

/* Modal Styles */
.modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 8px;
  width: 90%;
  max-width: 800px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  padding: 1.5rem;
  border-bottom: 1px solid #eee;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #7f8c8d;
}

.modal-body {
  padding: 1.5rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: #2c3e50;
}

.form-group input,
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.image-upload {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.upload-preview {
  width: 100%;
  height: 200px;
  border-radius: 4px;
  overflow: hidden;
}

.upload-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 2rem;
}

.cancel-btn,
.save-btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  transition: background-color 0.3s;
}

.cancel-btn {
  background: #f8f9fa;
  color: #7f8c8d;
}

.cancel-btn:hover {
  background: #e9ecef;
}

.save-btn {
  background: #2ecc71;
  color: white;
}

.save-btn:hover {
  background: #27ae60;
}

.notification {
  position: fixed;
  top: 20px;
  right: 20px;
  max-width: 350px;
  background-color: white;
  border-radius: 6px;
  overflow: hidden;
  box-shadow: 0 3px 15px rgba(0, 0, 0, 0.2);
  z-index: 9999;
  animation: slideInDown 0.3s ease-out;
  transition: all 0.3s ease;
}

@keyframes slideInDown {
  from {
    transform: translateY(-50px);
    opacity: 0;
  }

  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.notification-success {
  border-left: 4px solid #2ecc71;
}

.notification-error {
  border-left: 4px solid #e74c3c;
}

.notification-warning {
  border-left: 4px solid #f39c12;
}

.notification-info {
  border-left: 4px solid #3498db;
}

.notification-content {
  display: flex;
  align-items: center;
  padding: 12px 15px;
  gap: 10px;
}

.notification-icon {
  font-size: 1.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.notification-success .notification-icon {
  color: #2ecc71;
}

.notification-error .notification-icon {
  color: #e74c3c;
}

.notification-warning .notification-icon {
  color: #f39c12;
}

.notification-info .notification-icon {
  color: #3498db;
}

.notification-message {
  color: #333;
  font-size: 0.95rem;
  font-weight: 500;
  flex-grow: 1;
}

.notification-close {
  background: transparent;
  border: none;
  color: #999;
  cursor: pointer;
  font-size: 0.9rem;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  transition: all 0.2s;
}

.notification-close:hover {
  background-color: rgba(0, 0, 0, 0.05);
  color: #333;
}

.notification-progress {
  height: 3px;
  background: linear-gradient(to right, rgba(0, 0, 0, 0.1), rgba(0, 0, 0, 0.3));
  width: 100%;
  animation: progress-bar 3000ms linear forwards;
  transform-origin: left;
}

@keyframes progress-bar {
  from {
    transform: scaleX(1);
  }

  to {
    transform: scaleX(0);
  }
}

/* Add Responsive Styles */
@media (max-width: 992px) {
  .events-grid {
    /* Adjust minmax width or go to fewer columns */
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); 
  }
}

@media (max-width: 768px) {
  .admin-events {
    padding: 15px;
  }
  .page-header {
    flex-direction: column; /* Stack title and button */
    align-items: flex-start;
    gap: 10px;
    margin-bottom: 15px;
  }
  .filters {
    flex-direction: column; /* Stack filter groups */
    gap: 10px;
  }
  .filter-group {
    flex-direction: column; /* Stack select dropdowns */
    width: 100%; /* Make selects full width */
    gap: 10px;
  }
  .search-box {
    width: 100%; /* Make search full width */
  }
  .events-grid {
    grid-template-columns: 1fr; /* Single column grid */
    gap: 15px;
  }
  .event-card {
    /* Optional: Adjust card padding/styles */
  }
  .modal-content {
    max-width: 95%;
    max-height: 90vh;
  }
  .modal-body {
    max-height: calc(85vh - 120px); /* Adjust based on header/footer */
    overflow-y: auto; /* Ensure modal body scrolls */
  }
  .form-row {
    flex-direction: column; /* Stack form fields in rows */
    gap: 0; /* Remove gap if stacking */
  }
  .form-row .form-group {
     width: 100%; /* Make stacked fields full width */
  }
}

@media (max-width: 480px) {
  .event-details {
    flex-direction: column; /* Stack date/time details */
    align-items: flex-start;
    gap: 5px;
  }
  .event-stats {
     flex-direction: column; /* Stack stats */
     align-items: flex-start;
     gap: 5px;
  }
  .event-actions {
    flex-direction: column; /* Stack buttons */
    gap: 10px;
  }
  .event-actions button {
    width: 100%; /* Make buttons full width */
  }
  .form-actions {
    flex-direction: column; /* Stack modal buttons */
    gap: 10px;
  }
   .form-actions button {
    width: 100%;
   }
}
</style>