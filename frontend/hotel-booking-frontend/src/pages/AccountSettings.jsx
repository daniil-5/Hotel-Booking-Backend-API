import React, { useState, useEffect } from 'react';
import {
  Box,
  Container,
  Heading,
  Text,
  Button,
  VStack,
  HStack,
  Tabs,
  TabList,
  TabPanels,
  Tab,
  TabPanel,
  FormControl,
  FormLabel,
  Input,
  InputGroup,
  InputRightElement,
  Switch,
  Select,
  Card,
  CardBody,
  Divider,
  useColorModeValue,
  Alert,
  AlertIcon,
  useToast,
  Spinner,
  Flex
} from '@chakra-ui/react';
import { useNavigate } from 'react-router-dom';
import {
  FiArrowLeft,
  FiUser,
  FiLock,
  FiEye,
  FiEyeOff,
  FiBell,
  FiSave
} from 'react-icons/fi';
import apiClient from '../services/api';

function AccountSettings() {
  const navigate = useNavigate();
  const toast = useToast();
  
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [userData, setUserData] = useState(null);
  
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  
  const [notificationSettings, setNotificationSettings] = useState({
    emailNotifications: true,
    promotionalEmails: true,
    bookingReminders: true,
    appNotifications: true
  });
  
  const [showPassword, setShowPassword] = useState({
    current: false,
    new: false,
    confirm: false
  });
  
  const [preferences, setPreferences] = useState({
    language: 'en',
    currency: 'USD',
    timeFormat: '12h'
  });
  
  const bgColor = useColorModeValue('white', 'gray.800');
  const borderColor = useColorModeValue('gray.200', 'gray.700');
  
  useEffect(() => {
    const fetchUserData = async () => {
      setIsLoading(true);
      try {
        // Fetch user data
        const userResponse = await apiClient.get('/auth/current');
        setUserData(userResponse.data);
        
        // Fetch user preferences if API supports it
        try {
          const preferencesResponse = await apiClient.get('/users/preferences');
          setPreferences(preferencesResponse.data);
        } catch (prefsError) {
          console.warn('Could not fetch user preferences:', prefsError);
          // Continue using default preferences
        }
        
        // Fetch notification settings if API supports it
        try {
          const notificationsResponse = await apiClient.get('/users/notifications');
          setNotificationSettings(notificationsResponse.data);
        } catch (notifError) {
          console.warn('Could not fetch notification settings:', notifError);
          // Continue using default notification settings
        }
        
        setError(null);
      } catch (err) {
        console.error('Error fetching user data:', err);
        setError('Failed to load account settings. Please try again.');
        
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
    
    fetchUserData();
  }, [navigate, toast]);
  
  // Update the handlePasswordChange function to match your API
  const handlePasswordChange = async () => {
    // Validate passwords match
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      toast({
        title: 'Passwords do not match',
        description: 'New password and confirmation must match.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
      return;
    }
    
    // Validate password length
    if (passwordForm.newPassword.length < 8) {
      toast({
        title: 'Password too short',
        description: 'Password must be at least 8 characters long.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
      return;
    }
    
    try {
      // Use POST method and correct endpoint path with the right DTO structure
      await apiClient.post('/users/change-password', {
        userId: userData.id, // Include user ID from the loaded user data
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword,
        confirmPassword: passwordForm.confirmPassword // Include confirmation password
      });
      
      toast({
        title: 'Password updated',
        description: 'Your password has been updated successfully.',
        status: 'success',
        duration: 5000,
        isClosable: true,
      });
      
      // Clear password form
      setPasswordForm({
        currentPassword: '',
        newPassword: '',
        confirmPassword: ''
      });
    } catch (err) {
      // Improved error handling
      let errorMessage = 'An error occurred while updating your password.';
      
      if (err.response) {
        if (typeof err.response.data === 'object') {
          errorMessage = err.response.data.message || err.response.data.title || JSON.stringify(err.response.data);
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
  
  const handlePreferencesSave = async () => {
    try {
      await apiClient.put('/users/preferences', preferences);
      
      toast({
        title: 'Preferences updated',
        description: 'Your preferences have been updated successfully.',
        status: 'success',
        duration: 5000,
        isClosable: true,
      });
    } catch (err) {
      toast({
        title: 'Update failed',
        description: err.response?.data || 'An error occurred while updating your preferences.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    }
  };
  
  const handleNotificationSettingsSave = async () => {
    try {
      await apiClient.put('/users/notifications', notificationSettings);
      
      toast({
        title: 'Notification settings updated',
        description: 'Your notification preferences have been updated successfully.',
        status: 'success',
        duration: 5000,
        isClosable: true,
      });
    } catch (err) {
      toast({
        title: 'Update failed',
        description: err.response?.data || 'An error occurred while updating your notification settings.',
        status: 'error',
        duration: 5000,
        isClosable: true,
      });
    }
  };
  
  const handlePasswordInputChange = (e) => {
    const { name, value } = e.target;
    setPasswordForm(prev => ({
      ...prev,
      [name]: value
    }));
  };
  
  const handleNotificationToggle = (setting) => {
    setNotificationSettings(prev => ({
      ...prev,
      [setting]: !prev[setting]
    }));
  };
  
  const handlePreferenceChange = (e) => {
    const { name, value } = e.target;
    setPreferences(prev => ({
      ...prev,
      [name]: value
    }));
  };
  
  const togglePasswordVisibility = (field) => {
    setShowPassword(prev => ({
      ...prev,
      [field]: !prev[field]
    }));
  };
  
  if (isLoading) {
    return (
      <Flex justify="center" align="center" height="100vh">
        <Spinner size="xl" thickness="4px" speed="0.65s" color="brand.500" />
      </Flex>
    );
  }
  
  if (error || !userData) {
    return (
      <Box textAlign="center" py={10} px={6}>
        <Heading as="h2" size="xl" mt={6} mb={2}>
          Error Loading Settings
        </Heading>
        <Text>{error || 'User data not found'}</Text>
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
      
      <Heading as="h1" size="xl" mb={6}>
        Account Settings
      </Heading>
      
      <Card bg={bgColor} shadow="md" borderRadius="lg">
        <CardBody>
          <Tabs colorScheme="brand" isLazy>
            <TabList>
              <Tab><HStack><FiUser /><Text>Account</Text></HStack></Tab>
              <Tab><HStack><FiLock /><Text>Security</Text></HStack></Tab>
              <Tab><HStack><FiBell /><Text>Notifications</Text></HStack></Tab>
            </TabList>
            
            <TabPanels>
              {/* Account Settings Tab */}
              <TabPanel>
                <Box maxW="600px">
                  <Heading size="md" mb={4}>General Settings</Heading>
                  
                  <FormControl mb={4}>
                    <FormLabel>Display Language</FormLabel>
                    <Select 
                      name="language" 
                      value={preferences.language}
                      onChange={handlePreferenceChange}
                    >
                      <option value="en">English</option>
                      <option value="es">Spanish</option>
                      <option value="fr">French</option>
                      <option value="de">German</option>
                      <option value="zh">Chinese</option>
                    </Select>
                  </FormControl>
                  
                  <FormControl mb={4}>
                    <FormLabel>Currency</FormLabel>
                    <Select 
                      name="currency" 
                      value={preferences.currency}
                      onChange={handlePreferenceChange}
                    >
                      <option value="USD">US Dollar ($)</option>
                      <option value="EUR">Euro (€)</option>
                      <option value="GBP">British Pound (£)</option>
                      <option value="JPY">Japanese Yen (¥)</option>
                      <option value="CAD">Canadian Dollar (C$)</option>
                    </Select>
                  </FormControl>
                  
                  <FormControl mb={4}>
                    <FormLabel>Time Format</FormLabel>
                    <Select 
                      name="timeFormat" 
                      value={preferences.timeFormat}
                      onChange={handlePreferenceChange}
                    >
                      <option value="12h">12-hour (AM/PM)</option>
                      <option value="24h">24-hour</option>
                    </Select>
                  </FormControl>
                  
                  <Button 
                    mt={4}
                    colorScheme="brand"
                    leftIcon={<FiSave />}
                    onClick={handlePreferencesSave}
                  >
                    Save Preferences
                  </Button>
                  
                  <Divider my={8} />
                  
                  <Heading size="md" mb={4}>Delete Account</Heading>
                  <Text mb={4} color="gray.600">
                    Once you delete your account, there is no going back. Please be certain.
                  </Text>
                  <Button colorScheme="red" variant="outline">
                    Delete Account
                  </Button>
                </Box>
              </TabPanel>
              
              {/* Security Tab */}
              <TabPanel>
                <Box maxW="600px">
                  <Heading size="md" mb={4}>Change Password</Heading>
                  
                  <FormControl mb={4}>
                    <FormLabel>Current Password</FormLabel>
                    <InputGroup>
                      <Input
                        type={showPassword.current ? 'text' : 'password'}
                        name="currentPassword"
                        value={passwordForm.currentPassword}
                        onChange={handlePasswordInputChange}
                      />
                      <InputRightElement width="4.5rem">
                        <Button
                          h="1.75rem"
                          size="sm"
                          onClick={() => togglePasswordVisibility('current')}
                        >
                          {showPassword.current ? <FiEyeOff /> : <FiEye />}
                        </Button>
                      </InputRightElement>
                    </InputGroup>
                  </FormControl>
                  
                  <FormControl mb={4}>
                    <FormLabel>New Password</FormLabel>
                    <InputGroup>
                      <Input
                        type={showPassword.new ? 'text' : 'password'}
                        name="newPassword"
                        value={passwordForm.newPassword}
                        onChange={handlePasswordInputChange}
                      />
                      <InputRightElement width="4.5rem">
                        <Button
                          h="1.75rem"
                          size="sm"
                          onClick={() => togglePasswordVisibility('new')}
                        >
                          {showPassword.new ? <FiEyeOff /> : <FiEye />}
                        </Button>
                      </InputRightElement>
                    </InputGroup>
                  </FormControl>
                  
                  <FormControl mb={4}>
                    <FormLabel>Confirm New Password</FormLabel>
                    <InputGroup>
                      <Input
                        type={showPassword.confirm ? 'text' : 'password'}
                        name="confirmPassword"
                        value={passwordForm.confirmPassword}
                        onChange={handlePasswordInputChange}
                      />
                      <InputRightElement width="4.5rem">
                        <Button
                          h="1.75rem"
                          size="sm"
                          onClick={() => togglePasswordVisibility('confirm')}
                        >
                          {showPassword.confirm ? <FiEyeOff /> : <FiEye />}
                        </Button>
                      </InputRightElement>
                    </InputGroup>
                  </FormControl>
                  
                  <Button 
                    mt={4}
                    colorScheme="brand"
                    leftIcon={<FiSave />}
                    onClick={handlePasswordChange}
                  >
                    Update Password
                  </Button>
                  
                  <Divider my={8} />
                  
                  <Heading size="md" mb={4}>Two-Factor Authentication</Heading>
                  <Alert status="info" mb={4}>
                    <AlertIcon />
                    Two-factor authentication adds an extra layer of security to your account.
                  </Alert>
                  <Button colorScheme="brand" variant="outline">
                    Enable Two-Factor Authentication
                  </Button>
                </Box>
              </TabPanel>
              
              {/* Notifications Tab */}
              <TabPanel>
                <Box maxW="600px">
                  <Heading size="md" mb={4}>Email Notifications</Heading>
                  
                  <VStack spacing={4} align="stretch">
                    <FormControl display="flex" alignItems="center">
                      <FormLabel mb={0}>
                        Receive email notifications
                      </FormLabel>
                      <Switch 
                        colorScheme="brand" 
                        isChecked={notificationSettings.emailNotifications}
                        onChange={() => handleNotificationToggle('emailNotifications')}
                      />
                    </FormControl>
                    
                    <FormControl display="flex" alignItems="center">
                      <FormLabel mb={0}>
                        Promotional emails and offers
                      </FormLabel>
                      <Switch 
                        colorScheme="brand" 
                        isChecked={notificationSettings.promotionalEmails}
                        onChange={() => handleNotificationToggle('promotionalEmails')}
                        isDisabled={!notificationSettings.emailNotifications}
                      />
                    </FormControl>
                    
                    <FormControl display="flex" alignItems="center">
                      <FormLabel mb={0}>
                        Booking reminders
                      </FormLabel>
                      <Switch 
                        colorScheme="brand" 
                        isChecked={notificationSettings.bookingReminders}
                        onChange={() => handleNotificationToggle('bookingReminders')}
                        isDisabled={!notificationSettings.emailNotifications}
                      />
                    </FormControl>
                  </VStack>
                  
                  <Divider my={6} />
                  
                                    <Heading size="md" mb={4}>App Notifications</Heading>
                  
                  <VStack spacing={4} align="stretch">
                    <FormControl display="flex" alignItems="center">
                      <FormLabel mb={0}>
                        Enable in-app notifications
                      </FormLabel>
                      <Switch 
                        colorScheme="brand" 
                        isChecked={notificationSettings.appNotifications}
                        onChange={() => handleNotificationToggle('appNotifications')}
                      />
                    </FormControl>
                  </VStack>
                  
                  <Button 
                    mt={6}
                    colorScheme="brand"
                    leftIcon={<FiSave />}
                    onClick={handleNotificationSettingsSave}
                  >
                    Save Notification Settings
                  </Button>
                </Box>
              </TabPanel>
            </TabPanels>
          </Tabs>
        </CardBody>
      </Card>
    </Container>
  );
}

export default AccountSettings;
