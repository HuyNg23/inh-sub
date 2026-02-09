//using IBox.Common.Model;
//using IBox.Schedule.Library.HandleSqlDependency;
//using Microsoft.AspNetCore.Mvc;

//namespace IBox.Schedule.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class ListenTableDependencyController : ControllerBase
//    {
//        private readonly ISqlDependencyDatabase _sqlDependencyDatabase;

//        public ListenTableDependencyController(ISqlDependencyDatabase sqlDependencyDatabase)
//        {
//            _sqlDependencyDatabase = sqlDependencyDatabase;
//        }

//        [HttpGet("ReLoadSqlDependency")]
//        public IActionResult ReLoadSqlDependency()
//        {
//            if (_sqlDependencyDatabase is SqlDependencyDatabase db)
//            {
//                db.DisableDispose(); // Hàm này sẽ tắt Dispose
//            }

//            _sqlDependencyDatabase.OnLoadSqlDependency(TypeUserBase.Tenant);
//            return Ok();
//        }
//    }
//}