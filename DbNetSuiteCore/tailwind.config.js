/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ['./Views/**/*.cshtml', './Resources/JS/gridcontrol.js',],
  theme: {
    extend: {},
  },
    plugins: [require('@tailwindcss/forms'),],
}

