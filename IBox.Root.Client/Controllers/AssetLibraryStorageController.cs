using IBox.Database.Root.Tables;
using IBox.Database.Root;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using IBox.Common.Objects;
using IBox.Database.Tenant;

namespace IBox.Root.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxRootAuthorization]
    public class AssetLibraryStorageController : ActionController<A_AssetLibraryStorage, RootContext>
    {
        private readonly IBContext<RootContext> _rootContext;

        public AssetLibraryStorageController(IBContext<RootContext> context, IBContext<RootContext> rootContext) : base(context)
        {
            _rootContext = rootContext;
        }

        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", "AppLogs");   
            });
        }

        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", "AppLogs");
            });
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var libraryStorage = CheckValid(req);

                if (libraryStorage.TypeOS == null)
                {
                    throw new IboxLog("Operating system can't null.", "AppLogs");
                }

                if (libraryStorage.TypeStorage == null)
                {
                    throw new IboxLog("Type storage can't null.", "AppLogs");
                }

                if (libraryStorage.TypeStorage == TypeStorage.FTP || libraryStorage.TypeStorage == TypeStorage.SFTP)
                {
                    if (string.IsNullOrEmpty(libraryStorage.Host?.Trim()))
                    {
                        throw new IboxLog("Host can't null or empty.", "AppLogs");
                    }

                    if (string.IsNullOrEmpty(libraryStorage.UserName?.Trim()))
                    {
                        throw new IboxLog("Username can't null or empty.", "AppLogs");
                    }

                    if (string.IsNullOrEmpty(libraryStorage.Password?.Trim()))
                    {
                        throw new IboxLog("Password can't null or empty.", "AppLogs");
                    }
                }

                var data = _rootContext.Context.A_AssetLibraryStorages.FirstOrDefault(ptr => !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Config asset library storage already exists.", "AppLogs");
                }

                return new ResponseForm<dynamic>(() => EntityAction.TryCreate<A_AssetLibraryStorage>(null, _rootContext.Context, new A_AssetLibraryStorage()
                {
                    TypeOS = libraryStorage.TypeOS,
                    TypeStorage = libraryStorage.TypeStorage,
                    Host = libraryStorage.Host,
                    Port = libraryStorage.Port,
                    UserName = libraryStorage.UserName,
                    Password = libraryStorage.Password,
                    PathStorage = libraryStorage.PathStorage
                }));
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var libraryStorage = CheckValid(req);

                if (libraryStorage.TypeOS == null)
                {
                    throw new IboxLog("Operating system can't null.", "AppLogs");
                }

                if (libraryStorage.TypeStorage == null)
                {
                    throw new IboxLog("Type storage can't null.", "AppLogs");
                }

                if (libraryStorage.TypeStorage == TypeStorage.FTP || libraryStorage.TypeStorage == TypeStorage.SFTP)
                {
                    if (string.IsNullOrEmpty(libraryStorage.Host?.Trim()))
                    {
                        throw new IboxLog("Host can't null or empty.", "AppLogs");
                    }

                    if (string.IsNullOrEmpty(libraryStorage.UserName?.Trim()))
                    {
                        throw new IboxLog("Username can't null or empty.", "AppLogs");
                    }

                    if (string.IsNullOrEmpty(libraryStorage.Password?.Trim()))
                    {
                        throw new IboxLog("Password can't null or empty.", "AppLogs");
                    }
                }

                var data = _rootContext.Context.A_AssetLibraryStorages.FirstOrDefault(ptr => ptr.Id == libraryStorage.Id && !ptr.IsDelete);

                if (data == null)
                {
                    throw new IboxLog("Not found config library storage.", "AppLogs");
                }

                data.TryUpdate(_rootContext.Context, new A_AssetLibraryStorage()
                {
                    Id = libraryStorage.Id,
                    TypeOS = libraryStorage.TypeOS,
                    TypeStorage = libraryStorage.TypeStorage,
                    Host = libraryStorage.Host,
                    Port = libraryStorage.Port,
                    UserName = libraryStorage.UserName,
                    Password = libraryStorage.Password,
                    PathStorage = libraryStorage.PathStorage
                });

                return new ResponseForm<dynamic>(() => data);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }
    }
}
