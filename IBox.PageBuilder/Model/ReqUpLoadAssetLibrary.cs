using Microsoft.AspNetCore.Http;

namespace IBox.PageBuilder.Model
{
    public class ReqUpLoadAssetLibrary
    {
        public string Id { get; set; } = string.Empty;
        public List<IFormFile>? Files { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version {  get; set; } = string.Empty;
        public string Path {  get; set; } = string.Empty;
    }
}
