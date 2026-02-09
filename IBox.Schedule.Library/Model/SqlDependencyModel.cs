using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBox.Schedule.Library.Model
{
    public class SqlDependencyModel
    {
        public string querry { get; set; } = "";
        public SqlDependency sqlDependency { get; set; }
        public OnChangeEventHandler? onChangeEventHandler { get; set; }
    }
}
