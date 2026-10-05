// ESLint для минималистичного SPA (vanilla JS, без сборки).
// Запуск: npx eslint "BookingSystem.API/BookingSystem.API/wwwroot/app.js"
export default [
  {
    files: ['**/wwwroot/**/*.js'],
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: 'script',
      globals: {
        document: 'readonly', window: 'readonly', localStorage: 'readonly',
        fetch: 'readonly', atob: 'readonly', btoa: 'readonly',
        console: 'readonly', URLSearchParams: 'readonly', FormData: 'readonly',
        setTimeout: 'readonly', clearTimeout: 'readonly', setInterval: 'readonly',
        confirm: 'readonly', alert: 'readonly',
      },
    },
    rules: {
      'no-undef': 'error',
      'no-unused-vars': ['warn', { args: 'none', caughtErrors: 'none' }],
      'no-redeclare': 'error',
      'no-dupe-keys': 'error',
      'no-use-before-define': ['error', { functions: false }],
      eqeqeq: ['error', 'smart'],
      'no-var': 'error',
      semi: ['error', 'always'],
    },
  },
];
