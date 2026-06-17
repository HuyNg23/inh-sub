using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Workflow.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;
using System.Xml;

namespace IBox.Workflow.Execution
{
    public class ModelControl : ATenantContext<ModelControl>, IModelControl, IDisposable
    {
        private readonly IBaseObjectBuilder objectBuilder;

        public ModelControl(IBaseObjectBuilder objectBuilder)
        {
            this.objectBuilder = objectBuilder;
        }

        protected override void ImplementationDIInLocalObject()
        {
            if (this.tenantContext == null)
            {
                throw new IboxLog("Tenant context have not aviable", "AppLogs");
            }

            this.objectBuilder.SetTenantContext(this.tenantContext);
        }

        public override ModelControl SetTenantContext(IBContext<TenantContext> tenantContext)
        {
            base.SetTenantContext(tenantContext);
            this.ImplementationDIInLocalObject();
            return this;
        }

        public override ModelControl SetTenantContext(IBContext<TenantContext> tenantContext, string tenantID)
        {
            base.SetTenantContext(tenantContext, tenantID);
            this.ImplementationDIInLocalObject();
            return this;
        }

        #region binding and convert xml to object

        public object BindingDataXML(string? objid, string? data, string tenantId)
        {
            try
            {
                var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);

                if (tenantwfs.Key == null || tenantwfs.Value == null)
                {
                    throw new IboxLog(string.Format("ObjSchemaes {0} can not found in deploy", objid), tenantId);
                }

                var model = tenantwfs.Value.ObjSchemaes.FirstOrDefault(ptr => ptr.Key == objid);

                if (data == null || string.IsNullOrEmpty(data.ToString()))
                {
                    return JsonConvert.DeserializeObject((string)BindingDataDump(objid, tenantId), model.Value);
                }

                var str = data;
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(str);

                return BindingDataXML(objid, doc, tenantId);
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred {ex.Message}", tenantId, ex);
            }
        }

        public object? BindingDataXML(string? objid, object? data, string tenantId)
        {
            try
            {
                var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
                if (tenantwfs.Key == null || tenantwfs.Value == null)
                {
                    throw new IboxLog(string.Format("ObjSchemaes {0} can not found in deploy", objid), tenantId);
                }

                var model = tenantwfs.Value.ObjSchemaes.FirstOrDefault(ptr => ptr.Key == objid);
                if (data == null || string.IsNullOrEmpty(data.ToString()))
                {
                    return JsonConvert.DeserializeObject((string)BindingDataDump(objid, tenantId), model.Value);
                }

                var str = data;
                object? modelAfterConvert;
                str = JsonConvert.SerializeXmlNode((XmlNode)str, Newtonsoft.Json.Formatting.Indented);
                JObject xmlObject = JObject.Parse((string)str);
                foreach (var prop in model.Value.GetProperties())
                {
                    if (prop.PropertyType.Name == typeof(List<>).Name)
                    {
                        var JarrayXMLObject = new JArray(xmlObject[prop.Name]);
                        xmlObject[prop.Name] = JarrayXMLObject;
                        for (int i = 0; i < xmlObject[prop.Name].Count(); i++)
                        {
                            if (prop.PropertyType.GenericTypeArguments[0] != typeof(string) && prop.PropertyType.GenericTypeArguments[0] != typeof(int))
                            {
                                xmlObject[prop.Name][i] = BindingDataXML(prop.PropertyType.GenericTypeArguments[0], xmlObject[prop.Name][i]);
                            }
                        }
                    }
                    if (prop.PropertyType.Assembly.IsDynamic)
                    {
                        xmlObject[prop.Name] = BindingDataXML(prop.PropertyType, xmlObject[prop.Name]);
                    }
                }
                modelAfterConvert = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(xmlObject), model.Value);
                return modelAfterConvert;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred {ex.Message}", tenantId, ex);
            }
        }

        private JObject? BindingDataXML(Type? objType, object? data)
        {
            try
            {
                var str = data;
                if (str == null)
                {
                    return null;
                }
                var strString = JsonConvert.SerializeObject(str);
                if (string.IsNullOrEmpty(strString) || strString == "null")
                {
                    return null;
                }
                JObject xmlObject = JObject.Parse(strString);
                foreach (var prop in objType.GetProperties())
                {
                    if (prop.PropertyType.Name == typeof(List<>).Name)
                    {
                        var JArrayName = new JArray(xmlObject[prop.Name]);
                        if (xmlObject[prop.Name] is not JArray)
                        {
                            xmlObject[prop.Name] = JArrayName;
                        }
                        for (int i = 0; i < xmlObject[prop.Name].Count(); i++)
                        {
                            if (prop.PropertyType.GenericTypeArguments[0] != typeof(string) && prop.PropertyType.GenericTypeArguments[0] != typeof(int))
                            {
                                xmlObject[prop.Name][i] = BindingDataXML(prop.PropertyType.GenericTypeArguments[0], xmlObject[prop.Name][i]);
                            }
                        }
                    }
                    if (prop.PropertyType.Assembly.IsDynamic)
                    {
                        xmlObject[prop.Name] = BindingDataXML(prop.PropertyType, xmlObject[prop.Name]);
                    }
                }
                return xmlObject;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred {ex.Message}", "AppLogs", ex);
            }
        }

        #endregion binding and convert xml to object

        #region binding and convert object to object

        public object BindingData(string? objid, string? data, string tenantId)
        {
            try
            {
                var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
                if (tenantwfs.Key == null || tenantwfs.Value == null)
                {
                    throw new IboxLog(string.Format("ObjSchemaes {0} can not found in deploy", objid), tenantId);
                }

                var model = tenantwfs.Value.ObjSchemaes.FirstOrDefault(ptr => ptr.Key == objid);
                Log.Information("in ra model {Model}", model);
                if (data == null || string.IsNullOrEmpty(data.ToString()) || data == "{}")
                {
                    return JsonConvert.DeserializeObject((string)BindingDataDump(objid, tenantId), model.Value);
                }

                var str = data
                            .Replace("\\\\", "\\")  // Fix double backslash
                            .Replace("\\.", ".")    // Fix \. thành .
                            .Replace("\\? ", "?")   // Fix các ký tự khác nếu cần
                            .Replace("\\-", "-");

                // If the target model is a plain string, return the raw value (avoid JSON conversion errors)
                if (model.Value == typeof(string))
                {
                    return str;
                }

                try
                {
                    return JsonConvert.DeserializeObject(str, model.Value);
                }
                catch (JsonException)
                {
                    try
                    {
                        var pattern = @"(""[^""\s]+""\s*:\s*)([A-Za-z_][A-Za-z0-9_]*)(\s*[,}])";
                        var fixedStr = Regex.Replace(str, pattern, m => $"{m.Groups[1].Value}\"{m.Groups[2].Value}\"{m.Groups[3].Value}");
                        return JsonConvert.DeserializeObject(fixedStr, model.Value);
                    }
                    catch (Exception innerEx)
                    {
                        throw new IboxLog($"An Unexpected Error Has Occurred at bindingdata() upper {innerEx.Message}", tenantId, innerEx);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred at bindingdata() upper {ex.Message}", tenantId, ex);
            }
        }

        public object BindingData(string? objid, object? data, string tenantId)
        {
            try
            {
                var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);

                if (tenantwfs.Key == null || tenantwfs.Value == null)
                {
                    throw new IboxLog(string.Format("ObjSchemaes {0} can not found in deploy", objid), tenantId);
                }

                var model = tenantwfs.Value.ObjSchemaes.FirstOrDefault(ptr => ptr.Key == objid);
                Log.Information("in ra model {Model}", model);
                if (data == null || string.IsNullOrEmpty(data.ToString()))
                {
                    return JsonConvert.DeserializeObject((string)BindingDataDump(objid, tenantId), model.Value);
                }

                var str = data;
                object modelAfterConvert;
                var modelInstance = Activator.CreateInstance(model.Value);

                foreach (var prop in model.Value.GetProperties())
                {
                    if (prop.PropertyType.GetProperties().Any() && prop.PropertyType.Assembly.IsDynamic)
                    {
                        var subDatabinding = BindingDataDumpForChild(prop.PropertyType);
                        model.Value.GetProperty(prop.Name).SetValue(modelInstance, subDatabinding);
                    }
                }
                str = JsonConvert.SerializeObject(str);

                modelAfterConvert = JsonConvert.DeserializeObject((string)str, model.Value);

                if (modelAfterConvert == null)
                {
                    throw new IboxLog("model after convert is null", tenantId);
                }
                return modelAfterConvert;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred at bindingdata() lower {ex.Message}", tenantId, ex);
            }
        }

        public object BindingDataDump(string objid, string tenantId)
        {
            try
            {
                var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
                if (tenantwfs.Key == null || tenantwfs.Value == null)
                {
                    throw new IboxLog(string.Format("ObjSchemaes {0} can not found in deploy", objid), tenantId);
                }

                var model = tenantwfs.Value.ObjSchemaes.FirstOrDefault(ptr => ptr.Key == objid);

                var modelInstance = Activator.CreateInstance(model.Value);

                foreach (var prop in model.Value.GetProperties())
                {
                    if (prop.PropertyType.Assembly.IsDynamic)
                    {
                        var subDatabinding = BindingDataDumpForChild(prop.PropertyType);
                        model.Value.GetProperty(prop.Name)?.SetValue(modelInstance, subDatabinding);
                    }

                    if (prop.PropertyType.Name == typeof(List<>).Name)
                    {
                        Type typeList = typeof(List<>).MakeGenericType(prop.PropertyType.GenericTypeArguments.FirstOrDefault());
                        if (typeList == null)
                        {
                            throw new IboxLog("can not make generic type", tenantId);
                        }

                        IList list = (IList)Activator.CreateInstance(typeList);
                        if (list == null)
                        {
                            throw new IboxLog("can not create List", tenantId);
                        }

                        model.Value.GetProperty(prop.Name)?.SetValue(modelInstance, list);
                    }
                }
                var modelAfterConvert = JsonConvert.SerializeObject(modelInstance);

                if (modelAfterConvert == null)
                {
                    throw new IboxLog("model after convert is null", tenantId);
                }
                return modelAfterConvert;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred BindingDataDump: {ex.Message}", tenantId, ex);
            }
        }

        public object BindingDataToProperty(Type model, string propertyName, object? data, string tenantId)
        {
            try
            {
                var modelInstance = Activator.CreateInstance(model);

                foreach (var prop in model.GetProperties())
                {
                    if (data != null && prop.Name == propertyName)
                    {
                        if (prop.PropertyType.Name == typeof(List<>).Name)
                        {

                            var subDatabinding = BindingDataDumpForChild(prop.PropertyType.GenericTypeArguments.FirstOrDefault());



                            Type? typeList = typeof(List<>).MakeGenericType(prop.PropertyType.GenericTypeArguments.FirstOrDefault());

                            if (typeList == null)
                            {
                                throw new IboxLog("can not make generic type", tenantId);
                            }

                            IList list = (IList)Activator.CreateInstance(typeList);

                            if (list == null)
                            {
                                throw new IboxLog("can not create List", tenantId);
                            }
                            list.Add(subDatabinding);
                            model.GetProperty(prop.Name)?.SetValue(modelInstance, list);
                        }
                        else
                        {
                            if (prop.PropertyType.Assembly.IsDynamic)
                            {
                                model.GetProperty(prop.Name)?.SetValue(modelInstance, JsonConvert.DeserializeObject(data.ToString(), prop.PropertyType));
                            }
                            else
                            {
                                model.GetProperty(prop.Name)?.SetValue(modelInstance, data);
                            }
                        }
                    }
                    else
                    {
                        if (prop.PropertyType.Assembly.IsDynamic)
                        {
                            object data1 = BindingDataToProperty(prop.PropertyType, propertyName, data, tenantId);
                            model.GetProperty(prop.Name)?.SetValue(modelInstance, data1);
                        }
                    }
                }
                if (modelInstance == null)
                {
                    return new { };
                }
                return modelInstance;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred BindingDataToProperty: {ex.Message}", tenantId, ex);
            }
        }

        public object BindingDataToProperty(string? objid, string propertyName, object? data, string tenantId)
        {
            try
            {
                var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
                if (tenantwfs.Key == null || tenantwfs.Value == null)
                {
                    throw new IboxLog(string.Format("ObjSchemaes {0} can not found in deploy", objid), tenantId);
                }

                var model = tenantwfs.Value.ObjSchemaes.FirstOrDefault(ptr => ptr.Key == objid);

                var modelInstance = Activator.CreateInstance(model.Value);

                foreach (var prop in model.Value.GetProperties())
                {
                    if (data != null && prop.Name == propertyName)
                    {
                        if (prop.PropertyType.Name == typeof(List<>).Name)
                        {
                            var subDatabinding = BindingDataDumpForChild(prop.PropertyType.GenericTypeArguments[0]);

                            Type? typeList = typeof(List<>).MakeGenericType(prop.PropertyType.GenericTypeArguments[0]);
                            if (typeList == null)
                            {
                                throw new IboxLog("can not make generic type", tenantId);
                            }

                            IList list = (IList)Activator.CreateInstance(typeList);

                            if (list == null)
                            {
                                throw new IboxLog("can not create List", tenantId);
                            }
                            list.Add(subDatabinding);
                            model.Value.GetProperty(prop.Name)?.SetValue(modelInstance, list);

                            if (prop.Name == propertyName)
                            {
                                var dum = JsonConvert.SerializeObject(data);
                                model.Value.GetProperty(prop.Name)?.SetValue(modelInstance, JsonConvert.DeserializeObject(dum, typeList));
                            }
                        }
                        else
                        {
                            if (prop.PropertyType.Assembly.IsDynamic)
                            {

                                model.Value.GetProperty(prop.Name)?.SetValue(modelInstance, JsonConvert.DeserializeObject(data.ToString(), prop.PropertyType));

                            }
                            else
                            {
                                model.Value.GetProperty(prop.Name)?.SetValue(modelInstance, data);
                            }
                        }
                    }
                    else
                    {
                        if (prop.PropertyType.Assembly.IsDynamic)
                        {
                            object data1 = BindingDataToProperty(prop.PropertyType, propertyName, data, tenantId);
                            model.Value.GetProperty(prop.Name)?.SetValue(modelInstance, data1);
                        }
                    }
                }
                if (modelInstance == null)
                {
                    return new { };
                }
                return modelInstance;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }

        public object BindingDataToProperty(Type type, object? obj, string propertyName, string? strData = "", bool isOveride = false, string tenantId= "AppLogs")
        {
            try
            {
                foreach (var prop in type.GetProperties())
                {
                    if (strData != null && prop.Name == propertyName)
                    {
                        if (prop.PropertyType.Name == typeof(List<>).Name)
                        {

                            Type? typeList = typeof(List<>).MakeGenericType(prop.PropertyType.GenericTypeArguments.FirstOrDefault());

                            if (typeList == null)
                            {
                                throw new IboxLog("can not make generic type", tenantId);
                            }


                            IList list = (IList)Activator.CreateInstance(typeList);

                            if (list == null)
                            {
                                throw new IboxLog("can not create List", tenantId);
                            }

                            if (isOveride)
                            {

                                list.Add(JsonConvert.DeserializeObject(strData, prop.PropertyType.GenericTypeArguments.FirstOrDefault()));


                                obj.GetType().GetProperty(prop.Name)?.SetValue(obj, list);

                            }
                            else
                            {


                                list = (IList)obj.GetType().GetProperty(prop.Name)?.GetValue(obj);


                                if (list == null)
                                {
                                    throw new IboxLog($"haven't any property in list", tenantId);
                                }

                                list.Add(JsonConvert.DeserializeObject(strData, prop.PropertyType.GenericTypeArguments.FirstOrDefault()));

                                obj.GetType().GetProperty(prop.Name)?.SetValue(obj, list);
                            }
                        }
                        else
                        {
                            if (prop.PropertyType.Assembly.IsDynamic)
                            {
                                type.GetProperty(prop.Name)?.SetValue(obj, JsonConvert.DeserializeObject(strData.ToString(), prop.PropertyType));
                            }
                            else
                            {
                                type.GetProperty(prop.Name)?.SetValue(obj, strData);
                            }
                        }
                    }
                    else
                    {
                        if (prop.PropertyType.Assembly.IsDynamic)
                        {
                            object data1 = BindingDataToProperty(prop.PropertyType, propertyName, strData, tenantId);
                            type.GetProperty(prop.Name)?.SetValue(obj, data1);
                        }
                    }
                }
                if (obj == null)
                {
                    return new { };
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }

        private object BindingDataDumpForChild(Type type)
        {
            var modelInstance = Activator.CreateInstance(type);
            foreach (var prop in type.GetProperties())
            {
                if (prop.PropertyType.Assembly.IsDynamic)
                {
                    type.GetProperty(prop.Name)?.SetValue(modelInstance, BindingDataDumpForChild(prop.PropertyType));
                }
                if (prop.PropertyType.Name == typeof(List<>).Name)
                {
                    if (prop.PropertyType.GenericTypeArguments[0] != typeof(string) && prop.PropertyType.GenericTypeArguments[0] != typeof(int))
                    {

                        var subDatabinding = BindingDataDumpForChild(prop.PropertyType.GenericTypeArguments.FirstOrDefault());


                        Type typeList = typeof(List<>).MakeGenericType(prop.PropertyType.GenericTypeArguments.FirstOrDefault());

                        if (typeList == null)
                        {
                            throw new IboxLog("can not make generic type", "AppLogs");
                        }

                        IList list = (IList)Activator.CreateInstance(typeList);

                        if (list == null)
                        {
                            throw new IboxLog("can not create List", "AppLogs");
                        }
                        list.Add(subDatabinding);
                        type.GetProperty(prop.Name)?.SetValue(modelInstance, list);
                    }

                    return modelInstance;

                }
            }

            return modelInstance;

        }

        #endregion binding and convert object to object

        #region Build object on runtime

        public void BuildModel(string objid, string tenantId, IBContext<TenantContext> tenantContext)
        {
            try
            {
                if (string.IsNullOrEmpty(objid))
                {
                    throw new IboxLog("Build Model fail because some step is not setting param model or response model", tenantId);
                }

                var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
                if (tenantwfs.Key == null)
                {
                    ListTenantWF.TenantWFs.TryAdd(tenantId, new StaticListWF());
                    tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
                }

                if (tenantwfs.Value.ObjSchemaes.Any(ptr => ptr.Key == objid))
                {
                    tenantwfs.Value.ObjSchemaes[objid] = this.objectBuilder.CreateNewObject(objid, tenantId, tenantContext);
                }
                else
                {
                    tenantwfs.Value.ObjSchemaes.TryAdd(objid, this.objectBuilder.CreateNewObject(objid, tenantId, tenantContext));
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Build object was fail", ex);
                Thread.Sleep(300);
            }
        }

        #endregion Build object on runtime

        #region validate model

        /// <summary>
        /// Kiểm tra nếu obj có dữ liệu sẽ trả về true, còn không thì sẽ trả về failse
        /// </summary>
        /// <param name="objid"></param>
        /// <returns></returns>
        public bool IsObjExist(string objid, string tenantId)
        {
            var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
            return tenantwfs.Value.ObjSchemaes.Any(ptr => ptr.Key == objid);
        }

        public Type GetType(string? objid, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(objid))
                {
                    throw new IboxLog("Objectid is null or emply", tenantId);
                }

                if (!IsObjExist(objid, tenantId))
                {
                    throw new IboxLog(string.Format("Object {0} not build in memory cache", objid), tenantId);
                }

                var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);

                return tenantwfs.Value.ObjSchemaes.FirstOrDefault(ptr => ptr.Key == objid).Value;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }

        #endregion validate model

        #region get value

        /// <summary>
        /// Lấy giá trị trong 1 object tại 1 trường được chỉ định
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objOrigin"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object? GetValue(object objOrigin, string propertyName)
        {
            try
            {
                // Tách chuỗi thuộc tính theo dấu chấm
                var propertyNames = propertyName.Split('.');

                // Bắt đầu với object hiện tại
                var currentObject = objOrigin;

                foreach (var name in propertyNames)
                {
                    if (currentObject == null)
                    {
                        return null;
                    }

                    // Lấy thuộc tính của object hiện tại
                    var propertyInfo = currentObject.GetType().GetProperty(name);

                    if (propertyInfo == null)
                    {
                        return null;
                    }

                    // Lấy giá trị của thuộc tính
                    currentObject = propertyInfo.GetValue(currentObject);
                }

                return currentObject;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Tìm kiếm và lấy thông tin property của object
        /// </summary>
        /// <param name="objOrigin"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public PropertyInfo? GetProperty(object? objOrigin, string propertyName)
        {
            try
            {
                if (objOrigin == null)
                {
                    return null;
                }
                var properties = objOrigin.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.PropertyType.Assembly.IsDynamic)
                    {
                        if (prop.Name == propertyName)
                        {
                            return prop;
                        }
                        else
                        {
                            return GetProperty(prop.GetValue(objOrigin), propertyName);
                        }
                    }
                    else
                    {
                        if (prop.Name == propertyName)
                        {
                            return prop;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

        public void SetValue(object objOrigin, string propertyName, object dataSet)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            try
            {
                if (objOrigin == null)
                {
                    return;
                }

                var propertyNames = propertyName.Split('.');

                PropertyInfo property = null;

                object currentObj = objOrigin;

                for (int i = 0; i < propertyNames.Length; i++)
                {


                    property = currentObj.GetType().GetProperty(propertyNames[i]);


                    if (property == null)
                    {
                        throw new IboxLog($"Can't property with name {propertyName}", "AppLogs");
                    }

                    if (i == propertyNames.Length - 1)
                    {
                        if (property.CanWrite)
                        {
                            property.SetValue(currentObj, dataSet);
                        }
                    }
                    else
                    {

                        currentObj = property.GetValue(currentObj);

                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion get value
    }
}