/**
 * VINAMILK HRIS - API Helper
 * Dùng để giao tiếp với Backend MVC qua chuẩn Fetch API
 */

const Api = {
  // Thay đổi domain theo cấu hình XAMPP của bạn
  BASE_URL: 'http://localhost/vinamilk/public',

  /**
   * Thực hiện gọi GET request
   */
  async get(endpoint) {
    try {
      const response = await fetch(`${this.BASE_URL}/${endpoint}`);
      if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
      return await response.json();
    } catch (error) {
      console.error('API GET Error:', error);
      alert('Lỗi kết nối máy chủ!');
      return null;
    }
  },

  /**
   * Thực hiện gọi POST request (có đính kèm CSRF Token chống giả mạo)
   */
  async post(endpoint, data) {
    try {
      // 1. Lấy CSRF Token từ Server trước khi gửi POST
      let csrfToken = '';
      const tokenRes = await fetch(`${this.BASE_URL}/api-security/csrf-token`);
      if (tokenRes.ok) {
          const tokenJson = await tokenRes.json();
          csrfToken = tokenJson.csrf_token || '';
      }

      // 2. Chuyển object JS thành FormData để giả lập $_POST trong PHP
      const formData = new FormData();
      for (const key in data) {
        formData.append(key, data[key]);
      }

      // 3. Thực thi POST kèm Token bảo mật
      const response = await fetch(`${this.BASE_URL}/${endpoint}`, {
        method: 'POST',
        headers: {
            'X-CSRF-TOKEN': csrfToken
        },
        body: formData
      });
      
      if (!response.ok) {
          // Bắt lỗi 419 (CSRF hỏng) hoặc 403
          if (response.status === 419) throw new Error('Phiên làm việc hết hạn hoặc lỗi CSRF (419)');
          throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (error) {
      console.error('API POST Error:', error);
      alert('Lỗi kết nối máy chủ: ' + error.message);
      return null;
    }
  }
};
