using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.DLEx.Model;
using IBox.Documentation.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace IBox.Documentation.Workflow
{
    public class Expose : BBaseBusiness<Expose>
    {
        private List<WF_Define> wfDefines { get; set; } = new List<WF_Define>();
        private List<WF_Step> wfStep { get; set; } = new List<WF_Step>();
        private HashSet<WF_Step> readArrayWFReferNewSet = new HashSet<WF_Step>();
        private HashSet<WF_Step> RecallArrayWFReferNewSet = new HashSet<WF_Step>();
        private List<Obj> objs { get; set; } = new List<Obj>();
        private HashSet<Obj> uniqueobj = new HashSet<Obj>();
        private List<DBStructure> dBStructures { get; set; } = new List<DBStructure>();
        private List<Obj_Schema> obj_Schemas { get; set; } = new List<Obj_Schema>();
        private HashSet<Obj_Schema> uniqueObjSchemas = new HashSet<Obj_Schema>();

        private List<WF_Step_Edge> wFEdges { get; set; } = new List<WF_Step_Edge>();

        public string BackupFile(string wfid)
        {
            try
            {
                LoadDataForBackup(wfid);
                return this.Encryption.Encrypt(JsonConvert.SerializeObject(new BackUpAndRestore()
                {
                    WF_Define = wfDefines,
                    WF_Steps = wfStep,
                    DBStructure = dBStructures,
                    Objs = objs,
                    Obj_Schemas = obj_Schemas,
                    WF_Step_Edges = wFEdges
                }));
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void LoadDataForBackup(string wfid)
        {
            try
            {

                wfDefines.AddRange(this.tenantContext.Context.WF_Defines.Where(ptr => ptr.Id == wfid).ToList());


                if (wfDefines.Any())
                {
                    wfStep.AddRange(this.tenantContext.Context.WF_Steps.Where(ptr => ptr.WFid == wfid).ToList());

                    LoadAllWFReference();

                    List<string> paramids = wfStep.Where(ptr => !string.IsNullOrEmpty(ptr.Param)).Select(ptr => ptr.Param ?? "").GroupBy(ptr => ptr).Select(group => group.Key).ToList();
                    List<string> responseids = wfStep.Where(ptr => !string.IsNullOrEmpty(ptr.Response)).Select(ptr => ptr.Response ?? "").GroupBy(ptr => ptr).Select(group => group.Key).ToList();

                    LoadAllObjReference(paramids);
                    LoadAllObjReference(responseids);

                    wFEdges.AddRange(this.tenantContext
                                .Context
                                .WF_Step_Edges
                                .Where(ptr => ptr.WFid == wfid && !ptr.IsDelete)
                                .ToList());
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void LoadAllWFReference()
        {
            try
            {
                List<WF_Step> recallWFRefer = wfStep.Where(ptr => ptr.Type == WF_Type.RecallWF).ToList();
                List<WF_Step> readArrayWFRefer = wfStep.Where(ptr => ptr.Type == WF_Type.ReadArray).ToList();

                if (recallWFRefer.Any())
                {
                    recallWFRefer.ForEach(ptr =>
                    {
                        if (RecallArrayWFReferNewSet.Add(ptr))
                        {
                            var config = JsonConvert.DeserializeObject<RecallWfConfig>(ptr.Config ?? "{}");
                            if (config != null && !string.IsNullOrEmpty(config.Wfid))
                            {
                                BackupFile(config.Wfid);
                            }
                        }
                    });
                }

                if (readArrayWFRefer.Any())
                {
                    readArrayWFRefer.ForEach(ptr =>
                    {
                        if (readArrayWFReferNewSet.Add(ptr))
                        {
                            var config = JsonConvert.DeserializeObject<ReadArraysConfig>(ptr.Config ?? "{}");
                            if (config != null && !string.IsNullOrEmpty(config.Wfid))
                            {
                                BackupFile(config.Wfid);
                            }
                        }
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void LoadAllObjReference(List<string> objID)
        {

            List<Obj> objs_temp = this.tenantContext
                                        .Context
                                        .Objs
                                        .Where(ptr => objID.Any(ptx => ptx == ptr.Id))
                                        .ToList();


            foreach (var obj in objs_temp)
            {
                uniqueobj.Add(obj);
            }

            objs = uniqueobj.ToList();

            foreach (var item in objs)
            {

                DBStructure group_Objs = this.tenantContext
                                  .Context
                                  .DBStructures
                                  .FirstOrDefault(ptr => ptr.Id == item.DatabaseID);

                if (group_Objs == null)
                {
                    continue;
                }

                if (!dBStructures.Contains(group_Objs))
                {
                    dBStructures.Add(group_Objs);
                };
            }

            List<Obj_Schema> obj_Schemas_temp = this.tenantContext
            .Context
            .Obj_Schemas
                                    .Where(ptr => objID.Any(ptx => ptx == ptr.ObjID) && !ptr.IsDelete)
                                    .ToList();
            foreach (var obj in obj_Schemas_temp)
            {
                uniqueObjSchemas.Add(obj);
            }

            obj_Schemas = uniqueObjSchemas.ToList();

            List<string?> objectReferentids = obj_Schemas_temp.Where(ptr => !ptr.IsDelete && !string.IsNullOrEmpty(ptr.ObjectReferent))
                .Select(ptr => ptr.ObjectReferent)
                .GroupBy(ptr => ptr)
                .Select(group => group.Key)
                .ToList();
            if (objectReferentids.Any())
            {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                LoadAllObjReference(objectReferentids);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            }
        }

        public void Restore(IFormFile file)
        {
            try
            {
                if (!file.FileName.Contains(".bak"))
                {
                    throw new IboxLog("Can not read file not type bak", "AppLogs");
                }
                var result = new StringBuilder();
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    while (reader.Peek() >= 0)
                        result.AppendLine(reader.ReadLine());
                }

                string objJson = this.Encryption.Decrypt(result.ToString());

                BackUpAndRestore backUpAndRestore = JsonConvert.DeserializeObject<BackUpAndRestore>(objJson) ?? throw new IboxLog("Can read backup file", "AppLogs");


                using (var trans = this.tenantContext.Context.Database.BeginTransaction())
                {
                    try
                    {

                        this.tenantContext.Context.TryAddOrUpdateRange(backUpAndRestore.WF_Define);


                        this.tenantContext.Context.TryAddOrUpdateRange(backUpAndRestore.WF_Steps);


                        this.tenantContext.Context.TryAddOrUpdateRange(backUpAndRestore.WF_Step_Edges);


                        this.tenantContext.Context.TryAddOrUpdateRange(backUpAndRestore.DBStructure);


                        this.tenantContext.Context.TryAddOrUpdateRange(backUpAndRestore.Objs);


                        this.tenantContext.Context.TryAddOrUpdateRange(backUpAndRestore.Obj_Schemas);


                        this.tenantContext.Context.SaveChanges();
                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Restore: {ex}");
                        trans.Rollback();
                        throw;
                    }
                }

                this.tenantContext.Context.Dispose();
            }
            catch (Exception ex)
            {
#pragma warning disable CS8597 // Thrown value may be null.
                throw ex.InnerException;
#pragma warning restore CS8597 // Thrown value may be null.
            }
        }
    }
}