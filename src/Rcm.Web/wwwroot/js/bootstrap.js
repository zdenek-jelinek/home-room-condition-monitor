requirejs.config({
    baseUrl: "js",
    packages: [
        "flatpickr", { name: "flatpickr", location: "../lib/flatpickr/dist", main: "flatpickr" }
    ],
    bundles: {
        "compiled": ["History", "Daily"]
    }
});

if (pageModule)
{
    requirejs([pageModule], function (module) { module.initialize(); });
}