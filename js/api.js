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
   * Thực hiện gọi POST request
   */
  async post(endpoint, data) {
    try {
      // Chuyển object JS thành FormData để giả lập $_POST trong PHP
      const formData = new FormData();
      for (const key in data) {
        formData.append(key, data[key]);
      }

      const response = await fetch(`${this.BASE_URL}/${endpoint}`, {
        method: 'POST',
        body: formData
      });
      
      if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
      return await response.json();
    } catch (error) {
      console.error('API POST Error:', error);
      alert('Lỗi kết nối máy chủ!');
      return null;
    }
  }
};
