using FluentFTP;
using IBox.PageBuilder.Model;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBox.PageBuilder.Factories
{
    public interface IHandleFileAndDB
    {
        public void AddOrUpdateAssetLibraryDb(string name, string url, string version, string tags, string tenantId);
        public void DeleteFileSFTP(SftpClient sftpClient, string path);
        public void DeleteFileFTP(FtpClient ftpClient, string path);
        public string NormalizePath(string path);
        public void DeleteAssetLibraryOnDb(string id, string tenantId);
        public bool UpdateSaveFileComponentsDB(ReqWebResource req, string tenantId);
    }
}
