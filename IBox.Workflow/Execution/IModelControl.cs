using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using System.Reflection;

namespace IBox.Workflow.Execution
{
    public interface IModelControl : ITenantContext<ModelControl>, IDisposable
    {
        bool IsObjExist(string objid, string tenantId);

        /// <summary>
        /// Build model
        /// </summary>
        /// <param name="objid"></param>
        void BuildModel(string objid, string tenantId, IBContext<TenantContext> tenantContext);

        /// <summary>
        /// Binding data to object was defined
        /// </summary>
        /// <param name="objid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        object BindingDataDump(string objid, string tenantId);

        /// <summary>
        /// gắn data dạng object cho object trống
        /// </summary>
        /// <param name="objid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        object BindingData(string? objid, object? data, string tenantId);

        /// <summary>
        /// Gắn data dạng string cho object trống
        /// </summary>
        /// <param name="objid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        object BindingData(string? objid, string? data, string tenantId);

        /// <summary>
        /// Gắn data dạng xml cho object
        /// </summary>
        /// <param name="objid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        object BindingDataXML(string? objid, string? data, string tenantId);

        /// <summary>
        /// gắn data cho object trống
        /// </summary>
        /// <param name="objid"></param>
        /// <param name="propertyName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        object BindingDataToProperty(string? objid, string propertyName, object? data, string tenantId);

        /// <summary>
        /// Gắn data cho object đã có sẵn
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="strData"></param>
        /// <param name="isOveride"></param>
        /// <returns></returns>
        object BindingDataToProperty(Type type, object? obj, string propertyName, string? strData = "", bool isOveride = false, string tenantId = "AppLogs");

        /// <summary>
        /// Lấy kiểu giá trị của object
        /// </summary>
        /// <param name="objid"></param>
        /// <returns></returns>
        Type GetType(string? objid, string tenantId);

        /// <summary>
        /// Lấy giá trị của object
        /// </summary>
        /// <param name="objOrigin">Giá trị ban đầu</param>
        /// <param name="propertyName">Tên trường dữ liệu cần tìm kiếm</param>
        /// <returns>object có thể null</returns>
        object? GetValue(object objOrigin, string propertyName);

        /// <summary>
        /// Tìm kiếm và lấy thông tin property trong object
        /// </summary>
        /// <param name="objOrigin"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        PropertyInfo? GetProperty(object? objOrigin, string propertyName);

        /// <summary>
        /// Tìm kiếm và gán dữ liệu vào một trường chỉ định trong object
        /// </summary>
        /// <param name="objOrigin"></param>
        /// <param name="propertyName"></param>
        /// <param name="dataSet"></param>
        void SetValue(object? objOrigin, string propertyName, object dataSet);
    }
}