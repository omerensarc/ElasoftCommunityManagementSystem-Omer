import api from './api';

const eventService = {
  /**
   * Tüm etkinlikleri getirir
   * @returns {Promise} - API yanıtı
   */
  getAllEvents() {
    return api.get('/events/listele');
  },

  /**
   * Belirli bir etkinliğin detaylarını getirir
   * @param {number} id - Etkinlik ID'si
   * @returns {Promise} - API yanıtı
   */
  getEventById(id) {
    // Doğru endpoint'i kullanıyoruz
    // Swagger dokümanına göre doğru endpoint kullanılmalı
    return api.get(`/events/${id}`);
  },

  /**
   * Danışmanın yetkili olduğu etkinlikleri getirir
   * @returns {Promise} - API yanıtı 
   */
  getAdvisorEvents() {
    return api.get('/events/yetkili-etkinlikler');
  },

  /**
   * Belirli bir kulübe ait etkinlikleri getirir
   * @param {number} clubId - Kulüp ID'si
   * @returns {Promise} - API yanıtı
   */
  getEventsByClubId(clubId) {
    return api.get(`/events/listele`, { params: { clubId } });
  },
  
  /**
   * Belirli bir kulübe ait etkinlikleri getirir (alias)
   * @param {number} clubId - Kulüp ID'si
   * @returns {Promise} - API yanıtı
   */
  getClubEvents(clubId) {
    return this.getEventsByClubId(clubId);
  },

  /**
   * Yeni etkinlik oluşturur
   * @param {FormData} formData - Etkinlik bilgileri (FormData olarak)
   * @returns {Promise} - API yanıtı
   */
  async createEvent(formData) {
    try {
      console.log('Creating event with FormData:');
      for (let pair of formData.entries()) {
        console.log(pair[0] + ': ' + pair[1]);
      }

      const token = localStorage.getItem('token');
      if (!token) {
        throw new Error('Oturum bulunamadı');
      }

      const response = await fetch('https://localhost:7274/api/events/ekle', {
        method: 'POST',
        headers: {
          'Accept': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: formData,
        credentials: 'include'
      });

      if (!response.ok) {
        const errorData = await response.json();
        console.error('API error response:', response.status, errorData);
        throw {
          response: {
            status: response.status,
            data: errorData
          }
        };
      }

      const data = await response.json();
      console.log('API success response:', response.status, data);
      return {
        data: data,
        status: response.status
      };
    } catch (error) {
      console.error('Error in createEvent:', error);
      throw error;
    }
  },

  /**
   * Etkinlik bilgilerini günceller
   * @param {number} id - Etkinlik ID'si
   * @param {FormData} formData - Güncellenecek etkinlik bilgileri (FormData olarak)
   * @returns {Promise} - API yanıtı
   */
  async updateEvent(id, formData) {
    try {
      console.log('Updating event with ID:', id);
      const startDate = formData.get('StartDate');
      const endDate = formData.get('EndDate');
      console.log('Date values being sent to API:', { startDate, endDate });

      console.log('All form data being sent:');
      for (let pair of formData.entries()) {
        console.log(pair[0] + ': ' + pair[1]);
      }

      const token = localStorage.getItem('token');

      const response = await fetch(`https://localhost:7274/api/events/${id}/update`, {
        method: 'PUT',
        headers: {
          'Accept': 'application/json',
          'Authorization': token ? `Bearer ${token}` : ''
        },
        body: formData,
        mode: 'cors',
        credentials: 'same-origin',
        redirect: 'follow'
      });

      const data = await response.json();

      if (!response.ok) {
        console.error('API error response:', response.status, data);
        if (data.errors) {
          console.error('Validation errors:');
          for (const [key, value] of Object.entries(data.errors)) {
            console.error(`- ${key}: ${value.join(', ')}`);
          }
        }

        throw {
          response: {
            status: response.status,
            data: data
          }
        };
      }

      console.log('API success response:', response.status, data);
      if (data.startDate) console.log('Updated startDate from API:', data.startDate);
      if (data.endDate) console.log('Updated endDate from API:', data.endDate);

      return {
        data: data,
        status: response.status
      };
    } catch (error) {
      console.error('Error in updateEvent:', error);
      throw error;
    }
  },

  /**
   * Etkinlik siler
   * @param {number} id - Etkinlik ID'si
   * @returns {Promise} - API yanıtı
   */
  deleteEvent(id) {
    console.log(`Deleting event with ID: ${id}`);
    return api.delete(`/events/${id}/delete`);
  },

  /**
   * Etkinliğe katılma
   * @param {number} eventId - Etkinlik ID'si
   * @returns {Promise} - API yanıtı
   */
  joinEvent(eventId) {
    return api.post(`/events/${eventId}/katil`);
  },

  /**
   * Etkinlikten ayrılma
   * @param {number} eventId - Etkinlik ID'si
   * @returns {Promise} - API yanıtı
   */
  leaveEvent(eventId) {
    return api.delete(`/events/${eventId}/ayril`);
  },

  
 
  /**
   * Etkinlik durumunu günceller
   * @param {number} eventId - Etkinlik ID'si
   * @param {string} status - Yeni durum (APPROVED, REJECTED, PENDING)
   * @returns {Promise} - API yanıtı
   */
  updateEventStatus(eventId, status) {
    return api.put(`/events/${eventId}/status`, { status });
  },

  /**
   * Etkinlik onaylama
   * @param {number} eventId - Etkinlik ID'si
   * @returns {Promise} - API yanıtı
   */
  approveEvent(eventId) {
    return api.put(`/events/${eventId}/approve`);
  },

  /**
   * Etkinlik reddetme
   * @param {number} eventId - Etkinlik ID'si
   * @returns {Promise} - API yanıtı
   */
  rejectEvent(eventId) {
    return api.put(`/events/${eventId}/reject`);
  }
};

export default eventService;
