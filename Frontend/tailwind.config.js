/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#f2f8f0',
          100: '#e3f0df',
          200: '#c9e2c0',
          300: '#a6d297',
          400: '#7fbd68',
          500: '#559c40',
          600: '#367c29',
          700: '#286022',
          800: '#214d1d',
          900: '#1c3f1b',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
