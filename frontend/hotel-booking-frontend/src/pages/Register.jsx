
import React, { useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  FormControl,
  FormLabel,
  Input,
  InputGroup,
  InputRightElement,
  Stack,
  Heading,
  Text,
  Link,
  FormErrorMessage,
  useColorMode,
  Alert,
  AlertIcon,
  HStack,
  Container,
  Checkbox,
  useToast,
  Divider
} from '@chakra-ui/react';
import { ViewIcon, ViewOffIcon } from '@chakra-ui/icons';
import apiClient from '../services/api';
import authService from '../services/authService';

function Register() {
  const navigate = useNavigate();
  const toast = useToast();
  
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    username: '',         // Added username field
    phoneNumber: '',      // Added phoneNumber field
    password: '',
    confirmPassword: '',
  });
  
  const [errors, setErrors] = useState({});
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [acceptTerms, setAcceptTerms] = useState(false);
  
  const { colorMode } = useColorMode();
  const bgColor = colorMode === 'light' ? 'white' : 'gray.800';
  const textColor = colorMode === 'light' ? 'gray.600' : 'gray.200';
  const boxShadow = colorMode === 'light' ? 'lg' : 'dark-lg';
  
  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
    
    // Clear error for this field when user starts typing again
    if (errors[name]) {
      setErrors(prev => ({
        ...prev,
        [name]: ''
      }));
    }
    
    // Also clear general error message
    if (error) {
      setError('');
    }
  };
  
  const validateForm = () => {
    const newErrors = {};
    
    if (!formData.firstName.trim()) {
      newErrors.firstName = 'First name is required';
    }
    
    if (!formData.lastName.trim()) {
      newErrors.lastName = 'Last name is required';
    }
    
    if (!formData.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      newErrors.email = 'Email is invalid';
    }
    
    // Validate the new username field
    if (!formData.username.trim()) {
      newErrors.username = 'Username is required';
    }
    
    // Validate the new phoneNumber field
    if (!formData.phoneNumber.trim()) {
      newErrors.phoneNumber = 'Phone number is required';
    } else if (!/^[0-9+\-\s()]{7,15}$/.test(formData.phoneNumber)) {
      newErrors.phoneNumber = 'Phone number is invalid';
    }
    
    if (!formData.password) {
      newErrors.password = 'Password is required';
    } else if (formData.password.length < 8) {
      newErrors.password = 'Password must be at least 8 characters';
    }
    
    if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match';
    }
    
    if (!acceptTerms) {
      newErrors.terms = 'You must accept the terms and conditions';
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };
  
  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }
    
    setIsLoading(true);
    setError('');
    
    try {
      // Create the register DTO with ALL required fields
      const registerDto = {
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        username: formData.username,        // Include username
        phoneNumber: formData.phoneNumber,  // Include phoneNumber
        password: formData.password
      };
      
      console.log('Sending register data:', registerDto);
      
      // Call the register API endpoint
      const response = await apiClient.post('/auth/register', registerDto);
      
      console.log('Registration successful, response:', response.data);
      
      // Process the response data
      if (response.data && response.data.token) {
        // Store token if needed (usually not needed on registration)
        authService.processAuthResponse(response.data);
      }
      
      // Show success message
      toast({
        title: 'Registration successful',
        description: 'Your account has been created. You can now log in.',
        status: 'success',
        duration: 5000,
        isClosable: true,
      });
      
      // Navigate to login page with a query parameter indicating successful registration
      navigate('/login?registered=true');
      
    } catch (err) {
      console.error('Registration error:', err);
      
      // Handle error response
      if (err.response) {
        console.error('Error response data:', err.response.data);
        
        // Handle ASP.NET Core Problem Details format
        if (err.response.headers['content-type']?.includes('application/problem+json')) {
          const problemDetails = err.response.data;
          
          console.log('Problem details:', problemDetails);
          
          if (problemDetails.title) {
            setError(problemDetails.title);
            
            // If there are validation errors, extract them
            if (problemDetails.errors) {
              const serverErrors = {};
              
              Object.entries(problemDetails.errors).forEach(([key, value]) => {
                // Convert server field names to match our form field names
                let fieldName = key.charAt(0).toLowerCase() + key.slice(1);
                
                // Handle special cases
                if (fieldName === 'userName') fieldName = 'username';
                if (fieldName === 'phoneNumber') fieldName = 'phoneNumber';
                
                serverErrors[fieldName] = Array.isArray(value) ? value[0] : value;
              });
              
              setErrors(prev => ({ ...prev, ...serverErrors }));
            }
          } else {
            setError('Registration failed. Please check your information and try again.');
          }
        } else if (typeof err.response.data === 'string') {
          setError(err.response.data);
        } else {
          setError('Registration failed. Please try again with different information.');
        }
      } else if (err.request) {
        setError('No response from server. Please check your internet connection.');
      } else {
        setError(err.message || 'Registration failed. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };
  
  return (
    <Container maxW="lg" py={{ base: 10, md: 20 }} px={{ base: 4, sm: 8 }}>
      <Stack spacing={8}>
        <Stack align="center">
          <Heading fontSize="3xl" textAlign="center">
            Create your account
          </Heading>
          <Text fontSize="lg" color={textColor}>
            to start booking your dream hotel 
          </Text>
        </Stack>
        
        <Box rounded="lg" bg={bgColor} boxShadow={boxShadow} p={8}>
          {error && (
            <Alert status="error" mb={4} borderRadius="md">
              <AlertIcon />
              {error}
            </Alert>
          )}
          
          <form onSubmit={handleSubmit}>
            <Stack spacing={4}>
              <HStack>
                <FormControl id="firstName" isRequired isInvalid={!!errors.firstName}>
                  <FormLabel>First Name</FormLabel>
                  <Input
                    type="text"
                    name="firstName"
                    value={formData.firstName}
                    onChange={handleChange}
                  />
                  <FormErrorMessage>{errors.firstName}</FormErrorMessage>
                </FormControl>
                
                <FormControl id="lastName" isRequired isInvalid={!!errors.lastName}>
                  <FormLabel>Last Name</FormLabel>
                  <Input
                    type="text"
                    name="lastName"
                    value={formData.lastName}
                    onChange={handleChange}
                  />
                  <FormErrorMessage>{errors.lastName}</FormErrorMessage>
                </FormControl>
              </HStack>
              
              <FormControl id="email" isRequired isInvalid={!!errors.email}>
                <FormLabel>Email address</FormLabel>
                <Input
                  type="email"
                  name="email"
                  value={formData.email}
                  onChange={handleChange}
                  placeholder="your@email.com"
                />
                <FormErrorMessage>{errors.email}</FormErrorMessage>
              </FormControl>
              
              {/* New username field */}
              <FormControl id="username" isRequired isInvalid={!!errors.username}>
                <FormLabel>Username</FormLabel>
                <Input
                  type="text"
                  name="username"
                  value={formData.username}
                  onChange={handleChange}
                  placeholder="Choose a username"
                />
                <FormErrorMessage>{errors.username}</FormErrorMessage>
              </FormControl>
              
              {/* New phone number field */}
              <FormControl id="phoneNumber" isRequired isInvalid={!!errors.phoneNumber}>
                <FormLabel>Phone Number</FormLabel>
                <Input
                  type="tel"
                  name="phoneNumber"
                  value={formData.phoneNumber}
                  onChange={handleChange}
                  placeholder="+1 (555) 123-4567"
                />
                <FormErrorMessage>{errors.phoneNumber}</FormErrorMessage>
              </FormControl>
              
              <Divider my={2} />
              
              <FormControl id="password" isRequired isInvalid={!!errors.password}>
                <FormLabel>Password</FormLabel>
                <InputGroup>
                  <Input
                    type={showPassword ? 'text' : 'password'}
                    name="password"
                    value={formData.password}
                    onChange={handleChange}
                    placeholder="At least 8 characters"
                  />
                  <InputRightElement h="full">
                    <Button
                      variant="ghost"
                      onClick={() => setShowPassword((show) => !show)}
                    >
                      {showPassword ? <ViewOffIcon /> : <ViewIcon />}
                    </Button>
                  </InputRightElement>
                </InputGroup>
                <FormErrorMessage>{errors.password}</FormErrorMessage>
              </FormControl>
              
              <FormControl id="confirmPassword" isRequired isInvalid={!!errors.confirmPassword}>
                <FormLabel>Confirm Password</FormLabel>
                <InputGroup>
                  <Input
                    type={showConfirmPassword ? 'text' : 'password'}
                    name="confirmPassword"
                    value={formData.confirmPassword}
                    onChange={handleChange}
                    placeholder="Confirm your password"
                  />
                  <InputRightElement h="full">
                    <Button
                      variant="ghost"
                      onClick={() => setShowConfirmPassword((show) => !show)}
                    >
                      {showConfirmPassword ? <ViewOffIcon /> : <ViewIcon />}
                    </Button>
                  </InputRightElement>
                </InputGroup>
                <FormErrorMessage>{errors.confirmPassword}</FormErrorMessage>
              </FormControl>
              
              <FormControl isInvalid={!!errors.terms}>
                <Checkbox
                  colorScheme="brand"
                  isChecked={acceptTerms}
                  onChange={(e) => setAcceptTerms(e.target.checked)}
                >
                  I agree to the{' '}
                  <Link as={RouterLink} to="/terms" color="brand.500">
                    Terms of Service
                  </Link>{' '}
                  and{' '}
                  <Link as={RouterLink} to="/privacy" color="brand.500">
                    Privacy Policy
                  </Link>
                </Checkbox>
                <FormErrorMessage>{errors.terms}</FormErrorMessage>
              </FormControl>
              
              <Stack spacing={10} pt={2}>
                <Button
                  loadingText="Creating account"
                  size="lg"
                  bg="brand.500"
                  color="white"
                  _hover={{ bg: 'brand.600' }}
                  type="submit"
                  isLoading={isLoading}
                >
                  Sign up
                </Button>
              </Stack>
            </Stack>
          </form>
        </Box>
        
        <Stack pt={2} direction="row" justify="center">
          <Text>
            Already a user?{' '}
            <Link as={RouterLink} to="/login" color="brand.500" fontWeight={600}>
              Sign in
            </Link>
          </Text>
        </Stack>
      </Stack>
    </Container>
  );
}

export default Register;