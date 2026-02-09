using FluentFTP;
using Renci.SshNet;

namespace IBox.Common.Chat.CommonData.CallData
{
    public interface ICommonData
    {
        public void CreateFolder(string PathLocal);

        public void CreateFolder1(string PathLocal);
        public string CreateFolderFTP(FtpClient ftp, string pathLocal);
        public string CreateFolderSFTP(SftpClient sftp, string PathLocal);
        public string CreateFolderSFTP1(SftpClient sftp, string PathLocal);
        public int ShowSecond();

        public string formatDataStringSQL(string? data);

        public string GetFilePath(string Parth, DateTime messageDate);

        public DateTime ToDate(string date);

        public DateTime ToDate1(string? date);

        public DateTime? ToDate2(object x);

        public DateTime ToDate3(object x);

        public HttpResponseMessage ExecuteAPI(string body, string Url, string tokenBOT);

        public Configuration.CommonData.ReponeUploadFileBOT UploadFileToFPT(string Url, byte[] bytes, string filename, string token);

        public string CallApiPost(string url, string bodyJson, string userName, string pass);

        public string[] SplitFolder(string PathLocal);

        public byte[] ConvertUrlToByte(string url);

        /// <summary>
        ///
        /// </summary>
        /// <param name="sourceDir">thư mục cần copy</param>
        /// <param name="destinationDir">thư mục đích</param>
        public void CopyDirectory(string sourceDir, string destinationDir);

        public Dictionary<string, string> GetColumnsFromModelSQLite<T>();

        public long ConvertDateTimeToTimeSpan(DateTime dateTime);

        public DateTime ConvertTimeSpanToDateTime(long unixTimestamp);

        public void ExtractFileZip(string fullName, string extractPath);

        public void ExtractFileZipPattern(string fullName, string extractPath, string pattern);

        public void SaveFileLogCreateFile(string body, string path);
    }
}