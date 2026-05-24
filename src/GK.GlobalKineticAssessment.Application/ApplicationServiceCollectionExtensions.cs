using FluentValidation;
using GK.GlobalKineticAssessment.Application.DTOs;
using GK.GlobalKineticAssessment.Application.Interfaces;
using GK.GlobalKineticAssessment.Application.Services;
using GK.GlobalKineticAssessment.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace GK.GlobalKineticAssessment.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateCustomerRequest>, CreateCustomerRequestValidator>();
        services.AddScoped<IValidator<UpdateCustomerRequest>, UpdateCustomerRequestValidator>();
        services.AddScoped<IValidator<GetCustomersQuery>, GetCustomersQueryValidator>();
        services.AddScoped<ICustomerService, CustomerService>();
        return services;
    }
}
