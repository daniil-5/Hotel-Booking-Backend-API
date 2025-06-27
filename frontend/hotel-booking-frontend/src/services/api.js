// services/api.js
import axios from 'axios';
import { jwtDecode } from 'jwt-decode';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5044/api';
const TOKEN_KEY = 'token';

// Create base API client
const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor to include auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Auth services - maintain the original structure
export const authService = {
  // Login function
  login: async (email, password) => {
    try {
      const response = await apiClient.post('/auth/login', { email, password });
      if (response.data.token) {
        localStorage.setItem(TOKEN_KEY, response.data.token);
      }
      return response.data;
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  },
  
  // Register function
  register: async (userData) => {
    try {
      const response = await apiClient.post('/auth/register', userData);
      if (response.data.token) {
        localStorage.setItem(TOKEN_KEY, response.data.token); 
      }
      return response.data;
    } catch (error) {
      console.error('Registration error:', error);
      throw error;
    }
  },
  
  // Logout function
  logout: () => {
    localStorage.removeItem(TOKEN_KEY);
  },
  
  // Check if user is logged in
  isAuthenticated: () => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return false;
    
    try {
      const decoded = jwtDecode(token);
      const currentTime = Math.floor(Date.now() / 1000);
      
      // Check if token is expired
      return !(decoded.exp && decoded.exp < currentTime);
    } catch (error) {
      console.error('Token validation error:', error);
      localStorage.removeItem(TOKEN_KEY);
      return false;
    }
  },
  
  // Get current user data
  getCurrentUser: async () => {
    try {
      const response = await apiClient.get('/auth/current');
      return response.data;
    } catch (error) {
      console.error('Get current user error:', error);
      throw error;
    }
  },
  
  // Get user data from token
  getTokenData: () => {
    try {
      const token = localStorage.getItem(TOKEN_KEY);
      if (!token) return null;
      
      return jwtDecode(token);
    } catch (error) {
      console.error('Error decoding token:', error);
      return null;
    }
  }
};

// Export authApi with the same methods for backward compatibility
export const authApi = authService; // This line fixes the export error

// Export the API client as default
export default apiClient;