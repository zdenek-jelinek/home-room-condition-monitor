using System;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Rcm.Backend.Common.Http
{
    public class ExceptionHandler
    {
        private readonly ILogger _logger;
        private readonly string _environment;

        public ExceptionHandler(ILogger logger, string environment)
        {
            (_logger, _environment) = (logger, environment);
        }

        public IActionResult Handle(Exception exception)
        {
            _logger.LogError(exception.ToString());

            if (exception is InputValidationException validationException)
            {
                return new ObjectResult(JsonSerializer.Serialize(new { validationException.Path, validationException.ErrorMessage }))
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }
            else if (exception is AuthorizationException)
            {
                return new UnauthorizedResult();
            }
            else if (exception is ConflictException)
            {
                return new ConflictObjectResult(exception.Message);
            }
            else
            {
                if (EnvironmentNames.IsDevelopment(_environment) || EnvironmentNames.IsStaging(_environment))
                {
                    return new ObjectResult(exception.ToString())
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                }
                else
                {
                    return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
                }
            }
        }
    }
}
