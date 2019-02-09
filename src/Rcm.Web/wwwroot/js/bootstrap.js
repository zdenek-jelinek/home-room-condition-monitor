requirejs.config({
    baseUrl: "js",
    packages: [
        "flatpickr", { name: "flatpickr", location: "../lib/flatpickr/dist", main: "flatpickr" }
    ],
    bundles: {
        "compiled": ["History", "Latest"]
    }
});

requirejs([pageModule], function (module) { module.initialize(); });