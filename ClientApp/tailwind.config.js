/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  darkMode: ["class"],
  theme: {
    extend: {
      colors: {
        canvas: "#eef2f7",
        "canvas-dark": "#070c15",
        shell: "#0d1827",
        accent: "#2563eb",
      },
      boxShadow: {
        shell: "0 18px 44px -16px rgba(15,27,45,.3)",
      },
    },
  },
  plugins: [],
};
