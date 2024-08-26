/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ['./Views/**/*.cshtml'],
  theme: {
    extend: {},
  },
    plugins: [require('@tailwindcss/forms'),],
}

