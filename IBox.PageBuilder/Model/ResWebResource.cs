using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBox.PageBuilder.Model
{
    public class ResWebResource
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Library { get; set; } = string.Empty;
        public string? HTML { get; set; } = string.Empty;
        public string? CSS { get; set; } = string.Empty;
        public string? JavaScript { get; set; } = string.Empty;
    }
}
