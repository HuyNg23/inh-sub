using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBox.DLEx.Model
{
    public class ScriptConfig
    {
        public string Language { get; set; } = "js";       // "js" (mặc định) - trong tương lai có thể "csharp"
        public string Code { get; set; } = "";
        public int TimeoutMs { get; set; } = 1000;        // timeout mặc định 1000ms
        public long MemoryLimitBytes { get; set; } = 4_000_000; // giới hạn bộ nhớ Jint
        public bool AllowIO { get; set; } = false;       // hiện không expose IO cho JS
    }
}
