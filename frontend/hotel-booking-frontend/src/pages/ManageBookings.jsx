// src/pages/ManageBookings.jsx
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Container,
  Flex,
  Heading,
  Icon,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  IconButton,
  Badge,
  Input,
  InputGroup,
  InputLeftElement,
  Select,
  Tabs,
  TabList,
  Tab,
  TabPanels,
  TabPanel,
  useToast,
  Spinner,
  Card,
  CardBody,
  useColorModeValue,
  useDisclosure,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalCloseButton,
  ModalBody,
  ModalFooter,
  Text,
  HStack,
  VStack,
  FormControl,
  FormLabel,
  Stack,
} from '@chakra-ui/react';
import {
  FiArrowLeft,
  FiEdit,
  FiEye,
  FiFilter,
  FiMoreVertical,
  FiSearch,
  FiTrash2,
  FiCheckCircle,
  FiXCircle,
  FiCalendar,
  FiUser,
  FiHome,
} from 'react-icons/fi';
import apiClient, { authService } from '../services/api';

function ManageBookings() {
  const navigate = useNavigate();
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  
  const [bookings, setBookings] = useState([]);
  const [hotels, setHotels] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isCheckingAuth, setIsCheckingAuth] = useState(true);
  const [filter, setFilter] = useState('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedBooking, setSelectedBooking] = useState(null);
  
  const bg = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');

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
        
        // Load data
        await loadData();
      } catch (error) {
        console.error("Access check failed:", error);
        toast({
          title: 'Access Denied',
          description: 'You need manager permissions to manage bookings.',
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

  // Load bookings and hotels
  const loadData = async () => {
    setIsLoading(true);
    try {
      // Fetch hotels and bookings in parallel
      const [hotelsResponse, bookingsResponse] = await Promise.all([
        apiClient.get('/hotels'),
        apiClient.get('/bookings/all') // Endpoint to get all bookings (needs to be implemented on backend)
      ]);
      
      setHotels(hotelsResponse.data);
      
      // Process bookings to include hotel name
      const processedBookings = bookingsResponse.data.map(booking => {
        const hotel = hotelsResponse.data.find(h => h.id === booking.hotelId);
        return {
          ...booking,
          hotelName: hotel ? hotel.name : 'Unknown Hotel',
          hotel: hotel || null
        };
      });
      
      setBookings(processedBookings);
    } catch (error) {
      console.error("Error loading data:", error);
      toast({
        title: 'Error',
        description: error.response?.data || 'Failed to load data. Please try again.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setIsLoading(false);
    }
  };

  // View booking details
  const handleViewBooking = (booking) => {
    setSelectedBooking(booking);
    onOpen();
  };

  // Cancel booking
  const handleCancelBooking = async (bookingId) => {
    if (window.confirm('Are you sure you want to cancel this booking?')) {
      try {
        await apiClient.delete(`/bookings/${bookingId}`);
        
        toast({
          title: 'Booking Cancelled',
          description: 'The booking has been cancelled successfully.',
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
        
        // Reload bookings
        loadData();
      } catch (error) {
        toast({
          title: 'Error',
          description: 'Failed to cancel booking. Please try again.',
          status: 'error',
          duration: 5000,
          isClosable: true,
        });
      }
    }
  };

  // Approve booking
  const handleApproveBooking = async (bookingId) => {
    try {
      await apiClient.put(`/bookings/${bookingId}/approve`);
      
      toast({
        title: 'Booking Approved',
        description: 'The booking has been approved successfully.',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
      
      // Reload bookings
      loadData();
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to approve booking. Please try again.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    }
  };

  // Filter and search bookings
  const filteredBookings = bookings.filter(booking => {
    // Filter by status
    if (filter !== 'all') {
      const now = new Date();
      const checkInDate = new Date(booking.checkInDate);
      const checkOutDate = new Date(booking.checkOutDate);
      
      if (filter === 'upcoming' && checkInDate <= now) {
        return false;
      }
      if (filter === 'active' && (checkInDate > now || checkOutDate < now)) {
        return false;
      }
      if (filter === 'past' && checkOutDate >= now) {
        return false;
      }
    }
    
    // Search
    if (searchTerm) {
      const term = searchTerm.toLowerCase();
      return (
        booking.hotelName.toLowerCase().includes(term) ||
        booking.guestName?.toLowerCase().includes(term) ||
        booking.guestEmail?.toLowerCase().includes(term)
      );
    }
    
    return true;
  });

  // Format date
  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  // Determine booking status
  const getBookingStatus = (booking) => {
    const now = new Date();
    const checkInDate = new Date(booking.checkInDate);
    const checkOutDate = new Date(booking.checkOutDate);
    
    if (booking.isCancelled) {
      return { label: 'Cancelled', color: 'red' };
    } else if (checkInDate > now) {
      return { label: 'Upcoming', color: 'blue' };
    } else if (checkOutDate < now) {
      return { label: 'Completed', color: 'gray' };
    } else {
      return { label: 'Active', color: 'green' };
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
      </Flex>
      
      <Heading size="lg" mb={6}>Manage Bookings</Heading>
      
      <Flex mb={6} direction={{ base: 'column', md: 'row' }} justify="space-between" gap={4}>
        <InputGroup maxW={{ base: '100%', md: '400px' }}>
          <InputLeftElement pointerEvents="none">
            <FiSearch color="gray.300" />
          </InputLeftElement>
          <Input 
            placeholder="Search by hotel, guest name or email..." 
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            bg={bg}
          />
        </InputGroup>
        
        <HStack spacing={4}>
          <Select 
            value={filter} 
            onChange={(e) => setFilter(e.target.value)}
            maxW="200px"
            bg={bg}
          >
            <option value="all">All Bookings</option>
            <option value="upcoming">Upcoming</option>
            <option value="active">Active</option>
            <option value="past">Past</option>
          </Select>
          
          <Button 
            leftIcon={<FiFilter />} 
            variant="outline"
            display={{ base: 'none', md: 'flex' }}
          >
            More Filters
          </Button>
        </HStack>
      </Flex>
      
      <Card variant="outline" borderColor={borderColor} mb={6} bg={bg} shadow="md">
        <CardBody p={0}>
          <Box overflowX="auto">
            <Table variant="simple">
              <Thead>
                <Tr>
                  <Th>ID</Th>
                  <Th>Hotel</Th>
                  <Th>Check-in</Th>
                  <Th>Check-out</Th>
                  <Th>Guest</Th>
                  <Th>Status</Th>
                  <Th>Actions</Th>
                </Tr>
              </Thead>
              <Tbody>
                {filteredBookings.length > 0 ? (
                  filteredBookings.map(booking => {
                    const status = getBookingStatus(booking);
                    
                    return (
                      <Tr key={booking.id}>
                        <Td>#{booking.id}</Td>
                        <Td>{booking.hotelName}</Td>
                        <Td>{formatDate(booking.checkInDate)}</Td>
                        <Td>{formatDate(booking.checkOutDate)}</Td>
                        <Td>
                          <VStack align="start" spacing={0}>
                            <Text fontWeight="medium">{booking.guestName || 'Guest'}</Text>
                            <Text fontSize="xs" color="gray.500">{booking.guestEmail || 'No email'}</Text>
                          </VStack>
                        </Td>
                        <Td>
                          <Badge colorScheme={status.color}>{status.label}</Badge>
                        </Td>
                        <Td>
                          <Menu>
                            <MenuButton
                              as={IconButton}
                              icon={<FiMoreVertical />}
                              variant="ghost"
                              size="sm"
                            />
                            <MenuList>
                              <MenuItem icon={<FiEye />} onClick={() => handleViewBooking(booking)}>
                                View Details
                              </MenuItem>
                              
                              {status.label === 'Upcoming' && (
                                <>
                                  <MenuItem icon={<FiEdit />} onClick={() => navigate(`/bookings/edit/${booking.id}`)}>
                                    Edit Booking
                                  </MenuItem>
                                  <MenuItem icon={<FiCheckCircle />} onClick={() => handleApproveBooking(booking.id)}>
                                    Approve
                                  </MenuItem>
                                  <MenuItem icon={<FiTrash2 />} color="red.500" onClick={() => handleCancelBooking(booking.id)}>
                                    Cancel Booking
                                  </MenuItem>
                                </>
                              )}
                              
                              {status.label === 'Active' && (
                                <MenuItem icon={<FiCheckCircle />} onClick={() => handleApproveBooking(booking.id)}>
                                  Mark as Completed
                                </MenuItem>
                              )}
                            </MenuList>
                          </Menu>
                        </Td>
                      </Tr>
                    );
                  })
                ) : (
                  <Tr>
                    <Td colSpan={7} textAlign="center" py={8}>
                      <Text>No bookings found</Text>
                    </Td>
                  </Tr>
                )}
              </Tbody>
            </Table>
          </Box>
        </CardBody>
      </Card>

      {/* Booking Details Modal */}
      {selectedBooking && (
        <Modal isOpen={isOpen} onClose={onClose} size="lg">
          <ModalOverlay />
          <ModalContent>
            <ModalHeader>Booking Details #{selectedBooking.id}</ModalHeader>
            <ModalCloseButton />
            
            <ModalBody>
              <Stack spacing={4}>
                <Card p={4} bg="gray.50">
                  <HStack>
                    <Icon as={FiHome} boxSize={5} color="brand.500" />
                    <VStack align="start" spacing={0}>
                      <Text fontWeight="bold">Hotel</Text>
                      <Text>{selectedBooking.hotelName}</Text>
                    </VStack>
                  </HStack>
                </Card>
                
                <HStack spacing={4}>
                  <Card p={4} flex={1} bg="gray.50">
                    <HStack>
                      <Icon as={FiCalendar} boxSize={5} color="brand.500" />
                      <VStack align="start" spacing={0}>
                        <Text fontWeight="bold">Check-in</Text>
                        <Text>{formatDate(selectedBooking.checkInDate)}</Text>
                      </VStack>
                    </HStack>
                  </Card>
                  
                  <Card p={4} flex={1} bg="gray.50">
                    <HStack>
                      <Icon as={FiCalendar} boxSize={5} color="brand.500" />
                      <VStack align="start" spacing={0}>
                        <Text fontWeight="bold">Check-out</Text>
                        <Text>{formatDate(selectedBooking.checkOutDate)}</Text>
                      </VStack>
                    </HStack>
                  </Card>
                </HStack>
                
                <Card p={4} bg="gray.50">
                  <HStack>
                    <Icon as={FiUser} boxSize={5} color="brand.500" />
                    <VStack align="start" spacing={0}>
                      <Text fontWeight="bold">Guest Information</Text>
                      <Text>{selectedBooking.guestName || 'N/A'}</Text>
                      <Text fontSize="sm">{selectedBooking.guestEmail || 'No email provided'}</Text>
                    </VStack>
                  </HStack>
                </Card>
                
                <FormControl>
                  <FormLabel>Number of Guests</FormLabel>
                  <Input value={selectedBooking.numberOfGuests} readOnly bg="gray.50" />
                </FormControl>
                
                {selectedBooking.specialRequests && (
                  <FormControl>
                    <FormLabel>Special Requests</FormLabel>
                    <Input as="textarea" value={selectedBooking.specialRequests} readOnly bg="gray.50" />
                  </FormControl>
                )}
                
                <Box p={4} borderWidth="1px" borderRadius="md" borderColor={borderColor}>
                  <Heading size="sm" mb={2}>Booking Status</Heading>
                  <Badge colorScheme={getBookingStatus(selectedBooking).color} fontSize="md" px={2} py={1}>
                    {getBookingStatus(selectedBooking).label}
                  </Badge>
                </Box>
              </Stack>
            </ModalBody>
            
            <ModalFooter>
              <Button variant="ghost" mr={3} onClick={onClose}>
                Close
              </Button>
              
              {getBookingStatus(selectedBooking).label === 'Upcoming' && (
                <>
                  <Button 
                    colorScheme="green" 
                    mr={3}
                    onClick={() => {
                      handleApproveBooking(selectedBooking.id);
                      onClose();
                    }}
                  >
                    Approve
                  </Button>
                  
                  <Button 
                    colorScheme="red"
                    onClick={() => {
                      handleCancelBooking(selectedBooking.id);
                      onClose();
                    }}
                  >
                    Cancel
                  </Button>
                </>
              )}
            </ModalFooter>
          </ModalContent>
        </Modal>
      )}
    </Container>
  );
}

export default ManageBookings;