using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;

namespace IBox.Client.Business.Model
{
    public static class BefBusiness
    {
        public static ResponseForm<dynamic> Create<T, T1>(this T record, IBContext<T1> context) where T : BaseTable where T1 : AibContext
        {
            return new ResponseForm<dynamic>(() =>
            {
                context.Context.Set<T>().FirstOrDefault(ptr => ptr.Id == record.Id && !ptr.IsDelete).TryCreate(context.Context, record);
                return record;
            });
        }

        public static ResponseForm<dynamic> Update<T, T1>(this T record, IBContext<T1> context) where T : BaseTable where T1 : AibContext
        {
            return new ResponseForm<dynamic>(() =>
            {
                var obj = context.Context.Set<T>().FirstOrDefault(ptr => ptr.Id == record.Id && !ptr.IsDelete);
                if (obj == null)
                {
                    throw new IboxLog(string.Format("Can not find any record of [{0}] model", typeof(T).ToString().Split(".").LastOrDefault()), "AppLogs");
                }

                obj.TryUpdate(context.Context, record);
                return record;
            });
        }

        public static ResponseForm<dynamic> Delete<T, T1>(this T record, IBContext<T1> context) where T : BaseTable where T1 : AibContext
        {
            return new ResponseForm<dynamic>(() =>
            {
                context.Context.Set<T>().FirstOrDefault(ptr => ptr.Id == record.Id && !ptr.IsDelete).TryDelete(context.Context);
                return record;
            });
        }

        public static ResponseForm<dynamic> RollbackDelete<T, T1>(this T record, IBContext<T1> context) where T : BaseTable where T1 : AibContext
        {
            return new ResponseForm<dynamic>(() =>
            {
                context.Context.Set<T>().FirstOrDefault(ptr => ptr.Id == record.Id && ptr.IsDelete).TryRollback(context.Context);
            });
        }

        public static ResponseForm<List<dynamic>> GetAll<T, T1>(this T rec, IBContext<T1> context, int pageNumber) where T : BaseTable where T1 : AibContext
        {
            return new ResponseForm<List<dynamic>>(() =>
            {
                if (pageNumber == -1)
                {
                    var queryResultPage = context.Context.Set<T>().Where(ptr => !ptr.IsDelete).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();
                    return queryResultPage;
                }
                else
                {
                    int numberOfObjectsPerPage = 30;
                    var queryResultPage = context.Context.Set<T>()
                        .Where(ptr => !ptr.IsDelete)
                        .Skip(numberOfObjectsPerPage * (pageNumber - 1))
                        .Take(numberOfObjectsPerPage).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();
                    return queryResultPage;
                }
            });
        }

        public static ResponseForm<dynamic> GetDetail<T, T1>(this T rec, IBContext<T1> context, string id) where T : BaseTable where T1 : AibContext
        {
            return new ResponseForm<dynamic>(() =>
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new IboxLog("Not found detail with id", "AppLogs");
                }

                var record = context.Context.Set<T>().FirstOrDefault(ptr => ptr.Id == id);
                if (record == null)
                {
                    throw new IboxLog("The corresponding record does not exist", "AppLogs");
                }

                return record;
            });
        }

        public static ResponseForm<List<dynamic>> GetAllDeleted<T, T1>(this T rec, IBContext<T1> context, int pageNumber) where T : BaseTable where T1 : AibContext
        {
            return new ResponseForm<List<dynamic>>(() =>
            {
                if (pageNumber == -1)
                {
                    var queryResultPage = context.Context.Set<T>().Where(ptr => ptr.IsDelete).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();
                    return queryResultPage;
                }
                else
                {
                    int numberOfObjectsPerPage = 30;
                    var queryResultPage = context.Context.Set<T>()
                        .Where(ptr => ptr.IsDelete)
                        .Skip(numberOfObjectsPerPage * (pageNumber - 1))
                        .Take(numberOfObjectsPerPage).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();
                    return queryResultPage;
                }
            });
        }
    }
}