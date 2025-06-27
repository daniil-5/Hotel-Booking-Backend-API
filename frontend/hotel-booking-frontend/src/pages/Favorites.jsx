// src/pages/Favorites.jsx
import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Heading,
  Text,
  Button,
  SimpleGrid,
  Flex,
  IconButton,
  useToast,
  Spinner,
  useColorModeValue,
  Alert,
  AlertIcon,
  AlertTitle,
  AlertDescription,
  Tab,
  TabList,
  TabPanel,
  TabPanels,
  Tabs,
  Card,
  CardBody,
  Image,
  Stack,
  Badge,
  Divider,
  HStack,
  Icon
} from '@chakra-ui/react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import {
  FiArrowLeft,
  FiHeart,
  FiStar,
  FiMapPin,
  FiInfo,
  FiCalendar,
  FiTrash2
} from 'react-icons/fi';
import apiClient from '../services/api';

function Favorites() {
  const navigate = useNavigate();
  const toast = useToast();
  
  const [favorites, setFavorites] = useState([]);
  const [favoriteLocations, setFavoriteLocations] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  
  const bgColor = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  
  useEffect(() => {
    const fetchFavorites = async () => {
      setIsLoading(true);
      try {
        // Fetch user's favorite hotels
        const response = await apiClient.get('/favorites');
        setFavorites(response.data);
        
        // Extract unique locations from favorites
        const locations = [...new Set(response.data.map(fav => fav.hotel.location))];
        setFavoriteLocations(locations);
        
        setError(null);
      } catch (err) {
        console.error('Error fetching favorites:', err);
        
        // If API endpoint doesn't exist, use mock data
        if (err.response && err.response.status === 404) {
          console.log('Using mock favorites data');
          // Mock some favorite hotels
          const mockFavorites = [
            {
              id: 1,
              hotel: {
                id: 101,
                name: "Grand Hotel",
                location: "New York",
                rating: 4.8,
                basePrice: 299,
                description: "Luxury hotel in the heart of Manhattan",
                mainImage: "https://images.unsplash.com/photo-1566073771259-6a8506099945?ixid=MnwxMjA3fDB8MHxzZWFyY2h8Mnx8aG90ZWx8ZW58MHx8MHx8&ixlib=rb-1.2.1&w=1000&q=80"
              },
              dateAdded: "2025-01-15T10:30:00Z"
            },
            {
              id: 2,
              hotel: {
                id: 102,
                name: "Seaside Resort",
                location: "Miami",
                rating: 4.5,
                basePrice: 249,
                description: "Beautiful beachfront resort with stunning ocean views",
                mainImage: "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?ixid=MnwxMjA3fDB8MHxzZWFyY2h8NXx8aG90ZWx8ZW58MHx8MHx8&ixlib=rb-1.2.1&w=1000&q=80"
              },
              dateAdded: "2025-02-20T14:15:00Z"
            },
            {
              id: 3,
              hotel: {
                id: 103,
                name: "Mountain Lodge",
                location: "Denver",
                rating: 4.6,
                basePrice: 199,
                description: "Cozy mountain retreat with spectacular views",
                mainImage: "https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?ixid=MnwxMjA3fDB8MHxzZWFyY2h8NHx8aG90ZWx8ZW58MHx8MHx8&ixlib=rb-1.2.1&w=1000&q=80"
              },
              dateAdded: "2025-03-05T09:45:00Z"
            }
          ];
          
          setFavorites(mockFavorites);
          
          // Extract unique locations from mock favorites
          const locations = [...new Set(mockFavorites.map(fav => fav.hotel.location))];
          setFavoriteLocations(locations);
          
          setError(null);
          setIsLoading(false);
          return;
        }
        
        setError('Failed to load favorites. Please try again.');
        
        // Handle token issues
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
    
    fetchFavorites();
  }, [navigate, toast]);
  
  const handleRemoveFavorite = async (id) => {
    try {
      await apiClient.delete(`/favorites/${id}`);
      
      // Update state to remove the favorite
      setFavorites(favorites.filter(fav => fav.id !== id));
      
      toast({
        title: 'Favorite removed',
        description: 'Hotel has been removed from your favorites.',
        status: 'success',
        duration: 3000,
        isClosable: true,
      });
    } catch (err) {
      console.error('Error removing favorite:', err);
      
      // If using mock data, just remove from state
      if (!err.response || err.response.status === 404) {
        setFavorites(favorites.filter(fav => fav.id !== id));
        
        toast({
          title: 'Favorite removed',
          description: 'Hotel has been removed from your favorites.',
          status: 'success',
          duration: 3000,
          isClosable: true,
        });
        return;
      }
      
      toast({
        title: 'Error',
        description: 'Could not remove from favorites. Please try again.',
        status: 'error',
        duration: 3000,
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
  
  if (error) {
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
        
        <Alert status="error" variant="subtle" flexDirection="column" alignItems="center" justifyContent="center" textAlign="center" height="200px" borderRadius="lg">
          <AlertIcon boxSize="40px" mr={0} />
          <AlertTitle mt={4} mb={1} fontSize="lg">
            Error Loading Favorites
          </AlertTitle>
          <AlertDescription maxWidth="sm">
            {error}
          </AlertDescription>
          <Button colorScheme="red" mt={4} onClick={() => window.location.reload()}>
            Try Again
          </Button>
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
        onClick={() => navigate('/dashboard')}
      >
        Back to Dashboard
      </Button>
      
      <Flex justify="space-between" align="center" mb={6}>
        <Heading as="h1" size="xl">
          My Favorites
        </Heading>
        
        <HStack>
          <Text color="gray.600">
            {favorites.length} {favorites.length === 1 ? 'hotel' : 'hotels'} saved
          </Text>
        </HStack>
      </Flex>
      
      {favorites.length === 0 ? (
        <Box textAlign="center" py={10}>
          <Icon as={FiHeart} w={12} h={12} color="gray.400" mb={4} />
          <Heading size="md" mb={2}>No Favorites Yet</Heading>
          <Text mb={6}>
            Save hotels you love to your favorites list for quick access.
          </Text>
          <Button 
            as={RouterLink} 
            to="/dashboard" 
            colorScheme="brand"
            size="lg"
          >
            Browse Hotels
          </Button>
        </Box>
      ) : (
        <Tabs colorScheme="brand" variant="enclosed">
          <TabList>
            <Tab>All Favorites</Tab>
            {favoriteLocations.map((location, index) => (
              <Tab key={index}>{location}</Tab>
            ))}
          </TabList>
          
          <TabPanels>
            {/* All Favorites Tab */}
            <TabPanel>
              <SimpleGrid columns={{ base: 1, sm: 2, md: 3, lg: 4 }} spacing={6}>
                {favorites.map((favorite) => (
                  <Card key={favorite.id} maxW="sm" overflow="hidden" borderRadius="lg" bg={bgColor} borderColor={borderColor} borderWidth="1px">
                    <Box position="relative">
                      <Image
                        src={favorite.hotel.mainImage || 'https://via.placeholder.com/300x200?text=Hotel+Image'}
                        alt={favorite.hotel.name}
                        height="200px"
                        width="100%"
                        objectFit="cover"
                      />
                      <IconButton
                        aria-label="Remove from favorites"
                        icon={<FiTrash2 />}
                        size="sm"
                        colorScheme="red"
                        variant="solid"
                        position="absolute"
                        top="10px"
                        right="10px"
                        onClick={() => handleRemoveFavorite(favorite.id)}
                      />
                      <Badge
                        position="absolute"
                        bottom="10px"
                        left="10px"
                        colorScheme={favorite.hotel.rating >= 4.5 ? "green" : favorite.hotel.rating >= 3.5 ? "yellow" : "red"}
                        fontSize="0.8em"
                        p="5px 10px"
                        borderRadius="full"
                      >
                        {favorite.hotel.rating} <FiStar style={{ display: 'inline' }} />
                      </Badge>
                    </Box>
                    
                    <CardBody>
                      <Stack spacing="3">
                        <Heading size="md">{favorite.hotel.name}</Heading>
                        <HStack>
                          <FiMapPin />
                          <Text>{favorite.hotel.location}</Text>
                        </HStack>
                        <Text noOfLines={2} color="gray.600">
                          {favorite.hotel.description}
                        </Text>
                        <Text color="brand.600" fontSize="2xl">
                          ${favorite.hotel.basePrice}
                          <Box as="span" color="gray.600" fontSize="sm">
                            /night
                          </Box>
                        </Text>
                      </Stack>
                      
                      <Divider my={3} />
                      
                      <Flex justify="space-between">
                        <Button 
                          leftIcon={<FiInfo />} 
                          variant="ghost" 
                          colorScheme="brand"
                          size="sm"
                          onClick={() => navigate(`/hotels/${favorite.hotel.id}`)}
                        >
                          Details
                        </Button>
                        <Button 
                          rightIcon={<FiCalendar />} 
                          colorScheme="brand"
                          size="sm"
                          onClick={() => navigate(`/hotels/${favorite.hotel.id}`)}
                        >
                          Book Now
                        </Button>
                      </Flex>
                    </CardBody>
                  </Card>
                ))}
              </SimpleGrid>
            </TabPanel>
            
            {/* Location Tabs */}
            {favoriteLocations.map((location, index) => (
              <TabPanel key={index}>
                <SimpleGrid columns={{ base: 1, sm: 2, md: 3, lg: 4 }} spacing={6}>
                  {favorites
                    .filter(fav => fav.hotel.location === location)
                    .map((favorite) => (
                      <Card key={favorite.id} maxW="sm" overflow="hidden" borderRadius="lg" bg={bgColor} borderColor={borderColor} borderWidth="1px">
                        <Box position="relative">
                          <Image
                            src={favorite.hotel.mainImage || 'https://via.placeholder.com/300x200?text=Hotel+Image'}
                            alt={favorite.hotel.name}
                            height="200px"
                            width="100%"
                            objectFit="cover"
                          />
                          <IconButton
                            aria-label="Remove from favorites"
                            icon={<FiTrash2 />}
                            size="sm"
                            colorScheme="red"
                            variant="solid"
                            position="absolute"
                            top="10px"
                            right="10px"
                            onClick={() => handleRemoveFavorite(favorite.id)}
                          />
                          <Badge
                            position="absolute"
                            bottom="10px"
                            left="10px"
                            colorScheme={favorite.hotel.rating >= 4.5 ? "green" : favorite.hotel.rating >= 3.5 ? "yellow" : "red"}
                            fontSize="0.8em"
                            p="5px 10px"
                            borderRadius="full"
                          >
                            {favorite.hotel.rating} <FiStar style={{ display: 'inline' }} />
                          </Badge>
                        </Box>
                        
                        <CardBody>
                          <Stack spacing="3">
                            <Heading size="md">{favorite.hotel.name}</Heading>
                            <HStack>
                              <FiMapPin />
                              <Text>{favorite.hotel.location}</Text>
                            </HStack>
                            <Text noOfLines={2} color="gray.600">
                              {favorite.hotel.description}
                            </Text>
                            <Text color="brand.600" fontSize="2xl">
                              ${favorite.hotel.basePrice}
                              <Box as="span" color="gray.600" fontSize="sm">
                                /night
                              </Box>
                            </Text>
                          </Stack>
                          
                          <Divider my={3} />
                          
                          <Flex justify="space-between">
                            <Button 
                              leftIcon={<FiInfo />} 
                              variant="ghost" 
                              colorScheme="brand"
                              size="sm"
                              onClick={() => navigate(`/hotels/${favorite.hotel.id}`)}
                            >
                              Details
                            </Button>
                            <Button 
                              rightIcon={<FiCalendar />} 
                              colorScheme="brand"
                              size="sm"
                              onClick={() => navigate(`/hotels/${favorite.hotel.id}`)}
                            >
                              Book Now
                            </Button>
                          </Flex>
                        </CardBody>
                      </Card>
                    ))}
                </SimpleGrid>
              </TabPanel>
            ))}
          </TabPanels>
        </Tabs>
      )}
    </Container>
  );
}

export default Favorites;