using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Reflection;
using System.Reflection.Emit;

namespace IBox.Workflow.Model
{
    public class BaseObjectBuilder : ATenantContext<BaseObjectBuilder>, IBaseObjectBuilder
    {
        public List<KeyValuePair<string, string>> GetObjectzInBD(string rootObjectID)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            var obj = this.tenantContext.Context.Objs.FirstOrDefault(ptr => ptr.Id == rootObjectID && !ptr.IsDelete);

            string tenantId = this.tenantContext.Context.TenantInfo.Id;
            if (obj == null || obj.Name == null)
            {
                throw new IboxLog(string.Format("can not found object {0} or was deleted", rootObjectID), tenantId ?? "AppLogs");
            }

            var fields = this.tenantContext.Context.Obj_Schemas.Where(ptr => ptr.ObjID == rootObjectID && !ptr.IsDelete).ToList();
            if (fields.Count == 0)
            {
                throw new IboxLog(string.Format("can not found any field of object {0} or was deleted", rootObjectID), tenantId ?? "AppLogs");
            }

            foreach (var field in fields)
            {
                if (rootObjectID == field.ObjectReferent)
                {
                    throw new IboxLog("Object building in infinity loop", tenantId ?? "AppLogs");
                }
                switch (field.Type)
                {
                    case FieldType.Object:
                    case FieldType.ListObject:

                        result.Add(new KeyValuePair<string, string>(field.Id, field.FieldName));

                        if (field.ObjectReferent == null)
                        {
                            throw new IboxLog("Object Referent is null", tenantId ?? "AppLogs");
                        }
                        GetObjectzInBD(field.ObjectReferent).ToList().ForEach(ptr =>
                        {
                            result.Add(new KeyValuePair<string, string>(field.FieldName + "." + ptr.Key, field.FieldName + "." + ptr.Value));
                        });
                        break;

                    default:

                        result.Add(new KeyValuePair<string, string>(field.Id, field.FieldName));

                        break;
                }
            }

            return result;
        }

        public Type CreateNewObject(string rootObjectID, string tenantId, IBContext<TenantContext> tenantContext)
        {
            var myType = CompileResultType(rootObjectID, tenantId, tenantContext);
            return myType;
        }

        private Type CompileResultType(string rootObjectID, string tenantId, IBContext<TenantContext> tenantContext)
        {
            if (tenantContext.Context.TenantInfo == null)
            {
                tenantContext = this.tenantContext;
            }

            var obj = tenantContext.Context.Objs.FirstOrDefault(ptr => ptr.Id == rootObjectID && !ptr.IsDelete);

            if (obj == null || obj.Name == null)
            {
                throw new IboxLog(string.Format("can not found object {0} or was deleted", rootObjectID), tenantId ?? "AppLogs");
            }

            var fields = tenantContext.Context.Obj_Schemas.Where(ptr => ptr.ObjID == rootObjectID && !ptr.IsDelete).OrderBy(x => x.CreatedDate).ToList();
            if (fields.Count == 0)
            {
                Log.Warning(string.Format("can not found any field of object {0} or was deleted", rootObjectID));
            }

            // start build type
            TypeBuilder tb = GetTypeBuilder(obj.Name);
            tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var field in fields)
            {
                if (rootObjectID == field.ObjectReferent)
                {
                    throw new IboxLog("Object building in infinity loop", tenantId ?? "AppLogs");
                }
                switch (field.Type)
                {
                    case FieldType.Nvarchar:
                        CreateType(ref tb, field, typeof(string));
                        break;

                    case FieldType.Integer:
                        CreateType(ref tb, field, typeof(int?));
                        break;

                    case FieldType.Decimal:
                        CreateType(ref tb, field, typeof(decimal?));
                        break;

                    case FieldType.Datetime:
                        CreateType(ref tb, field, typeof(DateTime?));
                        break;

                    case FieldType.Float:
                        CreateType(ref tb, field, typeof(decimal?));
                        break;

                    case FieldType.Object:
                        if (field.ObjectReferent == null)
                        {
                            throw new IboxLog("Object Referent is null", tenantId ?? "AppLogs");
                        }
                        CreateType(ref tb, field, CompileResultType(field.ObjectReferent, tenantId, tenantContext));
                        break;

                    case FieldType.ListObject:
                        if (field.ObjectReferent == null)
                        {
                            throw new IboxLog("Object Referent is null", tenantId ?? "AppLogs");
                        }
                        CreateType(ref tb, field, typeof(List<>).MakeGenericType(CompileResultType(field.ObjectReferent, tenantId, tenantContext)));
                        break;

                    case FieldType.ListPrimitiveTypesString:
                        CreateType(ref tb, field, typeof(List<string?>));
                        break;

                    case FieldType.ListPrimitiveTypesNumber:
                        CreateType(ref tb, field, typeof(List<long?>));
                        break;
                }
            }

            if (tb == null)
            {
                throw new IboxLog("can not create object ", tenantId ?? "AppLogs");
            }

            Type objectType = tb.CreateType();

            return objectType;

        }

        private TypeBuilder GetTypeBuilder(string objName)
        {
            var typeSignature = objName;
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(typeSignature), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("dynamic_" + objName);
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout);
            return tb;
        }

        private void CreateType(ref TypeBuilder tb, Obj_Schema field, Type type)
        {
            FieldBuilder customerNameBldr = tb.DefineField("_" + field.FieldName, type, FieldAttributes.Private);

            PropertyBuilder custNamePropBldr = tb.DefineProperty(field.FieldName, PropertyAttributes.HasDefault, type, Type.EmptyTypes);


            //method get
            MethodBuilder custNameGetPropMthdBldr = tb.DefineMethod("Get" + field.FieldName,
                           MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                           type,
                           Type.EmptyTypes);
            ILGenerator custNameGetIL = custNameGetPropMthdBldr.GetILGenerator();

            custNameGetIL.Emit(OpCodes.Ldarg_0);
            custNameGetIL.Emit(OpCodes.Ldfld, customerNameBldr);
            custNameGetIL.Emit(OpCodes.Ret);

            // method set
            List<Type> types = new List<Type>();
            types.Add(type);
            MethodBuilder custNameSetPropMthdBldr = tb.DefineMethod("Set" + field.FieldName,
                           MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                           null, types.ToArray());
            ILGenerator custNameSetIL = custNameSetPropMthdBldr.GetILGenerator();

            custNameSetIL.Emit(OpCodes.Ldarg_0);
            custNameSetIL.Emit(OpCodes.Ldarg_1);
            custNameSetIL.Emit(OpCodes.Stfld, customerNameBldr);
            custNameSetIL.Emit(OpCodes.Ret);

            custNamePropBldr.SetGetMethod(custNameGetPropMthdBldr);
            custNamePropBldr.SetSetMethod(custNameSetPropMthdBldr);
        }
    }
}