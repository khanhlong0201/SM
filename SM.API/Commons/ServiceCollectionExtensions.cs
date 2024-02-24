using SM.API.Services;
namespace SM.API.Commons
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IMasterDataService, MasterDataService>();
            return services;
        }
    }
}
