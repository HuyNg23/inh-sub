using IBox.Common.Objects;
using IBox.Database.Root;

namespace IBox.Client.Business.Model
{
    public class BWorkstep : BBaseBusiness<BWorkstep>
    {
        public string? Id { get; set; }

        public void RemoveStep()
        {
            try
            {
                if (string.IsNullOrEmpty(this.Id))
                {
                    throw new IboxLog("target of Edge is null", tenantContext.Context.TenantInfo.Id);
                }


                using (var transaction = this.tenantContext.Context.Database.BeginTransaction())
                {
                    var wfs = this.tenantContext.Context.WF_Steps.FirstOrDefault(ptr => ptr.Id == this.Id);
                    if (wfs == null)
                    {
                        throw new IboxLog(string.Format("Can not be found work step id: {0}", this.Id), tenantContext.Context.TenantInfo.Id);
                    }

                    RestAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                    {
                        Id = tenantContext.Context.TenantInfo.Id,
                        MailTitle = "[Warning] Delete Step!",
                        MailBody = wfs.Description,
                        WFID = wfs.WFid,
                        typeWarning = TypeWarning.DeleteStepWF
                    });

                    wfs.IsDelete = true;

                    var wfsedge = this.tenantContext.Context.WF_Step_Edges.Where(ptr => (ptr.Target == this.Id || ptr.Source == this.Id) && !ptr.IsDelete).ToList();
                    wfsedge.ForEach(ptr => ptr.IsDelete = true);
                    this.tenantContext.Context.SaveChanges();
                    transaction.Commit();
                }

            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred {ex.Message}", tenantContext.Context.TenantInfo.Id,ex);
            }
        }
    }
}