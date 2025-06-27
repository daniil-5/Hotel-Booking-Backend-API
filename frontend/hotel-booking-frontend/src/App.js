import React from 'react';
import { ChakraProvider } from '@chakra-ui/react';
import { BrowserRouter as Router } from 'react-router-dom';
import theme from './theme';
import Routes from './Routes';

function App() {
  return (
    <ChakraProvider theme={theme}>
      <Router future={{
        v7_startTransition: true,
        v7_relativeSplatPath: true
      }}>
        <Routes />
      </Router>
    </ChakraProvider>
  );
}

export default App;
