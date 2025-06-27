import React, { useState, useEffect, useCallback } from 'react';
import {
  Modal, ModalOverlay, ModalContent, ModalHeader, ModalFooter,
  ModalBody, ModalCloseButton, FormControl, FormLabel, Input,
  Select, Button, Grid, GridItem, Box, Text, Flex, Stack,
  Divider, Spinner, FormErrorMessage, useToast, useColorModeValue
} from '@chakra-ui/react';
import apiClient from '../services/api';

const BookingForm = ({ isOpen, onClose, hotels, selectedHotel, onCreateBooking, userData }) => {
  const [roomTypes, setRoomTypes] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isCalculating, setIsCalculating] = useState(false);
  const [priceDetails, setPriceDetails] = useState(null);
  const toast = useToast();

  // Color values for the price details box
  const priceBgColor = useColorModeValue('blue.50', 'gray.700');
  const priceTextColor = useColorModeValue('brand.600', 'brand.300');
  const borderColor = useColorModeValue('gray.200', 'gray.700');

  // Form state
  const [formData, setFormData] = useState({
    hotelId: '',
    roomTypeId: '',
    checkInDate: '',
    checkOutDate: '',
    guestCount: 1,
    specialRequests: '',
    userId: userData?.id,
    totalPrice: 0
  });

  // Memoize fetchRoomTypes to avoid dependency array issues
  const fetchRoomTypes = useCallback(async (hotelId) => {
    if (!hotelId) return;
    
    setIsLoading(true);
    try {
      const response = await apiClient.get(`/RoomTypes/hotel/${hotelId}`);
      setRoomTypes(response.data);
    } catch (error) {
      console.error('Error fetching room types:', error);
      toast({
        title: 'Error',
        description: 'Failed to load room types for this hotel.',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setIsLoading(false);
    }
  }, [toast]);

  // Set selectedHotel when it changes
  useEffect(() => {
    if (selectedHotel) {
      setFormData(prev => ({
        ...prev,
        hotelId: selectedHotel.id
      }));
      
      // Fetch room types for the selected hotel
      fetchRoomTypes(selectedHotel.id);
    }
  }, [selectedHotel, fetchRoomTypes]);

  // Handle hotel change
  const handleHotelChange = (e) => {
    const hotelId = e.target.value;
    setFormData({
      ...formData,
      hotelId,
      roomTypeId: '', // Reset room type when hotel changes
    });
    
    fetchRoomTypes(hotelId);
  };

  // Handle form input changes
  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: value
    });
  };

  // Calculate price when relevant form fields change
  useEffect(() => {
    const calculatePrice = async () => {
      if (!formData.roomTypeId || !formData.checkInDate || !formData.checkOutDate || !formData.guestCount) {
        setPriceDetails(null);
        return;
      }
      
      setIsCalculating(true);
      
      try {
        // Calculate nights manually first
        const checkIn = new Date(formData.checkInDate);
        const checkOut = new Date(formData.checkOutDate);
        const nights = Math.floor((checkOut - checkIn) / (1000 * 60 * 60 * 24));
        
        // Find the selected room type to get its base price
        const selectedRoomType = roomTypes.find(rt => rt.id.toString() === formData.roomTypeId.toString());
        
        if (!selectedRoomType) {
          setPriceDetails(null);
          setIsCalculating(false);
          return;
        }
        
        // Try to get seasonal pricing if available
        let seasonalPricing = [];
        try {
          const pricingResponse = await apiClient.get(`/RoomPricings?roomTypeId=${formData.roomTypeId}`);
          seasonalPricing = pricingResponse.data || [];
        } catch (err) {
          console.warn("Couldn't fetch seasonal pricing, using base price", err);
        }
        
        // Calculate price based on base price and number of nights
        const basePrice = selectedRoomType.basePrice;
        const basePriceTotal = basePrice * nights;
        
        // Simplified price details (backend would typically handle this calculation)
        const calculatedPriceDetails = {
          basePrice: basePrice,
          numberOfNights: nights,
          basePriceTotal: basePriceTotal,
          extraGuestCharges: 0, // Would be calculated if needed
          totalPrice: basePriceTotal,
          hasSeasonalPricing: seasonalPricing.length > 0
        };
        
        setPriceDetails(calculatedPriceDetails);
        
        // Update form with calculated price
        setFormData(prev => ({
          ...prev,
          totalPrice: calculatedPriceDetails.totalPrice
        }));
      } catch (error) {
        console.error('Error calculating price:', error);
        setPriceDetails(null);
      } finally {
        setIsCalculating(false);
      }
    };
    
    calculatePrice();
  }, [formData.roomTypeId, formData.checkInDate, formData.checkOutDate, formData.guestCount, roomTypes]);

  // Handle form submission
  const handleSubmit = (e) => {
    e.preventDefault();
    
    // Validate form
    if (!formData.hotelId || !formData.roomTypeId || !formData.checkInDate || !formData.checkOutDate) {
      toast({
        title: 'Missing fields',
        description: 'Please fill all required fields',
        status: 'error',
        duration: 3000,
        isClosable: true,
      });
      return;
    }
    
    // Create a booking object that matches the backend DTO structure
    const bookingData = {
      hotelId: parseInt(formData.hotelId),
      roomTypeId: parseInt(formData.roomTypeId),
      checkInDate: formData.checkInDate,
      checkOutDate: formData.checkOutDate,
      guestCount: parseInt(formData.guestCount),
      specialRequests: formData.specialRequests,
      // userId will be set by the backend based on the JWT token
    };
    
    onCreateBooking(bookingData);
  };

  // Set today as minimum date for check-in
  const today = new Date().toISOString().split('T')[0];
  
  // Calculate checkout min date (day after check-in)
  const checkoutMinDate = formData.checkInDate || today;

  return (
    <Modal isOpen={isOpen} onClose={onClose} size="lg">
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>
          {selectedHotel 
            ? `Book ${selectedHotel.name}` 
            : 'Create New Booking'}
        </ModalHeader>
        <ModalCloseButton />
        
        <form onSubmit={handleSubmit}>
          <ModalBody>
            <FormControl mb={4} isRequired>
              <FormLabel>Hotel</FormLabel>
              <Select
                name="hotelId"
                value={formData.hotelId}
                onChange={handleHotelChange}
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

            <FormControl mb={4} isRequired isDisabled={!formData.hotelId || isLoading}>
              <FormLabel>Room Type</FormLabel>
              <Select
                name="roomTypeId"
                value={formData.roomTypeId}
                onChange={handleChange}
                placeholder={isLoading ? "Loading room types..." : "Select a room type"}
              >
                {roomTypes.map(roomType => (
                  <option key={roomType.id} value={roomType.id}>
                    {roomType.name} - ${roomType.basePrice}/night (Max {roomType.capacity} guests)
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
                max={formData.roomTypeId ? 
                  (roomTypes.find(rt => rt.id.toString() === formData.roomTypeId.toString())?.capacity || 10) : 
                  10}
                value={formData.guestCount}
                onChange={handleChange}
              />
              {formData.roomTypeId && formData.guestCount > 
                (roomTypes.find(rt => rt.id.toString() === formData.roomTypeId.toString())?.capacity || 10) && (
                <FormErrorMessage>
                  This room type can only accommodate up to {
                    roomTypes.find(rt => rt.id.toString() === formData.roomTypeId.toString())?.capacity
                  } guests.
                </FormErrorMessage>
              )}
            </FormControl>

            <FormControl mt={4}>
              <FormLabel>Special Requests</FormLabel>
              <Input
                name="specialRequests"
                as="textarea"
                placeholder="Any special requirements or requests..."
                value={formData.specialRequests}
                onChange={handleChange}
              />
            </FormControl>
            
            {/* Price Details */}
            {isCalculating ? (
              <Flex justify="center" mt={4}>
                <Spinner size="sm" mr={2} />
                <Text>Calculating price...</Text>
              </Flex>
            ) : priceDetails && (
              <Box 
                mt={4} 
                p={4} 
                borderWidth="1px" 
                borderRadius="md" 
                borderColor={borderColor}
                bg={priceBgColor}
              >
                <Text fontWeight="bold" mb={2}>Price Details</Text>
                <Stack spacing={1} fontSize="sm">
                  <Flex justify="space-between">
                    <Text fontWeight="medium">Base Price:</Text>
                    <Text>${priceDetails.basePrice?.toFixed(2)} per night</Text>
                  </Flex>
                  <Flex justify="space-between">
                    <Text fontWeight="medium">Nights:</Text>
                    <Text>{priceDetails.numberOfNights}</Text>
                  </Flex>
                  <Flex justify="space-between">
                    <Text fontWeight="medium">Room Total:</Text>
                    <Text>${priceDetails.basePriceTotal?.toFixed(2)}</Text>
                  </Flex>
                  {priceDetails.extraGuestCharges > 0 && (
                    <Flex justify="space-between">
                      <Text fontWeight="medium">Extra Guest Charges:</Text>
                      <Text>${priceDetails.extraGuestCharges?.toFixed(2)}</Text>
                    </Flex>
                  )}
                  <Divider my={2} />
                  <Flex justify="space-between" fontWeight="bold">
                    <Text color={priceTextColor}>Total Price:</Text>
                    <Text color={priceTextColor}>${priceDetails.totalPrice?.toFixed(2)}</Text>
                  </Flex>
                </Stack>
              </Box>
            )}
          </ModalBody>

          <ModalFooter>
            <Button mr={3} onClick={onClose}>
              Cancel
            </Button>
            <Button 
              type="submit" 
              colorScheme="brand" 
              isLoading={isCalculating || isLoading}
            >
              Book Now
            </Button>
          </ModalFooter>
        </form>
      </ModalContent>
    </Modal>
  );
};

export default BookingForm;