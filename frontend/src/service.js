import axios from 'axios';

axios.defaults.baseURL = process.env.REACT_APP_API_URL;

// Attach token to requests when present
axios.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    console.error('Request Error:', error);
    return Promise.reject(error);
  }
);

// Handle 401 globally and forward other errors
axios.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      // clear token and force login
      localStorage.removeItem('token');
      window.location.href = '/login';
      return Promise.reject(error);
    }

    console.error('API Error:', error);
    return Promise.reject(error);
  }
);

export default {
  getTasks: async () => {
    const result = await axios.get(`/items`);
    return result.data;
  },

  addTask: async (name) => {
    console.log('addTask', name);
    const newTask = { name, isComplete: false };
    const result = await axios.post(`/items`, newTask);
    return result.data;
  },

  setCompleted: async (id, isComplete) => {
    console.log('setCompleted', { id, isComplete });
    const results = await axios.put(`/items/${id}`, { isComplete });
    return results.data;
  },

  deleteTask: async (id) => {
    console.log('deleteTask');
    const results = await axios.delete(`/items/${id}`);
    return results.data;
  }
};
