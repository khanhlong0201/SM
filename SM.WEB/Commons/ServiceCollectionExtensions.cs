using SM.WEB.Providers;
using SM.WEB.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace SM.WEB.Commons
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRegisterComponents(this IServiceCollection services)
        {
            services.AddTransient<IProgressService, ProgressService>();
            services.AddScoped<LoaderService>();
            services.AddScoped<ToastService>();
            return services;
        }

        public static IServiceCollection AddClientScopeService(this IServiceCollection services)
        {
            services.AddScoped<ICliMasterDataService, CliMasterDataService>();
            services.AddScoped<LoginDialogService>();
            return services;
        }

        /// <summary>
        /// Lưu trạng thái đăng nhập
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddClientAuthorization(this IServiceCollection services)
        {
            services.AddAuthorizationCore();
            services.AddAuthentication();
            services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
            return services;
        }
    }
}
