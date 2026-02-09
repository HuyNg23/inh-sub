using Microsoft.Extensions.Options;

namespace IBox.Common.Objects
{
    public interface IConfiguration
    {
        public IOptions<Configuration> Config { get; }
    }
}