import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box, 
  Button,
  Container,
  Flex,
  Grid,
  GridItem,
  Heading,
  Text,
  SimpleGrid,
  VStack,
  HStack,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  FormControl,
  FormLabel,
  Input,
  Textarea,
  NumberInput,
  NumberInputField,
  NumberInputStepper,
  NumberIncrementStepper,
  NumberDecrementStepper,
  Select,
  Switch,
  Badge,
  Card,
  CardBody,
  CardHeader,
  CardFooter,
  Image,
  IconButton,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalFooter,
  ModalBody,
  ModalCloseButton,
  useDisclosure,
  useToast,
  Spinner,
  Divider,
  AlertDialog,
  AlertDialogBody,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogContent,
  AlertDialogOverlay,
  useColorModeValue,
  InputGroup,
  InputLeftAddon,
  Progress,
  Tag,
  Tooltip,
  Skeleton,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
} from '@chakra-ui/react';
import {
  FiArrowLeft,
  FiSave,
  FiPlus,
  FiEdit,
  FiTrash2,
  FiStar,
  FiImage,
  FiUpload,
  FiRefreshCw,
  FiX,
  FiCheck,
  FiInfo,
  FiAlertCircle,
  FiChevronRight,
  FiHome
} from 'react-icons/fi';
import apiClient, { authService } from '../services/api';

function ManageHotels() {
  const navigate = useNavigate();
  const toast = useToast();
  const { hotelId } = useParams(); // Optional: can be used to directly open a specific hotel
  
  // State for hotels list and selected hotel
  const [hotels, setHotels] = useState([]);
  const [selectedHotel, setSelectedHotel] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);
  
  // State for hotel form
  const [hotelForm, setHotelForm] = useState({
    id: 0,
    name: '',
    description: '',
    location: '',
    address: '',
    rating: 0,
    basePrice: 0,
    amenities: []
  });
  
  // State for room types
  const [roomTypes, setRoomTypes] = useState([]);
  const [selectedRoomType, setSelectedRoomType] = useState(null);
  const [roomTypeForm, setRoomTypeForm] = useState({
    id: 0,
    name: '',
    description: '',
    area: 25,
    capacity: 2,
    basePrice: 0,
    floor: 1,
    hotelId: 0
  });
  
  // State for photos
  const [photos, setPhotos] = useState([]);
  const [uploadedFiles, setUploadedFiles] = useState([]);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  
  // Refs
  const fileInputRef = useRef(null);
  const cancelRef = useRef();
  
  // Modals
  const { 
    isOpen: isHotelModalOpen, 
    onOpen: onHotelModalOpen, 
    onClose: onHotelModalClose 
  } = useDisclosure();
  
  const { 
    isOpen: isRoomTypeModalOpen, 
    onOpen: onRoomTypeModalOpen, 
    onClose: onRoomTypeModalClose 
  } = useDisclosure();
  
  const { 
    isOpen: isDeleteAlertOpen, 
    onOpen: onDeleteAlertOpen, 
    onClose: onDeleteAlertClose 
  } = useDisclosure();
  
  const { 
    isOpen: isPhotoModalOpen, 
    onOpen: onPhotoModalOpen, 
    onClose: onPhotoModalClose 
  } = useDisclosure();
  
  // Colors
  const cardBg = useColorModeValue('white', 'gray.700');
  const borderColor = useColorModeValue('gray.200', 'gray.600');
  
  // Check auth and load hotels
  useEffect(() => {
    const checkAuthAndLoadData = async () => {
      setIsLoading(true);
      
      try {
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
        
        // Check if user is manager or admin
        try {
          const userResponse = await apiClient.get('/auth/current');
          const userRole = userResponse.data.role || '';
          
          if (!(userRole === 'Manager' || userRole === 'Admin')) {
            throw new Error('Insufficient permissions');
          }
        } catch (error) {
          console.error("Access check failed:", error);
          toast({
            title: 'Access Denied',
            description: 'You need manager permissions to manage hotels.',
            status: 'error',
            duration: 5000,
            isClosable: true,
          });
          navigate('/dashboard');
          return;
        }
        
        // Load hotels
        const hotelsResponse = await apiClient.get('/Hotels');
        
        // Handle different response formats
        let hotelsData = [];
        if (hotelsResponse && hotelsResponse.data) {
          // Handle case: response.data.$values array (JSON.NET format)
          if (hotelsResponse.data.$values && Array.isArray(hotelsResponse.data.$values)) {
            hotelsData = hotelsResponse.data.$values;
          }
          // Handle case: response.data is directly an array
          else if (Array.isArray(hotelsResponse.data)) {
            hotelsData = hotelsResponse.data;
          } 
          // Handle other cases
          else if (hotelsResponse.data && typeof hotelsResponse.data === 'object' && hotelsResponse.data.id) {
            hotelsData = [hotelsResponse.data];
          }
        }
        
        setHotels(hotelsData);
        
        // If hotelId is provided in URL, select that hotel
        if (hotelId) {
          const selectedHotel = hotelsData.find(h => h.id === parseInt(hotelId));
          if (selectedHotel) {
            handleSelectHotel(selectedHotel);
          }
        }
        
        setError(null);
      } catch (err) {
        console.error('Error loading hotels:', err);
        setError('Failed to load hotels. Please try again.');
      } finally {
        setIsLoading(false);
      }
    };
    
    checkAuthAndLoadData();
  }, [navigate, toast, hotelId]);
  
  // Function to select a hotel for editing
  const handleSelectHotel = async (hotel) => {
    setSelectedHotel(hotel);
    setHotelForm({
      id: hotel.id,
      name: hotel.name,
      description: hotel.description || '',
      location: hotel.location || '',
      address: hotel.address || '',
      rating: hotel.rating || 0,
      basePrice: hotel.basePrice || 0,
      amenities: Array.isArray(hotel.amenities) ? hotel.amenities : 
                (typeof hotel.amenities === 'string' ? hotel.amenities.split(',').map(a => a.trim()) : [])
    });
    
    // Load room types for this hotel
    try {
      const roomTypesResponse = await apiClient.get(`/room-types/by-hotel/${hotel.id}`);
      
      // Handle different response formats
      let roomTypesData = [];
      if (roomTypesResponse && roomTypesResponse.data) {
        if (roomTypesResponse.data.$values && Array.isArray(roomTypesResponse.data.$values)) {
          roomTypesData = roomTypesResponse.data.$values;
        } else if (Array.isArray(roomTypesResponse.data)) {
          roomTypesData = roomTypesResponse.data;
        }
      }
      
      setRoomTypes(roomTypesData);
    } catch (error) {
      console.error('Error loading room types:', error);
      toast({
        title: 'Error',
        description: 'Failed to load room types for this hotel.',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
    }
    
    // Load photos for this hotel
    try {
      const photosResponse = await apiClient.get(`/HotelPhotos/hotel/${hotel.id}`);
      
      // Handle different response formats
      let photosData = [];
      if (photosResponse && photosResponse.data) {
        if (photosResponse.data.$values && Array.isArray(photosResponse.data.$values)) {
          photosData = photosResponse.data.$values;
        } else if (Array.isArray(photosResponse.data)) {
          photosData = photosResponse.data;
        }
      }
      
      setPhotos(photosData);
    } catch (error) {
      console.error('Error loading photos:', error);
      toast({
        title: 'Error',
        description: 'Failed to load photos for this hotel.',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
    }
  };
  
  // Function to handle hotel form changes
  const handleHotelFormChange = (e) => {
    const { name, value } = e.target;
    setHotelForm({
      ...hotelForm,
      [name]: value
    });
  };
  
  // Function to handle numeric input changes for hotel form
  const handleHotelNumericChange = (name, value) => {
    setHotelForm({
      ...hotelForm,
      [name]: value
    });
  };
  
  // Function to handle amenities input
  const handleAmenitiesChange = (e) => {
    const amenitiesText = e.target.value;
    const amenitiesArray = amenitiesText.split(',')
      .map(item => item.trim())
      .filter(item => item.length > 0);
    
    setHotelForm({
      ...hotelForm,
      amenities: amenitiesArray
    });
  };
  
  // Function to save hotel changes
  const handleSaveHotel = async () => {
    setIsSubmitting(true);
    
    try {
      const payload = {
        ...hotelForm,
        rating: parseFloat(hotelForm.rating),
        basePrice: parseFloat(hotelForm.basePrice)
      };
      
      await apiClient.put(`/Hotels/${hotelForm.id}`, payload);
      
      // Update the hotel in the list
      setHotels(hotels.map(h => 
        h.id === hotelForm.id ? { ...h, ...payload } : h
      ));
      
      // Update the selected hotel
      setSelectedHotel({ ...selectedHotel, ...payload });
      
      toast({
        title: 'Success',
        description: 'Hotel details saved successfully.',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
    } catch (error) {
      console.error('Error saving hotel:', error);
      toast({
        title: 'Error',
        description: 'Failed to save hotel details.',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setIsSubmitting(false);
    }
  };
  
  // Room Type Functions
  
  // Function to open the room type modal for creating/editing
  const handleOpenRoomTypeModal = (roomType = null) => {
    if (roomType) {
      setSelectedRoomType(roomType);
      setRoomTypeForm({
        id: roomType.id,
        name: roomType.name,
        description: roomType.description || '',
        area: roomType.area,
        capacity: roomType.capacity,
        basePrice: roomType.basePrice,
        floor: roomType.floor || 1,
        hotelId: selectedHotel.id
      });
    } else {
      setSelectedRoomType(null);
      setRoomTypeForm({
        id: 0,
        name: '',
        description: '',
        area: 25,
        capacity: 2,
        basePrice: 0,
        floor: 1,
        hotelId: selectedHotel.id
      });
    }
    
    onRoomTypeModalOpen();
  };
  
  // Function to handle room type form changes
  const handleRoomTypeFormChange = (e) => {
    const { name, value } = e.target;
    setRoomTypeForm({
      ...roomTypeForm,
      [name]: value
    });
  };
  
  // Function to handle numeric input changes for room type form
  const handleRoomTypeNumericChange = (name, value) => {
    setRoomTypeForm({
      ...roomTypeForm,
      [name]: value
    });
  };
  
  // Function to save room type
  const handleSaveRoomType = async () => {
    setIsSubmitting(true);
    
    try {
      const payload = {
        ...roomTypeForm,
        area: parseFloat(roomTypeForm.area),
        capacity: parseInt(roomTypeForm.capacity),
        basePrice: parseFloat(roomTypeForm.basePrice),
        floor: parseInt(roomTypeForm.floor),
        hotelId: selectedHotel.id
      };
      
      let response;
      
      if (selectedRoomType) {
        // Update existing room type
        response = await apiClient.put(`/room-types/${roomTypeForm.id}`, payload);
        
        // Update room types list
        setRoomTypes(roomTypes.map(rt => 
          rt.id === roomTypeForm.id ? { ...rt, ...payload } : rt
        ));
        
        toast({
          title: 'Success',
          description: 'Room type updated successfully.',
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
      } else {
        // Create new room type
        response = await apiClient.post('/room-types', payload);
        
        // Add to room types list
        setRoomTypes([...roomTypes, response.data]);
        
        toast({
          title: 'Success',
          description: 'Room type created successfully.',
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
      }
      
      onRoomTypeModalClose();
    } catch (error) {
      console.error('Error saving room type:', error);
      toast({
        title: 'Error',
        description: 'Failed to save room type.',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setIsSubmitting(false);
    }
  };
  
  // Function to confirm deletion of room type
  const handleDeleteRoomType = (roomType) => {
    setSelectedRoomType(roomType);
    onDeleteAlertOpen();
  };
  
  // Function to actually delete room type
  const confirmDeleteRoomType = async () => {
    try {
      await apiClient.delete(`/room-types/${selectedRoomType.id}`);
      
      // Remove from room types list
      setRoomTypes(roomTypes.filter(rt => rt.id !== selectedRoomType.id));
      
      toast({
        title: 'Success',
        description: 'Room type deleted successfully.',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
    } catch (error) {
      console.error('Error deleting room type:', error);
      toast({
        title: 'Error',
        description: 'Failed to delete room type.',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
    } finally {
      onDeleteAlertClose();
    }
  };
  
  // Photo Functions
  
  // Function to handle file selection
  const handleFileSelection = (e) => {
    if (e.target.files && e.target.files.length > 0) {
      const files = Array.from(e.target.files);
      setUploadedFiles(files);
    }
  };
  
  // Function to upload photos
  const handleUploadPhotos = async () => {
    if (uploadedFiles.length === 0) {
      toast({
        title: 'No files selected',
        description: 'Please select files to upload.',
        status: 'warning',
        duration: 3000,
        isClosable: true,
      });
      return;
    }
    
    setIsUploading(true);
    setUploadProgress(0);
    
    try {
      const formData = new FormData();
      
      uploadedFiles.forEach(file => {
        formData.append('files', file);
      });
      
      // Upload photos with progress tracking
      const response = await apiClient.post(
        `/HotelPhotos/upload/multiple?hotelId=${selectedHotel.id}`, 
        formData, 
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
          onUploadProgress: (progressEvent) => {
            const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
            setUploadProgress(percentCompleted);
          }
        }
      );
      
      // Add new photos to the list
      let newPhotos = [];
      if (response.data.$values && Array.isArray(response.data.$values)) {
        newPhotos = response.data.$values;
      } else if (Array.isArray(response.data)) {
        newPhotos = response.data;
      } else if (response.data && typeof response.data === 'object') {
        newPhotos = [response.data];
      }
      
      setPhotos([...photos, ...newPhotos]);
      setUploadedFiles([]);
      
      toast({
        title: 'Success',
        description: `${newPhotos.length} photos uploaded successfully.`,
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
      
      onPhotoModalClose();
    } catch (error) {
      console.error('Error uploading photos:', error);
      toast({
        title: 'Error',
        description: 'Failed to upload photos.',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setIsUploading(false);
    }
  };
  
  // Function to set a photo as main
  const handleSetMainPhoto = async (photo) => {
    try {
      await apiClient.put(`/HotelPhotos/${photo.id}/set-main?hotelId=${selectedHotel.id}`);
      
      // Update the photos list to reflect the change
      const updatedPhotos = photos.map(p => ({
        ...p,
        isMain: p.id === photo.id
      }));
      
      setPhotos(updatedPhotos);
      
      toast({
        title: 'Success',
        description: 'Main photo updated successfully.',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
    } catch (error) {
      console.error('Error setting main photo:', error);
      toast({
        title: 'Error',
        description: 'Failed to set main photo.',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
    }
  };
  
  // Function to delete a photo
  const handleDeletePhoto = async (photo) => {
    try {
      await apiClient.delete(`/HotelPhotos/${photo.id}`);
      
      // Remove from photos list
      setPhotos(photos.filter(p => p.id !== photo.id));
      
      toast({
        title: 'Success',
        description: 'Photo deleted successfully.',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
    } catch (error) {
      console.error('Error deleting photo:', error);
      toast({
        title: 'Error',
        description: 'Failed to delete photo.',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
    }
  };
  
  // Loading state
  if (isLoading) {
    return (
      <Container maxW="container.xl" py={8}>
        <Flex direction="column" justify="center" align="center" minH="60vh">
          <Spinner size="xl" thickness="4px" speed="0.65s" color="blue.500" mb={4} />
          <Heading size="md">Loading hotels...</Heading>
        </Flex>
      </Container>
    );
  }
  
  // Error state
  if (error) {
    return (
      <Container maxW="container.xl" py={8}>
        <Flex direction="column" justify="center" align="center" minH="60vh">
          <FiAlertCircle size="48px" color="red" />
          <Heading size="md" mt={4} mb={2}>{error}</Heading>
          <Button 
            leftIcon={<FiRefreshCw />}
            colorScheme="blue"
            onClick={() => window.location.reload()}
          >
            Try Again
          </Button>
        </Flex>
      </Container>
    );
  }
  
  return (
    <Container maxW="container.xl" py={8}>
      <Breadcrumb mb={6} separator={<FiChevronRight />}>
        <BreadcrumbItem>
          <BreadcrumbLink onClick={() => navigate('/dashboard')}>
            <Flex align="center">
              <FiHome /><Text ml={1}>Dashboard</Text>
            </Flex>
          </BreadcrumbLink>
        </BreadcrumbItem>
        <BreadcrumbItem isCurrentPage>
          <BreadcrumbLink>Manage Hotels</BreadcrumbLink>
        </BreadcrumbItem>
      </Breadcrumb>
      
      <Heading mb={6}>Hotel Management</Heading>
      
      {/* Hotel Selection */}
      {!selectedHotel ? (
        <Box>
          <Heading size="md" mb={4}>Select a Hotel to Manage</Heading>
          <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} spacing={6}>
            {hotels.map(hotel => (
              <Card 
                key={hotel.id} 
                variant="outline" 
                cursor="pointer" 
                _hover={{ shadow: 'md' }}
                onClick={() => handleSelectHotel(hotel)}
              >
                <CardBody>
                  <Heading size="md" mb={2}>{hotel.name}</Heading>
                  <Text color="gray.600" noOfLines={2} mb={3}>
                    {hotel.description || 'No description available'}
                  </Text>
                  <HStack>
                    <Badge>{hotel.location}</Badge>
                    <Badge colorScheme="green">{hotel.rating} stars</Badge>
                  </HStack>
                </CardBody>
              </Card>
            ))}
          </SimpleGrid>
          
          {hotels.length === 0 && (
            <Box textAlign="center" py={10}>
              <FiInfo size="48px" style={{ margin: '0 auto 16px' }} />
              <Heading size="md" mb={2}>No Hotels Found</Heading>
              <Text mb={4}>You don't have any hotels to manage yet.</Text>
              <Button 
                colorScheme="blue" 
                leftIcon={<FiPlus />}
                onClick={() => navigate('/hotels/create')}
              >
                Create New Hotel
              </Button>
            </Box>
          )}
        </Box>
      ) : (
        <Box>
          <HStack mb={6}>
            <Button 
              leftIcon={<FiArrowLeft />} 
              onClick={() => setSelectedHotel(null)}
              variant="outline"
            >
              Back to Hotels
            </Button>
            <Heading size="md">{selectedHotel.name}</Heading>
          </HStack>
          
          <Tabs colorScheme="blue" variant="enclosed">
            <TabList>
              <Tab>Hotel Details</Tab>
              <Tab>Room Types</Tab>
              <Tab>Photos</Tab>
            </TabList>
            
            <TabPanels>
              {/* Hotel Details Tab */}
              <TabPanel>
                <Card variant="outline" bg={cardBg}>
                  <CardHeader>
                    <Heading size="md">Edit Hotel Details</Heading>
                  </CardHeader>
                  <CardBody>
                    <VStack spacing={4} align="flex-start">
                      <FormControl isRequired>
                        <FormLabel>Hotel Name</FormLabel>
                        <Input 
                          name="name" 
                          value={hotelForm.name} 
                          onChange={handleHotelFormChange} 
                          placeholder="Enter hotel name"
                        />
                      </FormControl>
                      
                      <FormControl>
                        <FormLabel>Description</FormLabel>
                        <Textarea 
                          name="description" 
                          value={hotelForm.description} 
                          onChange={handleHotelFormChange} 
                          placeholder="Enter hotel description"
                          rows={4}
                        />
                      </FormControl>
                      
                      <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4} width="full">
                        <FormControl isRequired>
                          <FormLabel>Location</FormLabel>
                          <Input 
                            name="location" 
                            value={hotelForm.location} 
                            onChange={handleHotelFormChange} 
                            placeholder="Enter hotel location"
                          />
                        </FormControl>
                        
                        <FormControl isRequired>
                          <FormLabel>Address</FormLabel>
                          <Input 
                            name="address" 
                            value={hotelForm.address} 
                            onChange={handleHotelFormChange} 
                            placeholder="Enter hotel address"
                          />
                        </FormControl>
                      </SimpleGrid>
                      
                      <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4} width="full">
                        <FormControl isRequired>
                          <FormLabel>Rating</FormLabel>
                          <NumberInput 
                            value={hotelForm.rating} 
                            onChange={(value) => handleHotelNumericChange('rating', value)} 
                            min={0} 
                            max={5} 
                            step={0.1}
                            precision={1}
                          >
                            <NumberInputField placeholder="Enter hotel rating" />
                            <NumberInputStepper>
                              <NumberIncrementStepper />
                              <NumberDecrementStepper />
                            </NumberInputStepper>
                          </NumberInput>
                        </FormControl>
                        
                        <FormControl isRequired>
                          <FormLabel>Base Price</FormLabel>
                          <NumberInput 
                            value={hotelForm.basePrice} 
                            onChange={(value) => handleHotelNumericChange('basePrice', value)} 
                            min={0} 
                            precision={2}
                          >
                            <NumberInputField placeholder="Enter base price" />
                            <NumberInputStepper>
                              <NumberIncrementStepper />
                              <NumberDecrementStepper />
                            </NumberInputStepper>
                          </NumberInput>
                        </FormControl>
                      </SimpleGrid>
                      
                      <FormControl>
                        <FormLabel>Amenities</FormLabel>
                        <Textarea 
                          placeholder="Enter amenities separated by commas (e.g., WiFi, Pool, Gym)" 
                          value={Array.isArray(hotelForm.amenities) ? hotelForm.amenities.join(', ') : hotelForm.amenities} 
                          onChange={handleAmenitiesChange}
                        />
                      </FormControl>
                    </VStack>
                  </CardBody>
                  <CardFooter>
                    <Button 
                      colorScheme="blue" 
                      leftIcon={<FiSave />} 
                      onClick={handleSaveHotel}
                      isLoading={isSubmitting}
                    >
                      Save Changes
                    </Button>
                  </CardFooter>
                </Card>
              </TabPanel>
              
              {/* Room Types Tab */}
              <TabPanel>
                <Flex justify="space-between" align="center" mb={4}>
                  <Heading size="md">Room Types</Heading>
                  <Button 
                    leftIcon={<FiPlus />} 
                    colorScheme="blue"
                    onClick={() => handleOpenRoomTypeModal()}
                  >
                    Add Room Type
                  </Button>
                </Flex>
                
                {roomTypes.length > 0 ? (
                  <Table variant="simple">
                    <Thead>
                      <Tr>
                        <Th>Name</Th>
                        <Th>Capacity</Th>
                        <Th>Area (m²)</Th>
                        <Th>Base Price</Th>
                        <Th>Floor</Th>
                        <Th>Actions</Th>
                      </Tr>
                    </Thead>
                    <Tbody>
                      {roomTypes.map((roomType) => (
                        <Tr key={roomType.id}>
                          <Td fontWeight="medium">{roomType.name}</Td>
                          <Td>{roomType.capacity} guests</Td>
                          <Td>{roomType.area} m²</Td>
                          <Td>${roomType.basePrice}</Td>
                          <Td>{roomType.floor || 'N/A'}</Td>
                          <Td>
                            <HStack spacing={2}>
                              <IconButton
                                aria-label="Edit room type"
                                icon={<FiEdit />}
                                size="sm"
                                colorScheme="blue"
                                variant="ghost"
                                onClick={() => handleOpenRoomTypeModal(roomType)}
                              />
                              <IconButton
                                aria-label="Delete room type"
                                icon={<FiTrash2 />}
                                size="sm"
                                colorScheme="red"
                                variant="ghost"
                                onClick={() => handleDeleteRoomType(roomType)}
                              />
                            </HStack>
                          </Td>
                        </Tr>
                      ))}
                    </Tbody>
                  </Table>
                ) : (
                  <Box textAlign="center" py={10} borderWidth="1px" borderRadius="lg">
                    <FiInfo size="48px" style={{ margin: '0 auto 16px' }} />
                    <Heading size="md" mb={2}>No Room Types</Heading>
                    <Text mb={4}>This hotel doesn't have any room types yet.</Text>
                    <Button 
                      colorScheme="blue" 
                      leftIcon={<FiPlus />}
                      onClick={() => handleOpenRoomTypeModal()}
                    >
                      Add Room Type
                    </Button>
                  </Box>
                )}
              </TabPanel>
              
              {/* Photos Tab */}
              <TabPanel>
                <Flex justify="space-between" align="center" mb={4}>
                  <Heading size="md">Photos</Heading>
                  <Button 
                    leftIcon={<FiUpload />} 
                    colorScheme="blue"
                    onClick={onPhotoModalOpen}
                  >
                    Upload Photos
                  </Button>
                </Flex>
                
                {photos.length > 0 ? (
                  <SimpleGrid columns={{ base: 1, sm: 2, md: 3, lg: 4 }} spacing={4}>
                    {photos.map((photo) => (
                      <Card key={photo.id} variant="outline" overflow="hidden">
                        <Box position="relative">
                          <Image 
                            src={photo.url || photo.imageUrl} 
                            alt={photo.description || 'Hotel photo'} 
                            objectFit="cover"
                            height="200px"
                            width="100%"
                            fallback={<Skeleton height="200px" width="100%" />}
                          />
                          {photo.isMain && (
                            <Badge 
                              position="absolute" 
                              top="10px" 
                              left="10px"
                              colorScheme="green"
                              fontSize="0.8em"
                            >
                              Main Photo
                            </Badge>
                          )}
                        </Box>
                        <CardFooter pt={2} pb={2}>
                          <HStack spacing={2} width="100%" justify="space-between">
                            <Tooltip label={photo.isMain ? 'This is already the main photo' : 'Set as main photo'}>
                              <IconButton
                                aria-label="Set as main photo"
                                icon={<FiStar />}
                                size="sm"
                                colorScheme={photo.isMain ? "yellow" : "gray"}
                                isDisabled={photo.isMain}
                                onClick={() => handleSetMainPhoto(photo)}
                              />
                            </Tooltip>
                            
                            <IconButton
                              aria-label="Delete photo"
                              icon={<FiTrash2 />}
                              size="sm"
                              colorScheme="red"
                              variant="ghost"
                              onClick={() => handleDeletePhoto(photo)}
                            />
                          </HStack>
                        </CardFooter>
                      </Card>
                    ))}
                  </SimpleGrid>
                ) : (
                  <Box textAlign="center" py={10} borderWidth="1px" borderRadius="lg">
                    <FiImage size="48px" style={{ margin: '0 auto 16px' }} />
                    <Heading size="md" mb={2}>No Photos</Heading>
                    <Text mb={4}>This hotel doesn't have any photos yet.</Text>
                    <Button 
                      colorScheme="blue" 
                      leftIcon={<FiUpload />}
                      onClick={onPhotoModalOpen}
                    >
                      Upload Photos
                    </Button>
                  </Box>
                )}
              </TabPanel>
            </TabPanels>
          </Tabs>
        </Box>
      )}
      
      {/* Room Type Modal */}
      <Modal isOpen={isRoomTypeModalOpen} onClose={onRoomTypeModalClose} size="lg">
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>
            {selectedRoomType ? 'Edit Room Type' : 'Add Room Type'}
          </ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <VStack spacing={4} align="flex-start">
              <FormControl isRequired>
                <FormLabel>Room Type Name</FormLabel>
                <Input 
                  name="name" 
                  value={roomTypeForm.name} 
                  onChange={handleRoomTypeFormChange} 
                  placeholder="e.g., Standard Double, Deluxe Suite"
                />
              </FormControl>
              
              <FormControl>
                <FormLabel>Description</FormLabel>
                <Textarea 
                  name="description" 
                  value={roomTypeForm.description} 
                  onChange={handleRoomTypeFormChange} 
                  placeholder="Describe this room type"
                />
              </FormControl>
              
              <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4} width="full">
                <FormControl isRequired>
                  <FormLabel>Area (m²)</FormLabel>
                  <NumberInput 
                    value={roomTypeForm.area} 
                    onChange={(value) => handleRoomTypeNumericChange('area', value)} 
                    min={1}
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
                    value={roomTypeForm.capacity} 
                    onChange={(value) => handleRoomTypeNumericChange('capacity', value)} 
                    min={1} 
                    max={10}
                  >
                    <NumberInputField />
                    <NumberInputStepper>
                      <NumberIncrementStepper />
                      <NumberDecrementStepper />
                    </NumberInputStepper>
                  </NumberInput>
                </FormControl>
              </SimpleGrid>
              
              <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4} width="full">
                <FormControl isRequired>
                  <FormLabel>Base Price</FormLabel>
                  <NumberInput 
                    value={roomTypeForm.basePrice} 
                    onChange={(value) => handleRoomTypeNumericChange('basePrice', value)} 
                    min={0}
                    precision={2}
                  >
                    <NumberInputField />
                    <NumberInputStepper>
                      <NumberIncrementStepper />
                      <NumberDecrementStepper />
                    </NumberInputStepper>
                  </NumberInput>
                </FormControl>
                
                <FormControl>
                  <FormLabel>Floor</FormLabel>
                  <NumberInput 
                    value={roomTypeForm.floor} 
                    onChange={(value) => handleRoomTypeNumericChange('floor', value)} 
                    min={0}
                  >
                    <NumberInputField />
                    <NumberInputStepper>
                      <NumberIncrementStepper />
                      <NumberDecrementStepper />
                    </NumberInputStepper>
                  </NumberInput>
                </FormControl>
              </SimpleGrid>
            </VStack>
          </ModalBody>
          
          <ModalFooter>
            <Button mr={3} onClick={onRoomTypeModalClose}>
              Cancel
            </Button>
            <Button 
              colorScheme="blue" 
              onClick={handleSaveRoomType}
              isLoading={isSubmitting}
            >
              {selectedRoomType ? 'Save Changes' : 'Add Room Type'}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
      
      {/* Photo Upload Modal */}
      <Modal isOpen={isPhotoModalOpen} onClose={onPhotoModalClose}>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Upload Photos</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <VStack spacing={4} align="stretch">
              <FormControl>
                <FormLabel>Select Photos</FormLabel>
                <Input 
                  type="file" 
                  multiple 
                  accept="image/*" 
                  onChange={handleFileSelection}
                  ref={fileInputRef}
                />
              </FormControl>
              
              {uploadedFiles.length > 0 && (
                <Box>
                  <Text mb={2}>Selected files:</Text>
                  <VStack align="stretch" spacing={2} maxHeight="200px" overflowY="auto">
                    {uploadedFiles.map((file, index) => (
                      <HStack key={index} p={2} borderWidth="1px" borderRadius="md">
                        <FiImage />
                        <Text fontSize="sm" noOfLines={1}>{file.name}</Text>
                        <Text fontSize="xs" color="gray.500">({(file.size / 1024).toFixed(2)} KB)</Text>
                      </HStack>
                    ))}
                  </VStack>
                </Box>
              )}
              
              {isUploading && (
                <Box>
                  <Text mb={2}>Uploading: {uploadProgress}%</Text>
                  <Progress value={uploadProgress} size="sm" colorScheme="blue" />
                </Box>
              )}
            </VStack>
          </ModalBody>
          
          <ModalFooter>
            <Button mr={3} onClick={onPhotoModalClose} isDisabled={isUploading}>
              Cancel
            </Button>
            <Button 
              colorScheme="blue" 
              onClick={handleUploadPhotos}
              isLoading={isUploading}
              leftIcon={<FiUpload />}
            >
              Upload
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
      
      {/* Delete Confirmation Alert Dialog */}
      <AlertDialog
        isOpen={isDeleteAlertOpen}
        leastDestructiveRef={cancelRef}
        onClose={onDeleteAlertClose}
      >
        <AlertDialogOverlay>
          <AlertDialogContent>
            <AlertDialogHeader fontSize="lg" fontWeight="bold">
              Delete Room Type
            </AlertDialogHeader>
            
            <AlertDialogBody>
              Are you sure you want to delete the room type "{selectedRoomType?.name}"? This action cannot be undone.
            </AlertDialogBody>
            
            <AlertDialogFooter>
              <Button ref={cancelRef} onClick={onDeleteAlertClose}>
                Cancel
              </Button>
              <Button colorScheme="red" onClick={confirmDeleteRoomType} ml={3}>
                Delete
              </Button>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialogOverlay>
      </AlertDialog>
    </Container>
  );
}

export default ManageHotels;