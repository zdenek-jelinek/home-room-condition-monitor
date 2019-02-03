requirejs.config({
    baseUrl: "js",
    packages: [
        "flatpickr", { name: "flatpickr", location: "../lib/flatpickr/dist", main: "flatpickr" }
    ],
    bundles: {
        "compiled": ["Index"]
    }
});

requirejs([pageModule], function (module) { module.initialize(); });