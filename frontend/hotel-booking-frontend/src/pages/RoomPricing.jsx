import React, { useState, useEffect, useRef } from 'react';
import {
  Box,
  Container,
  Heading,
  Text,
  Button,
  SimpleGrid,
  FormControl,
  FormLabel,
  Input,
  NumberInput,
  NumberInputField,
  NumberInputStepper,
  NumberIncrementStepper,
  NumberDecrementStepper,
  Checkbox,
  Select,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  IconButton,
  HStack,
  VStack,
  Flex,
  Badge,
  Tooltip,
  useToast,
  useColorModeValue,
  Spinner,
  Alert,
  AlertIcon,
  Divider,
  Card,
  CardBody,
  Grid,
  GridItem
} from '@chakra-ui/react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  FiArrowLeft,
  FiSave,
  FiPlus,
  FiTrash2,
  FiEdit,
  FiCheck,
  FiX,
  FiCalendar,
  FiDollarSign,
  FiInfo
} from 'react-icons/fi';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';
import apiClient from '../services/api';

function RoomPricing() {
  const navigate = useNavigate();
  const toast = useToast();
  const { hotelId } = useParams();
  
  // Define all refs at the top level of the component
  const editNameInputRef = useRef(null);
  const editPriceInputRef = useRef(null);
  
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [hotel, setHotel] = useState(null);
  const [roomTypes, setRoomTypes] = useState([]);
  const [seasonalPrices, setSeasonalPrices] = useState([]);
  const [isEditing, setIsEditing] = useState(null);
  const [editedValues, setEditedValues] = useState({});
  const [newRoomType, setNewRoomType] = useState({
    name: '',
    basePrice: 0,
    capacity: 1,
    description: '',
    amenities: []
  });
  const [newSeasonalPrice, setNewSeasonalPrice] = useState({
    roomTypeId: '',
    startDate: new Date(),
    endDate: new Date(new Date().setDate(new Date().getDate() + 7)),
    price: 0,
    name: 'Special Season'
  });
  const [showAddRoomType, setShowAddRoomType] = useState(false);
  const [showAddSeasonalPrice, setShowAddSeasonalPrice] = useState(false);
  
  const bgColor = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  
  // List of common room amenities
  const amenitiesList = [
    'WiFi',
    'TV',
    'Air Conditioning',
    'Mini Bar',
    'Safe',
    'Balcony',
    'Sea View',
    'Mountain View',
    'Bathtub',
    'Breakfast Included'
  ];
  
  useEffect(() => {
    const fetchData = async () => {
      setIsLoading(true);
      try {
        // Fetch hotel details
        const hotelResponse = await apiClient.get(`/hotels/${hotelId}`);
        setHotel(hotelResponse.data);
        
        // Fetch room types for this hotel
        const roomTypesResponse = await apiClient.get(`/roomTypes/hotel/${hotelId}`);
        setRoomTypes(roomTypesResponse.data);
        
        // Fetch seasonal prices
        const seasonalPricesResponse = await apiClient.get(`/seasonalPrices/hotel/${hotelId}`);
        setSeasonalPrices(seasonalPricesResponse.data);
        
        setError(null);
      } catch (err) {
        console.error('Error fetching data:', err);
        setError('Failed to load pricing data. Please try again.');
        
        // If API endpoints don't exist, use mock data
        if (err.response && err.response.status === 404) {
          // Mock data for development
          setHotel({
            id: 1,
            name: "Grand Hotel",
            location: "New York",
            basePrice: 199
          });
          
          setRoomTypes([
            {
              id: 1,
              name: "Standard Room",
              basePrice: 199,
              capacity: 2,
              description: "Comfortable room with essential amenities.",
              amenities: ["WiFi", "TV", "Air Conditioning"]
            },
            {
              id: 2,
              name: "Deluxe Room",
              basePrice: 299,
              capacity: 2,
              description: "Spacious room with premium amenities and city view.",
              amenities: ["WiFi", "TV", "Air Conditioning", "Mini Bar", "Balcony"]
            },
            {
              id: 3,
              name: "Suite",
              basePrice: 499,
              capacity: 4,
              description: "Luxury suite with separate living area and premium amenities.",
              amenities: ["WiFi", "TV", "Air Conditioning", "Mini Bar", "Balcony", "Sea View", "Bathtub"]
            }
          ]);
          
          setSeasonalPrices([
            {
              id: 1,
              roomTypeId: 1,
              name: "Summer Season",
              startDate: new Date(2025, 5, 1), // June 1
              endDate: new Date(2025, 7, 31),  // August 31
              price: 249
            },
            {
              id: 2,
              roomTypeId: 2,
              name: "Summer Season",
              startDate: new Date(2025, 5, 1),
              endDate: new Date(2025, 7, 31),
              price: 349
            },
            {
              id: 3,
              roomTypeId: 3,
              name: "Summer Season",
              startDate: new Date(2025, 5, 1),
              endDate: new Date(2025, 7, 31),
              price: 599
            },
            {
              id: 4,
              roomTypeId: 1,
              name: "Holiday Season",
              startDate: new Date(2025, 11, 20), // December 20
              endDate: new Date(2026, 0, 5),     // January 5
              price: 299
            }
          ]);
          
          setError(null);
        }
      } finally {
        setIsLoading(false);
      }
    };
    
    fetchData();
  }, [hotelId]);
  
  const handleSaveRoomType = async () => {
    try {
      // Add the hotelId to the new room type
      const roomTypeData = {
        ...newRoomType,
        hotelId: hotelId
      };
      
      const response = await apiClient.post('/roomTypes', roomTypeData);
      
      // Add the new room type to the list
      setRoomTypes([...roomTypes, response.data]);
      
      // Reset form
      setNewRoomType({
        name: '',
        basePrice: 0,
        capacity: 1,
        description: '',
        amenities: []
      });
      
      setShowAddRoomType(false);
      
      toast({
        title: 'Room type added',
        description: 'The room type has been added successfully.',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
    } catch (err) {
      console.error('Error adding room type:', err);
      
      toast({
        title: 'Error',
        description: err.response?.data || 'An error occurred while adding the room type.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
      
      // If API endpoint doesn't exist, just add to local state
      if (err.response && err.response.status === 404) {
        const newId = Math.max(...roomTypes.map(rt => rt.id), 0) + 1;
        const newRoomTypeWithId = {
          ...newRoomType,
          id: newId,
          hotelId: hotelId
        };
        
        setRoomTypes([...roomTypes, newRoomTypeWithId]);
        
        setNewRoomType({
          name: '',
          basePrice: 0,
          capacity: 1,
          description: '',
          amenities: []
        });
        
        setShowAddRoomType(false);
        
        toast({
          title: 'Room type added (local)',
          description: 'The room type has been added to the local state.',
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
      }
    }
  };
  
  const handleSaveSeasonalPrice = async () => {
    try {
      const response = await apiClient.post('/seasonalPrices', newSeasonalPrice);
      
      // Add the new seasonal price to the list
      setSeasonalPrices([...seasonalPrices, response.data]);
      
      // Reset form
      setNewSeasonalPrice({
        roomTypeId: '',
        startDate: new Date(),
        endDate: new Date(new Date().setDate(new Date().getDate() + 7)),
        price: 0,
        name: 'Special Season'
      });
      
      setShowAddSeasonalPrice(false);
      
      toast({
        title: 'Seasonal price added',
        description: 'The seasonal price has been added successfully.',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
    } catch (err) {
      console.error('Error adding seasonal price:', err);
      
      toast({
        title: 'Error',
        description: err.response?.data || 'An error occurred while adding the seasonal price.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
      
      // If API endpoint doesn't exist, just add to local state
      if (err.response && err.response.status === 404) {
        const newId = Math.max(...seasonalPrices.map(sp => sp.id), 0) + 1;
        const newSeasonalPriceWithId = {
          ...newSeasonalPrice,
          id: newId
        };
        
        setSeasonalPrices([...seasonalPrices, newSeasonalPriceWithId]);
        
        setNewSeasonalPrice({
          roomTypeId: '',
          startDate: new Date(),
          endDate: new Date(new Date().setDate(new Date().getDate() + 7)),
          price: 0,
          name: 'Special Season'
        });
        
        setShowAddSeasonalPrice(false);
        
        toast({
          title: 'Seasonal price added (local)',
          description: 'The seasonal price has been added to the local state.',
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
      }
    }
  };
  
  const handleDeleteRoomType = async (id) => {
    if (window.confirm('Are you sure you want to delete this room type? This will also delete all associated seasonal prices.')) {
      try {
        await apiClient.delete(`/roomTypes/${id}`);
        
        // Remove the room type from the list
        setRoomTypes(roomTypes.filter(rt => rt.id !== id));
        
        // Also remove any seasonal prices for this room type
        setSeasonalPrices(seasonalPrices.filter(sp => sp.roomTypeId !== id));
        
        toast({
          title: 'Room type deleted',
          description: 'The room type has been deleted successfully.',
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
      } catch (err) {
        console.error('Error deleting room type:', err);
        
        toast({
          title: 'Error',
          description: err.response?.data || 'An error occurred while deleting the room type.',
          status: 'error',
          duration: 5000,
          isClosable: true,
        });
        
        // If API endpoint doesn't exist, just remove from local state
        if (err.response && err.response.status === 404) {
          setRoomTypes(roomTypes.filter(rt => rt.id !== id));
          setSeasonalPrices(seasonalPrices.filter(sp => sp.roomTypeId !== id));
          
          toast({
            title: 'Room type deleted (local)',
            description: 'The room type has been removed from the local state.',
            status: 'success',
            duration: 3000,
            isClosable: true,
          });
        }
      }
    }
  };
  
  const handleDeleteSeasonalPrice = async (id) => {
    if (window.confirm('Are you sure you want to delete this seasonal price?')) {
      try {
        await apiClient.delete(`/seasonalPrices/${id}`);
        
        // Remove the seasonal price from the list
        setSeasonalPrices(seasonalPrices.filter(sp => sp.id !== id));
        
        toast({
          title: 'Seasonal price deleted',
          description: 'The seasonal price has been deleted successfully.',
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
      } catch (err) {
        console.error('Error deleting seasonal price:', err);
        
        toast({
          title: 'Error',
          description: err.response?.data || 'An error occurred while deleting the seasonal price.',
          status: 'error',
          duration: 5000,
          isClosable: true,
        });
        
        // If API endpoint doesn't exist, just remove from local state
        if (err.response && err.response.status === 404) {
          setSeasonalPrices(seasonalPrices.filter(sp => sp.id !== id));
          
          toast({
            title: 'Seasonal price deleted (local)',
            description: 'The seasonal price has been removed from the local state.',
            status: 'success',
            duration: 3000,
            isClosable: true,
          });
        }
      }
    }
  };
  
  const handleStartEditing = (type, id, initialValues) => {
    setIsEditing({ type, id });
    setEditedValues(initialValues);
    
    // Focus on first input after rendering (using the refs we defined at the top)
    setTimeout(() => {
      if (type === 'roomType') {
        if (editNameInputRef.current) {
          editNameInputRef.current.focus();
        }
      } else if (type === 'seasonalPrice') {
        if (editPriceInputRef.current) {
          editPriceInputRef.current.focus();
        }
      }
    }, 100);
  };
  
  const handleSaveEdit = async () => {
    const { type, id } = isEditing;
    
    try {
      if (type === 'roomType') {
        await apiClient.put(`/roomTypes/${id}`, editedValues);
        
        // Update the room type in the list
        setRoomTypes(roomTypes.map(rt => 
          rt.id === id ? { ...rt, ...editedValues } : rt
        ));
      } else if (type === 'seasonalPrice') {
        await apiClient.put(`/seasonalPrices/${id}`, editedValues);
        
        // Update the seasonal price in the list
        setSeasonalPrices(seasonalPrices.map(sp => 
          sp.id === id ? { ...sp, ...editedValues } : sp
        ));
      }
      
      setIsEditing(null);
      setEditedValues({});
      
      toast({
        title: 'Update successful',
        description: `The ${type === 'roomType' ? 'room type' : 'seasonal price'} has been updated.`,
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
    } catch (err) {
      console.error('Error updating:', err);
      
      toast({
        title: 'Error',
        description: err.response?.data || 'An error occurred while updating.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
      
      // If API endpoint doesn't exist, just update local state
      if (err.response && err.response.status === 404) {
        if (type === 'roomType') {
          setRoomTypes(roomTypes.map(rt => 
            rt.id === id ? { ...rt, ...editedValues } : rt
          ));
        } else if (type === 'seasonalPrice') {
          setSeasonalPrices(seasonalPrices.map(sp => 
            sp.id === id ? { ...sp, ...editedValues } : sp
          ));
        }
        
        setIsEditing(null);
        setEditedValues({});
        
        toast({
          title: 'Update successful (local)',
          description: `The ${type === 'roomType' ? 'room type' : 'seasonal price'} has been updated in the local state.`,
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
      }
    }
  };
  
  const handleCancelEdit = () => {
    setIsEditing(null);
    setEditedValues({});
  };
  
  const handleAmenityChange = (amenity) => {
    const amenities = newRoomType.amenities || [];
    
    if (amenities.includes(amenity)) {
      setNewRoomType({
        ...newRoomType,
        amenities: amenities.filter(a => a !== amenity)
      });
    } else {
      setNewRoomType({
        ...newRoomType,
        amenities: [...amenities, amenity]
      });
    }
  };
  
  const handleEditAmenityChange = (amenity) => {
    const amenities = editedValues.amenities || [];
    
    if (amenities.includes(amenity)) {
      setEditedValues({
        ...editedValues,
        amenities: amenities.filter(a => a !== amenity)
      });
    } else {
      setEditedValues({
        ...editedValues,
        amenities: [...amenities, amenity]
      });
    }
  };
  
  if (isLoading) {
    return (
      <Flex justify="center" align="center" height="100vh">
        <Spinner size="xl" thickness="4px" speed="0.65s" color="brand.500" />
      </Flex>
    );
  }
  
  if (error || !hotel) {
    return (
      <Container maxW="container.xl" py={8}>
        <Button
          leftIcon={<FiArrowLeft />}
          variant="ghost"
          mb={4}
          onClick={() => navigate('/dashboard')}
        >
          Back to Dashboard
        </Button>
        
        <Alert status="error" variant="subtle" borderRadius="md">
          <AlertIcon />
          {error || 'Could not load hotel data.'}
        </Alert>
      </Container>
    );
  }
  
  return (
    <Container maxW="container.xl" py={8}>
      <Button
        leftIcon={<FiArrowLeft />}
        variant="ghost"
        mb={4}
        onClick={() => navigate(`/hotels/${hotelId}`)}
      >
        Back to Hotel
      </Button>
      
      <Heading as="h1" size="xl" mb={2}>
        Room Pricing - {hotel.name}
      </Heading>
      <Text color="gray.600" mb={6}>
        Manage your room types and seasonal pricing
      </Text>
      
      <SimpleGrid columns={{ base: 1, lg: 2 }} spacing={8}>
        {/* Room Types Section */}
        <Box>
          <Flex justify="space-between" align="center" mb={4}>
            <Heading size="md">Room Types</Heading>
            <Button 
              leftIcon={<FiPlus />} 
              colorScheme="brand" 
              size="sm"
              onClick={() => setShowAddRoomType(!showAddRoomType)}
            >
              Add Room Type
            </Button>
          </Flex>
          
          {showAddRoomType && (
            <Card mb={6} variant="outline" bg={bgColor}>
              <CardBody>
                <VStack spacing={4} align="stretch">
                  <FormControl isRequired>
                    <FormLabel>Room Type Name</FormLabel>
                    <Input 
                      value={newRoomType.name}
                      onChange={(e) => setNewRoomType({...newRoomType, name: e.target.value})}
                      placeholder="e.g. Standard Room, Deluxe Suite"
                    />
                  </FormControl>
                  
                  <FormControl isRequired>
                    <FormLabel>Base Price Per Night</FormLabel>
                    <NumberInput 
                      min={0} 
                      precision={2}
                      value={newRoomType.basePrice}
                      onChange={(value) => setNewRoomType({...newRoomType, basePrice: parseFloat(value)})}
                    >
                      <NumberInputField />
                      <NumberInputStepper>
                        <NumberIncrementStepper />
                        <NumberDecrementStepper />
                      </NumberInputStepper>
                    </NumberInput>
                  </FormControl>
                  
                  <FormControl isRequired>
                    <FormLabel>Capacity (Max Guests)</FormLabel>
                    <NumberInput 
                      min={1} 
                      max={10}
                      value={newRoomType.capacity}
                      onChange={(value) => setNewRoomType({...newRoomType, capacity: parseInt(value)})}
                    >
                      <NumberInputField />
                      <NumberInputStepper>
                        <NumberIncrementStepper />
                        <NumberDecrementStepper />
                      </NumberInputStepper>
                    </NumberInput>
                  </FormControl>
                  
                  <FormControl>
                    <FormLabel>Description</FormLabel>
                    <Input
                      value={newRoomType.description}
                      onChange={(e) => setNewRoomType({...newRoomType, description: e.target.value})}
                      placeholder="Brief description of the room"
                    />
                  </FormControl>
                  
                  <FormControl>
                    <FormLabel>Amenities</FormLabel>
                    <SimpleGrid columns={2} spacing={2}>
                      {amenitiesList.map(amenity => (
                        <Checkbox 
                          key={amenity}
                          isChecked={newRoomType.amenities?.includes(amenity)}
                          onChange={() => handleAmenityChange(amenity)}
                        >
                          {amenity}
                        </Checkbox>
                      ))}
                    </SimpleGrid>
                  </FormControl>
                  
                  <HStack spacing={3} justify="flex-end">
                    <Button variant="ghost" onClick={() => setShowAddRoomType(false)}>
                      Cancel
                    </Button>
                    <Button 
                      colorScheme="brand" 
                      leftIcon={<FiSave />}
                      onClick={handleSaveRoomType}
                    >
                      Save Room Type
                    </Button>
                  </HStack>
                </VStack>
              </CardBody>
            </Card>
          )}
          
          {roomTypes.length > 0 ? (
            <VStack spacing={4} align="stretch">
              {roomTypes.map(roomType => (
                <Card key={roomType.id} variant="outline" bg={bgColor}>
                  <CardBody>
                    {isEditing && isEditing.type === 'roomType' && isEditing.id === roomType.id ? (
                      <VStack spacing={4} align="stretch">
                        <FormControl isRequired>
                          <FormLabel>Room Type Name</FormLabel>
                          <Input 
                            ref={editNameInputRef}
                            value={editedValues.name || ''}
                            onChange={(e) => setEditedValues({...editedValues, name: e.target.value})}
                          />
                        </FormControl>
                        
                        <FormControl isRequired>
                          <FormLabel>Base Price Per Night</FormLabel>
                          <NumberInput 
                            min={0} 
                            precision={2}
                            value={editedValues.basePrice || 0}
                            onChange={(value) => setEditedValues({...editedValues, basePrice: parseFloat(value)})}
                          >
                            <NumberInputField />
                            <NumberInputStepper>
                              <NumberIncrementStepper />
                              <NumberDecrementStepper />
                            </NumberInputStepper>
                          </NumberInput>
                        </FormControl>
                        
                        <FormControl isRequired>
                          <FormLabel>Capacity (Max Guests)</FormLabel>
                          <NumberInput 
                            min={1} 
                            max={10}
                            value={editedValues.capacity || 1}
                            onChange={(value) => setEditedValues({...editedValues, capacity: parseInt(value)})}
                          >
                            <NumberInputField />
                            <NumberInputStepper>
                              <NumberIncrementStepper />
                              <NumberDecrementStepper />
                            </NumberInputStepper>
                          </NumberInput>
                        </FormControl>
                        
                        <FormControl>
                          <FormLabel>Description</FormLabel>
                          <Input
                            value={editedValues.description || ''}
                            onChange={(e) => setEditedValues({...editedValues, description: e.target.value})}
                          />
                        </FormControl>
                        
                        <FormControl>
                          <FormLabel>Amenities</FormLabel>
                          <SimpleGrid columns={2} spacing={2}>
                            {amenitiesList.map(amenity => (
                              <Checkbox 
                                key={amenity}
                                isChecked={editedValues.amenities?.includes(amenity)}
                                onChange={() => handleEditAmenityChange(amenity)}
                              >
                                {amenity}
                              </Checkbox>
                            ))}
                          </SimpleGrid>
                        </FormControl>
                        
                        <HStack spacing={3} justify="flex-end">
                          <Button leftIcon={<FiX />} onClick={handleCancelEdit}>
                            Cancel
                          </Button>
                          <Button 
                            colorScheme="brand" 
                            leftIcon={<FiCheck />}
                            onClick={handleSaveEdit}
                          >
                            Save Changes
                          </Button>
                        </HStack>
                      </VStack>
                    ) : (
                      <>
                        <Flex justify="space-between" align="flex-start">
                          <Box>
                            <Heading size="md">{roomType.name}</Heading>
                            <HStack mt={1}>
                              <Badge colorScheme="green">
                                ${roomType.basePrice.toFixed(2)}/night
                              </Badge>
                              <Badge colorScheme="blue">
                                Max {roomType.capacity} guests
                              </Badge>
                            </HStack>
                          </Box>
                          <HStack>
                            <IconButton
                              icon={<FiEdit />}
                              aria-label="Edit room type"
                              variant="ghost"
                              onClick={() => handleStartEditing('roomType', roomType.id, { ...roomType })}
                            />
                            <IconButton
                              icon={<FiTrash2 />}
                              aria-label="Delete room type"
                              variant="ghost"
                              colorScheme="red"
                              onClick={() => handleDeleteRoomType(roomType.id)}
                            />
                          </HStack>
                        </Flex>
                        
                        {roomType.description && (
                          <Text mt={2} color="gray.600">
                            {roomType.description}
                          </Text>
                        )}
                        
                        {roomType.amenities && roomType.amenities.length > 0 && (
                          <Box mt={3}>
                            <Text fontWeight="medium" mb={1}>Amenities:</Text>
                            <Flex wrap="wrap" gap={2}>
                              {roomType.amenities.map(amenity => (
                                <Badge key={amenity} colorScheme="purple" variant="subtle">
                                  {amenity}
                                </Badge>
                              ))}
                            </Flex>
                          </Box>
                        )}
                      </>
                    )}
                  </CardBody>
                </Card>
              ))}
            </VStack>
          ) : (
            <Alert status="info">
              <AlertIcon />
              No room types defined yet. Add your first room type to get started.
            </Alert>
          )}
        </Box>
        
        {/* Seasonal Pricing Section */}
        <Box>
          <Flex justify="space-between" align="center" mb={4}>
            <Heading size="md">Seasonal Pricing</Heading>
            <Button 
              leftIcon={<FiPlus />} 
              colorScheme="brand" 
              size="sm"
              onClick={() => setShowAddSeasonalPrice(!showAddSeasonalPrice)}
              isDisabled={roomTypes.length === 0}
            >
              Add Seasonal Price
            </Button>
          </Flex>
          
          {roomTypes.length === 0 && (
            <Alert status="warning" mb={4}>
              <AlertIcon />
              You need to create at least one room type before setting seasonal prices.
            </Alert>
          )}
          
          {showAddSeasonalPrice && (
            <Card mb={6} variant="outline" bg={bgColor}>
              <CardBody>
                <VStack spacing={4} align="stretch">
                  <FormControl isRequired>
                    <FormLabel>Room Type</FormLabel>
                    <Select
                      value={newSeasonalPrice.roomTypeId}
                      onChange={(e) => setNewSeasonalPrice({...newSeasonalPrice, roomTypeId: e.target.value})}
                      placeholder="Select room type"
                    >
                      {roomTypes.map(rt => (
                        <option key={rt.id} value={rt.id}>
                          {rt.name} (Base: ${rt.basePrice})
                        </option>
                      ))}
                    </Select>
                  </FormControl>
                  
                  <FormControl isRequired>
                    <FormLabel>Season Name</FormLabel>
                    <Input 
                      value={newSeasonalPrice.name}
                      onChange={(e) => setNewSeasonalPrice({...newSeasonalPrice, name: e.target.value})}
                      placeholder="e.g. Summer Season, Holiday Season"
                    />
                  </FormControl>
                  
                  <Grid templateColumns="repeat(2, 1fr)" gap={4}>
                    <GridItem>
                      <FormControl isRequired>
                        <FormLabel>Start Date</FormLabel>
                        <DatePicker
                          selected={newSeasonalPrice.startDate}
                          onChange={(date) => setNewSeasonalPrice({...newSeasonalPrice, startDate: date})}
                          customInput={
                            <Input />
                          }
                        />
                      </FormControl>
                    </GridItem>
                    
                    <GridItem>
                      <FormControl isRequired>
                        <FormLabel>End Date</FormLabel>
                        <DatePicker
                          selected={newSeasonalPrice.endDate}
                          onChange={(date) => setNewSeasonalPrice({...newSeasonalPrice, endDate: date})}
                          customInput={
                            <Input />
                          }
                          minDate={newSeasonalPrice.startDate}
                        />
                      </FormControl>
                    </GridItem>
                  </Grid>
                  
                  <FormControl isRequired>
                    <FormLabel>Seasonal Price Per Night</FormLabel>
                    <NumberInput 
                      min={0} 
                      precision={2}
                      value={newSeasonalPrice.price}
                      onChange={(value) => setNewSeasonalPrice({...newSeasonalPrice, price: parseFloat(value)})}
                    >
                      <NumberInputField />
                      <NumberInputStepper>
                        <NumberIncrementStepper />
                        <NumberDecrementStepper />
                      </NumberInputStepper>
                    </NumberInput>
                  </FormControl>
                  
                  <HStack spacing={3} justify="flex-end">
                    <Button variant="ghost" onClick={() => setShowAddSeasonalPrice(false)}>
                      Cancel
                    </Button>
                    <Button 
                      colorScheme="brand" 
                      leftIcon={<FiSave />}
                      onClick={handleSaveSeasonalPrice}
                      isDisabled={!newSeasonalPrice.roomTypeId}
                    >
                      Save Seasonal Price
                    </Button>
                  </HStack>
                </VStack>
              </CardBody>
            </Card>
          )}
          
          {seasonalPrices.length > 0 ? (
            <VStack spacing={4} align="stretch">
              {roomTypes.map(roomType => {
                const roomTypePrices = seasonalPrices.filter(sp => 
                  String(sp.roomTypeId) === String(roomType.id)
                );
                
                if (roomTypePrices.length === 0) return null;
                
                return (
                  <Card key={roomType.id} variant="outline" bg={bgColor}>
                    <CardBody>
                      <Heading size="sm" mb={3}>
                        {roomType.name} - Seasonal Prices
                      </Heading>
                      
                      <Table size="sm" variant="simple">
                        <Thead>
                          <Tr>
                            <Th>Season</Th>
                            <Th>Dates</Th>
                            <Th isNumeric>Price</Th>
                            <Th>Actions</Th>
                          </Tr>
                        </Thead>
                        <Tbody>
                          {roomTypePrices.map(price => (
                            <Tr key={price.id}>
                              {isEditing && isEditing.type === 'seasonalPrice' && isEditing.id === price.id ? (
                                <>
                                  <Td>
                                    <Input 
                                      size="sm" 
                                      value={editedValues.name || ''}
                                      onChange={(e) => setEditedValues({...editedValues, name: e.target.value})}
                                    />
                                  </Td>
                                  <Td>
                                    <HStack spacing={1}>
                                      <DatePicker
                                        selected={editedValues.startDate instanceof Date ? editedValues.startDate : new Date(editedValues.startDate)}
                                        onChange={(date) => setEditedValues({...editedValues, startDate: date})}
                                        customInput={
                                          <Input size="sm" width="110px" />
                                        }
                                      />
                                      <Text>to</Text>
                                      <DatePicker
                                        selected={editedValues.endDate instanceof Date ? editedValues.endDate : new Date(editedValues.endDate)}
                                        onChange={(date) => setEditedValues({...editedValues, endDate: date})}
                                        customInput={
                                          <Input size="sm" width="110px" />
                                        }
                                        minDate={editedValues.startDate instanceof Date ? editedValues.startDate : new Date(editedValues.startDate)}
                                      />
                                    </HStack>
                                  </Td>
                                  <Td isNumeric>
                                    <NumberInput 
                                      size="sm"
                                      min={0} 
                                      precision={2}
                                      value={editedValues.price || 0}
                                      onChange={(value) => setEditedValues({...editedValues, price: parseFloat(value)})}
                                      width="100px"
                                    >
                                      <NumberInputField ref={editPriceInputRef} />
                                      <NumberInputStepper>
                                        <NumberIncrementStepper />
                                        <NumberDecrementStepper />
                                      </NumberInputStepper>
                                    </NumberInput>
                                  </Td>
                                  <Td>
                                    <HStack spacing={1}>
                                      <IconButton
                                        icon={<FiCheck />}
                                        aria-label="Save changes"
                                        size="sm"
                                        colorScheme="green"
                                        onClick={handleSaveEdit}
                                      />
                                      <IconButton
                                        icon={<FiX />}
                                        aria-label="Cancel"
                                        size="sm"
                                        onClick={handleCancelEdit}
                                      />
                                    </HStack>
                                  </Td>
                                </>
                              ) : (
                                <>
                                  <Td>{price.name}</Td>
                                  <Td>
                                    {new Date(price.startDate).toLocaleDateString()} to {new Date(price.endDate).toLocaleDateString()}
                                  </Td>
                                  <Td isNumeric>
                                    <Badge colorScheme="green">${price.price.toFixed(2)}</Badge>
                                  </Td>
                                  <Td>
                                    <HStack spacing={1}>
                                      <IconButton
                                        icon={<FiEdit />}
                                        aria-label="Edit seasonal price"
                                        size="sm"
                                        variant="ghost"
                                        onClick={() => handleStartEditing('seasonalPrice', price.id, { ...price })}
                                      />
                                      <IconButton
                                        icon={<FiTrash2 />}
                                        aria-label="Delete seasonal price"
                                        size="sm"
                                        variant="ghost"
                                        colorScheme="red"
                                        onClick={() => handleDeleteSeasonalPrice(price.id)}
                                      />
                                    </HStack>
                                  </Td>
                                </>
                              )}
                            </Tr>
                          ))}
                        </Tbody>
                      </Table>
                    </CardBody>
                  </Card>
                );
              })}
            </VStack>
          ) : (
            <Alert status="info">
              <AlertIcon />
              No seasonal prices defined yet. Add seasonal pricing to increase revenue during peak periods.
            </Alert>
          )}
        </Box>
      </SimpleGrid>
    </Container>
  );
}

export default RoomPricing;