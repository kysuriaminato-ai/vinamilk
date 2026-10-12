// ============================================
// VINAMILK HRIS - localStorage Wrapper
// ============================================

const Storage = {
  prefix: 'vinamilk_hris_',

  get(key) {
    try {
      const data = localStorage.getItem(this.prefix + key);
      return data ? JSON.parse(data) : null;
    } catch (e) {
      return null;
    }
  },

  set(key, value) {
    try {
      localStorage.setItem(this.prefix + key, JSON.stringify(value));
    } catch (e) {
      console.warn('Storage full or unavailable');
    }
  },

  remove(key) {
    localStorage.removeItem(this.prefix + key);
  },

  clear() {
    Object.keys(localStorage)
      .filter(k => k.startsWith(this.prefix))
      .forEach(k => localStorage.removeItem(k));
  }
};
