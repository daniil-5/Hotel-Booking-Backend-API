import React from 'react';
import { Routes as RouterRoutes, Route } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';
import Home from './pages/Home';
import Dashboard from './pages/Dashboard';
import CreateHotel from './pages/CreateHotel'; 
import ManageBookings from './pages/ManageBookings';  
import ManagePhotos from './pages/ManagePhotos';
import HotelDetail from './pages/HotelDetail';
import Profile from './pages/Profile';
import AccountSettings from './pages/AccountSettings';
import Favorites from './pages/Favorites';
import BookingDetails from './pages/BookingDetails';
import BookingForm from './pages/BookingForm';
import RoomPricing from './pages/RoomPricing';
import ManageHotels from './pages/ManageHotels';
import ProtectedRoute from './components/shared/ProtectedRoute';

const Routes = () => {
  return (
    <RouterRoutes>
      {/* Public routes */}
      <Route path="/" element={<Home />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      
      {/* Protected routes - require authentication */}
      <Route path="/dashboard" element={
        <ProtectedRoute>
          <Dashboard />
        </ProtectedRoute>
      } />
      
      {/* User profile routes */}
      <Route path="/profile" element={
        <ProtectedRoute>
          <Profile />
        </ProtectedRoute>
      } />
      <Route path="/account-settings" element={
        <ProtectedRoute>
          <AccountSettings />
        </ProtectedRoute>
      } />
      <Route path="/favorites" element={
        <ProtectedRoute>
          <Favorites />
        </ProtectedRoute>
      } />
      
      {/* Hotel routes */}
      <Route path="/hotels/:id" element={<HotelDetail />} />
      <Route path="/booking-form" element={
        <ProtectedRoute>
          <BookingForm />
        </ProtectedRoute>
      } />
      <Route path="/bookings/:id" element={
        <ProtectedRoute>
          <BookingDetails />
        </ProtectedRoute>
      } />
      <Route path="/room-pricing" element={
        <ProtectedRoute>
          <RoomPricing />
        </ProtectedRoute>
      } />
      
      {/* Manager/Admin routes */}
      <Route path="/hotels/create" element={
        <ProtectedRoute requiredRole="Manager">
          <CreateHotel />
        </ProtectedRoute>
      } />
      <Route path="/bookings/manage" element={
        <ProtectedRoute requiredRole="Manager">
          <ManageBookings />
        </ProtectedRoute>
      } />
      <Route path="/photos/manage" element={
        <ProtectedRoute requiredRole="Manager">
          <ManagePhotos />
        </ProtectedRoute>
      } />
      <Route path="/hotels/manage" element={
        <ProtectedRoute requiredRole="Manager">
          <ManageHotels />
        </ProtectedRoute>
      } />
      <Route path="/hotels/manage/:hotelId" element={
        <ProtectedRoute requiredRole="Manager">
          <ManageHotels />
        </ProtectedRoute>
      } />
    </RouterRoutes>
  );
};

export default Routes;