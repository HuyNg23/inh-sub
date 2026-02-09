using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    public class A_AssetLibraryStorage: BaseTable
    {
        public TypeOS? TypeOS { get; set; }

        public TypeStorage? TypeStorage { get; set; }

        [MaxLength(256)]
        public string Host { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Port { get; set; } = string.Empty;
        
        [MaxLength(450)]
        public string UserName { get; set; } = string.Empty;
        
        [MaxLength(450)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string PathStorage { get; set; } = string.Empty;
    }

    public enum TypeOS
    {
        Window = 0,
        Linux = 1
    }

    public enum TypeStorage
    {
        Local = 0,
        FTP = 1,
        SFTP = 2,
    }
}
