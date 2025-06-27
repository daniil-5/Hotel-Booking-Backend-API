// src/components/hotels/HotelInfo.jsx
import React from 'react';
import {
  Box,
  Heading,
  Text,
  SimpleGrid,
  Flex,
  Icon,
  Badge,
  Divider,
  List,
  ListItem,
  ListIcon,
  HStack
} from '@chakra-ui/react';
import {
  FiClock,
  FiWifi,
  FiCoffee,
  FiSmile,
  FiHome,
  FiCheck,
  FiAward,
  FiAlertCircle,
  FiMapPin,
  FiTruck,
  FiCar,
  FiNavigation,
  FiCheckCircle
} from 'react-icons/fi';
import { FaCarSide } from 'react-icons/fa';

const HotelInfo = ({ hotel, amenities }) => {
  // Default check-in and check-out times if not provided
  const checkInTime = hotel.checkInTime || '3:00 PM';
  const checkOutTime = hotel.checkOutTime || '11:00 AM';
  
  // Map of amenity names to icons
  const amenityIcons = {
    'WiFi': FiWifi,
    'Breakfast': FiCoffee,
    'Pool': FiSmile,
    'Gym': FiAward,
    'Parking': FiHome,
    'Restaurant': FiCoffee,
    'Air Conditioning': FiSmile,
    'Bar': FiCoffee
  };
  
  console.log('Rendering HotelInfo with amenities:', amenities);

  // Helper function to get the right icon for common amenities
  function getAmenityIcon(amenity) {
    const amenityLower = amenity.toLowerCase();
    if (amenityLower.includes('wifi')) return FiWifi;
    if (amenityLower.includes('breakfast') || amenityLower.includes('coffee')) return FiCoffee;
    // Add more mappings as needed
    return FiCheck; // Default icon
  }
  
  return (
    <Box>
      <Heading size="md" mb={3}>About {hotel.name}</Heading>
      <Text mb={6}>{hotel.description}</Text>
      
      <SimpleGrid columns={{ base: 1, md: 2 }} spacing={8} mb={6}>
        <Box>
          <Heading size="sm" mb={3}>Property Highlights</Heading>
          <List spacing={2}>
            {hotel.freeCancellation && (
              <ListItem>
                <ListIcon as={FiCheck} color="green.500" />
                Free cancellation available
              </ListItem>
            )}
            <ListItem>
              <ListIcon as={FiClock} color="blue.500" />
              Check-in: {checkInTime}, Check-out: {checkOutTime}
            </ListItem>
            
            {hotel.rating >= 4.5 && (
              <ListItem>
                <ListIcon as={FiAward} color="yellow.500" />
                Top-rated property
              </ListItem>
            )}
            
            {hotel.isPetFriendly && (
              <ListItem>
                <ListIcon as={FiSmile} color="green.500" />
                Pet friendly
              </ListItem>
            )}
          </List>
        </Box>
        
      </SimpleGrid>
      
      <Divider my={6} />
      
      <Box mb={6}>
        <Heading size="sm" mb={3}>House Rules</Heading>
        <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4}>
          <Flex align="center">
            <Icon as={FiClock} mr={2} />
            <Box>
              <Text fontWeight="medium">Check-in</Text>
              <Text fontSize="sm">{checkInTime}</Text>
            </Box>
          </Flex>
          
          <Flex align="center">
            <Icon as={FiClock} mr={2} />
            <Box>
              <Text fontWeight="medium">Check-out</Text>
              <Text fontSize="sm">{checkOutTime}</Text>
            </Box>
          </Flex>
        </SimpleGrid>
        
        {hotel.policyDescription && (
          <Box mt={4}>
            <Text fontSize="sm">{hotel.policyDescription}</Text>
          </Box>
        )}
        
        {hotel.importantInfo && (
          <Flex mt={4} bg="yellow.50" p={3} borderRadius="md" align="start">
            <Icon as={FiAlertCircle} mr={2} color="yellow.500" mt={1} />
            <Text fontSize="sm">{hotel.importantInfo}</Text>
          </Flex>
        )}
      </Box>
      
      <Box>
        <Heading size="sm" mb={3}>Hotel Features & Services</Heading>
        <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4} mt={4}>
          <Box p={3} borderWidth="1px" borderRadius="md" borderColor="gray.200">
            <Heading size="xs" mb={2} color="brand.600">Popular Services</Heading>
            <List spacing={2}>
              <ListItem>
                <ListIcon as={FiCheckCircle} color="green.500" />
                24/7 Front Desk
              </ListItem>
              <ListItem>
                <ListIcon as={FiCheckCircle} color="green.500" />
                Room Service
              </ListItem>
              <ListItem>
                <ListIcon as={FiCheckCircle} color="green.500" />
                {hotel.hasFreeWifi ? "Free Wi-Fi" : "Wi-Fi Available"}
              </ListItem>
              <ListItem>
                <ListIcon as={FiCheckCircle} color="green.500" />
                {hotel.hasParking ? "Free Parking" : "Parking Available"}
              </ListItem>
            </List>
          </Box>
          
          <Box p={3} borderWidth="1px" borderRadius="md" borderColor="gray.200">
            <Heading size="xs" mb={2} color="brand.600">Dining Options</Heading>
            <List spacing={2}>
              <ListItem>
                <ListIcon as={FiCoffee} color="orange.500" />
                {hotel.hasRestaurant ? "On-site Restaurant" : "Nearby Restaurants"}
              </ListItem>
              <ListItem>
                <ListIcon as={FiCoffee} color="orange.500" />
                {hotel.hasBreakfast ? "Breakfast Included" : "Breakfast Available"}
              </ListItem>
              <ListItem>
                <ListIcon as={FiCoffee} color="orange.500" />
                Room Service
              </ListItem>
              <ListItem>
                <ListIcon as={FiCoffee} color="orange.500" />
                Bar/Lounge
              </ListItem>
            </List>
          </Box>
        </SimpleGrid>
        
        <Box mt={6}>
          <Heading size="sm" mb={3}>Nearby Attractions</Heading>
          <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4}>
            <HStack align="flex-start">
              <Icon as={FiMapPin} mt={1} color="red.500" />
              <Box>
                <Text fontWeight="medium">City Center</Text>
                <Text fontSize="sm" color="gray.600">0.5 miles away</Text>
              </Box>
            </HStack>
            <HStack align="flex-start">
              <Icon as={FiMapPin} mt={1} color="red.500" />
              <Box>
                <Text fontWeight="medium">Central Park</Text>
                <Text fontSize="sm" color="gray.600">1.2 miles away</Text>
              </Box>
            </HStack>
            <HStack align="flex-start">
              <Icon as={FiMapPin} mt={1} color="red.500" />
              <Box>
                <Text fontWeight="medium">Shopping District</Text>
                <Text fontSize="sm" color="gray.600">0.8 miles away</Text>
              </Box>
            </HStack>
            <HStack align="flex-start">
              <Icon as={FiMapPin} mt={1} color="red.500" />
              <Box>
                <Text fontWeight="medium">Airport</Text>
                <Text fontSize="sm" color="gray.600">12 miles away</Text>
              </Box>
            </HStack>
          </SimpleGrid>
        </Box>
        
        <Box mt={6}>
          <Heading size="sm" mb={3}>Transportation</Heading>
          <SimpleGrid columns={{ base: 1, md: 2 }} spacing={4}>
            <HStack>
              <Icon as={FiTruck} color="blue.500" />
              <Text>Airport Shuttle {hotel.hasAirportShuttle ? "Available" : "Not Available"}</Text>
            </HStack>
            <HStack>
              <Icon as={FiMapPin} color="blue.500" />
              <Text>Public Transit Nearby</Text>
            </HStack>
            <HStack>
              <Icon as={FaCarSide} color="blue.500" />
              <Text>Car Rental Services</Text>
            </HStack>
            <HStack>
              <Icon as={FiNavigation} color="blue.500" />
              <Text>Tour Booking Available</Text>
            </HStack>
          </SimpleGrid>
        </Box>
      </Box>
    </Box>
  );
};

export default HotelInfo;