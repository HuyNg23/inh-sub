using IBox.Common.Objects;
using Serilog;
using System.Text;

namespace IBox.Common.Version
{
    public class CheckVersion : ICheckVersion
    {
        public CheckVersionIBoxModel ShowVersion(string parth)
        {
            if (File.Exists(parth))
            {
                try
                {
                    string content = File.ReadAllText(parth, Encoding.UTF8);
                    return new CheckVersionIBoxModel()
                    {
                        Data = content
                    };
                }
                catch (Exception ex)
                {
                    new IboxLog($"ShowVersion: {ex.Message}", "AppLogs", "Error");
                    throw;
                }
            }
            else
            {
                throw new IboxLog($"ShowVersion is not Parth: {parth}", "AppLogs");
            }
        }
    }
}