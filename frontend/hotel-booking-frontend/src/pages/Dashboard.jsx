import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Flex,
  Grid,
  GridItem,
  Heading,
  Text,
  Button,
  Image,
  Badge,
  SimpleGrid,
  Card,
  CardBody,
  CardFooter,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  IconButton,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  Input,
  InputGroup,
  InputLeftElement,
  InputRightElement,
  Select,
  HStack,
  VStack,
  Tag,
  useDisclosure,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalFooter,
  ModalBody,
  ModalCloseButton,
  FormControl,
  FormLabel,
  Divider,
  useToast,
  Spinner,
  useColorModeValue,
  Container,
  Avatar,
  AvatarBadge,
  Tooltip,
  MenuDivider,
  TabIndicator,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  TableContainer,
  Textarea,
  Drawer,
  DrawerBody,
  DrawerFooter,
  DrawerHeader,
  DrawerOverlay,
  DrawerContent,
  DrawerCloseButton,
  RangeSlider,
  RangeSliderTrack,
  RangeSliderFilledTrack,
  RangeSliderThumb,
  Stack,
  Checkbox,
  Radio,
  RadioGroup,
  CheckboxGroup,
  Progress,
  Collapse,
  Icon,          
  Spacer,        
  ButtonGroup    
} from '@chakra-ui/react';
import {
  FiPlus,
  FiCalendar,
  FiMapPin,
  FiStar,
  FiEdit,
  FiTrash2,
  FiSearch,
  FiHome,
  FiCamera,
  FiFilter,
  FiRefreshCw,
  FiLogOut,
  FiUser,
  FiSettings,
  FiBookmark,
  FiInfo,
  FiChevronDown,
  FiCheck,
  FiX,
  FiEye,
  FiDollarSign,
  FiSliders,
  FiBell,
  FiClock,
  FiHeart,
  FiChevronRight,
  FiChevronLeft,
  FiGrid,
  FiList,
  FiCoffee,
  FiWifi,
  FiSlash,
} from 'react-icons/fi';
import apiClient, { authApi } from '../services/api';

function Dashboard() {
  const [hotels, setHotels] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [filteredHotels, setFilteredHotels] = useState([]);
  const [filteredBookings, setFilteredBookings] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedHotel, setSelectedHotel] = useState(null);
  const [userRole, setUserRole] = useState('Guest');
  const [userData, setUserData] = useState(null);
  const [bookingFilter, setBookingFilter] = useState('all'); 
  const [hotelSearchTerm, setHotelSearchTerm] = useState('');
  const [viewType, setViewType] = useState('grid');
  const [isFilterDrawerOpen, setIsFilterDrawerOpen] = useState(false);
  const [priceRange, setPriceRange] = useState([50, 500]);
  const [selectedStars, setSelectedStars] = useState([]);
  const [selectedAmenities, setSelectedAmenities] = useState([]);
  const [selectedLocations, setSelectedLocations] = useState([]);
  const [dateRange, setDateRange] = useState({ from: '', to: '' });
  const [isMobileFilterVisible, setIsMobileFilterVisible] = useState(false);
  
  const { isOpen, onOpen, onClose } = useDisclosure();
  const {
    isOpen: isFilterOpen,
    onOpen: onFilterOpen,
    onClose: onFilterClose
  } = useDisclosure();
  
  const filterDrawerRef = useRef();
  const toast = useToast();
  const navigate = useNavigate();
  
  // Color mode values
  const bg = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  const subtleColor = useColorModeValue('gray.500', 'gray.400');
  const highlightColor = useColorModeValue('brand.50', 'gray.700');
  const tagBg = useColorModeValue('gray.100', 'gray.700');
  const cardHoverBg = useColorModeValue('gray.50', 'gray.700');

  // Parse JWT token to get user info (keeping the existing function)
  const parseJwt = (token) => {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
      }).join(''));
      
      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('Error parsing JWT token:', error);
      return null;
    }
  };

  // Format user's name properly from API or token data (keeping the existing function)
  const formatUserName = (userData) => {
    if (!userData) return 'User';
    
    if (userData.firstName && userData.lastName) {
      return `${userData.firstName} ${userData.lastName}`;
    }
    
    if (userData.name) {
      return userData.name;
    }
    
    if (userData.email) {
      const emailName = userData.email.split('@')[0];
      return emailName.charAt(0).toUpperCase() + emailName.slice(1);
    }
    
    return 'User';
  };

  // Helper function to parse amenities in different formats
  const parseAmenities = (amenitiesData) => {
    // If amenitiesData is undefined or null
    if (!amenitiesData) return [];
    
    // If amenitiesData is an object with $values property (JSON.NET format)
    if (amenitiesData.$values && Array.isArray(amenitiesData.$values)) {
      return amenitiesData.$values;
    }
    
    // If amenitiesData is already an array
    if (Array.isArray(amenitiesData)) {
      return amenitiesData;
    }
    
    // If amenitiesData is a string
    if (typeof amenitiesData === 'string') {
      return amenitiesData.split(',').map(item => item.trim());
    }
    
    return [];
  };

  // Check auth and load dashboard data
  useEffect(() => {
    const token = localStorage.getItem('token');
    
    if (!token) {
      navigate('/login');
      return;
    }
    
    const fetchData = async () => {
      setIsLoading(true);
      
      try {
        // First try to get user data from the API
        let userResponse;
        try {
          userResponse = await apiClient.get('/auth/current');
          const formattedUser = {
            ...userResponse.data,
            name: formatUserName(userResponse.data)
          };
          
          setUserData(formattedUser);
          setUserRole(userResponse.data.role);
        } catch (userError) {
          console.warn('Failed to fetch user from API, falling back to token:', userError);
          
          // Fallback to token parsing if API call fails
          const decodedToken = parseJwt(token);
          
          if (!decodedToken) {
            throw new Error('Invalid token');
          }
          
          // Check if token is expired
          const currentTime = Math.floor(Date.now() / 1000);
          if (decodedToken.exp && decodedToken.exp < currentTime) {
            throw new Error('Token expired');
          }
          
          // Format user data from token
          const tokenUserData = {
            id: decodedToken.sub,
            email: decodedToken.email || '',
            firstName: decodedToken.given_name || '',
            lastName: decodedToken.family_name || '',
            name: decodedToken.name || '',
            role: decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 
                  decodedToken.role || 'Guest'
          };
          
          tokenUserData.name = formatUserName(tokenUserData);
          
          setUserData(tokenUserData);
          setUserRole(tokenUserData.role);
        }
        
        // Fetch hotels
        try {
          const hotelsResponse = await apiClient.get('/hotels');
          
          // Add logging to debug API response
          console.log('Raw hotels response:', hotelsResponse);
          
          // Ensure hotels data is an array before processing
          let hotelsData = [];

          if (hotelsResponse && hotelsResponse.data) {
            // Handle case: response.data.$values array (JSON.NET format)
            if (hotelsResponse.data.$values && Array.isArray(hotelsResponse.data.$values)) {
              hotelsData = hotelsResponse.data.$values;
              console.log('Extracted hotels from $values array:', hotelsData);
            }
            // Handle case: response.data is directly an array
            else if (Array.isArray(hotelsResponse.data)) {
              hotelsData = hotelsResponse.data;
            } 
            // Handle case: response.data is a single object
            else if (hotelsResponse.data && typeof hotelsResponse.data === 'object' && hotelsResponse.data.id) {
              hotelsData = [hotelsResponse.data];
            }
            // Handle case: response.data.value is an array (OData format)
            else if (hotelsResponse.data.value && Array.isArray(hotelsResponse.data.value)) {
              hotelsData = hotelsResponse.data.value;
            }
            // Handle case: response.data.items is an array
            else if (hotelsResponse.data.items && Array.isArray(hotelsResponse.data.items)) {
              hotelsData = hotelsResponse.data.items;
            }
            // Handle case: response.data.hotels is an array
            else if (hotelsResponse.data.hotels && Array.isArray(hotelsResponse.data.hotels)) {
              hotelsData = hotelsResponse.data.hotels;
            }
          }
          
          console.log('Processed hotels data:', hotelsData);
          
          // Create a lookup table for hotels by ID - safely
          const hotelLookup = {};
          if (Array.isArray(hotelsData)) {
            hotelsData.forEach(hotel => {
              if (hotel && hotel.id) {
                hotelLookup[hotel.id] = hotel;
                hotelLookup[String(hotel.id)] = hotel;
              }
            });
          }
          
          // Extract all unique locations for filtering - safely
          const locations = Array.isArray(hotelsData) 
            ? [...new Set(hotelsData.map(hotel => hotel.location).filter(Boolean))]
            : [];
          
          setSelectedLocations(locations);
          setHotels(hotelsData || []);
          setFilteredHotels(hotelsData || []);
        } catch (err) {
          console.error('Error fetching hotels:', err);
          setError('Failed to load hotels. Please try again.');
          setHotels([]);
          setFilteredHotels([]);
        }
        
        // Fetch bookings
        try {
          // For managers/admins, get all bookings using the dedicated endpoint
          // Regular users automatically get only their bookings from the standard endpoint
          const bookingsEndpoint = (userRole === 'Manager' || userRole === 'Admin') 
            ? '/bookings/all' 
            : '/bookings';
            
          const bookingsResponse = await apiClient.get(bookingsEndpoint);
          
          if (bookingsResponse.data) {
            let bookingsData = Array.isArray(bookingsResponse.data) 
              ? bookingsResponse.data 
              : (bookingsResponse.data.$values || []);
            
            // Fetch all hotels if we don't have them yet (needed for names)
            let hotelsForNames = hotels;
            if (!hotelsForNames || hotelsForNames.length === 0) {
              try {
                const hotelsResponse = await apiClient.get('/hotels');
                hotelsForNames = Array.isArray(hotelsResponse.data) 
                  ? hotelsResponse.data 
                  : (hotelsResponse.data.$values || []);
                console.log('Fetched hotels for booking names:', hotelsForNames);
              } catch (err) {
                console.warn('Could not fetch hotels for booking names:', err);
              }
            }
            
            // Process bookings to include hotel info
            const processedBookings = bookingsData.map(booking => {
              const hotelId = booking.hotelId;
              
              // Find the matching hotel using string comparison to avoid type issues
              const hotel = hotelsForNames.find(h => String(h.id) === String(hotelId));
              
              // Log if hotel not found to help with debugging
              if (!hotel && hotelId) {
                console.warn(`Hotel with ID ${hotelId} not found for booking`, booking);
              }
              
              return {
                ...booking,
                hotelName: hotel ? hotel.name : `Hotel #${hotelId}`,
                hotel: hotel || null
              };
            });
            
            console.log('Processed bookings with hotel names:', processedBookings);
            setBookings(processedBookings);
            setFilteredBookings(processedBookings);
          } else {
            console.warn('Unexpected bookings response format:', bookingsResponse);
            setBookings([]);
            setFilteredBookings([]);
          }
        } catch (bookingsError) {
          console.error('Error fetching bookings:', bookingsError);
          setBookings([]);
          setFilteredBookings([]);
        }
        
        setError(null);
      } catch (err) {
        console.error('Error fetching data:', err);
        
        // Handle token issues
        if (err.message === 'Invalid token' || err.message === 'Token expired' || 
            (err.response && err.response.status === 401)) {
          localStorage.removeItem('token');
          toast({
            title: 'Session expired',
            description: 'Please log in again to continue.',
            status: 'warning',
            duration: 5000,
            isClosable: true,
          });
          navigate('/login');
          return;
        }
        
        setError('Failed to load data. Please try again.');
      } finally {
        setIsLoading(false);
      }
    };

    fetchData();
  }, [navigate, toast]);

  // Apply hotel filters when criteria change
  useEffect(() => {
    if (!hotels.length) return;
    
    let filtered = [...hotels];
    
    // Apply search term filter
    if (hotelSearchTerm) {
      const searchLower = hotelSearchTerm.toLowerCase();
      filtered = filtered.filter(hotel => 
        hotel.name.toLowerCase().includes(searchLower) ||
        hotel.location.toLowerCase().includes(searchLower) ||
        (hotel.description && hotel.description.toLowerCase().includes(searchLower))
      );
    }
    
    // Apply price range filter if hotels have basePrice
    if (priceRange[0] > 50 || priceRange[1] < 500) {
      filtered = filtered.filter(hotel => {
        const price = hotel.basePrice || 
                     (hotel.roomTypes && hotel.roomTypes[0]?.basePrice) || 
                     200; // fallback price
        return price >= priceRange[0] && price <= priceRange[1];
      });
    }
    
    // Apply star rating filter
    if (selectedStars.length > 0) {
      filtered = filtered.filter(hotel => {
        // Round the rating to the nearest integer for filtering
        const roundedRating = Math.round(hotel.rating);
        return selectedStars.includes(roundedRating.toString());
      });
    }
    
    // Apply location filter
    if (selectedLocations.length > 0 && selectedLocations.length < hotels.length) {
      filtered = filtered.filter(hotel => 
        selectedLocations.includes(hotel.location)
      );
    }
    
    // Apply amenities filter
    if (selectedAmenities.length > 0) {
      filtered = filtered.filter(hotel => {
        // Parse amenities which could be stored as a string or array
        const hotelAmenities = typeof hotel.amenities === 'string' 
          ? hotel.amenities.split(',').map(a => a.trim().toLowerCase())
          : (Array.isArray(hotel.amenities) ? hotel.amenities.map(a => a.toLowerCase()) : []);
        
        // Check if hotel has at least one of the selected amenities
        return selectedAmenities.some(amenity => 
          hotelAmenities.includes(amenity.toLowerCase())
        );
      });
    }
    
    setFilteredHotels(filtered);
  }, [hotels, hotelSearchTerm, priceRange, selectedStars, selectedLocations, selectedAmenities]);

  // Filter bookings based on status and date range
  useEffect(() => {
    if (!bookings.length) {
      setFilteredBookings([]);
      return;
    }
    
    const now = new Date();
    let filtered = [...bookings];
    
    // Apply status filter
    if (bookingFilter === 'upcoming') {
      filtered = bookings.filter(b => new Date(b.checkInDate) > now);
    } else if (bookingFilter === 'active') {
      filtered = bookings.filter(b => 
        new Date(b.checkInDate) <= now && new Date(b.checkOutDate) >= now
      );
    } else if (bookingFilter === 'past') {
      filtered = bookings.filter(b => new Date(b.checkOutDate) < now);
    }
    
    // Apply date range filter
    if (dateRange.from) {
      const fromDate = new Date(dateRange.from);
      filtered = filtered.filter(b => new Date(b.checkInDate) >= fromDate);
    }
    
    if (dateRange.to) {
      const toDate = new Date(dateRange.to);
      filtered = filtered.filter(b => new Date(b.checkOutDate) <= toDate);
    }
    
    // Sort by check-in date, most recent first
    filtered.sort((a, b) => new Date(b.checkInDate) - new Date(a.checkInDate));
    
    setFilteredBookings(filtered);
  }, [bookings, bookingFilter, dateRange]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    navigate('/login');
    toast({
      title: 'Logged out',
      description: 'You have been successfully logged out.',
      status: 'info',
      duration: 3000,
      isClosable: true,
    });
  };
  
  const handleCreateBooking = async (bookingData) => {
    try {
      if (!bookingData.hotelId) {
        toast({
          title: 'Missing Hotel',
          description: 'Please select a hotel for your booking.',
          status: 'error',
          duration: 3000,
          isClosable: true,
        });
        return;
      }
      
      // The backend extracts userId from JWT token, but still include it for clarity
      if (userData && userData.id) {
        bookingData.userId = userData.id;
      }
      
      // Create the booking
      const response = await apiClient.post('/bookings', bookingData);
      
      // Refresh bookings list
      const updatedResponse = await apiClient.get('/bookings');
      
      // Process bookings
      let updatedBookings = Array.isArray(updatedResponse.data) 
        ? updatedResponse.data 
        : (updatedResponse.data.$values || []);
    
      const processedBookings = updatedBookings.map(booking => {
        const hotelId = booking.hotelId;
        const hotel = hotels.find(h => String(h.id) === String(hotelId));
        
        return {
          ...booking,
          hotelName: hotel ? hotel.name : `Hotel #${hotelId}`,
          hotel: hotel || null
        };
      });
      
      setBookings(processedBookings);
      setFilteredBookings(processedBookings);
      
      toast({
        title: 'Booking created',
        description: 'Your booking has been successfully created.',
        status: 'success',
        duration: 5000,
        isClosable: true,
      });
      
      onClose();
    } catch (err) {
      if (err.response && err.response.status === 401) {
        localStorage.removeItem('token');
        toast({
          title: 'Session expired',
          description: 'Please log in again to continue.',
          status: 'warning',
          duration: 5000,
          isClosable: true,
        });
        navigate('/login');
        return;
      }
      
      toast({
        title: 'Error creating booking.',
        description: err.response?.data || 'An error occurred while creating the booking.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    }
  };

  const handleDeleteBooking = async (id) => {
    if (window.confirm('Are you sure you want to cancel this booking?')) {
      try {
        await apiClient.delete(`/bookings/${id}`);
        
        setBookings(bookings.filter(booking => booking.id !== id));
        
        toast({
          title: 'Booking cancelled.',
          description: 'Your booking has been cancelled successfully.',
          status: 'info',
          duration: 5000,
          isClosable: true,
        });
      } catch (err) {
        if (err.response && err.response.status === 401) {
          localStorage.removeItem('token');
          navigate('/login');
          return;
        }
        
        toast({
          title: 'Error cancelling booking.',
          description: err.response?.data || 'An error occurred while cancelling the booking.',
          status: 'error',
          duration: 5000,
          isClosable: true,
        });
      }
    }
  };

  const handleViewHotel = (hotel) => {
    navigate(`/hotels/${hotel.id}`);
  };
  
  const handleBookHotel = (hotel) => {
    setSelectedHotel(hotel);
    onOpen();
  };

  // Clear all filters
  const clearAllFilters = () => {
    setHotelSearchTerm('');
    setPriceRange([50, 500]);
    setSelectedStars([]);
    setSelectedAmenities([]);
    
    // Reset locations to all available locations
    const allLocations = [...new Set(hotels.map(hotel => hotel.location))];
    setSelectedLocations(allLocations);
    
    setDateRange({ from: '', to: '' });
    
    onFilterClose();
  };

  // Compact InfoBar Component - replaces large stats section
  const InfoBar = () => {
    const upcomingCount = bookings.filter(b => new Date(b.checkInDate) > new Date()).length;
    const activeCount = bookings.filter(b => {
      const now = new Date();
      return new Date(b.checkInDate) <= now && new Date(b.checkOutDate) >= now;
    }).length;
    
    return (
      <Flex 
        justify="space-between" 
        align="center" 
        p={3} 
        bg={highlightColor}
        borderRadius="lg"
        mb={6}
        wrap="wrap"
      >
        <HStack spacing={6} wrap="wrap">
          <HStack>
            <Icon as={FiHome} color="brand.500" />
            <Text fontSize="sm">
              <Text as="span" fontWeight="bold">{hotels.length}</Text> Hotels Available
            </Text>
          </HStack>
          
          <HStack>
            <Icon as={FiCalendar} color="green.500" />
            <Text fontSize="sm">
              <Text as="span" fontWeight="bold">{activeCount}</Text> Active Bookings
            </Text>
          </HStack>
          
          <HStack>
            <Icon as={FiClock} color="blue.500" />
            <Text fontSize="sm">
              <Text as="span" fontWeight="bold">{upcomingCount}</Text> Upcoming
            </Text>
          </HStack>
        </HStack>
        
        <HStack spacing={2}>
          <Button 
            size="sm" 
            leftIcon={<FiPlus />} 
            colorScheme="brand"
            onClick={onOpen}
          >
            New Booking
          </Button>
        </HStack>
      </Flex>
    );
  };

  // Account Menu Component - streamlined version
  const AccountMenu = () => {
    const displayName = userData?.name || 'User';
    
    return (
      <Flex justify="flex-end" mb={6} align="center">
        {userData && (
          <Menu>
            <MenuButton
              as={Button}
              rightIcon={<FiChevronDown />}
              variant="ghost"
              _hover={{ bg: 'gray.100' }}
            >
              <HStack spacing={2}>
                <Avatar 
                  size="sm" 
                  name={displayName}
                  bg="brand.500"
                  color="white"
                >
                  {userRole === 'Manager' && <AvatarBadge boxSize="1.25em" bg="green.500" />}
                </Avatar>
                <Text fontWeight="medium">{displayName}</Text>
              </HStack>
            </MenuButton>
            <MenuList zIndex={100}>
              <MenuItem icon={<FiUser />} onClick={() => navigate('/profile')}>
                My Profile
              </MenuItem>
              <MenuItem icon={<FiSettings />} onClick={() => navigate('/account-settings')}>
                Account Settings
              </MenuItem>
              <MenuDivider />
              {(userRole === 'Manager' || userRole === 'Admin') && (
                <>
                  <MenuItem icon={<FiHome />} onClick={() => navigate('/hotels/create')}>
                    Add New Hotel
                  </MenuItem>
                  {/* <MenuItem icon={<FiCamera />} onClick={() => navigate('/photos/manage')}>
                    Manage Photos
                  </MenuItem> */}
                  <MenuItem icon={<FiEdit />} onClick={() => navigate('/hotels/manage')}>
                    Manage Hotels
                  </MenuItem>
                  <MenuDivider />
                </>
              )}
              <MenuItem icon={<FiLogOut />} onClick={handleLogout}>
                Logout
              </MenuItem>
            </MenuList>
          </Menu>
        )}
      </Flex>
    );
  };

  // Hotel Card Component - grid view
  const HotelCard = ({ hotel }) => {
    const [mainPhoto, setMainPhoto] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    
    useEffect(() => {
      const fetchMainPhoto = async () => {
        try {
          setIsLoading(true);
          console.log(`Fetching photos for hotel ${hotel.id}`);
          
          const response = await apiClient.get(`/hotelPhotos/hotel/${hotel.id}`);
          
          // Debug the actual response
          console.log(`Photo response for hotel ${hotel.id}:`, response.data);
          
          // Handle different possible response structures
          let photoArray = response.data;
          
          // Check if it's already an array
          if (Array.isArray(photoArray) && photoArray.length > 0) {
            console.log(`Found array of ${photoArray.length} photos`);
            // Find the main photo or use the first one
            const main = photoArray.find(photo => photo.isMain === true) || photoArray[0];
            console.log('Main photo:', main);
            
            // Check both url and imageUrl properties
            if (main.url) {
              setMainPhoto(main.url);
            } else if (main.imageUrl) {
              setMainPhoto(main.imageUrl);
            } else if (main.publicId) {
              // Some Cloudinary implementations use publicId 
              setMainPhoto(`https://res.cloudinary.com/your-cloud-name/image/upload/${main.publicId}`);
            }
          } 
          // If it's in a $values array (common .NET serialization)
          else if (photoArray && photoArray.$values && Array.isArray(photoArray.$values) && photoArray.$values.length > 0) {
            console.log(`Found $values array with ${photoArray.$values.length} photos`);
            const main = photoArray.$values.find(photo => photo.isMain === true) || photoArray.$values[0];
            
            if (main.url) {
              setMainPhoto(main.url);
            } else if (main.imageUrl) {
              setMainPhoto(main.imageUrl);
            }
          }
          // If it's a single object instead of an array
          else if (photoArray && !Array.isArray(photoArray) && typeof photoArray === 'object') {
            console.log('Found single photo object');
            if (photoArray.url) {
              setMainPhoto(photoArray.url);
            } else if (photoArray.imageUrl) {
              setMainPhoto(photoArray.imageUrl);
            }
          }
          // If it's empty or null
          else {
            console.log(`No photos found for hotel ${hotel.id}`);
          }
        } catch (error) {
          console.error(`Error fetching photos for hotel ${hotel.id}:`, error);
        } finally {
          setIsLoading(false);
        }
      };
      
      fetchMainPhoto();
    }, [hotel.id]);
    
    return (
      <Card 
        maxW="sm" 
        overflow="hidden" 
        variant="outline" 
        bg={bg} 
        transition="all 0.2s"
        _hover={{ transform: 'translateY(-4px)', shadow: 'md', bg: cardHoverBg }}
        cursor="pointer"
        onClick={() => handleViewHotel(hotel)}
      >
        <Box position="relative" height="180px">
          {isLoading ? (
            <Flex height="100%" justify="center" align="center" bg="gray.100">
              <Spinner size="md" color="brand.500" />
            </Flex>
          ) : mainPhoto ? (
            <Image
              src={mainPhoto}
              alt={hotel.name}
              objectFit="cover"
              w="100%"
              h="100%"
            />
          ) : (
            <Flex 
              bg="gray.100" 
              color="gray.500" 
              align="center" 
              justify="center"
              height="100%"
            >
              <FiCamera size="32px" />
            </Flex>
          )}
          <Badge
            position="absolute"
            top="10px"
            right="10px"
            colorScheme={hotel.rating >= 4.5 ? "green" : hotel.rating >= 3.5 ? "yellow" : "red"}
            fontSize="0.8em"
            p="5px 10px"
            borderRadius="full"
          >
            {hotel.rating} <FiStar style={{ display: 'inline' }} />
          </Badge>
          
          <HStack 
            position="absolute" 
            bottom="10px" 
            left="10px" 
            spacing={1}
          >
            {parseAmenities(hotel.amenities).slice(0, 3).map((amenity, i) => (
              <Tag size="sm" key={i} bg="blackAlpha.700" color="white" fontSize="xs">
                {amenity}
              </Tag>
            ))}
            {parseAmenities(hotel.amenities).length > 3 && (
              <Tag size="sm" bg="blackAlpha.700" color="white" fontSize="xs">
                +more
              </Tag>
            )}
          </HStack>
        </Box>
        
        <CardBody py={3} px={4}>
          <HStack justify="space-between" mb={1}>
            <Heading size="md" noOfLines={1}>{hotel.name}</Heading>
            <Text fontWeight="bold" color="brand.500">
              ${hotel.basePrice || 199}
            </Text>
          </HStack>
          
          <HStack spacing={2} mb={2} color={subtleColor}>
            <FiMapPin size="14px" />
            <Text fontSize="sm" noOfLines={1}>{hotel.location}</Text>
          </HStack>
          
          <Text fontSize="sm" noOfLines={2} color="gray.600" mb={3}>
            {hotel.description || 'Experience a comfortable stay at this hotel.'}
          </Text>
        </CardBody>
        
        <CardFooter pt={0} pb={3} px={4}>
          <HStack spacing={2} width="full">
            <Button 
              size="sm"
              rightIcon={<FiCalendar />} 
              colorScheme="brand" 
              flex="1"
              onClick={(e) => {
                e.stopPropagation();
                handleBookHotel(hotel);
              }}
            >
              Book Now
            </Button>
          </HStack>
        </CardFooter>
      </Card>
    );
  };

  // Hotel List Item Component - list view
  const HotelListItem = ({ hotel }) => {
    const [mainPhoto, setMainPhoto] = useState(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
      const fetchMainPhoto = async () => {
        try {
          setIsLoading(true);
          console.log(`Fetching photos for hotel ${hotel.id}`);
          
          const response = await apiClient.get(`/hotelPhotos/hotel/${hotel.id}`);
          
          // Debug the actual response
          console.log(`Photo response for hotel ${hotel.id}:`, response.data);
          
          // Handle different possible response structures
          let photoArray = response.data;
          
          // Check if it's already an array
          if (Array.isArray(photoArray) && photoArray.length > 0) {
            console.log(`Found array of ${photoArray.length} photos`);
            // Find the main photo or use the first one
            const main = photoArray.find(photo => photo.isMain === true) || photoArray[0];
            console.log('Main photo:', main);
            
            // Check both url and imageUrl properties
            if (main.url) {
              setMainPhoto(main.url);
            } else if (main.imageUrl) {
              setMainPhoto(main.imageUrl);
            } else if (main.publicId) {
              // Some Cloudinary implementations use publicId 
              setMainPhoto(`https://res.cloudinary.com/your-cloud-name/image/upload/${main.publicId}`);
            }
          } 
          // If it's in a $values array (common .NET serialization)
          else if (photoArray && photoArray.$values && Array.isArray(photoArray.$values) && photoArray.$values.length > 0) {
            console.log(`Found $values array with ${photoArray.$values.length} photos`);
            const main = photoArray.$values.find(photo => photo.isMain === true) || photoArray.$values[0];
            
            if (main.url) {
              setMainPhoto(main.url);
            } else if (main.imageUrl) {
              setMainPhoto(main.imageUrl);
            }
          }
          // If it's a single object instead of an array
          else if (photoArray && !Array.isArray(photoArray) && typeof photoArray === 'object') {
            console.log('Found single photo object');
            if (photoArray.url) {
              setMainPhoto(photoArray.url);
            } else if (photoArray.imageUrl) {
              setMainPhoto(photoArray.imageUrl);
            }
          }
          // If it's empty or null
          else {
            console.log(`No photos found for hotel ${hotel.id}`);
          }
        } catch (error) {
          console.error(`Error fetching photos for hotel ${hotel.id}:`, error);
        } finally {
          setIsLoading(false);
        }
      };

      fetchMainPhoto();
    }, [hotel.id]);

    return (
      <Card 
        direction="row" 
        overflow="hidden"
        variant="outline"
        bg={bg}
        mb={4}
        transition="all 0.2s"
        _hover={{ shadow: 'md', bg: cardHoverBg }}
        cursor="pointer"
        onClick={() => handleViewHotel(hotel)}
      >
        <Box width="180px" height="130px" flexShrink={0}>
          {isLoading ? (
            <Flex height="100%" justify="center" align="center" bg="gray.100">
              <Spinner size="md" color="brand.500" />
            </Flex>
          ) : mainPhoto ? (
            <Image
              src={mainPhoto}
              alt={hotel.name}
              objectFit="cover"
              height="100%"
              width="100%"
            />
          ) : (
            <Flex 
              bg="gray.100" 
              color="gray.500" 
              align="center" 
              justify="center"
              height="100%"
            >
              <FiCamera size="32px" />
            </Flex>
          )}
        </Box>
        
        <CardBody py={3} px={4}>
          <Flex justify="space-between" align="start">
            <Box>
              <HStack mb={1}>
                <Heading size="md">{hotel.name}</Heading>
                <Badge 
                  colorScheme={hotel.rating >= 4.5 ? "green" : hotel.rating >= 3.5 ? "yellow" : "red"}
                >
                  {hotel.rating} <FiStar style={{ display: 'inline', fontSize: '12px' }} />
                </Badge>
              </HStack>
              
              <HStack spacing={2} mb={2} color={subtleColor}>
                <FiMapPin size="14px" />
                <Text fontSize="sm">{hotel.location}</Text>
              </HStack>
              
              <Text fontSize="sm" noOfLines={2} color="gray.600" mb={2}>
                {hotel.description || 'Experience a comfortable stay at this hotel.'}
              </Text>
              
              <HStack spacing={1} wrap="wrap">
                {parseAmenities(hotel.amenities).slice(0, 4).map((amenity, i) => (
                  <Tag size="sm" key={i} bg={tagBg} fontSize="xs">
                    {amenity}
                  </Tag>
                ))}
              </HStack>
            </Box>
            
            <VStack align="flex-end" spacing={1}>
              <Text fontWeight="bold" color="brand.500" fontSize="lg">
                ${hotel.basePrice || 199}
              </Text>
              <Text fontSize="xs" color={subtleColor}>per night</Text>
              
              <Button 
                size="sm"
                colorScheme="brand" 
                rightIcon={<FiCalendar />}
                mt={2}
                onClick={(e) => {
                  e.stopPropagation();
                  handleBookHotel(hotel);
                }}
              >
                Book Now
              </Button>
            </VStack>
          </Flex>
        </CardBody>
      </Card>
    );
  };

  // Booking Table Component - simplified
  const BookingsTable = () => {
    return (
      <TableContainer>
        <Table variant="simple" size="sm">
          <Thead>
            <Tr>
              <Th>Hotel</Th>
              <Th>Dates</Th>
              <Th>Guests</Th>
              <Th>Status</Th>
              <Th>Actions</Th>
            </Tr>
          </Thead>
          <Tbody>
            {filteredBookings.map(booking => {
              const hotelId = booking.hotelId || booking.hotel_id;
              let hotelName = booking.hotelName;
              
              if (!hotelName || hotelName === 'Unknown Hotel') {
                const hotel = booking.hotel || 
                              hotels.find(h => String(h.id) === String(hotelId));
                
                hotelName = hotel ? hotel.name : (hotelId ? `Hotel #${hotelId}` : 'Unknown Hotel');
              }
              
              const now = new Date();
              const checkInDate = new Date(booking.checkInDate);
              const checkOutDate = new Date(booking.checkOutDate);
              
              let status = 'Upcoming';
              let statusColor = 'blue';
              
              if (checkInDate <= now && checkOutDate >= now) {
                status = 'Active';
                statusColor = 'green';
              } else if (checkOutDate < now) {
                status = 'Completed';
                statusColor = 'gray';
              }
              
              return (
                <Tr key={booking.id} _hover={{ bg: highlightColor }}>
                  <Td fontWeight="medium">
                    <Text 
                      as="span" 
                      cursor="pointer" 
                      color="brand.600" 
                      _hover={{ textDecoration: 'underline' }}
                      onClick={() => navigate(`/hotels/${hotelId}`)}
                    >
                      {hotelName}
                    </Text>
                  </Td>
                  <Td>
                    <VStack align="start" spacing={0}>
                      <Text fontSize="xs">{checkInDate.toLocaleDateString()} - </Text>
                      <Text fontSize="xs">{checkOutDate.toLocaleDateString()}</Text>
                    </VStack>
                  </Td>
                  <Td>{booking.guestCount || booking.numberOfGuests}</Td>
                  <Td>
                    <Badge colorScheme={statusColor}>{status}</Badge>
                  </Td>
                  <Td>
                    <HStack spacing={1}>
                      {status !== 'Completed' && (
                        <Tooltip label="Cancel booking">
                          <IconButton
                            aria-label="Cancel booking"
                            icon={<FiTrash2 />}
                            size="sm"
                            variant="ghost"
                            colorScheme="red"
                            onClick={() => handleDeleteBooking(booking.id)}
                          />
                        </Tooltip>
                      )}
                      <Tooltip label="View details">
                        <IconButton
                          aria-label="View details"
                          icon={<FiEye />}
                          size="sm"
                          variant="ghost"
                          onClick={() => navigate(`/hotels/${hotelId}`)}
                        />
                      </Tooltip>
                    </HStack>
                  </Td>
                </Tr>
              );
            })}
          </Tbody>
        </Table>
        
        {filteredBookings.length === 0 && (
          <Box textAlign="center" py={8}>
            <FiCalendar size="48px" color="gray" style={{ margin: '0 auto 16px' }} />
            <Heading size="md" mb={2}>No bookings found</Heading>
            <Text mb={4} color={subtleColor}>
              {bookingFilter === 'all' 
                ? "You haven't made any bookings yet."
                : `You don't have any ${bookingFilter} bookings.`}
            </Text>
            <Button 
              colorScheme="brand"
              leftIcon={<FiPlus />}
              onClick={onOpen}
            >
              Create a new booking
            </Button>
          </Box>
        )}
      </TableContainer>
    );
  };

  // Booking Form Component - simplified
  const BookingForm = () => {
    const [formData, setFormData] = useState({
      hotelId: selectedHotel ? selectedHotel.id : '',
      userId: userData?.id,
      checkInDate: '',
      checkOutDate: '',
      guestCount: 1,
      specialRequests: ''
    });

    const handleChange = (e) => {
      const { name, value } = e.target;
      setFormData({
        ...formData,
        [name]: value
      });
    };

    const handleSubmit = (e) => {
      e.preventDefault();
      handleCreateBooking(formData);
    };

    const today = new Date().toISOString().split('T')[0];
    const checkoutMinDate = formData.checkInDate || today;

    return (
      <form onSubmit={handleSubmit}>
        <ModalBody>
          <FormControl mb={4} isRequired>
            <FormLabel>Hotel</FormLabel>
            <Select
              name="hotelId"
              value={formData.hotelId}
              onChange={handleChange}
              placeholder="Select a hotel"
              isDisabled={!!selectedHotel}
            >
              {hotels.map(hotel => (
                <option key={hotel.id} value={hotel.id}>
                  {hotel.name}
                </option>
              ))}
            </Select>
          </FormControl>

          <Grid templateColumns="repeat(2, 1fr)" gap={4}>
            <GridItem>
              <FormControl isRequired>
                <FormLabel>Check-in Date</FormLabel>
                <Input
                  name="checkInDate"
                  type="date"
                  min={today}
                  value={formData.checkInDate}
                  onChange={handleChange}
                />
              </FormControl>
            </GridItem>
            <GridItem>
              <FormControl isRequired>
                <FormLabel>Check-out Date</FormLabel>
                <Input
                  name="checkOutDate"
                  type="date"
                  min={checkoutMinDate}
                  value={formData.checkOutDate}
                  onChange={handleChange}
                />
              </FormControl>
            </GridItem>
          </Grid>

          <FormControl mt={4} isRequired>
            <FormLabel>Number of Guests</FormLabel>
            <Input
              name="guestCount"
              type="number"
              min="1"
              max="10"
              value={formData.guestCount}
              onChange={handleChange}
            />
          </FormControl>

          <FormControl mt={4}>
            <FormLabel>Special Requests</FormLabel>
            <Textarea
              name="specialRequests"
              placeholder="Any special requirements or requests..."
              value={formData.specialRequests}
              onChange={handleChange}
              resize="vertical"
              rows={3}
            />
          </FormControl>
        </ModalBody>

        <ModalFooter>
          <Button mr={3} onClick={onClose} variant="ghost">
            Cancel
          </Button>
          <Button type="submit" colorScheme="brand">
            Book Now
          </Button>
        </ModalFooter>
      </form>
    );
  };

  // Filter Drawer/Panel Component
  const FilterPanel = () => {
    // Get all unique locations from hotels
    const locations = [...new Set(hotels.map(hotel => hotel.location))];
    
    // Improved amenities extraction with debugging
    const allAmenities = new Set();
    console.log("Extracting amenities from", hotels.length, "hotels");
    
    hotels.forEach((hotel, index) => {
      console.log(`Hotel ${index+1} (${hotel.name}) amenities:`, hotel.amenities);
      
      // Handle string format
      if (typeof hotel.amenities === 'string') {
        hotel.amenities.split(',').forEach(amenity => {
          const trimmed = amenity.trim();
          if (trimmed) allAmenities.add(trimmed);
        });
      } 
      // Handle array format
      else if (Array.isArray(hotel.amenities)) {
        hotel.amenities.forEach(amenity => {
          if (amenity) allAmenities.add(amenity);
        });
      }
      // Handle object with $values array (common .NET format)
      else if (hotel.amenities && hotel.amenities.$values && Array.isArray(hotel.amenities.$values)) {
        hotel.amenities.$values.forEach(amenity => {
          if (amenity) allAmenities.add(amenity);
        });
      }
      // Handle nested amenities property
      else if (hotel.amenities && typeof hotel.amenities === 'object') {
        Object.values(hotel.amenities).forEach(amenity => {
          if (amenity && typeof amenity === 'string') allAmenities.add(amenity);
        });
      }
    });
    
    console.log("Extracted amenities:", Array.from(allAmenities));
    
    return (
      <DrawerContent>
        <DrawerCloseButton />
        <DrawerHeader borderBottomWidth="1px">Filter Hotels</DrawerHeader>

        <DrawerBody>
          <VStack spacing={6} align="stretch">
            <Box>
              <Heading size="sm" mb={3}>Price Range (per night)</Heading>
              <HStack mb={2}>
                <Text>${priceRange[0]}</Text>
                <Spacer />
                <Text>${priceRange[1]}</Text>
              </HStack>
              <RangeSlider
                min={50}
                max={500}
                step={10}
                value={priceRange}
                onChange={setPriceRange}
                colorScheme="brand"
              >
                <RangeSliderTrack>
                  <RangeSliderFilledTrack />
                </RangeSliderTrack>
                <RangeSliderThumb index={0} boxSize={6}>
                  <Box color="brand.500" as={FiDollarSign} />
                </RangeSliderThumb>
                <RangeSliderThumb index={1} boxSize={6}>
                  <Box color="brand.500" as={FiDollarSign} />
                </RangeSliderThumb>
              </RangeSlider>
            </Box>
            
            <Divider />
            
            <Box>
              <Heading size="sm" mb={3}>Star Rating</Heading>
              <CheckboxGroup 
                value={selectedStars} 
                onChange={setSelectedStars}
                colorScheme="brand"
              >
                <SimpleGrid columns={3} spacing={2}>
                  {[5, 4, 3, 2, 1].map(star => (
                    <Checkbox key={star} value={star.toString()}>
                      <HStack>
                        <Text>{star}</Text>
                        <FiStar />
                      </HStack>
                    </Checkbox>
                  ))}
                </SimpleGrid>
              </CheckboxGroup>
            </Box>
            
            <Divider />
            
            <Box>
              <Heading size="sm" mb={3}>Location</Heading>
              <VStack align="start" maxHeight="150px" overflowY="auto" spacing={1}>
                {locations.map(location => (
                  <Checkbox 
                    key={location} 
                    isChecked={selectedLocations.includes(location)}
                    onChange={(e) => {
                      if (e.target.checked) {
                        setSelectedLocations([...selectedLocations, location]);
                      } else {
                        setSelectedLocations(selectedLocations.filter(loc => loc !== location));
                      }
                    }}
                    colorScheme="brand"
                  >
                    {location}
                  </Checkbox>
                ))}
              </VStack>
            </Box>
            
            <Divider />
            
            <Box>
              <Heading size="sm" mb={3}>Amenities</Heading>
              
              {/* Debug output to check amenities extraction */}
              <Text fontSize="xs" color="gray.500" mb={2}>
                Found {Array.from(allAmenities).length} amenities
              </Text>
              
              {Array.from(allAmenities).length === 0 ? (
                <Text fontSize="sm" color="gray.500">No amenities found</Text>
              ) : (
                <VStack align="start" maxHeight="150px" overflowY="auto" spacing={1}>
                  {Array.from(allAmenities).map((amenity, index) => (
                    <Checkbox 
                      key={index}
                      isChecked={selectedAmenities.includes(amenity)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedAmenities([...selectedAmenities, amenity]);
                        } else {
                          setSelectedAmenities(selectedAmenities.filter(a => a !== amenity));
                        }
                      }}
                      colorScheme="brand"
                    >
                      {amenity || "Unnamed amenity"}
                    </Checkbox>
                  ))}
                </VStack>
              )}
            </Box>
          </VStack>
        </DrawerBody>

        <DrawerFooter borderTopWidth="1px">
          <Button 
            variant="outline" 
            mr={3} 
            onClick={clearAllFilters}
            leftIcon={<FiSlash />}
          >
            Clear All
          </Button>
          <Button 
            colorScheme="brand" 
            onClick={onFilterClose}
            rightIcon={<FiCheck />}
          >
            Apply Filters
          </Button>
        </DrawerFooter>
      </DrawerContent>
    );
  };

  // Mobile filters panel (collapsible)
  const MobileFilters = () => {
    return (
      <Collapse in={isMobileFilterVisible} animateOpacity>
        <Box 
          p={4} 
          bg={bg} 
          borderWidth="1px" 
          borderRadius="md" 
          borderColor={borderColor}
          mb={4}
        >
          <VStack spacing={4} align="stretch">
            <Box>
              <Heading size="xs" mb={2}>Price Range</Heading>
              <HStack mb={1} fontSize="sm">
                <Text>${priceRange[0]}</Text>
                <Spacer />
                <Text>${priceRange[1]}</Text>
              </HStack>
              <RangeSlider
                min={50}
                max={500}
                step={10}
                value={priceRange}
                onChange={setPriceRange}
                colorScheme="brand"
                size="sm"
              >
                <RangeSliderTrack>
                  <RangeSliderFilledTrack />
                </RangeSliderTrack>
                <RangeSliderThumb index={0} />
                <RangeSliderThumb index={1} />
              </RangeSlider>
            </Box>
            
            <Box>
              <Heading size="xs" mb={2}>Star Rating</Heading>
              <HStack spacing={2}>
                {[5, 4, 3, 2, 1].map(star => (
                  <Button
                    key={star}
                    size="xs"
                    variant={selectedStars.includes(star.toString()) ? "solid" : "outline"}
                    colorScheme={selectedStars.includes(star.toString()) ? "brand" : "gray"}
                    onClick={() => {
                      if (selectedStars.includes(star.toString())) {
                        setSelectedStars(selectedStars.filter(s => s !== star.toString()));
                      } else {
                        setSelectedStars([...selectedStars, star.toString()]);
                      }
                    }}
                  >
                    {star} <FiStar style={{ marginLeft: '2px' }} />
                  </Button>
                ))}
              </HStack>
            </Box>
            
            <HStack justify="space-between">
              <Button 
                size="sm" 
                variant="outline" 
                leftIcon={<FiSlash />}
                onClick={clearAllFilters}
              >
                Clear All
              </Button>
              <Button 
                size="sm"
                colorScheme="brand" 
                rightIcon={<FiCheck />}
                onClick={() => setIsMobileFilterVisible(false)}
              >
                Apply Filters
              </Button>
            </HStack>
          </VStack>
        </Box>
      </Collapse>
    );
  };

  // Loading state
  if (isLoading) {
    return (
      <Flex direction="column" justify="center" align="center" height="100vh">
        <Spinner size="xl" thickness="4px" speed="0.65s" color="brand.500" mb={4} />
        <Heading size="md">Loading your dashboard</Heading>
        <Progress isIndeterminate size="xs" colorScheme="brand" width="200px" mt={4} />
      </Flex>
    );
  }

  // Error state
  if (error) {
    return (
      <Box textAlign="center" py={10} px={6}>
        <Heading as="h2" size="xl" mt={6} mb={2}>
          Error Loading Data
        </Heading>
        <Text mb={6}>{error}</Text>
        <Button
          colorScheme="brand"
          onClick={() => window.location.reload()}
          leftIcon={<FiRefreshCw />}
        >
          Try Again
        </Button>
      </Box>
    );
  }

  return (
    <Container maxW="container.xl" py={6}>
      {/* User Account Menu */}
      <AccountMenu />
      
      <Box mb={8}>
        <Heading as="h1" size="xl" mb={2}>
          Hotel Explorer
        </Heading>
        <Text color={subtleColor}>Find and book your perfect stay</Text>
      </Box>
      
      {/* Compact Info Bar */}
      <InfoBar />
      
      {/* Main Content Tabs */}
      <Tabs colorScheme="brand" variant="line" isLazy>
        <TabList>
          <Tab>Browse Hotels</Tab>
          <Tab>Your Bookings</Tab>
        </TabList>
        <TabIndicator height="3px" bg="brand.500" borderRadius="1px" />
        
        <TabPanels>
          {/* Hotels Tab */}
          <TabPanel px={0}>
            <Flex 
              mb={4} 
              justify="space-between" 
              align="center" 
              wrap={{ base: "wrap", md: "nowrap" }}
              gap={2}
            >
              <InputGroup maxW={{ base: "100%", md: "400px" }} mb={{ base: 2, md: 0 }}>
                <InputLeftElement pointerEvents="none">
                  <FiSearch color="gray.300" />
                </InputLeftElement>
                <Input 
                  placeholder="Search hotels by name or location..." 
                  value={hotelSearchTerm}
                  onChange={(e) => setHotelSearchTerm(e.target.value)}
                  bg={bg}
                />
                {hotelSearchTerm && (
                  <InputRightElement>
                    <IconButton
                      aria-label="Clear search"
                      icon={<FiX />}
                      size="sm"
                      variant="ghost"
                      onClick={() => setHotelSearchTerm('')}
                    />
                  </InputRightElement>
                )}
              </InputGroup>
              
              <HStack spacing={2}>
                {/* Active filters count badge */}
                {(selectedStars.length > 0 || 
                  selectedAmenities.length > 0 || 
                  priceRange[0] > 50 || 
                  priceRange[1] < 500 || 
                  selectedLocations.length < hotels.length) && (
                  <Badge colorScheme="brand" fontSize="0.8em" borderRadius="full" px={2}>
                    {selectedStars.length + selectedAmenities.length + 
                     (priceRange[0] > 50 || priceRange[1] < 500 ? 1 : 0) + 
                     (selectedLocations.length < hotels.length ? 1 : 0)} filters
                  </Badge>
                )}
                
                {/* Mobile filter toggle */}
                <Button 
                  display={{ base: "flex", md: "none" }}
                  size="sm"
                  leftIcon={<FiFilter />}
                  onClick={() => setIsMobileFilterVisible(!isMobileFilterVisible)}
                  variant={isMobileFilterVisible ? "solid" : "outline"}
                  colorScheme={isMobileFilterVisible ? "brand" : "gray"}
                >
                  Filter
                </Button>
                
                {/* Desktop filter button */}
                <Button 
                  display={{ base: "none", md: "flex" }}
                  leftIcon={<FiFilter />}
                  onClick={onFilterOpen}
                  variant="outline"
                >
                  Filter
                </Button>
                
                {/* View type toggle */}
                <ButtonGroup size="sm" isAttached variant="outline" display={{ base: "none", md: "flex" }}>
                  <IconButton
                    aria-label="Grid view"
                    icon={<FiGrid />}
                    isActive={viewType === 'grid'}
                    onClick={() => setViewType('grid')}
                  />
                  <IconButton
                    aria-label="List view"
                    icon={<FiList />}
                    isActive={viewType === 'list'}
                    onClick={() => setViewType('list')}
                  />
                </ButtonGroup>
              </HStack>
            </Flex>
            
            {/* Mobile filters (collapsible) */}
            <MobileFilters />
            
            {/* Display count of results */}
            <HStack mb={4} justify="space-between">
              <Text fontSize="sm" color={subtleColor}>
                Showing {filteredHotels.length} of {hotels.length} hotels
              </Text>
              
              {/* Clear filters button (only show if filters are active) */}
              {(selectedStars.length > 0 || 
                selectedAmenities.length > 0 || 
                priceRange[0] > 50 || 
                priceRange[1] < 500 || 
                selectedLocations.length < hotels.length) && (
                <Button 
                  size="xs" 
                  variant="ghost" 
                  colorScheme="red"
                  leftIcon={<FiSlash />}
                  onClick={clearAllFilters}
                >
                  Clear Filters
                </Button>
              )}
            </HStack>
            
            {/* Hotel Grid/List View */}
            {viewType === 'grid' ? (
              <SimpleGrid columns={{ base: 1, sm: 2, md: 3, lg: 4 }} spacing={6}>
                {filteredHotels.map(hotel => (
                  <HotelCard key={hotel.id} hotel={hotel} />
                ))}
              </SimpleGrid>
            ) : (
              <VStack spacing={4} align="stretch">
                {filteredHotels.map(hotel => (
                  <HotelListItem key={hotel.id} hotel={hotel} />
                ))}
              </VStack>
            )}
            
            {/* Empty state for hotels */}
            {filteredHotels.length === 0 && (
              <Box textAlign="center" py={10}>
                <FiHome size="48px" color="gray" style={{ margin: '0 auto 16px' }} />
                <Heading size="md" mb={2}>No hotels found</Heading>
                <Text mb={4} color={subtleColor}>
                  {hotels.length === 0 
                    ? "There are currently no hotels in the system." 
                    : "No hotels match your search criteria."}
                </Text>
                <Button 
                  onClick={clearAllFilters}
                  leftIcon={<FiSlash />}
                >
                  Clear All Filters
                </Button>
              </Box>
            )}
          </TabPanel>
          
          {/* Bookings Tab */}
          <TabPanel px={0}>
            <Flex mb={4} justify="space-between" align="center" wrap={{ base: "wrap", md: "nowrap" }} gap={2}>
              <HStack>
                <Text fontWeight="medium">Filter:</Text>
                <Select
                  size="sm"
                  width="150px"
                  value={bookingFilter}
                  onChange={(e) => setBookingFilter(e.target.value)}
                >
                  <option value="all">All Bookings</option>
                  <option value="upcoming">Upcoming</option>
                  <option value="active">Active</option>
                  <option value="past">Past</option>
                </Select>
              </HStack>
              
              <HStack>
                {/* Date range filter for bookings */}
                <HStack spacing={1} display={{ base: "none", md: "flex" }}>
                  <Input
                    type="date"
                    size="sm"
                    placeholder="From"
                    value={dateRange.from}
                    onChange={(e) => setDateRange({...dateRange, from: e.target.value})}
                    w="130px"
                  />
                  <Text>to</Text>
                  <Input
                    type="date"
                    size="sm"
                    placeholder="To"
                    value={dateRange.to}
                    onChange={(e) => setDateRange({...dateRange, to: e.target.value})}
                    w="130px"
                  />
                </HStack>
                
                <Button 
                  leftIcon={<FiPlus />} 
                  colorScheme="brand"
                  size="sm"
                  onClick={onOpen}
                >
                  New Booking
                </Button>
              </HStack>
            </Flex>
            
            {/* Mobile date filter (collapsible) */}
            <Collapse in={isMobileFilterVisible} animateOpacity>
              <Box 
                p={4} 
                bg={bg} 
                borderWidth="1px" 
                borderRadius="md" 
                borderColor={borderColor}
                mb={4}
                display={{ base: "block", md: "none" }}
              >
                <Heading size="xs" mb={2}>Date Range</Heading>
                <SimpleGrid columns={2} spacing={2}>
                  <FormControl>
                    <FormLabel fontSize="xs">From</FormLabel>
                    <Input
                      type="date"
                      size="sm"
                      value={dateRange.from}
                      onChange={(e) => setDateRange({...dateRange, from: e.target.value})}
                    />
                  </FormControl>
                  <FormControl>
                    <FormLabel fontSize="xs">To</FormLabel>
                    <Input
                      type="date"
                      size="sm"
                      value={dateRange.to}
                      onChange={(e) => setDateRange({...dateRange, to: e.target.value})}
                    />
                  </FormControl>
                </SimpleGrid>
              </Box>
            </Collapse>
            
            {/* Display count of results for bookings */}
            <Text fontSize="sm" color={subtleColor} mb={4}>
              Showing {filteredBookings.length} of {bookings.length} bookings
            </Text>
            
            {/* Bookings Table */}
            <Card variant="outline" bg={bg} mb={6}>
              <CardBody p={0}>
                <BookingsTable />
              </CardBody>
            </Card>
          </TabPanel>
        </TabPanels>
      </Tabs>
      
      {/* Booking Modal */}
      <Modal isOpen={isOpen} onClose={onClose} size="lg">
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>
            {selectedHotel 
              ? `Book ${selectedHotel.name}` 
              : 'Create New Booking'}
          </ModalHeader>
          <ModalCloseButton />
          <BookingForm />
        </ModalContent>
      </Modal>
      
      {/* Filter Drawer */}
      <Drawer
        isOpen={isFilterOpen}
        placement="right"
        onClose={onFilterClose}
        finalFocusRef={filterDrawerRef}
        size="md"
      >
        <DrawerOverlay />
        <FilterPanel />
      </Drawer>
    </Container>
  );
}

export default Dashboard;