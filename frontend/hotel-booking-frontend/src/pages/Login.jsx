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
  Flex,
  Container,
} from '@chakra-ui/react';
import { ViewIcon, ViewOffIcon } from '@chakra-ui/icons';
import { authService } from '../services/api'; // Import the auth service

function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [emailError, setEmailError] = useState('');
  const [passwordError, setPasswordError] = useState('');
  
  const navigate = useNavigate(); // Initialize the navigate function

  const { colorMode } = useColorMode();
  const bgColor = colorMode === 'light' ? 'white' : 'gray.800';
  const textColor = colorMode === 'light' ? 'gray.600' : 'gray.200';
  const boxShadow = colorMode === 'light' ? 'lg' : 'dark-lg';

  const validateForm = () => {
    let isValid = true;
    setEmailError('');
    setPasswordError('');
    
    if (!email) {
      setEmailError('Email is required');
      isValid = false;
    } else if (!/\S+@\S+\.\S+/.test(email)) {
      setEmailError('Email is invalid');
      isValid = false;
    }
    
    if (!password) {
      setPasswordError('Password is required');
      isValid = false;
    }
    
    return isValid;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }
    
    setIsLoading(true);
    setError('');
    
    try {
      console.log('Attempting login with:', { email });
      const response = await authService.login(email, password);
      console.log('Login response:', response);
      
      // On successful login, redirect to home/dashboard
      navigate('/dashboard');
    } catch (err) {
      console.error('Login error:', err);
      setError(err.response?.data || err.message || 'Failed to log in. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Container maxW="lg" py={{ base: 12, md: 20 }} px={{ base: 4, sm: 8 }}>
      <Stack spacing={8}>
        <Stack align="center">
          <Heading fontSize="3xl" textAlign="center">
            Sign in to your account
          </Heading>
          <Text fontSize="lg" color={textColor}>
            to enjoy all our cool features 
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
              <FormControl id="email" isRequired isInvalid={!!emailError}>
                <FormLabel>Email address</FormLabel>
                <Input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="your@email.com"
                />
                <FormErrorMessage>{emailError}</FormErrorMessage>
              </FormControl>
              
              <FormControl id="password" isRequired isInvalid={!!passwordError}>
                <FormLabel>Password</FormLabel>
                <InputGroup>
                  <Input
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Enter your password"
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
                <FormErrorMessage>{passwordError}</FormErrorMessage>
              </FormControl>
              
              <Stack spacing={6}>
                <Flex justify="flex-end">
                  <Link as={RouterLink} to="/reset-password" color="brand.500">
                    Forgot password?
                  </Link>
                </Flex>
                
                <Button
                  colorScheme="brand"
                  bg="brand.500"
                  _hover={{ bg: 'brand.600' }}
                  type="submit"
                  size="lg"
                  fontSize="md"
                  isLoading={isLoading}
                  loadingText="Signing in"
                >
                  Sign in
                </Button>
              </Stack>
            </Stack>
          </form>
        </Box>
        
        <Stack pt={4} direction="row" justify="center">
          <Text>
            New to our platform?{' '}
            <Link as={RouterLink} to="/register" color="brand.500" fontWeight={600}>
              Sign up
            </Link>
          </Text>
        </Stack>
      </Stack>
    </Container>
  );
}

export default Login;