/// <binding Clean='clean:0' />
module.exports = function (grunt)
{
    grunt.initConfig({
        clean: ["wwwroot/lib/**"],
        copy: {
            all: {
                files: [
                    {
                        src: "node_modules/requirejs/require.js",
                        dest: "wwwroot/lib/requirejs/require.js"
                    },
                    {
                        expand: true,
                        cwd: "node_modules/flatpickr/dist",
                        src: ["**/*.js", "**/*.css"],
                        dest: "wwwroot/lib/flatpickr/dist",
                        filter: "isFile"
                    }
                ]
            }
        }
    });

    grunt.loadNpmTasks("grunt-contrib-clean");
    grunt.loadNpmTasks("grunt-contrib-copy");
};