import React from 'react';
import { Box, Flex, Text, Button, Stack, useColorModeValue, useColorMode } from '@chakra-ui/react';
import { Link as RouterLink } from 'react-router-dom';
import { MoonIcon, SunIcon } from '@chakra-ui/icons';

export default function Header() {
  const { colorMode, toggleColorMode } = useColorMode();
  const bgColor = useColorModeValue('white', 'gray.800');
  
  return (
    <Box>
      <Flex
        bg={bgColor}
        color={useColorModeValue('gray.600', 'white')}
        minH={'60px'}
        py={{ base: 2 }}
        px={{ base: 4 }}
        borderBottom={1}
        borderStyle={'solid'}
        borderColor={useColorModeValue('gray.200', 'gray.700')}
        align={'center'}
        justify={'space-between'}
      >
        <Text
          as={RouterLink}
          to="/"
          fontFamily={'heading'}
          fontWeight={'bold'}
          color={useColorModeValue('gray.1000', 'white')}
        >
          Hotel Booking
        </Text>
        
        <Stack direction={'row'} spacing={4}>
          <Button onClick={toggleColorMode}>
            {colorMode === 'light' ? <MoonIcon /> : <SunIcon />}
          </Button>
          <Button as={RouterLink} to="/login" variant={'ghost'}>
            Sign In
          </Button>
          <Button as={RouterLink} to="/register" colorScheme={'brand'}>
            Sign Up
          </Button>
        </Stack>
      </Flex>
    </Box>
  );
}
