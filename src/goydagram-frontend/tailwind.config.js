/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        ink: {
          950: "#0E0E14",
          900: "#15151F",
          800: "#1D1D2A",
          700: "#2A2A3B",
          600: "#3D3D54",
          400: "#7A7A96",
          200: "#C7C7DA",
          100: "#EDEDF4",
        },
        flare: {
          500: "#FF3D6E",
          600: "#E62B5B",
          400: "#FF6B93",
        },
        mint: {
          400: "#37E6C4",
          500: "#1FCBAA",
        },
      },
      fontFamily: {
        display: ["'Space Grotesk'", "sans-serif"],
        body: ["'Inter'", "sans-serif"],
      },
    },
  },
  plugins: [],
};
