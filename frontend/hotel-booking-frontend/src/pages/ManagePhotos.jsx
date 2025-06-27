// src/pages/ManagePhotos.jsx
import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Container,
  Flex,
  Grid,
  Heading,
  IconButton,
  Image,
  Select,
  Spinner,
  Text,
  useToast,
  VStack,
  HStack,
  Card,
  CardBody,
  FormControl,
  FormLabel,
  Switch,
  Badge,
  useColorModeValue,
  useDisclosure,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalCloseButton,
  ModalBody,
  ModalFooter,
  Input,
} from '@chakra-ui/react';
import { FiArrowLeft, FiUpload, FiTrash2, FiStar, FiX, FiPlus, FiImage } from 'react-icons/fi';
import apiClient, { authService } from '../services/api';

function ManagePhotos() {
  const { hotelId } = useParams(); // If hotelId provided, show photos for that hotel only
  const navigate = useNavigate();
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  
  const [hotels, setHotels] = useState([]);
  const [photos, setPhotos] = useState([]);
  const [selectedHotel, setSelectedHotel] = useState(hotelId || '');
  const [isLoading, setIsLoading] = useState(true);
  const [isCheckingAuth, setIsCheckingAuth] = useState(true);
  const [uploadFile, setUploadFile] = useState(null);
  const [isUploading, setIsUploading] = useState(false);
  
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  const bg = useColorModeValue('white', 'gray.800');

  // Check authentication and access rights
  useEffect(() => {
    const checkAccess = async () => {
      setIsCheckingAuth(true);
      
      // If no token exists, redirect to login
      if (!authService.isAuthenticated()) {
        toast({
          title: 'Authentication Required',
          description: 'Please log in to access this page.',
          status: 'warning',
          duration: 5000,
          isClosable: true,
        });
        navigate('/login');
        return;
      }
      
      try {
        // First try using current user endpoint if available
        try {
          const userResponse = await apiClient.get('/auth/current');
          const userRole = userResponse.data.role || '';
          console.log("Role from API:", userRole);
          
          // Check if user is manager or admin
          if (!(userRole === 'Manager' || userRole === 'Admin' || 
                userRole.toLowerCase() === 'manager' || 
                userRole.toLowerCase() === 'admin')) {
            throw new Error('Insufficient permissions');
          }
        } catch (apiError) {
          console.log("Falling back to token check:", apiError);
          
          // Fallback to token parsing
          const token = localStorage.getItem('token');
          
          if (!token) {
            throw new Error('No token available');
          }
          
          // Manual token parsing for debugging
          const base64Url = token.split('.')[1];
          const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
          const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
          }).join(''));
          
          const decoded = JSON.parse(jsonPayload);
          console.log("Token payload:", decoded);
          
          // Check various role formats in the token
          const msRole = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
          const standardRole = decoded.role;
          
          console.log("Roles found in token:", { 
            msRole, 
            standardRole,
            allRoles: Object.keys(decoded).filter(key => key.toLowerCase().includes('role'))
          });
          
          // Get role from token using various formats
          const tokenRole = msRole || standardRole || '';
          
          // Flexible role checking
          const isManager = 
            tokenRole === 'Manager' || 
            tokenRole === 'Admin' ||
            tokenRole.toLowerCase() === 'manager' || 
            tokenRole.toLowerCase() === 'admin';
            
          if (!isManager) {
            throw new Error(`Insufficient permissions with role: ${tokenRole}`);
          }
        }
        
        // Load hotels and photos data
        await loadData();
      } catch (error) {
        console.error("Access check failed:", error);
        toast({
          title: 'Access Denied',
          description: 'You need manager permissions to manage hotel photos.',
          status: 'error',
          duration: 5000,
          isClosable: true,
        });
        navigate('/dashboard');
      } finally {
        setIsCheckingAuth(false);
      }
    };

    checkAccess();
  }, [navigate, toast, hotelId]);

  // Load hotels and photos data
  const loadData = async () => {
    setIsLoading(true);
    try {
      // Fetch hotels
      const hotelsResponse = await apiClient.get('/hotels');
      setHotels(hotelsResponse.data);
      
      // If hotelId provided or selected, fetch photos for that hotel
      if (hotelId || selectedHotel) {
        const id = hotelId || selectedHotel;
        setSelectedHotel(id);
        await loadHotelPhotos(id);
      } else if (hotelsResponse.data.length > 0) {
        // If no hotel selected, use the first one
        setSelectedHotel(hotelsResponse.data[0].id);
        await loadHotelPhotos(hotelsResponse.data[0].id);
      } else {
        setPhotos([]);
      }
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to load data. Please try again.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setIsLoading(false);
    }
  };

  // Load photos for a specific hotel
  const loadHotelPhotos = async (hotelId) => {
    try {
      const photosResponse = await apiClient.get(`/hotelPhotos/hotel/${hotelId}`);
      setPhotos(photosResponse.data);
    } catch (error) {
      console.error('Error loading photos:', error);
      toast({
        title: 'Error',
        description: 'Failed to load photos. Please try again.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    }
  };

  // Handle hotel selection change
  const handleHotelChange = async (e) => {
    const hotelId = e.target.value;
    setSelectedHotel(hotelId);
    if (hotelId) {
      await loadHotelPhotos(hotelId);
    } else {
      setPhotos([]);
    }
  };

  // Handle file selection
  const handleFileChange = (e) => {
    if (e.target.files && e.target.files[0]) {
      setUploadFile(e.target.files[0]);
    }
  };

  // Upload photo
  const handleUpload = async () => {
    if (!uploadFile || !selectedHotel) {
      toast({
        title: 'Error',
        description: 'Please select a file and a hotel',
        status: 'warning',
        duration: 3000,
        isClosable: true,
      });
      return;
    }

    setIsUploading(true);
    const formData = new FormData();
    formData.append('file', uploadFile);
    formData.append('hotelId', selectedHotel);
    formData.append('isMain', photos.length === 0 ? 'true' : 'false'); // First photo becomes main

    try {
      await apiClient.post('/hotelPhotos', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      
      // Reload photos
      await loadHotelPhotos(selectedHotel);
      
      toast({
        title: 'Success',
        description: 'Photo uploaded successfully',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
      
      // Reset file input
      setUploadFile(null);
      onClose();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to upload photo. Please try again.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setIsUploading(false);
    }
  };

  // Set photo as main
  const setMainPhoto = async (photoId) => {
    try {
      await apiClient.put(`/hotelPhotos/${photoId}/setmain`);
      toast({
        title: 'Success',
        description: 'Main photo updated',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
      
      // Reload photos
      await loadHotelPhotos(selectedHotel);
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to update main photo. Please try again.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    }
  };

  // Delete photo
  const deletePhoto = async (photoId) => {
    if (window.confirm('Are you sure you want to delete this photo?')) {
      try {
        await apiClient.delete(`/hotelPhotos/${photoId}`);
        
        toast({
          title: 'Success',
          description: 'Photo deleted successfully',
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
        
        // Reload photos
        await loadHotelPhotos(selectedHotel);
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to delete photo. Please try again.',
          status: 'error',
          duration: 5000,
          isClosable: true,
        });
      }
    }
  };

  // Show loading spinner while checking authentication
  if (isCheckingAuth) {
    return (
      <Flex justify="center" align="center" height="100vh">
        <Spinner size="xl" thickness="4px" color="brand.500" />
        <Text ml={4}>Checking permissions...</Text>
      </Flex>
    );
  }

  if (isLoading) {
    return (
      <Flex justify="center" align="center" height="80vh">
        <Spinner size="xl" thickness="4px" color="brand.500" />
      </Flex>
    );
  }

  return (
    <Container maxW="container.xl" py={8}>
      <Flex justify="space-between" align="center" mb={8}>
        <Button 
          leftIcon={<FiArrowLeft />} 
          variant="ghost" 
          onClick={() => navigate('/dashboard')}
        >
          Back to Dashboard
        </Button>
        <Button 
          colorScheme="brand" 
          leftIcon={<FiUpload />}
          onClick={onOpen}
        >
          Upload Photo
        </Button>
      </Flex>
      
      <Heading size="lg" mb={6}>Manage Hotel Photos</Heading>
      
      {!hotelId && (
        <FormControl mb={8}>
          <FormLabel>Select Hotel</FormLabel>
          <Select 
            value={selectedHotel} 
            onChange={handleHotelChange}
            placeholder="Select a hotel"
            bg={bg}
          >
            {hotels.map(hotel => (
              <option key={hotel.id} value={hotel.id}>
                {hotel.name}
              </option>
            ))}
          </Select>
        </FormControl>
      )}
      
      {selectedHotel ? (
        <>
          <Text mb={4}>
            {photos.length} photo{photos.length !== 1 ? 's' : ''} available for {' '}
            <strong>{hotels.find(h => h.id === selectedHotel)?.name || 'this hotel'}</strong>
          </Text>
          
          {photos.length === 0 ? (
            <Card p={8} borderWidth="1px" borderColor={borderColor} bg={bg}>
              <CardBody textAlign="center">
                <VStack spacing={4}>
                  <FiImage size="48px" color="gray" />
                  <Heading size="md">No Photos Available</Heading>
                  <Text>Upload photos to showcase this hotel.</Text>
                  <Button 
                    colorScheme="brand" 
                    leftIcon={<FiUpload />}
                    onClick={onOpen}
                  >
                    Upload First Photo
                  </Button>
                </VStack>
              </CardBody>
            </Card>
          ) : (
            <Grid templateColumns={{ base: '1fr', md: 'repeat(2, 1fr)', lg: 'repeat(3, 1fr)' }} gap={6}>
              {photos.map(photo => (
                <Card key={photo.id} borderWidth="1px" borderColor={borderColor} overflow="hidden" bg={bg}>
                  <Box position="relative">
                    {photo.isMain && (
                      <Badge 
                        position="absolute" 
                        top={2} 
                        right={2} 
                        colorScheme="green"
                        px={2}
                      >
                        Main Photo
                      </Badge>
                    )}
                    <Image 
                      src={photo.url} 
                      alt="Hotel photo" 
                      objectFit="cover"
                      h="200px"
                      w="100%"
                    />
                  </Box>
                  <CardBody>
                    <Flex justify="space-between" align="center">
                      <HStack>
                        {!photo.isMain && (
                          <Button 
                            size="sm" 
                            leftIcon={<FiStar />} 
                            onClick={() => setMainPhoto(photo.id)}
                          >
                            Set as Main
                          </Button>
                        )}
                      </HStack>
                      <IconButton
                        icon={<FiTrash2 />}
                        colorScheme="red"
                        variant="ghost"
                        aria-label="Delete photo"
                        onClick={() => deletePhoto(photo.id)}
                      />
                    </Flex>
                  </CardBody>
                </Card>
              ))}
            </Grid>
          )}
        </>
      ) : (
        <Box textAlign="center" p={8}>
          <Heading size="md">No Hotel Selected</Heading>
          <Text mt={2}>Please select a hotel to manage its photos.</Text>
        </Box>
      )}
      
      {/* Upload Photo Modal */}
      <Modal isOpen={isOpen} onClose={onClose}>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Upload Hotel Photo</ModalHeader>
          <ModalCloseButton />
          
          <ModalBody>
            <VStack spacing={4}>
              <FormControl>
                <FormLabel>Select Hotel</FormLabel>
                <Select 
                  value={selectedHotel} 
                  onChange={handleHotelChange}
                  isDisabled={!!hotelId || isUploading}
                >
                  {hotels.map(hotel => (
                    <option key={hotel.id} value={hotel.id}>
                      {hotel.name}
                    </option>
                  ))}
                </Select>
              </FormControl>
              
              <FormControl>
                <FormLabel>Select Photo</FormLabel>
                <Input
                  type="file"
                  accept="image/*"
                  onChange={handleFileChange}
                  isDisabled={isUploading}
                  p={1}
                />
              </FormControl>
              
              {uploadFile && (
                <Box borderWidth="1px" p={2} borderRadius="md" w="100%">
                  <Text fontSize="sm">
                    Selected: {uploadFile.name} ({Math.round(uploadFile.size / 1024)} KB)
                  </Text>
                </Box>
              )}
            </VStack>
          </ModalBody>
          
          <ModalFooter>
            <Button mr={3} onClick={onClose} isDisabled={isUploading}>
              Cancel
            </Button>
            <Button 
              colorScheme="brand" 
              onClick={handleUpload}
              isLoading={isUploading}
              loadingText="Uploading"
              leftIcon={<FiUpload />}
            >
              Upload
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </Container>
  );
}

export default ManagePhotos;