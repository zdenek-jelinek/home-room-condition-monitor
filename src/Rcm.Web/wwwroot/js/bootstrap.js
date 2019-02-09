requirejs.config({
    baseUrl: "js",
    packages: [
        "flatpickr", { name: "flatpickr", location: "../lib/flatpickr/dist", main: "flatpickr" }
    ],
    bundles: {
        "compiled": ["History", "Daily"]
    }
});

requirejs([pageModule], function (module) { module.initialize(); });