import React, { useEffect, useState } from 'react';
import {
  Box,
  Text,
  Skeleton,
  Alert,
  AlertIcon,
  useColorModeValue
} from '@chakra-ui/react';

const LocationMap = ({ location, name, coordinates }) => {
  const [isLoaded, setIsLoaded] = useState(false);
  const [error, setError] = useState(null);
  const bgColor = useColorModeValue('white', 'gray.700');
  
  // If we have explicit coordinates, use them, otherwise generate from location
  const mapQuery = coordinates 
    ? `${coordinates.latitude},${coordinates.longitude}`
    : encodeURIComponent(location);
  
  const mapUrl = `https://maps.google.com/maps?q=${mapQuery}&t=&z=13&ie=UTF8&iwloc=&output=embed`;
  
  const handleMapLoad = () => {
    setIsLoaded(true);
  };
  
  const handleMapError = () => {
    setError('Could not load the map. Please check your internet connection.');
    setIsLoaded(true);
  };
  
  return (
    <Box borderRadius="lg" overflow="hidden" borderWidth="1px" borderColor={useColorModeValue('gray.200', 'gray.600')}>
      <Skeleton isLoaded={isLoaded} height={isLoaded ? "auto" : "400px"}>
        {error ? (
          <Alert status="error" variant="solid">
            <AlertIcon />
            {error}
          </Alert>
        ) : (
          <Box position="relative" height="400px" width="100%">
            <iframe
              title={`Map showing location of ${name}`}
              src={mapUrl}
              width="100%"
              height="100%"
              style={{ border: 0 }}
              allowFullScreen=""
              loading="lazy"
              onLoad={handleMapLoad}
              onError={handleMapError}
            />
          </Box>
        )}
      </Skeleton>
      
      <Box p={4} bg={bgColor}>
        <Text fontWeight="medium">{name}</Text>
        <Text fontSize="sm" color="gray.600">{location}</Text>
      </Box>
    </Box>
  );
};

export default LocationMap;