import React from 'react';
import { 
  Box, 
  Button, 
  Container, 
  Flex, 
  Heading, 
  Text, 
  Stack, 
  Image, 
  useColorMode,
  VStack,
  SimpleGrid,
  Icon
} from '@chakra-ui/react';
import { Link as RouterLink } from 'react-router-dom';
import { FaHotel, FaCalendarAlt, FaHeadset } from 'react-icons/fa';

function Home() {
  const { colorMode } = useColorMode();
  
  return (
    <Box>
      {/* Hero Section with Background */}
      <Box
        position="relative"
        h={{ base: "400px", md: "500px", lg: "600px" }}
        overflow="hidden"
      >
        {/* Background Image */}
        <Box
          position="absolute"
          top={0}
          left={0}
          right={0}
          bottom={0}
          bgImage="url('https://images.unsplash.com/photo-1445019980597-93fa8acb246c?q=80&w=2948&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D')"
          bgPosition="center"
          bgRepeat="no-repeat"
          bgSize="cover"
          zIndex={1}
          _after={{
            content: '""',
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            bg: 'rgba(0,0,0,0.4)',
            zIndex: 2
          }}
        />
        
        {/* Hero Content */}
        <Container maxW="container.xl" position="relative" zIndex={3} h="100%">
          <Flex
            direction="column"
            align="center"
            justify="center"
            h="100%"
            textAlign="center"
            color="white"
          >
            <Heading
              as="h1"
              fontSize={{ base: "3xl", md: "4xl", lg: "5xl" }}
              fontWeight="bold"
              mb={4}
            >
              Find Your Perfect Hotel Stay
            </Heading>
            <Text fontSize={{ base: "lg", md: "xl" }} mb={8} maxW="2xl">
              Browse thousands of hotels and find the perfect accommodation for your next trip
            </Text>
            
            {/* Call to Action Buttons */}
            <Stack
              direction={{ base: "column", md: "row" }}
              spacing={4}
              align="center"
            >
              <Button
                as={RouterLink}
                to="/register"
                size="lg"
                colorScheme="brand"
                fontWeight="bold"
                px={8}
                _hover={{ transform: "translateY(-2px)", boxShadow: "lg" }}
              >
                Sign Up
              </Button>
              <Button
                as={RouterLink}
                to="/login"
                size="lg"
                bg="white"
                color="gray.800"
                fontWeight="bold"
                px={8}
                _hover={{ 
                  bg: "gray.100", 
                  transform: "translateY(-2px)",
                  boxShadow: "lg"
                }}
              >
                Sign In
              </Button>
            </Stack>
          </Flex>
        </Container>
      </Box>

      {/* Features Section */}
      <Box py={20} bg={colorMode === 'light' ? 'gray.50' : 'gray.800'}>
        <Container maxW="container.xl">
          <VStack spacing={12}>
            <VStack spacing={4} textAlign="center">
              <Heading as="h2" size="xl">Why Choose Our Service</Heading>
              <Text 
                fontSize="lg" 
                color={colorMode === 'light' ? 'gray.600' : 'gray.300'}
                maxW="2xl"
              >
                We provide a seamless hotel booking experience with the best rates and excellent customer service.
              </Text>
            </VStack>
            
            <SimpleGrid columns={{ base: 1, md: 3 }} spacing={10} width="full">
              <Feature
                icon={FaHotel}
                title="Thousands of Hotels"
                text="Access to over 100,000 hotels worldwide, from budget-friendly to luxury accommodations."
              />
              <Feature
                icon={FaCalendarAlt}
                title="Flexible Booking"
                text="Free cancellation options and flexible booking policies to accommodate changing travel plans."
              />
              <Feature
                icon={FaHeadset}
                title="24/7 Customer Support"
                text="Our dedicated team is available around the clock to assist with your booking needs and answer questions."
              />
            </SimpleGrid>
          </VStack>
        </Container>
      </Box>

      {/* Call to Action Section */}
      <Box py={16} bg={colorMode === 'light' ? 'white' : 'gray.700'}>
        <Container maxW="container.md" textAlign="center">
          <Heading as="h3" size="lg" mb={6}>
            Ready to Start Your Journey?
          </Heading>
          <Text fontSize="lg" mb={8} color={colorMode === 'light' ? 'gray.600' : 'gray.300'}>
            Create your account now and get exclusive deals on your first booking.
          </Text>
          <Button
            as={RouterLink}
            to="/register"
            size="lg"
            colorScheme="brand"
            px={8}
            _hover={{ transform: "translateY(-2px)", boxShadow: "lg" }}
          >
            Create Free Account
          </Button>
        </Container>
      </Box>
    </Box>
  );
}

// Feature Component
function Feature({ title, text, icon }) {
  const { colorMode } = useColorMode();
  
  return (
    <VStack
      align="center"
      p={8}
      bg={colorMode === 'light' ? 'white' : 'gray.700'}
      borderRadius="lg"
      boxShadow="md"
      transition="all 0.3s"
      _hover={{ transform: "translateY(-5px)", boxShadow: "xl" }}
    >
      <Box
        rounded="full"
        p={3}
        bg={colorMode === 'light' ? 'brand.50' : 'brand.900'}
        color={colorMode === 'light' ? 'brand.500' : 'brand.200'}
        mb={4}
      >
        <Icon as={icon} boxSize={8} />
      </Box>
      <Heading as="h3" size="md" mb={3}>
        {title}
      </Heading>
      <Text textAlign="center" color={colorMode === 'light' ? 'gray.600' : 'gray.300'}>
        {text}
      </Text>
    </VStack>
  );
}

export default Home;