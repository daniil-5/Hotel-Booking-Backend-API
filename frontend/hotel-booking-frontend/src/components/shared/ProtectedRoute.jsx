// src/components/shared/ProtectedRoute.jsx
import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { jwtDecode } from 'jwt-decode';

const ProtectedRoute = ({ children, requiredRole }) => {
  const location = useLocation();
  const token = localStorage.getItem('token');
  
  // Check if the user is authenticated
  if (!token) {
    // Redirect to login if not authenticated
    return <Navigate to="/login" state={{ from: location }} replace />;
  }
  
  // If a specific role is required, check user's role
  if (requiredRole) {
    try {
      const decoded = jwtDecode(token);
      const userRole = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      
      if (userRole !== requiredRole && userRole !== 'Admin') {
        // Redirect to dashboard if user doesn't have the required role
        return <Navigate to="/dashboard" replace />;
      }
    } catch (error) {
      console.error('Error decoding token:', error);
      // If token decoding fails, redirect to login
      localStorage.removeItem('token');
      return <Navigate to="/login" state={{ from: location }} replace />;
    }
  }
  
  // If all checks pass, render the protected component
  return children;
};

export default ProtectedRoute;