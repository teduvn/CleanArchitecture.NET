using Microsoft.AspNetCore.Mvc;
using OrderManagement.Domain.Common;

namespace OrderManagement.WebAPI.Extensions
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Map Result.Failure sang IActionResult theo chuẩn RFC 7807 Problem Details.
        /// Được dùng trong mọi Controller — không cần viết if/else lặp lại.
        /// </summary>
        public static IActionResult ToProblemDetails<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                throw new InvalidOperationException("Cannot convert success result to problem details");


            return result.Error.Code switch
            {
                // Business rule violation → 400 Bad Request
                "Error.BusinessRule" => new BadRequestObjectResult(
                    new ProblemDetails
                    {
                        Title = "Business Rule Violation",
                        Detail = result.Error.Description,
                        Status = StatusCodes.Status400BadRequest
                    }),


                // Entity không tồn tại → 404 Not Found
                "Error.NotFound" => new NotFoundObjectResult(
                    new ProblemDetails
                    {
                        Title = "Resource Not Found",
                        Detail = result.Error.Description,
                        Status = StatusCodes.Status404NotFound
                    }),


                // Validation thất bại → 422 Unprocessable Entity
                "Error.Validation" => new UnprocessableEntityObjectResult(
                    new ProblemDetails
                    {
                        Title = "Validation Failed",
                        Detail = result.Error.Description,
                        Status = StatusCodes.Status422UnprocessableEntity
                    }),


                // Fallback
                _ => new BadRequestObjectResult(
                    new ProblemDetails { Detail = result.Error.Description })
            };
        }
    }

}
