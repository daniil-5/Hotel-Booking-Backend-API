import React, { useState } from 'react';
import {
  Box,
  Image,
  SimpleGrid,
  Modal,
  ModalOverlay,
  ModalContent,
  ModalBody,
  ModalCloseButton,
  useDisclosure,
  IconButton,
  Flex,
  useColorModeValue
} from '@chakra-ui/react';
import { FiArrowLeft, FiArrowRight, FiMaximize, FiCamera } from 'react-icons/fi';

const HotelGallery = ({ photos, hotelName }) => {
  const { isOpen, onOpen, onClose } = useDisclosure();
  const [currentPhotoIndex, setCurrentPhotoIndex] = useState(0);
  const bgColor = useColorModeValue('gray.100', 'gray.700');
  
  // Ensure we have an array of photos
  const photoArray = Array.isArray(photos) ? photos : [];
  
  // Function to handle navigation in the gallery modal
  const navigateGallery = (direction) => {
    if (direction === 'next') {
      setCurrentPhotoIndex((prevIndex) => 
        prevIndex === photoArray.length - 1 ? 0 : prevIndex + 1
      );
    } else {
      setCurrentPhotoIndex((prevIndex) => 
        prevIndex === 0 ? photoArray.length - 1 : prevIndex - 1
      );
    }
  };
  
  // Open the gallery modal with a specific photo
  const openGalleryWithPhoto = (index) => {
    setCurrentPhotoIndex(index);
    onOpen();
  };
  
  // If no photos are available
  if (photoArray.length === 0) {
    return (
      <Flex 
        height="400px" 
        bg={bgColor} 
        borderRadius="lg" 
        align="center" 
        justify="center"
        direction="column"
      >
        <FiCamera size="48px" />
        <Box mt={2}>No photos available for {hotelName}</Box>
      </Flex>
    );
  }
  
  // Get main photo (first photo or one marked as main)
  const mainPhoto = photoArray.find(photo => photo.isMain) || photoArray[0];
  
  // Get other photos for the thumbnail grid
  const otherPhotos = photoArray.filter(photo => photo !== mainPhoto).slice(0, 4);
  
  return (
    <>
      <Box position="relative" borderRadius="lg" overflow="hidden">
        {/* Main Featured Photo */}
        <Box 
          height="400px" 
          position="relative" 
          cursor="pointer"
          onClick={() => openGalleryWithPhoto(photoArray.indexOf(mainPhoto))}
        >
          <Image 
            src={mainPhoto.url} 
            alt={`${hotelName} - Main Photo`}
            objectFit="cover"
            width="100%"
            height="100%"
          />
          <IconButton
            aria-label="View full gallery"
            icon={<FiMaximize />}
            position="absolute"
            bottom="10px"
            right="10px"
            colorScheme="blackAlpha"
            size="md"
            onClick={(e) => {
              e.stopPropagation();
              onOpen();
            }}
          />
        </Box>
        
        {/* Thumbnail Grid */}
        {otherPhotos.length > 0 && (
          <SimpleGrid columns={4} spacing={2} mt={2}>
            {otherPhotos.map((photo, index) => (
              <Box 
                key={index} 
                height="100px" 
                cursor="pointer"
                onClick={() => openGalleryWithPhoto(photoArray.indexOf(photo))}
              >
                <Image 
                  src={photo.url} 
                  alt={`${hotelName} - Photo ${index + 2}`}
                  objectFit="cover"
                  width="100%"
                  height="100%"
                  borderRadius="md"
                />
              </Box>
            ))}
            
            {/* Show "View All" tile if there are more photos */}
            {photoArray.length > 5 && (
              <Flex 
                height="100px" 
                bg="blackAlpha.700" 
                color="white"
                align="center"
                justify="center"
                borderRadius="md"
                cursor="pointer"
                onClick={onOpen}
              >
                View All ({photoArray.length})
              </Flex>
            )}
          </SimpleGrid>
        )}
      </Box>
      
      {/* Full Gallery Modal */}
      <Modal isOpen={isOpen} onClose={onClose} size="xl" isCentered>
        <ModalOverlay />
        <ModalContent>
          <ModalCloseButton zIndex="modal" color="white" />
          <ModalBody p={0} position="relative">
            <Box height="600px">
              <Image 
                src={photoArray[currentPhotoIndex].url} 
                alt={`${hotelName} - Gallery Photo ${currentPhotoIndex + 1}`}
                objectFit="cover"
                width="100%"
                height="100%"
              />
            </Box>
            
            <Flex 
              position="absolute" 
              bottom="0" 
              left="0" 
              right="0" 
              justify="space-between" 
              p={4}
              bgGradient="linear(to-t, blackAlpha.800, blackAlpha.300, transparent)"
              color="white"
            >
              <Box>{currentPhotoIndex + 1} / {photoArray.length}</Box>
              <Box>
                {photoArray[currentPhotoIndex].description || hotelName}
              </Box>
            </Flex>
            
            <IconButton
              aria-label="Previous photo"
              icon={<FiArrowLeft size="20px" />}
              position="absolute"
              left="10px"
              top="50%"
              transform="translateY(-50%)"
              colorScheme="blackAlpha"
              size="lg"
              isRound
              onClick={() => navigateGallery('prev')}
            />
            
            <IconButton
              aria-label="Next photo"
              icon={<FiArrowRight size="20px" />}
              position="absolute"
              right="10px"
              top="50%"
              transform="translateY(-50%)"
              colorScheme="blackAlpha"
              size="lg"
              isRound
              onClick={() => navigateGallery('next')}
            />
          </ModalBody>
        </ModalContent>
      </Modal>
    </>
  );
};

export default HotelGallery;