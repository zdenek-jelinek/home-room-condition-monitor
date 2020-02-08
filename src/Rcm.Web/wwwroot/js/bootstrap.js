requirejs.config({
    baseUrl: "/js",
    urlArgs: "v=" + new Date().getTime(),
    packages: [
        "flatpickr", { name: "flatpickr", location: "../lib/flatpickr/dist", main: "flatpickr" }
    ],
    bundles: {
        "compiled": ["Views/History", "Views/Daily", "Views/Now", "Views/ConfigureConnection"]
    }
});

if (pageModule)
{
    requirejs([`Views/${pageModule}`], function (module) { module.initialize(); });
}