/** @type {import('tailwindcss').Config} */
module.exports = {
    important: '.dbnetsuite',
    content: ['./Views/**/*.cshtml', './Resources/JS/gridcontrol.js',],
    theme: {
        extend: {},
    },
    plugins: [require('@tailwindcss/forms'),],
}

