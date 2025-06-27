// services/authService.js
import { jwtDecode } from 'jwt-decode';  // <-- Changed from import jwtDecode from 'jwt-decode'

const TOKEN_KEY = 'token';

/**
 * Authentication service to handle JWT tokens
 */
const authService = {
  /**
   * Get token from localStorage
   * @returns {string|null} The stored JWT token
   */
  getToken() {
    return localStorage.getItem(TOKEN_KEY);
  },

  /**
   * Save token to localStorage
   * @param {string} token - JWT token
   */
  setToken(token) {
    localStorage.setItem(TOKEN_KEY, token);
  },

  /**
   * Remove token from localStorage
   */
  removeToken() {
    localStorage.removeItem(TOKEN_KEY);
  },

  /**
   * Check if token is valid and not expired
   * @returns {boolean} True if user is authenticated with valid token
   */
  isAuthenticated() {
    const token = this.getToken();
    if (!token) return false;
    
    try {
      const decoded = jwtDecode(token);
      const currentTime = Math.floor(Date.now() / 1000);
      
      // Check if token is expired
      return !(decoded.exp && decoded.exp < currentTime);
    } catch (error) {
      console.error('Token validation error:', error);
      this.removeToken();
      return false;
    }
  },

  /**
   * Extract user data from token
   * @returns {Object|null} Decoded user data from token
   */
  getTokenData() {
    try {
      const token = this.getToken();
      if (!token) return null;
      
      return jwtDecode(token);
    } catch (error) {
      console.error('Error decoding token:', error);
      return null;
    }
  },

  /**
   * Process login response and store token
   * @param {Object} userData - User data with token
   * @returns {Object} User data without token
   */
  processAuthResponse(userData) {
    const { token, ...userInfo } = userData;
    if (token) {
      this.setToken(token);
    }
    return userInfo;
  }
};

export default authService;