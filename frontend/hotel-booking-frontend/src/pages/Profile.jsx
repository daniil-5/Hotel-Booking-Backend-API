import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Heading,
  Text,
  Button,
  VStack,
  HStack,
  Avatar,
  Flex,
  Grid,
  GridItem,
  SimpleGrid,
  Card,
  CardBody,
  Divider,
  Badge,
  Stat,
  StatLabel,
  StatNumber,
  StatHelpText,
  useColorModeValue,
  IconButton,
  useToast,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalHeader,
  ModalFooter,
  ModalBody,
  ModalCloseButton,
  FormControl,
  FormLabel,
  Input,
  FormHelperText,
  useDisclosure,
  Spinner
} from '@chakra-ui/react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { FiEdit, FiUser, FiPhone, FiMail, FiCalendar, FiHome, FiStar, FiArrowLeft, FiImage } from 'react-icons/fi';
import apiClient from '../services/api';

function Profile() {
  const navigate = useNavigate();
  const toast = useToast();
  const { isOpen, onOpen, onClose } = useDisclosure();
  
  const [userData, setUserData] = useState(null);
  const [bookings, setBookings] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [profileForm, setProfileForm] = useState({
    id: 0,
    username: '',
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
  });
  
  const bgColor = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  
  useEffect(() => {
    const fetchUserData = async () => {
      setIsLoading(true);
      try {
        // Changed from '/Users/profile' to '/users/profile' to match API controller route
        const userResponse = await apiClient.get('/users/profile');
        console.log('User profile data:', userResponse.data);
        
        setUserData(userResponse.data);
        
        setProfileForm({
          id: userResponse.data.id,
          username: userResponse.data.username || '',
          firstName: userResponse.data.firstName || '',
          lastName: userResponse.data.lastName || '',
          email: userResponse.data.email || '',
          phoneNumber: userResponse.data.phoneNumber || '',
        });
        
        try {
          // Changed from '/Bookings' to '/bookings' for consistency
          const bookingsResponse = await apiClient.get('/bookings');
          console.log('Raw bookings response:', bookingsResponse.data);
          
          let bookingsData = [];
          
          if (Array.isArray(bookingsResponse.data)) {
            bookingsData = bookingsResponse.data;
          } else if (bookingsResponse.data && bookingsResponse.data.$values) {
            bookingsData = bookingsResponse.data.$values;
          } else if (bookingsResponse.data && typeof bookingsResponse.data === 'object' && !Array.isArray(bookingsResponse.data)) {
            bookingsData = [bookingsResponse.data];
          }
          
          console.log('Processed bookings data:', bookingsData);
          setBookings(bookingsData);
          
        } catch (bookingsErr) {
          console.error('Error fetching bookings:', bookingsErr);
          setBookings([]);
        }
        
        setError(null);
      } catch (err) {
        console.error('Error fetching user data:', err);
        setError('Failed to load profile data. Please try again.');
        
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
        }
      } finally {
        setIsLoading(false);
      }
    };
    
    fetchUserData();
  }, [navigate, toast]);
  
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setProfileForm(prev => ({
      ...prev,
      [name]: value
    }));
  };
  
  // Update handleProfileUpdate function to match UpdateUserDto exactly
  const handleProfileUpdate = async () => {
    try {
      // Convert role string to numeric enum value 
      // UserRole enum: Guest = 0, Manager = 1, Admin = 2
      let roleValue = 0; // Default to Guest
      if (userData.role === 'Manager') roleValue = 1;
      if (userData.role === 'Admin') roleValue = 2;
      
      // Match the exact case and structure expected by the API
      const updateData = {
        id: profileForm.id,
        username: profileForm.username || userData.username,
        email: profileForm.email,
        firstName: profileForm.firstName,
        lastName: profileForm.lastName,
        phoneNumber: profileForm.phoneNumber,
        role: roleValue // Send numeric enum value instead of string
      };
      
      console.log('Sending update data:', updateData);
      
      // Use the exact casing from your API controller
      const response = await apiClient.put(`/users/${profileForm.id}`, updateData);
      
      setUserData({...userData, ...updateData, role: userData.role});
      
      toast({
        title: 'Profile updated',
        description: 'Your profile has been updated successfully.',
        status: 'success',
        duration: 5000,
        isClosable: true,
      });
      
      onClose();
    } catch (err) {
      console.error('Update error:', err);
      
      // Enhanced error handling for validation errors
      let errorMessage = 'An error occurred while updating your profile.';
      if (err.response) {
        console.log('Error response:', err.response.data);
        
        if (typeof err.response.data === 'object') {
          if (err.response.data.errors) {
            // Handle validation error object
            const validationErrors = Object.values(err.response.data.errors)
              .flat()
              .join(', ');
            errorMessage = validationErrors || 'Validation failed. Please check your inputs.';
          } else {
            errorMessage = err.response.data.title || 
                          err.response.data.message || 
                          JSON.stringify(err.response.data);
          }
        } else if (typeof err.response.data === 'string') {
          errorMessage = err.response.data;
        }
      }
      
      toast({
        title: 'Update failed',
        description: errorMessage,
        status: 'error',
        duration: 5000,
        isClosable: true,
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
  
  // Update the error display in the error state
  if (error || !userData) {
    return (
      <Box textAlign="center" py={10} px={6}>
        <Heading as="h2" size="xl" mt={6} mb={2}>
          Error Loading Profile
        </Heading>
        <Text>
          {typeof error === 'object' 
            ? (error.message || 'An unexpected error occurred') 
            : (error || 'User data not found')}
        </Text>
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
  
  const safeBookings = Array.isArray(bookings) ? bookings : [];
  const totalBookings = safeBookings.length;
  const upcomingBookings = safeBookings.filter(b => new Date(b.checkInDate) > new Date()).length;
  const completedBookings = safeBookings.filter(b => new Date(b.checkOutDate) < new Date()).length;
  
  const displayName = userData ? 
    `${userData.firstName || ''} ${userData.lastName || ''}`.trim() || userData.email : 
    'User';
  
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
      
      <Grid templateColumns={{ base: "1fr", lg: "300px 1fr" }} gap={8}>
        <GridItem>
          <Card bg={bgColor} shadow="md" borderRadius="lg">
            <CardBody>
              <VStack spacing={4} align="center">
                <Box position="relative">
                  <Avatar 
                    size="2xl" 
                    name={displayName}
                    src={userData.profileImage}
                    bg="brand.500"
                    color="white"
                  />
                  <IconButton
                    aria-label="Change profile picture"
                    icon={<FiImage />}
                    size="sm"
                    colorScheme="brand"
                    isRound
                    position="absolute"
                    bottom="0"
                    right="0"
                  />
                </Box>
                
                <VStack spacing={1}>
                  <Heading size="md">{displayName}</Heading>
                  <Badge colorScheme={userData.role === 'Admin' ? 'purple' : userData.role === 'Manager' ? 'green' : 'blue'}>
                    {userData.role}
                  </Badge>
                </VStack>
                
                <Button 
                  leftIcon={<FiEdit />} 
                  colorScheme="brand" 
                  variant="outline"
                  width="full"
                  onClick={onOpen}
                >
                  Edit Profile
                </Button>
                
                <Divider />
                
                <VStack spacing={2} align="start" width="full">
                  <HStack>
                    <FiUser />
                    <Text>Member since {new Date(userData.createdAt || Date.now()).toLocaleDateString()}</Text>
                  </HStack>
                  
                  <HStack>
                    <FiMail />
                    <Text>{userData.email}</Text>
                  </HStack>
                  
                  {userData.phoneNumber && (
                    <HStack>
                      <FiPhone />
                      <Text>{userData.phoneNumber}</Text>
                    </HStack>
                  )}
                  
                  {userData.address && (
                    <HStack>
                      <FiHome />
                      <Text>{userData.address}</Text>
                    </HStack>
                  )}
                </VStack>
              </VStack>
            </CardBody>
          </Card>
          
          <Card bg={bgColor} shadow="md" borderRadius="lg" mt={4}>
            <CardBody>
              <Heading size="md" mb={4}>Account Statistics</Heading>
              
              <SimpleGrid columns={1} spacing={4}>
                <Stat>
                  <StatLabel>Total Bookings</StatLabel>
                  <StatNumber>{totalBookings}</StatNumber>
                  <StatHelpText>Lifetime</StatHelpText>
                </Stat>
                
                <Stat>
                  <StatLabel>Upcoming Stays</StatLabel>
                  <StatNumber>{upcomingBookings}</StatNumber>
                  <StatHelpText>Future bookings</StatHelpText>
                </Stat>
                
                <Stat>
                  <StatLabel>Completed Stays</StatLabel>
                  <StatNumber>{completedBookings}</StatNumber>
                  <StatHelpText>Past bookings</StatHelpText>
                </Stat>
                
                {userData.loyaltyPoints !== undefined && (
                  <Stat>
                    <StatLabel>Loyalty Points</StatLabel>
                    <StatNumber>{userData.loyaltyPoints}</StatNumber>
                    <StatHelpText>Available to redeem</StatHelpText>
                  </Stat>
                )}
              </SimpleGrid>
            </CardBody>
          </Card>
        </GridItem>
        
        <GridItem>
          <Card bg={bgColor} shadow="md" borderRadius="lg">
            <CardBody>
              <Heading size="md" mb={6}>Recent Bookings</Heading>
              
              {bookings.length > 0 ? (
                <VStack spacing={4} align="stretch">
                  {bookings.slice(0, 5).map(booking => {
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
                      <Box 
                        key={booking.id} 
                        p={4} 
                        borderWidth="1px" 
                        borderRadius="lg" 
                        borderColor={borderColor}
                      >
                        <Flex justify="space-between" align="start">
                          <Box>
                            <Heading size="sm">{booking.hotelName || 'Hotel Stay'}</Heading>
                            <HStack mt={1} spacing={2}>
                              <FiCalendar />
                              <Text fontSize="sm">
                                {checkInDate.toLocaleDateString()} - {checkOutDate.toLocaleDateString()}
                              </Text>
                            </HStack>
                            <HStack mt={1} spacing={2}>
                              <FiHome />
                              <Text fontSize="sm">
                                {booking.roomTypeName || 'Standard Room'}
                              </Text>
                            </HStack>
                          </Box>
                          
                          <VStack align="end">
                            <Badge colorScheme={statusColor}>{status}</Badge>
                            {booking.totalPrice && (
                              <Text fontWeight="bold">${booking.totalPrice.toFixed(2)}</Text>
                            )}
                          </VStack>
                        </Flex>
                      </Box>
                    );
                  })}
                  
                  {bookings.length > 5 && (
                    <Button 
                      as={RouterLink} 
                      to="/dashboard" 
                      variant="outline" 
                      colorScheme="brand"
                      width="full"
                    >
                      View All Bookings
                    </Button>
                  )}
                </VStack>
              ) : (
                <Box textAlign="center" py={6}>
                  <Text mb={4}>You haven't made any bookings yet.</Text>
                  <Button 
                    as={RouterLink} 
                    to="/dashboard" 
                    colorScheme="brand"
                  >
                    Browse Hotels
                  </Button>
                </Box>
              )}
            </CardBody>
          </Card>
          
          <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4} mt={4}>
            <Card bg={bgColor} shadow="md" borderRadius="lg">
              <CardBody>
                <Heading size="md" mb={4}>Favorite Destinations</Heading>
                <Text>Coming soon! We'll track your favorite destinations here.</Text>
              </CardBody>
            </Card>
            
            <Card bg={bgColor} shadow="md" borderRadius="lg">
              <CardBody>
                <Heading size="md" mb={4}>Recent Reviews</Heading>
                <Text>Coming soon! Your hotel reviews will appear here.</Text>
              </CardBody>
            </Card>
          </SimpleGrid>
        </GridItem>
      </Grid>
      
      {/* Edit Profile Modal */}
      <Modal isOpen={isOpen} onClose={onClose}>
        <ModalOverlay />
        <ModalContent>
          <ModalHeader>Edit Profile</ModalHeader>
          <ModalCloseButton />
          <ModalBody>
            <VStack spacing={4}>
              <FormControl>
                <FormLabel>First Name</FormLabel>
                <Input 
                  name="firstName"
                  value={profileForm.firstName}
                  onChange={handleInputChange}
                />
              </FormControl>
              
              <FormControl>
                <FormLabel>Last Name</FormLabel>
                <Input 
                  name="lastName"
                  value={profileForm.lastName}
                  onChange={handleInputChange}
                />
              </FormControl>
              
              <FormControl>
                <FormLabel>Email</FormLabel>
                <Input 
                  name="email"
                  value={profileForm.email}
                  isReadOnly
                  variant="filled"
                  fontWeight="medium"
                />
                <FormHelperText>Email address cannot be changed</FormHelperText>
              </FormControl>
              
              <FormControl>
                <FormLabel>Phone Number</FormLabel>
                <Input 
                  name="phoneNumber"
                  value={profileForm.phoneNumber}
                  onChange={handleInputChange}
                />
              </FormControl>
              
              <FormControl>
                <FormLabel>Username</FormLabel>
                <Input 
                  name="username"
                  value={profileForm.username}
                  onChange={handleInputChange}
                />
              </FormControl>
            </VStack>
          </ModalBody>
          
          <ModalFooter>
            <Button variant="ghost" mr={3} onClick={onClose}>
              Cancel
            </Button>
            <Button colorScheme="brand" onClick={handleProfileUpdate}>
              Save Changes
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    </Container>
  );
}

export default Profile;