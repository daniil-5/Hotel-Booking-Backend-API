import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box, Container, Heading, Grid, GridItem, Text, Button, Flex,
  Badge, Divider, Spinner, HStack, VStack, Icon, Card, CardBody,
  Tag, Alert, AlertIcon, useColorModeValue, IconButton,
  Table, Thead, Tbody, Tr, Th, Td, TableContainer
} from '@chakra-ui/react';
import {
  FiCalendar, FiMapPin, FiUser, FiClock,
  FiHome, FiArrowLeft, FiPrinter, FiTrash2
} from 'react-icons/fi';
import apiClient from '../services/api';

const BookingDetails = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [booking, setBooking] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  
  // Color modes
  const cardBg = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  
  // Price section colors with better contrast
  const priceBgColor = useColorModeValue('blue.50', 'gray.700');
  const headerBgColor = useColorModeValue('blue.600', 'blue.700');
  const headerTextColor = 'white';
  const totalRowBgColor = useColorModeValue('blue.100', 'blue.800');
  const totalTextColor = useColorModeValue('blue.800', 'white');
  
  useEffect(() => {
    const fetchBookingDetails = async () => {
      setIsLoading(true);
      try {
        // Fetch enhanced booking details
        const response = await apiClient.get(`/bookings/${id}/details`);
        setBooking(response.data);
      } catch (error) {
        console.error('Error fetching booking details:', error);
        setError('Failed to load booking details');
      } finally {
        setIsLoading(false);
      }
    };
    
    fetchBookingDetails();
  }, [id]);
  
  const handleCancelBooking = async () => {
    if (window.confirm('Are you sure you want to cancel this booking?')) {
      try {
        await apiClient.delete(`/bookings/${id}`);
        navigate('/bookings');
      } catch (error) {
        console.error('Error cancelling booking:', error);
        alert('Failed to cancel booking');
      }
    }
  };
  
  const getStatusInfo = (booking) => {
    const now = new Date();
    const checkInDate = new Date(booking.checkInDate);
    const checkOutDate = new Date(booking.checkOutDate);
    
    if (checkInDate <= now && checkOutDate >= now) {
      return { status: 'Active', color: 'green' };
    } else if (checkOutDate < now) {
      return { status: 'Completed', color: 'gray' };
    } else {
      return { status: 'Upcoming', color: 'blue' };
    }
  };
  
  if (isLoading) {
    return (
      <Container maxW="container.lg" py={8}>
        <Flex justify="center" align="center" height="50vh">
          <Spinner size="xl" thickness="4px" color="brand.500" />
        </Flex>
      </Container>
    );
  }
  
  if (error || !booking) {
    return (
      <Container maxW="container.lg" py={8}>
        <Alert status="error" mb={4}>
          <AlertIcon />
          {error || 'Booking not found'}
        </Alert>
        <Button leftIcon={<FiArrowLeft />} onClick={() => navigate('/bookings')}>
          Back to Bookings
        </Button>
      </Container>
    );
  }
  
  const { status, color } = getStatusInfo(booking);
  const checkInDate = new Date(booking.checkInDate);
  const checkOutDate = new Date(booking.checkOutDate);
  const nights = booking.numberOfNights || Math.round((checkOutDate - checkInDate) / (1000 * 60 * 60 * 24));
  
  // Calculate base price total if not provided
  const basePrice = booking.basePrice || 0;
  const basePriceTotal = booking.basePriceTotal || (basePrice * nights);
  const extraGuestCharges = booking.extraGuestCharges || 0;
  const totalPrice = booking.totalPrice || basePriceTotal + extraGuestCharges;
  
  return (
    <Container maxW="container.lg" py={8}>
      <Flex justify="space-between" align="center" mb={6}>
        <Button 
          leftIcon={<FiArrowLeft />} 
          variant="outline" 
          onClick={() => navigate('/bookings')}
        >
          Back to Bookings
        </Button>
        <HStack>
          <IconButton 
            icon={<FiPrinter />} 
            aria-label="Print" 
            onClick={() => window.print()}
          />
          {status === 'Upcoming' && (
            <Button 
              leftIcon={<FiTrash2 />} 
              colorScheme="red" 
              variant="outline"
              onClick={handleCancelBooking}
            >
              Cancel Booking
            </Button>
          )}
        </HStack>
      </Flex>
      
      <Card bg={cardBg} mb={6} borderWidth="1px" borderColor={borderColor} boxShadow="sm">
        <CardBody>
          <Flex 
            justify="space-between" 
            align={{ base: "start", md: "center" }}
            direction={{ base: "column", md: "row" }}
            mb={6}
          >
            <Box>
              <Heading size="lg">Booking #{booking.id}</Heading>
              <Text color="gray.500" mt={1}>
                Created on {new Date(booking.createdAt).toLocaleDateString()}
              </Text>
            </Box>
            <Badge 
              colorScheme={color} 
              fontSize="md" 
              px={3} 
              py={1} 
              borderRadius="full"
              mt={{ base: 2, md: 0 }}
            >
              {status}
            </Badge>
          </Flex>
          
          <Divider mb={6} />
          
          <Grid 
            templateColumns={{ base: "1fr", md: "repeat(2, 1fr)" }}
            gap={6}
          >
            {/* Hotel Information */}
            <GridItem>
              <Heading size="md" mb={4}>Hotel Information</Heading>
              <VStack align="start" spacing={3}>
                <HStack>
                  <Icon as={FiHome} color="gray.500" />
                  <Text fontWeight="medium">{booking.hotelName}</Text>
                </HStack>
                {booking.hotelLocation && (
                  <HStack>
                    <Icon as={FiMapPin} color="gray.500" />
                    <Text>{booking.hotelLocation}</Text>
                  </HStack>
                )}
                <HStack>
                  <Icon as={FiHome} color="gray.500" />
                  <Text>
                    {booking.roomTypeName || 'Standard Room'}
                  </Text>
                </HStack>
              </VStack>
            </GridItem>
            
            {/* Booking Information */}
            <GridItem>
              <Heading size="md" mb={4}>Booking Information</Heading>
              <VStack align="start" spacing={3}>
                <HStack>
                  <Icon as={FiCalendar} color="gray.500" />
                  <Text>Check-in: {checkInDate.toLocaleDateString()}</Text>
                </HStack>
                <HStack>
                  <Icon as={FiCalendar} color="gray.500" />
                  <Text>Check-out: {checkOutDate.toLocaleDateString()}</Text>
                </HStack>
                <HStack>
                  <Icon as={FiClock} color="gray.500" />
                  <Text>Duration: {nights} {nights === 1 ? 'night' : 'nights'}</Text>
                </HStack>
                <HStack>
                  <Icon as={FiUser} color="gray.500" />
                  <Text>Guests: {booking.guestCount || booking.numberOfGuests}</Text>
                </HStack>
              </VStack>
            </GridItem>
          </Grid>
          
          <Divider my={6} />
          
          {/* Price Breakdown Section */}
          <Heading size="md" mb={4}>Price Breakdown</Heading>
          
          <Box 
            mb={6} 
            borderWidth="2px" 
            borderRadius="md" 
            borderColor={headerBgColor}
            overflow="hidden"
          >
            <TableContainer>
              <Table variant="simple">
                <Thead bg={headerBgColor}>
                  <Tr>
                    <Th color={headerTextColor}>Item</Th>
                    <Th color={headerTextColor} isNumeric>Amount</Th>
                  </Tr>
                </Thead>
                <Tbody>
                  <Tr bg={priceBgColor}>
                    <Td fontWeight="medium">Base Price ({nights} {nights === 1 ? 'night' : 'nights'})</Td>
                    <Td isNumeric fontWeight="medium">${basePrice.toFixed(2)} x {nights}</Td>
                  </Tr>
                  <Tr bg={priceBgColor}>
                    <Td fontWeight="medium">Room Total</Td>
                    <Td isNumeric fontWeight="medium">${basePriceTotal.toFixed(2)}</Td>
                  </Tr>
                  
                  {extraGuestCharges > 0 && (
                    <Tr bg={priceBgColor}>
                      <Td fontWeight="medium">Extra Guest Charges</Td>
                      <Td isNumeric fontWeight="medium">${extraGuestCharges.toFixed(2)}</Td>
                    </Tr>
                  )}
                  
                  <Tr bg={totalRowBgColor}>
                    <Td fontWeight="bold" color={totalTextColor}>Total Price</Td>
                    <Td isNumeric fontWeight="bold" color={totalTextColor} fontSize="lg">
                      ${totalPrice.toFixed(2)}
                    </Td>
                  </Tr>
                </Tbody>
              </Table>
            </TableContainer>
          </Box>
          
          {/* Special Requests */}
          {booking.specialRequests && (
            <>
              <Heading size="md" mb={4}>Special Requests</Heading>
              <Box p={4} borderWidth="1px" borderRadius="md" borderColor={borderColor} mb={6}>
                <Text>{booking.specialRequests}</Text>
              </Box>
            </>
          )}
          
          {/* Room Information */}
          {booking.roomTypeDescription && (
            <>
              <Heading size="md" mb={4}>Room Information</Heading>
              <Box p={4} borderWidth="1px" borderRadius="md" borderColor={borderColor}>
                <Text mb={3}>{booking.roomTypeDescription}</Text>
                
                <Text fontWeight="medium" mb={2}>Amenities:</Text>
                <Flex wrap="wrap" gap={2}>
                  {booking.amenities?.map((amenity, index) => (
                    <Tag key={index} size="md" colorScheme="blue">
                      {amenity}
                    </Tag>
                  ))}
                </Flex>
              </Box>
            </>
          )}
        </CardBody>
      </Card>
    </Container>
  );
};

export default BookingDetails;