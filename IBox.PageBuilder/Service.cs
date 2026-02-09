using IBox.PageBuilder.Factories;
using IBox.PageBuilder.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.PageBuilder
{
    public static class Service
    {
        public static IServiceCollection AddServicePageBuilder(this IServiceCollection services)
        {
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IUpLoadAssetLibrary, UpLoadAssetLibrary>();
            services.AddScoped<IFileAssetLibraryBase, FileAssetLibraryBase>();
            services.AddScoped<IHandleFileAssetLibraryFTP, HandleFileAssetLibraryFTP>();
            services.AddScoped<IHandleFileAssetLibraryLocal, HandleFileAssetLibraryLocal>();
            services.AddScoped<IHandleFileAssetLibrarySFTP, HandleFileAssetLibrarySFTP>();
            services.AddScoped<IHandleFileAndDB, HandleFileAndDB>();
            return services;
        }
    }
}