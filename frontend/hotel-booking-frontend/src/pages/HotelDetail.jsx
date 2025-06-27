import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Container,
  Heading,
  Text,
  Badge,
  Button,
  Grid,
  GridItem,
  HStack,
  VStack,
  SimpleGrid,
  Flex,
  Divider,
  Icon,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  useDisclosure,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalFooter,
  ModalBody,
  ModalCloseButton,
  useToast,
  Spinner,
  useColorModeValue,
  FormControl,  
  FormLabel,    
  Input,        
  Textarea,
  Alert,
  AlertIcon,
  Tag,
  TagLabel,
  Image,
  Stack
} from '@chakra-ui/react';
import {
  FiMapPin,
  FiStar,
  FiCalendar,
  FiUsers,
  FiWifi,
  FiCoffee,
  FiHome,
  FiBook,
  FiArrowLeft,
  FiCheckCircle,
  FiInfo
} from 'react-icons/fi';
import apiClient from '../services/api';
import HotelGallery from '../components/hotels/HotelGallery';
import HotelInfo from '../components/hotels/HotelInfo';
import LocationMap from '../components/hotels/LocationMap';

function HotelDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  
  const [hotel, setHotel] = useState(null);
  const [photos, setPhotos] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [roomTypes, setRoomTypes] = useState([]);
  const [roomPricings, setRoomPricings] = useState([]);
  const [calculatedPrices, setCalculatedPrices] = useState({});
  const [selectedRoomType, setSelectedRoomType] = useState(null);
  const [bookingData, setBookingData] = useState({
    hotelId: id,
    checkInDate: '',
    checkOutDate: '',
    guestCount: 1,
    roomTypeId: '',
    specialRequests: ''
  });
  
  const bgColor = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  const priceSummaryBg = useColorModeValue('gray.50', 'gray.700');
const priceSummaryBorder = useColorModeValue('gray.200', 'gray.600');
const priceSummaryText = useColorModeValue('gray.800', 'white');
const priceSummaryAccent = useColorModeValue('brand.600', 'brand.300');
const seasonalTagBg = useColorModeValue('yellow.50', 'yellow.900');
const seasonalTagText = useColorModeValue('yellow.800', 'yellow.200');

  // Get default base price based on room type tier
  const getDefaultBasePrice = (roomTypeName) => {
    if (!roomTypeName) return 199;
    
    const name = roomTypeName.toLowerCase();
    if (name.includes('deluxe') || name.includes('premium')) return 299;
    if (name.includes('suite') || name.includes('executive')) return 399;
    if (name.includes('family')) return 349;
    if (name.includes('standard')) return 199;
    return 199; // Default price
  };

  // Normalize room types to ensure they have proper base prices
  // Memoize the function with useCallback to avoid dependency issues
  // Update the normalizeRoomTypes function to handle circular references
  const normalizeRoomTypes = useCallback((roomTypes) => {
    if (!roomTypes || !Array.isArray(roomTypes) || roomTypes.length === 0) {
      console.log('No room types to normalize');
      return [];
    }
    
    console.log('Normalizing room types:', roomTypes);
    
    // First, create a lookup map of room types by their $id
    const roomTypeMap = {};
    roomTypes.forEach(roomType => {
      if (roomType.$id) {
        roomTypeMap[roomType.$id] = roomType;
      }
    });
    
    return roomTypes
      .map((roomType, index) => {
        if (!roomType) return null;
        
        // If this is a reference, resolve it
        if (roomType.$ref) {
          const refId = roomType.$ref;
          // Try to find the referenced room type
          const resolvedRoomType = roomTypeMap[refId];
          if (!resolvedRoomType) {
            console.warn(`Could not resolve reference ${refId}`);
            return null;
          }
          roomType = resolvedRoomType;
        }
        
        // Extract data from various possible structures
        let data = roomType;
        
        // Handle room type name extraction
        let name = null;
        if (data.name && typeof data.name === 'string') {
          name = data.name.trim();
        } else if (data.type && typeof data.type === 'string') {
          name = data.type.trim();
        } else if (data.roomTypeName && typeof data.roomTypeName === 'string') {
          name = data.roomTypeName.trim();
        } else if (data.title && typeof data.title === 'string') {
          name = data.title.trim();
        } else {
          // Use a default name with the index to make it unique
          name = `Room Type ${index + 1}`;
        }
        
        // The rest of your normalization code...
        let basePrice = 199; // Default price
        if (typeof data.basePrice === 'number' && !isNaN(data.basePrice) && data.basePrice > 0) {
          basePrice = data.basePrice;
        } else if (typeof data.price === 'number' && !isNaN(data.price) && data.price > 0) {
          basePrice = data.price;
        } else if (typeof data.rate === 'number' && !isNaN(data.rate) && data.rate > 0) {
          basePrice = data.rate;
        } else {
          basePrice = getDefaultBasePrice(name);
        }
        
        // Ensure unique ID (prefer actual ID, fallback to index+1)
        let id = data.id || index + 1;
        
        return {
          id: id,
          name: name,
          description: data.description || `Comfortable ${name.toLowerCase()} with all amenities`,
          basePrice: basePrice,
          capacity: data.capacity || data.bedCount || data.maxOccupancy || 2,
          amenities: Array.isArray(data.amenities) ? data.amenities : []
        };
      })
      .filter(Boolean) // Remove any null items
      .filter((roomType, index, self) => 
        // Remove duplicates (in case resolving references created duplicates)
        index === self.findIndex(rt => rt.id === roomType.id)
      );
}, []);

  // Add this helper function to check if roomTypes array is truly empty
  const hasRoomTypes = (roomTypes) => {
    if (!roomTypes || !Array.isArray(roomTypes)) return false;
    return roomTypes.length > 0;
  };

  useEffect(() => {
    const fetchHotelData = async () => {
      setIsLoading(true);
      try {
        // Fetch hotel details
        console.log(`Fetching hotel with ID: ${id}`);
        const hotelResponse = await apiClient.get(`/Hotels/${id}`);
        setHotel(hotelResponse.data);
        
        // Extract room types from hotel data
        let roomTypesData = [];
        
        // Check for room types in different possible formats
        if (hotelResponse.data.roomTypes) {
          if (Array.isArray(hotelResponse.data.roomTypes)) {
            roomTypesData = hotelResponse.data.roomTypes;
          } else if (hotelResponse.data.roomTypes.$values && 
                    Array.isArray(hotelResponse.data.roomTypes.$values)) {
            roomTypesData = hotelResponse.data.roomTypes.$values;
          } else {
            // It might be a single object
            roomTypesData = [hotelResponse.data.roomTypes];
          }
        }
        
        // Try separate endpoint if no room types found
        if (roomTypesData.length === 0) {
          try {
            console.log(`Fetching room types for hotel ID: ${id}`);
            const roomTypesResponse = await apiClient.get(`/room-types/by-hotel/${id}`);
            
            if (Array.isArray(roomTypesResponse.data)) {
              roomTypesData = roomTypesResponse.data;
            } else if (roomTypesResponse.data && roomTypesResponse.data.$values && 
                      Array.isArray(roomTypesResponse.data.$values)) {
              roomTypesData = roomTypesResponse.data.$values;
            }
          } catch (roomTypeErr) {
            console.warn(`Error fetching room types: ${roomTypeErr.message}`);
          }
        }
        
        // Process the room types data
        if (roomTypesData.length > 0) {
          console.log(`Found ${roomTypesData.length} room types`);
          const normalizedRoomTypes = normalizeRoomTypes(roomTypesData);
          setRoomTypes(normalizedRoomTypes);
        } else {
          console.log('No room types found for this hotel');
          setRoomTypes([]);
        }
        
        // Fetch hotel photos
        try {
          console.log(`Fetching photos for hotel ID: ${id}`);
          const photosResponse = await apiClient.get(`/HotelPhotos/hotel/${id}`);
          
          // Debug the actual response structure
          console.log('Hotel photos response:', photosResponse.data);
          
          // Process the photos data based on structure
          let processedPhotos = [];
          
          if (Array.isArray(photosResponse.data)) {
            // Direct array response from controller
            processedPhotos = photosResponse.data;
          } else if (photosResponse.data && photosResponse.data.$values && 
                     Array.isArray(photosResponse.data.$values)) {
            // If it's in a $values property (common in .NET serialization)
            processedPhotos = photosResponse.data.$values;
          } else if (photosResponse.data && typeof photosResponse.data === 'object') {
            // If it's a single photo object
            processedPhotos = [photosResponse.data];
          }
          
          console.log('Processed photos:', processedPhotos);
          setPhotos(processedPhotos);
        } catch (photoErr) {
          console.warn(`Error fetching photos: ${photoErr.message}`);
          // Use photos from hotel response if available
          if (hotelResponse.data.photos && hotelResponse.data.photos.length > 0) {
            setPhotos(hotelResponse.data.photos);
          } else {
            setPhotos([]);
          }
        }
        
        setError(null);
      } catch (err) {
        console.error('Error fetching hotel data:', err);
        setError('Failed to load hotel details. Please try again.');
        
        // Show more specific error if available
        if (err.response) {
          if (err.response.status === 404) {
            setError(`Hotel with ID ${id} could not be found.`);
          } else if (err.response.data) {
            setError(`Error: ${err.response.data}`);
          }
        }
      } finally {
        setIsLoading(false);
      }
    };
    
    fetchHotelData();
  }, [id, normalizeRoomTypes]); // Added normalizeRoomTypes to dependency array

  // Move room pricing fetch to a separate useEffect that runs after roomTypes is populated
  useEffect(() => {
    const fetchRoomPricing = async () => {
      if (roomTypes.length === 0) return;
      
      try {
        console.log('Fetching room pricing data');
        // First try to fetch hotel-specific pricing
        try {
          const hotelPricingResponse = await apiClient.get(`/RoomPricings/hotel/${id}`);
          console.log('Hotel pricing data:', hotelPricingResponse.data);
          
          // Filter pricing data for this hotel's room types
          const relevantPricings = hotelPricingResponse.data.filter(pricing => 
            roomTypes.some(rt => rt.id === pricing.roomTypeId)
          );
          
          if (relevantPricings.length > 0) {
            console.log('Found hotel-specific pricing data:', relevantPricings);
            setRoomPricings(relevantPricings);
            return;
          }
        } catch (hotelPricingErr) {
          console.warn('No hotel-specific pricing found, fetching all pricing data');
        }
        
        // If hotel-specific pricing fails, try general pricing
        const allPricingResponse = await apiClient.get(`/RoomPricings`);
        
        // Filter pricing data for this hotel's room types
        const relevantPricings = allPricingResponse.data.filter(pricing => 
          roomTypes.some(rt => rt.id === pricing.roomTypeId)
        );
        
        console.log('Found relevant pricing data:', relevantPricings);
        setRoomPricings(relevantPricings || []);
      } catch (pricingErr) {
        console.warn(`Error fetching room pricing: ${pricingErr.message}`);
        setRoomPricings([]);
      }
    };
    
    fetchRoomPricing();
  }, [roomTypes, id]); // This will run whenever roomTypes changes

  // Calculate prices based on selected dates and room types
  useEffect(() => {
    if (!bookingData.checkInDate || !bookingData.checkOutDate || roomTypes.length === 0) {
      return;
    }
    
    const checkIn = new Date(bookingData.checkInDate);
    const checkOut = new Date(bookingData.checkOutDate);
    
    // Calculate number of nights
    const nights = Math.max(1, Math.floor((checkOut - checkIn) / (1000 * 60 * 60 * 24)));
    
    // Calculate price for each room type
    const newCalculatedPrices = {};
    
    roomTypes.forEach(roomType => {
      // Ensure roomType.basePrice is a valid number and > 0
      const basePrice = typeof roomType.basePrice === 'number' && roomType.basePrice > 0 
        ? roomType.basePrice 
        : getDefaultBasePrice(roomType.name);
      
      console.log(`Room Type ${roomType.name} has base price: ${basePrice}`);
      
      // Find seasonal pricing for this room type and date range
      const seasonalPricings = roomPricings.filter(p => 
        p.roomTypeId === roomType.id &&
        new Date(p.date) >= checkIn &&
        new Date(p.date) < checkOut
      );
      
      if (seasonalPricings.length > 0) {
        // Calculate average price from all seasonal pricings that apply
        const totalSeasonalPrice = seasonalPricings.reduce((sum, pricing) => sum + pricing.price, 0);
        const avgSeasonalPrice = totalSeasonalPrice / seasonalPricings.length;
        
        newCalculatedPrices[roomType.id] = {
          perNight: avgSeasonalPrice,
          total: avgSeasonalPrice * nights,
          nights,
          hasSeasonalPrice: true,
          seasonName: "Seasonal Rate",
          regularPrice: basePrice
        };
      } else {
        // Use base price if no seasonal pricing found
        newCalculatedPrices[roomType.id] = {
          perNight: basePrice,
          total: basePrice * nights,
          nights,
          hasSeasonalPrice: false
        };
      }
    });
    
    setCalculatedPrices(newCalculatedPrices);
  }, [bookingData.checkInDate, bookingData.checkOutDate, roomTypes, roomPricings]);

  const handleBookingSubmit = async () => {
    try {
      // Add user ID from local storage or context
      const userData = JSON.parse(localStorage.getItem('userData')) || {};
      const selectedRoom = roomTypes.find(rt => rt.id === parseInt(bookingData.roomTypeId));
      
      if (!selectedRoom) {
        toast({
          title: 'Invalid room selection',
          description: 'Please select a valid room type.',
          status: 'error',
          duration: 5000,
          isClosable: true,
        });
        return;
      }
      
      // Get price information
      const pricingInfo = calculatedPrices[selectedRoom.id] || {
        perNight: selectedRoom.basePrice,
        total: selectedRoom.basePrice * Math.max(1, Math.floor((new Date(bookingData.checkOutDate) - new Date(bookingData.checkInDate)) / (1000 * 60 * 60 * 24))),
        nights: Math.max(1, Math.floor((new Date(bookingData.checkOutDate) - new Date(bookingData.checkInDate)) / (1000 * 60 * 60 * 24)))
      };
      
      // Create booking payload with correct property names to match API
      const bookingPayload = {
        userId: userData.id,
        hotelId: parseInt(id),
        roomTypeId: parseInt(bookingData.roomTypeId),
        checkInDate: new Date(bookingData.checkInDate).toISOString(),
        checkOutDate: new Date(bookingData.checkOutDate).toISOString(),
        guestCount: parseInt(bookingData.guestCount),
        totalPrice: pricingInfo.total,
        status: 0, // Pending
        isPaid: false,
        specialRequests: bookingData.specialRequests
      };
      
      console.log('Submitting booking:', bookingPayload);
      
      try {
        // Fixed: Don't store the response in an unused variable
        await apiClient.post('/Bookings', bookingPayload);
        
        toast({
          title: 'Booking successful',
          description: 'Your room has been booked successfully.',
          status: 'success',
          duration: 5000,
          isClosable: true,
        });
        
        onClose();
        navigate('/dashboard');
      } catch (err) {
        console.error('Booking API error:', err);
        
        if (process.env.NODE_ENV === 'development') {
          // For development, show more detailed error
          toast({
            title: 'Booking failed',
            description: `API Error: ${err.response?.data || err.message}`,
            status: 'error',
            duration: 5000,
            isClosable: true,
          });
        } else {
          throw err;
        }
      }
    } catch (err) {
      toast({
        title: 'Booking failed',
        description: 'An error occurred while booking. Please try again.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    }
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setBookingData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const selectRoomType = (roomTypeId) => {
    setSelectedRoomType(roomTypeId);
    setBookingData(prev => ({
      ...prev,
      roomTypeId
    }));
    onOpen();
  };

  // Check if dates are selected
  const areDatesSelected = Boolean(bookingData.checkInDate && bookingData.checkOutDate);

  if (isLoading) {
    return (
      <Flex justify="center" align="center" height="100vh">
        <Spinner size="xl" thickness="4px" speed="0.65s" color="brand.500" />
      </Flex>
    );
  }

  if (error || !hotel) {
    return (
      <Box textAlign="center" py={10} px={6}>
        <Heading as="h2" size="xl" mt={6} mb={2}>
          Error Loading Hotel
        </Heading>
        <Text>{error || 'Hotel not found'}</Text>
        <Button
          colorScheme="brand"
          mt={4}
          leftIcon={<FiArrowLeft />}
          onClick={() => navigate('/dashboard')}
        >
          Return to Dashboard
        </Button>
      </Box>
    );
  }

  // Parse amenities if they're stored as a string
  const amenities = typeof hotel.amenities === 'string'
    ? hotel.amenities.split(',').map(a => a.trim())
    : Array.isArray(hotel.amenities) ? hotel.amenities : [];

  // Get room type images (use first photo for now)
  const getRoomTypeImage = (index) => {
    // Get some of the hotel photos for room types
    if (photos.length > index) {
      const photo = photos[index];
      
      // Check different possible URL properties
      if (photo.url) {
        return photo.url;
      } else if (photo.imageUrl) {
        return photo.imageUrl;
      } else if (photo.publicId) {
        return `https://res.cloudinary.com/your-cloud-name/image/upload/${photo.publicId}`;
      }
    }
    
    // Fallback image URLs for room types
    const fallbackImages = [
      "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=800&q=80",
      "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=800&q=80"
    ];
    return fallbackImages[index % fallbackImages.length];
  };

  // 2. Create a helper function to normalize amenities format
const normalizeAmenities = (amenitiesData) => {
  if (!amenitiesData) return [];
  
  if (typeof amenitiesData === 'string') {
    return amenitiesData.split(',').map(a => a.trim()).filter(a => a);
  }
  
  if (Array.isArray(amenitiesData)) {
    return amenitiesData.filter(a => a);
  }
  
  return [];
};

// 3. Updated amenities parsing with the helper function
const hotelAmenities = normalizeAmenities(hotel.amenities);

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
      
      <Grid templateColumns={{ base: "1fr", lg: "2fr 1fr" }} gap={8}>
        <GridItem>
          {/* Hotel Gallery */}
          <HotelGallery photos={photos} hotelName={hotel.name} />
          
          <Box mt={6}>
            <HStack spacing={2} mb={2}>
              <Heading as="h1" size="xl">{hotel.name}</Heading>
              <Badge 
                colorScheme={hotel.rating >= 4.5 ? "green" : hotel.rating >= 3.5 ? "yellow" : "red"}
                fontSize="md"
                px={2}
                py={1}
                borderRadius="md"
              >
                {hotel.rating} <Icon as={FiStar} />
              </Badge>
            </HStack>
            
            <HStack spacing={2} color="gray.600" mb={4}>
              <Icon as={FiMapPin} />
              <Text>{hotel.location}</Text>
            </HStack>
            
            <Divider my={4} />
            
            <Tabs colorScheme="brand" isLazy>
              <TabList>
                <Tab>Overview</Tab>
                <Tab>Room Types</Tab>
                <Tab>Location</Tab>
                <Tab>Reviews</Tab>
              </TabList>
              
              <TabPanels>
                <TabPanel>
                  {/* Hotel Info */}
                  <HotelInfo 
  hotel={hotel} 
  amenities={amenities} // Use the state variable that contains the normalized amenities
/>
                </TabPanel>
                
                <TabPanel>
                  <Heading size="md" mb={4}>Room Types & Pricing</Heading>
                  
                  <Box mb={4}>
                    <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4}>
                      <FormControl>
                        <FormLabel>Check-in Date</FormLabel>
                        <Input 
                          type="date" 
                          name="checkInDate"
                          value={bookingData.checkInDate}
                          onChange={handleInputChange}
                          min={new Date().toISOString().split('T')[0]}
                        />
                      </FormControl>
                      
                      <FormControl>
                        <FormLabel>Check-out Date</FormLabel>
                        <Input 
                          type="date" 
                          name="checkOutDate"
                          value={bookingData.checkOutDate}
                          onChange={handleInputChange}
                          min={bookingData.checkInDate || new Date().toISOString().split('T')[0]}
                        />
                      </FormControl>
                    </SimpleGrid>
                  </Box>
                  
                  {hasRoomTypes(roomTypes) ? (
                    <>
                      {!areDatesSelected && (
                        <Alert status="info" mb={4}>
                          <AlertIcon />
                          <Box>
                            <Text fontWeight="medium">Select dates to see accurate pricing</Text>
                            <Text fontSize="sm">Prices may vary based on seasonal rates and availability</Text>
                          </Box>
                        </Alert>
                      )}
                      
                      <VStack spacing={6} align="stretch">
                        {roomTypes.map((roomType, index) => {
                          const pricing = calculatedPrices[roomType.id];
                          const basePrice = roomType.basePrice || getDefaultBasePrice(roomType.name);
                          const displayPrice = pricing ? pricing.perNight : basePrice;
                          const isSeasonalPrice = pricing && pricing.hasSeasonalPrice;
                          
                          return (
                            <Box 
                              key={`room-type-${roomType.id}-${index}-${roomType.name.replace(/\s+/g, '-')}`} // Use a composite key to ensure uniqueness
                              p={5} 
                              borderWidth="1px" 
                              borderRadius="lg" 
                              bg={bgColor}
                              borderColor={borderColor}
                              transition="transform 0.2s"
                              _hover={{ transform: 'translateY(-4px)', shadow: 'md' }}
                            >
                              <Grid templateColumns={{ base: "1fr", md: "1fr 2fr" }} gap={5}>
                                {/* Room Image */}
                                <Box borderRadius="md" overflow="hidden" height="180px">
                                  <Image 
                                    src={getRoomTypeImage(index)} 
                                    alt={roomType.name || `Room Type ${index + 1}`}
                                    objectFit="cover"
                                    w="100%"
                                    h="100%"
                                    fallbackSrc="https://via.placeholder.com/300x200?text=Room+Image"
                                  />
                                </Box>
                                
                                {/* Room Details */}
                                <Box>
                                  <Flex justify="space-between" align="flex-start" mb={2}>
                                    <Heading size="md">
                                      {roomType.name || `Room Type ${index + 1}`}
                                    </Heading>
                                    <VStack align="flex-end" spacing={0}>
                                      <Heading size="md" color="brand.500">
                                        ${displayPrice.toFixed(2)}<Text as="span" fontSize="sm">/night</Text>
                                      </Heading>
                                      
                                      {isSeasonalPrice && (
                                        <>
                                          <Text fontSize="xs" textDecoration="line-through" color="gray.500">
                                            ${pricing.regularPrice.toFixed(2)}
                                          </Text>
                                          <Tag size="sm" colorScheme="red" mt={1}>
                                            <TagLabel>{pricing.seasonName}</TagLabel>
                                          </Tag>
                                        </>
                                      )}
                                      
                                      {pricing && pricing.nights > 1 && (
                                        <Text fontSize="xs" color="gray.600">
                                          ${pricing.total.toFixed(2)} total for {pricing.nights} nights
                                        </Text>
                                      )}
                                    </VStack>
                                  </Flex>
                                  
                                  <Text fontSize="sm" mb={3}>
                                    {roomType.description}
                                  </Text>
                                  
                                  <Stack direction={{ base: "column", md: "row" }} spacing={4} mb={3}>
                                    <HStack>
                                      <Icon as={FiUsers} />
                                      <Text fontSize="sm">Max {roomType.capacity || 2} guests</Text>
                                    </HStack>
                                    
                                    {/* Show amenities if available */}
                                    {roomType.amenities && (
                                      <>
                                        {roomType.amenities.includes('WiFi') && (
                                          <HStack>
                                            <Icon as={FiWifi} />
                                            <Text fontSize="sm">WiFi</Text>
                                          </HStack>
                                        )}
                                        {roomType.amenities.includes('Breakfast') && (
                                          <HStack>
                                            <Icon as={FiCoffee} />
                                            <Text fontSize="sm">Breakfast</Text>
                                          </HStack>
                                        )}
                                      </>
                                    )}
                                  </Stack>
                                  
                                  <HStack mt={2} wrap="wrap" spacing={2}>
                                    {roomType.amenities && roomType.amenities.map((amenity, i) => (
                                      <Tag key={i} size="sm" colorScheme="brand" variant="subtle">
                                        {amenity}
                                      </Tag>
                                    ))}
                                  </HStack>
                                  
                                  <Button 
                                    mt={4} 
                                    colorScheme="brand" 
                                    rightIcon={<FiCalendar />}
                                    onClick={() => selectRoomType(roomType.id)}
                                    isDisabled={!areDatesSelected}
                                    size="md"
                                  >
                                    {areDatesSelected ? 'Book This Room Type' : 'Select Dates to Book'}
                                  </Button>
                                </Box>
                              </Grid>
                            </Box>
                          );
                        })}
                      </VStack>
                    </>
                  ) : (
                    <Alert status="info" borderRadius="md">
                      <AlertIcon />
                      <Box>
                        <Text fontWeight="medium">There are no room types yet</Text>
                        <Text fontSize="sm">This hotel has not added any room types for booking.</Text>
                      </Box>
                    </Alert>
                  )}
                </TabPanel>
                
                <TabPanel>
                  <Heading size="md" mb={4}>Location</Heading>
                  <LocationMap 
                    location={hotel.location} 
                    name={hotel.name} 
                    coordinates={hotel.coordinates || null}
                  />
                </TabPanel>
                
                <TabPanel>
                  <Heading size="md" mb={4}>Guest Reviews</Heading>
                  <Text>Reviews feature coming soon.</Text>
                </TabPanel>
              </TabPanels>
            </Tabs>
          </Box>
        </GridItem>
        
        <GridItem>
          <Box 
            p={6} 
            borderWidth="1px" 
            borderRadius="lg" 
            bg={bgColor}
            borderColor={borderColor}
            position="sticky"
            top="20px"
          >
            <Heading size="md" mb={4}>Book Your Stay</Heading>
            <VStack spacing={4} align="stretch">
              <HStack>
                <Icon as={FiHome} />
                <Text fontWeight="medium">{hotel.name}</Text>
              </HStack>
              <HStack>
                <Icon as={FiMapPin} />
                <Text>{hotel.location}</Text>
              </HStack>
              <HStack>
                <Icon as={FiStar} />
                <Text>{hotel.rating} Star Rating</Text>
              </HStack>
              
              <Divider />
              
              <FormControl>
                <FormLabel>Check-in Date</FormLabel>
                <Input 
                  type="date" 
                  name="checkInDate"
                  value={bookingData.checkInDate}
                  onChange={handleInputChange}
                  min={new Date().toISOString().split('T')[0]}
                />
              </FormControl>
              
              <FormControl>
                <FormLabel>Check-out Date</FormLabel>
                <Input 
                  type="date" 
                  name="checkOutDate"
                  value={bookingData.checkOutDate}
                  onChange={handleInputChange}
                  min={bookingData.checkInDate || new Date().toISOString().split('T')[0]}
                />
              </FormControl>
              
              <FormControl>
                <FormLabel>Number of Guests</FormLabel>
                <Input 
                  type="number" 
                  name="guestCount"
                  value={bookingData.guestCount}
                  onChange={handleInputChange}
                  min={1}
                  max={10}
                />
              </FormControl>
              
              <Divider />
              
              <Text fontWeight="bold">
                Starting from ${(hotel.basePrice || roomTypes[0]?.basePrice || 199).toFixed(2)}/night
              </Text>
              
              {areDatesSelected ? (
                <Box>
                  <Heading size="xs" mb={2}>Select a Room Type:</Heading>
                  <SimpleGrid columns={1} spacing={2}>
                    {roomTypes.map((roomType, index) => { // Add the index parameter here
                      const pricing = calculatedPrices[roomType.id];
                      const basePrice = roomType.basePrice || getDefaultBasePrice(roomType.name);
                      const displayPrice = pricing ? pricing.perNight : basePrice;
                      const isSeasonalPrice = pricing && pricing.hasSeasonalPrice;
                      
                      return (
                        <Button 
                          key={`sidebar-room-${roomType.id}-${index}`} // Now index is defined
                          variant="outline"
                          colorScheme="brand"
                          justifyContent="space-between"
                          onClick={() => selectRoomType(roomType.id)}
                          size="sm"
                        >
                          <Text>{roomType.name || `Room Type ${index + 1}`}</Text> {/* Now index is defined */}
                          <HStack>
                            {isSeasonalPrice && (
                              <Text fontSize="xs" textDecoration="line-through" color="gray.500">
                                ${pricing.regularPrice.toFixed(2)}
                              </Text>
                            )}
                            <Text fontWeight="bold">${displayPrice.toFixed(2)}</Text>
                          </HStack>
                        </Button>
                      );
                    })}
                  </SimpleGrid>
                </Box>
              ) : (
                <Button
                  colorScheme="brand"
                  size="lg"
                  rightIcon={<FiBook />}
                  isDisabled={true}
                >
                  Select Dates to Book
                </Button>
              )}
              
              <VStack spacing={2} align="stretch" mt={2}>
                <HStack color="green.500">
                  <Icon as={FiCheckCircle} />
                  <Text fontSize="sm">Free cancellation up to 24 hours before check-in</Text>
                </HStack>
                <HStack color="green.500">
                  <Icon as={FiCheckCircle} />
                  <Text fontSize="sm">No payment needed today</Text>
                </HStack>
              </VStack>
            </VStack>
          </Box>
        </GridItem>
      </Grid>
      
      {/* Booking Modal */}
      <Modal isOpen={isOpen} onClose={onClose} size="lg">
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Book Your Stay at {hotel.name}</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <VStack spacing={4} align="stretch">
              {selectedRoomType && (
                <Alert status="info" mb={2}>
                  <AlertIcon />
                  <Box>
                    <Text fontWeight="medium">
                      {roomTypes.find(rt => rt.id === selectedRoomType)?.name || 'Selected Room Type'}
                    </Text>
                    {calculatedPrices[selectedRoomType] && (
                      <Text fontSize="sm">
                        ${calculatedPrices[selectedRoomType].perNight.toFixed(2)}/night
                      </Text>
                    )}
                  </Box>
                </Alert>
              )}
              
              <FormControl isRequired>
                <FormLabel>Check-in Date</FormLabel>
                <Input 
                  type="date" 
                  name="checkInDate"
                  value={bookingData.checkInDate}
                  onChange={handleInputChange}
                  min={new Date().toISOString().split('T')[0]}
                />
              </FormControl>
              
              <FormControl isRequired>
                <FormLabel>Check-out Date</FormLabel>
                <Input 
                  type="date" 
                  name="checkOutDate"
                  value={bookingData.checkOutDate}
                  onChange={handleInputChange}
                  min={bookingData.checkInDate || new Date().toISOString().split('T')[0]}
                />
              </FormControl>
              
              <FormControl isRequired>
                <FormLabel>Number of Guests</FormLabel>
                <Input 
                  type="number" 
                  name="guestCount"
                  value={bookingData.guestCount}
                  onChange={handleInputChange}
                  min={1}
                  max={roomTypes.find(rt => rt.id === selectedRoomType)?.capacity || 10}
                />
              </FormControl>
              
              {selectedRoomType && calculatedPrices[selectedRoomType] && (
                <Box 
                  p={3} 
                  borderWidth="1px" 
                  borderRadius="md" 
                  bg={priceSummaryBg}
                  borderColor={priceSummaryBorder}
                >
                  <Heading size="sm" mb={2} color={priceSummaryText}>Price Summary</Heading>
                  <HStack justify="space-between">
                    <Text fontSize="sm" color={priceSummaryText}>Price per night:</Text>
                    <Text fontSize="sm" fontWeight="bold" color={priceSummaryAccent}>
                      ${calculatedPrices[selectedRoomType].perNight.toFixed(2)}
                    </Text>
                  </HStack>
                  <HStack justify="space-between">
                    <Text fontSize="sm" color={priceSummaryText}>Number of nights:</Text>
                    <Text fontSize="sm" color={priceSummaryText}>
                      {calculatedPrices[selectedRoomType].nights}
                    </Text>
                  </HStack>
                  <Divider my={2} />
                  <HStack justify="space-between">
                    <Text fontSize="sm" fontWeight="bold" color={priceSummaryAccent}>Total:</Text>
                    <Text fontSize="sm" fontWeight="bold" color={priceSummaryAccent}>
                      ${calculatedPrices[selectedRoomType].total.toFixed(2)}
                    </Text>
                  </HStack>
                  
                  {calculatedPrices[selectedRoomType].hasSeasonalPrice && (
                    <Box 
                      mt={2} 
                      p={2} 
                      bg={seasonalTagBg} 
                      borderRadius="sm"
                    >
                      <HStack>
                        <Icon as={FiInfo} color={seasonalTagText} />
                        <Text fontSize="xs" color={seasonalTagText}>
                          {calculatedPrices[selectedRoomType].seasonName} pricing applied
                        </Text>
                      </HStack>
                    </Box>
                  )}
                </Box>
              )}
              
              <FormControl>
                <FormLabel>Special Requests</FormLabel>
                <Textarea 
                  name="specialRequests"
                  value={bookingData.specialRequests}
                  onChange={handleInputChange}
                  placeholder="Any special requests or requirements..."
                  resize="vertical"
                />
              </FormControl>
            </VStack>
          </ModalBody>
          
          <ModalFooter>
            <Button variant="ghost" mr={3} onClick={onClose}>
              Cancel
            </Button>
            <Button 
              colorScheme="brand" 
              rightIcon={<FiCalendar />}
              onClick={handleBookingSubmit}
              isDisabled={!bookingData.checkInDate || !bookingData.checkOutDate || !bookingData.roomTypeId}
            >
              Confirm Booking
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </Container>
  );
}

export default HotelDetail;