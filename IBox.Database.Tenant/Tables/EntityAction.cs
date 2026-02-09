using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;

namespace IBox.Database.Tenant
{
    public static class EntityAction
    {
        /// <summary>
        /// Tạo bản ghi với dòng dữ liệu tương ứng, dữ liệu chỉ được phép tạo khi đã gọi hàm TryCreate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="record"></param>
        /// <exception cref="Exception"></exception>
        public static void TryCreate<T>(this T? record, AibContext dbContext, T newRecord) where T : BaseTable
        {
            if (dbContext == null)
            {
                throw new IboxLog("Can not connect to main database", "AppLogs");
            }

            using (var dbContextTransaction = dbContext.Database.BeginTransaction())
            {
                if (record != null)
                {
                    throw new IboxLog(String.Format("record in [{0}] was exist", typeof(T).ToString().Split(".").LastOrDefault()), "AppLogs");
                }

                try
                {
                    newRecord.IsDelete = false;
                    newRecord.CreatedDate = DateTime.Now;
                    newRecord.ModificationDate = DateTime.Now;
                    dbContext.Add(newRecord);
                    dbContext.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch (Exception)
                {
                    dbContextTransaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Tự động đánh dấu bản ghi bị xóa
        /// </summary>
        /// <typeparam name="T">Type of table</typeparam>
        /// <param name="record">Record selected in querry</param>
        public static void TryDelete<T>(this T? record, AibContext dbContext) where T : BaseTable
        {
            if (dbContext == null)
            {
                throw new IboxLog("Can not connect to main database", "AppLogs");
            }

            using (var dbContextTransaction = dbContext.Database.BeginTransaction())
            {
                if (record == null)
                {
                    throw new IboxLog(String.Format("record in [{0}] is not found", typeof(T).ToString().Split(".").LastOrDefault()), "AppLogs");
                }
                try
                {
                    record.IsDelete = true;
                    record.ModificationDate = DateTime.Now;
                    dbContext.Set<T>().Update(record);
                    dbContext.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch
                {
                    dbContextTransaction.Rollback();
                    throw new IboxLog(String.Format("Can not commit transaction in [{0}]", typeof(T).ToString().Split(".").LastOrDefault()), "AppLogs");
                }
            }
        }

        /// <summary>
        /// Tự động khôi phục bản ghi bị xóa
        /// </summary>
        /// <typeparam name="T">Type of table</typeparam>
        /// <param name="record">Record selected in querry</param>
        public static void TryRollback<T>(this T? record, AibContext dbContext) where T : BaseTable
        {
            if (dbContext == null)
            {
                throw new IboxLog("Can not connect to main database", "AppLogs");
            }

            using (var dbContextTransaction = dbContext.Database.BeginTransaction())
            {
                if (record == null)
                {
                    throw new IboxLog(String.Format("record in [{0}] is not found", typeof(T).ToString().Split(".").LastOrDefault()), "AppLogs");
                }

                try
                {
                    record.IsDelete = false;
                    record.ModificationDate = DateTime.Now;
                    dbContext.Set<T>().Update(record);
                    dbContext.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch
                {
                    dbContextTransaction.Rollback();
                    throw new IboxLog(String.Format("Can not commit transaction in [{0}]", typeof(T).ToString().Split(".").LastOrDefault()), "AppLogs");
                }
            }
        }

        /// <summary>
        /// Tự động cập nhất tất cả các trường dữ liệu được update
        /// </summary>
        /// <param name="record"></param>
        /// <exception cref="Exception"></exception>
        public static void TryUpdate<T>(this T? rec, AibContext dbContext, T record) where T : BaseTable
        {
            if (dbContext == null)
            {
                throw new IboxLog("Can not connect to main database", "AppLogs");
            }

            using (var dbContextTransaction = dbContext.Database.BeginTransaction())
            {
                if (rec == null)
                {
                    throw new IboxLog(String.Format("record in [{0}] was existed", typeof(T).ToString().Split(".").LastOrDefault()), "AppLogs");
                }

                if (record == null)
                {
                    throw new IboxLog(String.Format("record in to [{0}] was not input data", typeof(T).ToString().Split(".").LastOrDefault()), "AppLogs");
                }

                try
                {
                    rec.ModificationDate = DateTime.Now;
                    var props = typeof(T).GetProperties();
                    foreach (var prop in props.Select(ptr => ptr.Name))
                    {
                        var propItem = record.GetType().GetProperty(prop);
                        if (propItem == null)
                        {
                            continue;
                        }

                        var value = propItem.GetValue(record, null);
                        if (value == null)
                        {
                            continue;
                        }

                        rec.GetType().GetProperty(prop)?.SetValue(rec, value);
                    }
                    dbContext.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    throw new IboxLog(String.Format("Can not commit transaction in [{0}]", typeof(T).ToString().Split(".").LastOrDefault()),"AppLogs", ex);
                }
            }
        }

        /// <summary>
        /// Cố gắng thêm mới hoặc update nếu có thể
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="context"></param>
        /// <param name="obj"></param>
        public static void TryAddOrUpdate<TContext, TEntity>(this TContext context, TEntity obj) where TContext : DbContext where TEntity : BaseTable
        {
            try
            {
                bool data = context.Set<TEntity>().Any(ptr => ptr.Id == obj.Id);
                if (data)
                {
                    context.Update(obj);
                }
                else
                {
                    context.Add(obj);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Cố gắng thêm mới hoặc update danh sách nếu có thể
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="context"></param>
        /// <param name="obj"></param>
        public static void TryAddOrUpdateRange<TContext, TEntity>(this TContext context, List<TEntity> obj) where TContext : DbContext where TEntity : BaseTable
        {
            try
            {
                var ids = obj.Select(x => x.Id).ToList();

                var existingIds = context.Set<TEntity>()
                                         .Where(x => ids.Contains(x.Id))
                                         .Select(x => x.Id)
                                         .ToHashSet();

                foreach (var item in obj)
                {
                    var localEntity = context.Set<TEntity>().Local.FirstOrDefault(x => x.Id == item.Id);

                    if (localEntity != null)
                    {
                        context.Entry(localEntity).CurrentValues.SetValues(item);
                    }
                    else if (existingIds.Contains(item.Id))
                    {
                        context.Update(item);
                    }
                    else
                    {
                        context.Add(item);
                    }
                }

                //foreach (var item in obj)
                //{
                //    //Check trong database
                //    bool dataBase = context.Set<TEntity>().Any(ptr => ptr.Id == item.Id);
                //    var dataLocal = context.Set<TEntity>().Local.FirstOrDefault(ptr => ptr.Id == item.Id);
                //    if (dataLocal != null)
                //    {
                //        context.Entry<TEntity>(dataLocal).CurrentValues.SetValues(item);
                //        continue;
                //    }

                //    if (dataBase)
                //    {
                //        context.Update(item);
                //    }
                //    else
                //    {
                //        context.Add(item);
                //    }
                //}
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}