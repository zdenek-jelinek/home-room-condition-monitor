requirejs.config({
    baseUrl: "js",
    packages: [
        "flatpickr", { name: "flatpickr", location: "../lib/flatpickr/dist", main: "flatpickr" }
    ],
    bundles: {
        "compiled": ["Views/History", "Views/Daily", "Views/Now"]
    }
});

if (pageModule)
{
    requirejs([`Views/${pageModule}`], function (module) { module.initialize(); });
}