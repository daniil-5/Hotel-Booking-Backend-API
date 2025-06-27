// src/pages/CreateHotel.jsx
import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Container,
  Divider,
  FormControl,
  FormLabel,
  Heading,
  Input,
  NumberInput,
  NumberInputField,
  NumberInputStepper,
  NumberIncrementStepper,
  NumberDecrementStepper,
  Stack,
  Textarea,
  useToast,
  Select,
  FormHelperText,
  InputGroup,
  InputLeftAddon,
  Flex,
  Card,
  CardBody,
  Text,
  useColorModeValue,
  IconButton,
  VStack,
  HStack,
  Image,
  Grid,
  GridItem,
  Spinner,
  Badge,
  Center,
  Wrap,
  WrapItem,
  SimpleGrid,
} from '@chakra-ui/react';
import { 
  FiArrowLeft, 
  FiSave, 
  FiUpload, 
  FiX, 
  FiImage, 
  FiStar, 
  FiTrash2, 
  FiPlusCircle,
  FiCheck 
} from 'react-icons/fi';
import apiClient, { authService } from '../services/api';

function CreateHotel() {
  const navigate = useNavigate();
  const toast = useToast();
  const fileInputRef = useRef(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isCheckingAuth, setIsCheckingAuth] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [hotelData, setHotelData] = useState({
    name: '',
    description: '',
    location: '',
    address: '',
    rating: 0,
    basePrice: 0, // Add missing basePrice field
    contactPhone: '',
    contactEmail: '',
    amenities: [],
    roomTypes: [] // Add missing roomTypes array
  });
  
  // Photo upload states
  const [uploadedPhotos, setUploadedPhotos] = useState([]);
  const [mainPhotoIndex, setMainPhotoIndex] = useState(0);
  const [isDraggingOver, setIsDraggingOver] = useState(false);

  const formBg = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  const dropzoneColor = useColorModeValue('gray.100', 'gray.700');
  const dropzoneActiveColor = useColorModeValue('blue.50', 'blue.900');

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
      } catch (error) {
        console.error("Access check failed:", error);
        toast({
          title: 'Access Denied',
          description: 'You need manager permissions to create hotels.',
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
  }, [navigate, toast]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setHotelData({
      ...hotelData,
      [name]: value,
    });
  };

  const handleRatingChange = (value) => {
    setHotelData({
      ...hotelData,
      rating: value,
    });
  };

  // Handle file selection via button
  const handleFileSelection = (e) => {
    if (e.target.files && e.target.files.length > 0) {
      const newFiles = Array.from(e.target.files);
      
      // Create preview URLs and store the file objects
      const newPhotos = newFiles.map(file => ({
        file: file,
        previewUrl: URL.createObjectURL(file),
        name: file.name,
        size: file.size,
        uploading: false,
        uploaded: false,
        id: null // Will be filled after upload
      }));
      
      setUploadedPhotos([...uploadedPhotos, ...newPhotos]);
      
      // If this is the first photo, make it the main photo
      if (uploadedPhotos.length === 0 && newPhotos.length > 0) {
        setMainPhotoIndex(0);
      }
    }
  };

  // Handle drag events
  const handleDragEnter = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDraggingOver(true);
  };

  const handleDragLeave = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDraggingOver(false);
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDrop = (e) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDraggingOver(false);
    
    const files = e.dataTransfer.files;
    if (files && files.length > 0) {
      const newFiles = Array.from(files).filter(file => file.type.startsWith('image/'));
      
      // Create preview URLs and store the file objects
      const newPhotos = newFiles.map(file => ({
        file: file,
        previewUrl: URL.createObjectURL(file),
        name: file.name,
        size: file.size,
        uploading: false,
        uploaded: false,
        id: null
      }));
      
      setUploadedPhotos([...uploadedPhotos, ...newPhotos]);
      
      // If this is the first photo, make it the main photo
      if (uploadedPhotos.length === 0 && newPhotos.length > 0) {
        setMainPhotoIndex(0);
      }
    }
  };

  // Remove a photo from the list
  const handleRemovePhoto = (index) => {
    const updatedPhotos = [...uploadedPhotos];
    
    // Release the object URL to avoid memory leaks
    URL.revokeObjectURL(updatedPhotos[index].previewUrl);
    
    // Remove the photo
    updatedPhotos.splice(index, 1);
    setUploadedPhotos(updatedPhotos);
    
    // Update the main photo index if needed
    if (mainPhotoIndex === index) {
      setMainPhotoIndex(updatedPhotos.length > 0 ? 0 : -1);
    } else if (mainPhotoIndex > index) {
      setMainPhotoIndex(mainPhotoIndex - 1);
    }
  };

  // Set a photo as the main photo
  const handleSetMainPhoto = (index) => {
    setMainPhotoIndex(index);
  };

  // Improved photo upload function with more detailed error handling and logging
  const uploadPhotos = async (hotelId) => {
    if (uploadedPhotos.length === 0) return [];
    
    try {
      const formData = new FormData();
      
      // The key issue: ASP.NET Core expects files[] for binding to List<IFormFile>
      // When using [FromForm] List<IFormFile> files in the controller
      uploadedPhotos.forEach(photo => {
        // Use files (matching the parameter name in the controller)
        formData.append('files', photo.file);
      });
      
      // Set the isMain parameter separately for the first photo if needed
      formData.append('isMain', uploadedPhotos[mainPhotoIndex]?.name || '');
      
      console.log(`Attempting to upload ${uploadedPhotos.length} photos to hotelId ${hotelId}`);
      
      // The correct endpoint with the proper formatting
      const uploadResponse = await apiClient.post(
        `/HotelPhotos/upload/multiple?hotelId=${hotelId}`, 
        formData, 
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          }
        }
      );
      
      console.log('Upload successful:', uploadResponse.data);
      return uploadResponse.data;
    } catch (error) {
      console.error('Photo upload failed:', error.response?.data || error.message);
      
      // Fall back to single uploads if multiple upload fails
      try {
        console.log('Attempting individual uploads as fallback...');
        return await uploadPhotosSingly(hotelId);
      } catch (singleUploadError) {
        console.error('All photo upload attempts failed:', singleUploadError);
        return [];
      }
    }
  };

  // Alternative implementation if the multiple upload endpoint has issues
  const uploadPhotosSingly = async (hotelId) => {
    if (uploadedPhotos.length === 0) return [];
    
    const uploadedPhotosData = [];
    
    for (let i = 0; i < uploadedPhotos.length; i++) {
      const photo = uploadedPhotos[i];
      const isMain = i === mainPhotoIndex;
      
      try {
        const formData = new FormData();
        formData.append('file', photo.file);
        
        // Use the single upload endpoint with query parameters
        const response = await apiClient.post(
          `/HotelPhotos/upload?hotelId=${hotelId}&isMain=${isMain}&description=${photo.name}`, 
          formData,
          {
            headers: {
              'Content-Type': 'multipart/form-data',
            }
          }
        );
        
        console.log(`Successfully uploaded photo ${i+1}/${uploadedPhotos.length}`);
        uploadedPhotosData.push(response.data);
      } catch (error) {
        console.error(`Error uploading photo ${photo.name}:`, error);
      }
    }
    
    return uploadedPhotosData;
  };

  // Add a function to handle amenities as an array rather than a string
  const handleAmenitiesChange = (e) => {
    const amenitiesText = e.target.value;
    // Split by commas and trim whitespace
    const amenitiesArray = amenitiesText.split(',')
      .map(item => item.trim())
      .filter(item => item.length > 0);
    
    setHotelData({
      ...hotelData,
      amenities: amenitiesArray
    });
  };

  // Add state for room types
  const [roomTypeForm, setRoomTypeForm] = useState({
    name: '',
    description: '',
    area: 25,
    capacity: 2,
    basePrice: 0,
    floor: 1
  });

  // Add a function to handle room type input changes
  const handleRoomTypeInputChange = (field, value) => {
    setRoomTypeForm({
      ...roomTypeForm,
      [field]: value
    });
  };

  // Add function to add a room type
  const addRoomType = () => {
    // Validate room type
    if (!roomTypeForm.name) {
      toast({
        title: 'Room type name required',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
      return;
    }
    
    if (roomTypeForm.basePrice <= 0) {
      toast({
        title: 'Base price must be greater than zero',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
      return;
    }
    
    // Add the room type to the hotel data
    setHotelData({
      ...hotelData,
      roomTypes: [...hotelData.roomTypes, {...roomTypeForm}]
    });
    
    // Reset the form
    setRoomTypeForm({
      name: '',
      description: '',
      area: 25,
      capacity: 2,
      basePrice: 0,
      floor: 1
    });
    
    // Show success toast
    toast({
      title: 'Room type added',
      description: `${roomTypeForm.name} has been added to your hotel.`,
      status: 'success',
      duration: 3000,
      isClosable: true,
    });
  };

  // Add a function to remove room types
  const removeRoomType = (index) => {
    const updatedRoomTypes = [...hotelData.roomTypes];
    updatedRoomTypes.splice(index, 1);
    
    setHotelData({
      ...hotelData,
      roomTypes: updatedRoomTypes
    });
    
    toast({
      title: 'Room type removed',
      status: 'info',
      duration: 2000,
      isClosable: true,
    });
  };

  // Update handleSubmit to better handle the photo upload process
  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      // Validate if at least one room type exists
      if (hotelData.roomTypes.length === 0) {
        toast({
          title: 'Room type required',
          description: 'Please add at least one room type to your hotel.',
          status: 'error',
          duration: 5000,
          isClosable: true,
        });
        setIsLoading(false);
        return;
      }

      // Format the hotel data to match the API requirements
      const hotelPayload = {
        name: hotelData.name,
        description: hotelData.description,
        location: hotelData.location,
        address: hotelData.address,
        rating: parseFloat(hotelData.rating),
        basePrice: parseFloat(hotelData.basePrice),
        amenities: hotelData.amenities,
        // Room types will be created separately after hotel creation
        photos: [] // Will be filled after photo upload
      };

      console.log('Creating hotel with data:', hotelPayload);
      
      // Create the hotel
      const response = await apiClient.post('/Hotels', hotelPayload);
      const newHotelId = response.data.id;
      
      // Create room types
      const roomTypePromises = hotelData.roomTypes.map(roomType => {
        const roomTypePayload = {
          name: roomType.name,
          description: roomType.description || `${roomType.name} room type`,
          area: parseFloat(roomType.area),
          capacity: parseInt(roomType.capacity),
          basePrice: parseFloat(roomType.basePrice),
          floor: roomType.floor ? parseInt(roomType.floor) : null,
          hotelId: newHotelId
        };
        
        return apiClient.post('/room-types', roomTypePayload);
      });
      
      // Wait for all room types to be created
      await Promise.all(roomTypePromises);
      console.log(`Created ${hotelData.roomTypes.length} room types for hotel ID ${newHotelId}`);
      
      // Upload photos if any exist
      if (uploadedPhotos.length > 0) {
        setIsUploading(true);
        try {
          console.log(`Uploading ${uploadedPhotos.length} photos for hotel ID ${newHotelId}`);
          
          // Try to upload photos with enhanced error handling
          const uploadedPhotosData = await uploadPhotos(newHotelId);
          
          if (uploadedPhotosData && uploadedPhotosData.length > 0) {
            console.log('Photos uploaded successfully:', uploadedPhotosData);
            
            toast({
              title: 'Hotel Created Successfully',
              description: `${hotelData.name} has been added with ${uploadedPhotosData.length} photos.`,
              status: 'success',
              duration: 5000,
              isClosable: true,
            });
          } else {
            // Partial success - hotel created but photos failed
            toast({
              title: 'Hotel Created',
              description: `${hotelData.name} was created, but there was an issue uploading photos. You can add photos later.`,
              status: 'warning',
              duration: 5000,
              isClosable: true,
            });
          }
        } catch (photoError) {
          console.error('Photo upload error:', photoError);
          
          toast({
            title: 'Hotel Created, Photos Error',
            description: `${hotelData.name} was created, but photos couldn't be uploaded: ${photoError.message}. You can add them later.`,
            status: 'warning',
            duration: 5000,
            isClosable: true,
          });
        } finally {
          setIsUploading(false);
        }
      } else {
        toast({
          title: 'Hotel Created',
          description: `${hotelData.name} has been added successfully.`,
          status: 'success',
          duration: 5000,
          isClosable: true,
        });
      }
      
      // Navigate to dashboard
      navigate('/dashboard');
    } catch (error) {
      console.error('Error creating hotel:', error);
      toast({
        title: 'Error Creating Hotel',
        description: error.response?.data || 'There was an error creating the hotel. Please try again later.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Container maxW="7xl" py={{ base: '12', md: '24' }} px={{ base: '4', sm: '6' }}>
      <Stack spacing={{ base: '8', md: '10' }} textAlign="center">
        <Stack spacing={{ base: '2', md: '4' }} textAlign="center">
          <Heading size={{ base: 'xl', md: '2xl' }} fontWeight="extrabold">
            Create Hotel
          </Heading>
          <HStack spacing="4" justify="center" wrap="wrap">
            <Badge colorScheme="green">Beta</Badge>
            <Badge colorScheme="blue">New Feature</Badge>
          </HStack>
        </Stack>
        
        <Stack spacing={{ base: '6', md: '8' }}>
          <Stack spacing={{ base: '4', md: '5' }}>
            <Text fontSize={{ base: 'lg', md: 'xl' }} color="gray.600">
              Fill in the details below to create a new hotel listing.
            </Text>
          </Stack>
          
          <Box 
            as="form" 
            onSubmit={handleSubmit} 
            borderWidth={{ base: '1px', md: '2px' }} 
            borderRadius="lg" 
            overflow="hidden"
            bg={formBg}
            boxShadow={{ base: 'md', md: 'lg' }}
          >
            <Stack spacing={{ base: '0', md: '6' }}>
              <VStack 
                spacing={{ base: '0', md: '4' }} 
                align={{ base: 'start', md: 'stretch' }}
                p={{ base: '4', md: '6' }}
                borderBottomWidth={{ base: '1px', md: '2px' }}
                borderColor={borderColor}
              >
                <Heading size="md" fontWeight="semibold" mb={{ base: '2', md: '4' }}>
                  Hotel Details
                </Heading>
                
                <Stack spacing={{ base: '4', md: '5' }} width="full">
                  <FormControl isRequired>
                    <FormLabel htmlFor="name">Hotel Name</FormLabel>
                    <Input 
                      id="name" 
                      name="name" 
                      value={hotelData.name} 
                      onChange={handleChange} 
                      placeholder="Enter hotel name"
                      _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px rgba(66, 153, 225, 0.6)' }}
                    />
                  </FormControl>
                  
                  <FormControl isRequired>
                    <FormLabel htmlFor="description">Description</FormLabel>
                    <Textarea 
                      id="description" 
                      name="description" 
                      value={hotelData.description} 
                      onChange={handleChange} 
                      placeholder="Enter hotel description"
                      size="sm"
                      resize="vertical"
                      _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px rgba(66, 153, 225, 0.6)' }}
                    />
                  </FormControl>
                  
                  <FormControl isRequired>
                    <FormLabel htmlFor="location">Location</FormLabel>
                    <Input 
                      id="location" 
                      name="location" 
                      value={hotelData.location} 
                      onChange={handleChange} 
                      placeholder="Enter hotel location"
                      _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px rgba(66, 153, 225, 0.6)' }}
                    />
                  </FormControl>
                  
                  <FormControl isRequired>
                    <FormLabel htmlFor="address">Address</FormLabel>
                    <Input 
                      id="address" 
                      name="address" 
                      value={hotelData.address} 
                      onChange={handleChange} 
                      placeholder="Enter hotel address"
                      _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px rgba(66, 153, 225, 0.6)' }}
                    />
                  </FormControl>
                  
                  <FormControl isRequired>
                    <FormLabel htmlFor="rating">Rating</FormLabel>
                    <NumberInput 
                      id="rating" 
                      name="rating" 
                      value={hotelData.rating} 
                      onChange={(valueString, valueNumber) => handleRatingChange(valueNumber)} 
                      min={0} 
                      max={5} 
                      step={0.1}
                      precision={1}
                      _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px rgba(66, 153, 225, 0.6)' }}
                    >
                      <NumberInputField 
                        placeholder="Enter hotel rating" 
                        _placeholder={{ color: 'gray.500' }}
                      />
                      <NumberInputStepper>
                        <NumberIncrementStepper />
                        <NumberDecrementStepper />
                      </NumberInputStepper>
                    </NumberInput>
                    <FormHelperText>Rating from 0 to 5, e.g., 4.5</FormHelperText>
                  </FormControl>
                  
                  <FormControl isRequired>
                    <FormLabel htmlFor="basePrice">Base Price</FormLabel>
                    <NumberInput 
                      id="basePrice" 
                      name="basePrice" 
                      value={hotelData.basePrice} 
                      onChange={(valueString, valueNumber) => handleChange({ target: { name: 'basePrice', value: valueNumber }})} 
                      min={0} 
                      step={1}
                      _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px rgba(66, 153, 225, 0.6)' }}
                    >
                      <NumberInputField 
                        placeholder="Enter base price" 
                        _placeholder={{ color: 'gray.500' }}
                      />
                      <NumberInputStepper>
                        <NumberIncrementStepper />
                        <NumberDecrementStepper />
                      </NumberInputStepper>
                    </NumberInput>
                    <FormHelperText>Base price in your currency</FormHelperText>
                  </FormControl>
                  
                  <FormControl>
                    <FormLabel htmlFor="contactPhone">Contact Phone</FormLabel>
                    <Input 
                      id="contactPhone" 
                      name="contactPhone" 
                      value={hotelData.contactPhone} 
                      onChange={handleChange} 
                      placeholder="Enter contact phone"
                      _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px rgba(66, 153, 225, 0.6)' }}
                    />
                  </FormControl>
                  
                  <FormControl>
                    <FormLabel htmlFor="contactEmail">Contact Email</FormLabel>
                    <Input 
                      id="contactEmail" 
                      name="contactEmail" 
                      value={hotelData.contactEmail} 
                      onChange={handleChange} 
                      placeholder="Enter contact email"
                      type="email"
                      _focus={{ borderColor: 'blue.500', boxShadow: '0 0 0 1px rgba(66, 153, 225, 0.6)' }}
                    />
                  </FormControl>
                </Stack>
              </VStack>
              
              <VStack 
                spacing={{ base: '0', md: '4' }} 
                align={{ base: 'start', md: 'stretch' }}
                p={{ base: '4', md: '6' }}
                borderBottomWidth={{ base: '1px', md: '2px' }}
                borderColor={borderColor}
              >
                <Heading size="md" fontWeight="semibold" mb={{ base: '2', md: '4' }}>
                  Photos
                </Heading>
                
                <Stack spacing={{ base: '4', md: '5' }} width="full">
                  <FormControl>
                    <FormLabel htmlFor="photos">Upload Photos</FormLabel>
                    <Input 
                      id="photos" 
                      type="file" 
                      accept="image/*" 
                      multiple 
                      onChange={handleFileSelection}
                      ref={fileInputRef}
                      display="none"
                    />
                    
                    <Button 
                      as="label" 
                      htmlFor="photos" 
                      leftIcon={<FiUpload />} 
                      colorScheme="blue" 
                      variant="solid"
                      size="sm"
                      cursor="pointer"
                    >
                      Choose Photos
                    </Button>
                    
                    <Button 
                      onClick={() => fileInputRef.current?.click()} 
                      leftIcon={<FiPlusCircle />} 
                      colorScheme="green" 
                      variant="outline"
                      size="sm"
                    >
                      Add More Photos
                    </Button>
                    
                    <FormHelperText>Upload hotel photos (max. 10 MB each)</FormHelperText>
                  </FormControl>
                  
                  <Box 
                    borderWidth="1px" 
                    borderRadius="md" 
                    overflow="hidden"
                    bg={dropzoneColor}
                    _hover={{ bg: dropzoneActiveColor }}
                    p="4"
                    position="relative"
                    onDragEnter={handleDragEnter}
                    onDragLeave={handleDragLeave}
                    onDragOver={handleDragOver}
                    onDrop={handleDrop}
                  >
                    <Text 
                      textAlign="center" 
                      color="gray.500" 
                      fontSize="sm"
                      py="10"
                    >
                      Drag and drop your photos here, or click to select files
                    </Text>
                    
                    {isDraggingOver && (
                      <Box 
                        position="absolute" 
                        top="0" 
                        left="0" 
                        right="0" 
                        bottom="0" 
                        bg="blue.100" 
                        borderRadius="md"
                        opacity="0.7"
                        zIndex="1"
                      />
                    )}
                  </Box>
                  
                  <VStack spacing="4" align="stretch">
                    {uploadedPhotos.length > 0 && (
                      <Grid templateColumns={{ base: '1fr', sm: 'repeat(2, 1fr)' }} gap="4">
                        {uploadedPhotos.map((photo, index) => (
                          <GridItem key={index} borderWidth="1px" borderRadius="md" overflow="hidden">
                            <Box position="relative">
                              <Image 
                                src={photo.previewUrl} 
                                alt={`Uploaded photo ${index + 1}`} 
                                objectFit="cover" 
                                width="100%" 
                                height="200px"
                                fallback={<Spinner />}
                              />
                              
                              <IconButton 
                                aria-label="Remove photo"
                                icon={<FiTrash2 />}
                                size="sm"
                                colorScheme="red"
                                variant="outline"
                                position="absolute"
                                top="2"
                                right="2"
                                onClick={() => handleRemovePhoto(index)}
                                zIndex="2"
                              />
                              
                              <IconButton 
                                aria-label="Set as main photo"
                                icon={<FiStar />}
                                size="sm"
                                colorScheme={mainPhotoIndex === index ? "yellow" : "gray"}
                                variant={mainPhotoIndex === index ? "solid" : "outline"}
                                position="absolute"
                                bottom="2"
                                right="2"
                                onClick={() => handleSetMainPhoto(index)}
                                zIndex="2"
                              />
                            </Box>
                          </GridItem>
                        ))}
                      </Grid>
                    )}
                  </VStack>
                </Stack>
              </VStack>
              
              <VStack 
                spacing={{ base: '0', md: '4' }} 
                align={{ base: 'start', md: 'stretch' }}
                p={{ base: '4', md: '6' }}
                borderBottomWidth={{ base: '1px', md: '2px' }}
                borderColor={borderColor}
              >
                <Heading size="md" fontWeight="semibold" mb={{ base: '2', md: '4' }}>
                  Room Types
                </Heading>
                
                <Stack spacing={{ base: '4', md: '5' }} width="full">
                  <Text fontSize="sm" color="gray.600">
                    Add different room types available at your hotel. At least one room type is required.
                  </Text>
                  
                  {/* Room type form */}
                  <Card variant="outline" bg={useColorModeValue('gray.50', 'gray.900')}>
                    <CardBody>
                      <VStack spacing={4} align="stretch">
                        <FormControl isRequired>
                          <FormLabel>Room Type Name</FormLabel>
                          <Input 
                            placeholder="e.g., Standard Double, Deluxe Suite"
                            value={roomTypeForm.name}
                            onChange={(e) => setRoomTypeForm({...roomTypeForm, name: e.target.value})}
                          />
                        </FormControl>
                        
                        <FormControl>
                          <FormLabel>Description</FormLabel>
                          <Textarea 
                            placeholder="Describe this room type"
                            value={roomTypeForm.description}
                            onChange={(e) => setRoomTypeForm({...roomTypeForm, description: e.target.value})}
                            size="sm"
                            resize="vertical"
                          />
                        </FormControl>
                        
                        <SimpleGrid columns={{ base: 1, md: 3 }} spacing={4}>
                          <FormControl isRequired>
                            <FormLabel>Area (m²)</FormLabel>
                            <NumberInput 
                              min={1} 
                              value={roomTypeForm.area} 
                              onChange={(value) => setRoomTypeForm({...roomTypeForm, area: parseFloat(value)})}
                            >
                              <NumberInputField />
                              <NumberInputStepper>
                                <NumberIncrementStepper />
                                <NumberDecrementStepper />
                              </NumberInputStepper>
                            </NumberInput>
                          </FormControl>
                          
                          <FormControl isRequired>
                            <FormLabel>Capacity (Guests)</FormLabel>
                            <NumberInput 
                              min={1} 
                              max={10} 
                              value={roomTypeForm.capacity} 
                              onChange={(value) => setRoomTypeForm({...roomTypeForm, capacity: parseInt(value)})}
                            >
                              <NumberInputField />
                              <NumberInputStepper>
                                <NumberIncrementStepper />
                                <NumberDecrementStepper />
                              </NumberInputStepper>
                            </NumberInput>
                          </FormControl>
                          
                          <FormControl isRequired>
                            <FormLabel>Base Price</FormLabel>
                            <NumberInput 
                              min={0} 
                              value={roomTypeForm.basePrice} 
                              onChange={(value) => setRoomTypeForm({...roomTypeForm, basePrice: parseFloat(value)})}
                            >
                              <NumberInputField />
                              <NumberInputStepper>
                                <NumberIncrementStepper />
                                <NumberDecrementStepper />
                              </NumberInputStepper>
                            </NumberInput>
                          </FormControl>
                        </SimpleGrid>
                        
                        <FormControl>
                          <FormLabel>Floor (Optional)</FormLabel>
                          <NumberInput 
                            min={0} 
                            value={roomTypeForm.floor} 
                            onChange={(value) => setRoomTypeForm({...roomTypeForm, floor: parseInt(value)})}
                          >
                            <NumberInputField />
                            <NumberInputStepper>
                              <NumberIncrementStepper />
                              <NumberDecrementStepper />
                            </NumberInputStepper>
                          </NumberInput>
                        </FormControl>
                        
                        <Button 
                          onClick={addRoomType} 
                          leftIcon={<FiPlusCircle />} 
                          colorScheme="blue" 
                          alignSelf="flex-end"
                        >
                          Add Room Type
                        </Button>
                      </VStack>
                    </CardBody>
                  </Card>
                  
                  {/* Display added room types */}
                  {hotelData.roomTypes.length > 0 && (
                    <Box mt={4}>
                      <Heading size="sm" mb={2}>Added Room Types</Heading>
                      
                      <VStack spacing={3} align="stretch">
                        {hotelData.roomTypes.map((roomType, index) => (
                          <Card key={index} variant="outline">
                            <CardBody>
                              <Grid templateColumns="1fr auto" gap={4}>
                                <Box>
                                  <HStack mb={1}>
                                    <Heading size="sm">{roomType.name}</Heading>
                                    <Badge colorScheme="green">${roomType.basePrice}</Badge>
                                    <Badge colorScheme="blue">{roomType.capacity} Guests</Badge>
                                    <Badge colorScheme="purple">{roomType.area} m²</Badge>
                                  </HStack>
                                  
                                  <Text fontSize="sm" color="gray.600" noOfLines={2}>
                                    {roomType.description || "No description provided"}
                                  </Text>
                                </Box>
                                
                                <HStack>
                                  <IconButton
                                    icon={<FiTrash2 />}
                                    colorScheme="red"
                                    variant="ghost"
                                    size="sm"
                                    aria-label="Remove room type"
                                    onClick={() => removeRoomType(index)}
                                  />
                                </HStack>
                              </Grid>
                            </CardBody>
                          </Card>
                        ))}
                      </VStack>
                    </Box>
                  )}
                </Stack>
              </VStack>
              
              <VStack 
                spacing={{ base: '0', md: '4' }} 
                align={{ base: 'start', md: 'stretch' }}
                p={{ base: '4', md: '6' }}
              >
                <Button 
                  type="submit" 
                  colorScheme="blue" 
                  size="lg" 
                  width="full"
                  isLoading={isLoading}
                  leftIcon={<FiSave />}
                >
                  Save Hotel
                </Button>
                
                <Button 
                  onClick={() => navigate('/dashboard')} 
                  colorScheme="gray" 
                  size="lg" 
                  width="full"
                  leftIcon={<FiArrowLeft />}
                >
                  Cancel
                </Button>
              </VStack>
            </Stack>
          </Box>
        </Stack>
      </Stack>
    </Container>
  );
}

export default CreateHotel;