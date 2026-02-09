using IBox.Common.Objects;
using IBox.Database.Root;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data;

namespace IBox.Database.UpdateVersion.Implementation
{
    public class DatabaseSynchronizer<TContext> where TContext : DbContext, IBContext<TContext>
    {
        private readonly TContext _dbContext;

        public DatabaseSynchronizer(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void SynchronizeDatabase()
        {
            //using var connection = _dbContext.Database.GetDbConnection();
            //connection.Open();
            //using var transaction = connection.BeginTransaction();

            //try
            //{
            //    var entityTypes = _dbContext.Model.GetEntityTypes()
            //        .Where(e => !e.ClrType.Name.StartsWith("VW_")); // Bỏ qua views

            //    foreach (var entityType in entityTypes)
            //    {
            //        string tableName = entityType.GetTableName() ?? entityType.ClrType.Name;

            //        if (!DoesTableExist(connection, tableName, transaction))
            //        {
            //            CreateTable(connection, entityType, transaction);
            //        }
            //        else
            //        {
            //            SynchronizeTable(connection, entityType, transaction);
            //        }
            //    }

            //    transaction.Commit();
            //    Console.WriteLine($"Database synchronization completed for {_dbContext.Database.GetDbConnection().Database}");
            //}
            //catch (Exception ex)
            //{
            //    transaction.Rollback();
            //    throw new IboxException($"Failed to synchronize database: {ex.Message}", ex);
            //}
        }

        //private bool DoesTableExist(IDbConnection connection, string tableName, IDbTransaction transaction)
        //{
        //    using var command = connection.CreateCommand();
        //    command.Transaction = transaction;
        //    command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
        //    var parameter = command.CreateParameter();
        //    parameter.ParameterName = "@TableName";
        //    parameter.Value = tableName;
        //    command.Parameters.Add(parameter);

        //    return (int)command.ExecuteScalar() > 0;
        //}

        //private void CreateTable(IDbConnection connection, IEntityType entityType, IDbTransaction transaction)
        //{
        //    string tableName = entityType.GetTableName() ?? entityType.ClrType.Name;
        //    var properties = entityType.GetProperties();
        //    var primaryKey = entityType.FindPrimaryKey();

        //    using var command = connection.CreateCommand();
        //    command.Transaction = transaction;

        //    // Tạo cột
        //    var columns = properties.Select(p =>
        //    {
        //        string columnName = p.GetColumnName() ?? p.Name;
        //        string sqlDataType = GetSqlDataType(p);
        //        return $"{columnName} {sqlDataType}";
        //    }).ToList();

        //    // Thêm Primary Key nếu có
        //    if (primaryKey != null)
        //    {
        //        var pkColumns = string.Join(", ", primaryKey.Properties.Select(p => p.GetColumnName() ?? p.Name));
        //        columns.Add($"CONSTRAINT PK_{tableName} PRIMARY KEY ({pkColumns})");
        //    }

        //    //string createTableSql = $"CREATE TABLE {tableName} ({string.Join(", ", columns)})";
        //    string createTableSql = "";
        //    command.CommandText = createTableSql;
        //    command.ExecuteNonQuery();
        //    Console.WriteLine($"Created new table {tableName}");

        //    // Tạo Index và Constraint khác
        //    CreateIndexesAndConstraints(connection, entityType, transaction);
        //}

        //private void SynchronizeTable(IDbConnection connection, IEntityType entityType, IDbTransaction transaction)
        //{
        //    string tableName = entityType.GetTableName() ?? entityType.ClrType.Name;
        //    var properties = entityType.GetProperties();
        //    var existingColumns = GetExistingColumns(connection, tableName, transaction);

        //    using var command = connection.CreateCommand();
        //    command.Transaction = transaction;

        //    foreach (var property in properties)
        //    {
        //        string columnName = property.GetColumnName() ?? property.Name;
        //        string sqlDataType = GetSqlDataType(property);

        //        if (!existingColumns.ContainsKey(columnName))
        //        {
        //            string addColumnSql = $"ALTER TABLE {tableName} ADD {columnName} {sqlDataType}";
        //            command.CommandText = addColumnSql;
        //            command.ExecuteNonQuery();
        //            Console.WriteLine($"Added column {columnName} to {tableName}");
        //        }
        //        else
        //        {
        //            string existingType = existingColumns[columnName];
        //            if (!IsTypeCompatible(existingType, sqlDataType))
        //            {
        //                DropDependentObjects(connection, transaction, tableName, columnName);
        //                string modifyColumnSql = $"ALTER TABLE {tableName} ALTER COLUMN {columnName} {sqlDataType}";
        //                command.CommandText = modifyColumnSql;
        //                command.ExecuteNonQuery();
        //                Console.WriteLine($"Modified column {columnName} in {tableName} from {existingType} to {sqlDataType}");
        //            }
        //        }
        //    }

        //    // Đồng bộ Index và Constraint
        //    CreateIndexesAndConstraints(connection, entityType, transaction);
        //}

        //private void CreateIndexesAndConstraints(IDbConnection connection, IEntityType entityType, IDbTransaction transaction)
        //{
        //    string tableName = entityType.GetTableName() ?? entityType.ClrType.Name;
        //    using var command = connection.CreateCommand();
        //    command.Transaction = transaction;

        //    // 1. Kiểm tra và tạo Indexes
        //    var indexes = entityType.GetIndexes();
        //    var existingIndexes = GetExistingIndexes(connection, tableName, transaction);

        //    foreach (var index in indexes)
        //    {
        //        string indexName = index.GetDatabaseName() ?? $"IX_{tableName}_{string.Join("_", index.Properties.Select(p => p.Name))}";
        //        var indexColumns = string.Join(", ", index.Properties.Select(p => p.GetColumnName() ?? p.Name));
        //        string indexSql = $"CREATE {(index.IsUnique ? "UNIQUE " : "")}INDEX {indexName} ON {tableName} ({indexColumns})";

        //        if (!existingIndexes.Contains(indexName))
        //        {
        //            command.CommandText = indexSql;
        //            command.ExecuteNonQuery();
        //            Console.WriteLine($"Created index {indexName} on {tableName}");
        //        }
        //    }

        //    // 2. Kiểm tra và tạo Constraints (ngoài PK đã xử lý trong CreateTable)
        //    var foreignKeys = entityType.GetForeignKeys();
        //    var existingConstraints = GetExistingConstraints(connection, tableName, transaction);

        //    foreach (var fk in foreignKeys)
        //    {
        //        string fkName = fk.GetConstraintName() ?? $"FK_{tableName}_{fk.PrincipalEntityType.GetTableName()}_{fk.Properties.First().Name}";
        //        var fkColumns = string.Join(", ", fk.Properties.Select(p => p.GetColumnName() ?? p.Name));
        //        var principalTable = fk.PrincipalEntityType.GetTableName() ?? fk.PrincipalEntityType.ClrType.Name;
        //        var principalColumns = string.Join(", ", fk.PrincipalKey.Properties.Select(p => p.GetColumnName() ?? p.Name));

        //        string fkSql = $"ALTER TABLE {tableName} ADD CONSTRAINT {fkName} FOREIGN KEY ({fkColumns}) REFERENCES {principalTable} ({principalColumns})";

        //        if (!existingConstraints.Contains(fkName))
        //        {
        //            command.CommandText = fkSql;
        //            command.ExecuteNonQuery();
        //            Console.WriteLine($"Created foreign key {fkName} on {tableName}");
        //        }
        //    }

        //    // 3. Default Constraints (nếu có trong metadata)
        //    foreach (var property in entityType.GetProperties())
        //    {
        //        var defaultValue = property.GetDefaultValueSql();
        //        if (!string.IsNullOrEmpty(defaultValue))
        //        {
        //            string columnName = property.GetColumnName() ?? property.Name;
        //            string constraintName = $"DF_{tableName}_{columnName}";
        //            if (!existingConstraints.Contains(constraintName))
        //            {
        //                command.CommandText = $"ALTER TABLE {tableName} ADD CONSTRAINT {constraintName} DEFAULT {defaultValue} FOR {columnName}";
        //                command.ExecuteNonQuery();
        //                Console.WriteLine($"Created default constraint {constraintName} on {tableName}.{columnName}");
        //            }
        //        }
        //    }
        //}

        private void DropDependentObjects(IDbConnection connection, IDbTransaction transaction, string tableName, string columnName)
        {
            //using var command = connection.CreateCommand();
            //command.Transaction = transaction;

            //// Xóa Default Constraint
            //string defaultConstraintQuery = @"
            //SELECT d.name
            //FROM sys.default_constraints d
            //INNER JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
            //INNER JOIN sys.tables t ON t.object_id = c.object_id
            //WHERE t.name = @TableName AND c.name = @ColumnName";

            //command.CommandText = defaultConstraintQuery;
            //var param1 = command.CreateParameter();
            //param1.ParameterName = "@TableName";
            //param1.Value = tableName;
            //command.Parameters.Add(param1);
            //var param2 = command.CreateParameter();
            //param2.ParameterName = "@ColumnName";
            //param2.Value = columnName;
            //command.Parameters.Add(param2);

            //string? defaultConstraintName = null;
            //using (var reader = command.ExecuteReader())
            //{
            //    if (reader.Read())
            //    {
            //        defaultConstraintName = reader.GetString(0);
            //    }
            //}

            //if (!string.IsNullOrEmpty(defaultConstraintName))
            //{
            //    command.Parameters.Clear();
            //    command.CommandText = $"ALTER TABLE {tableName} DROP CONSTRAINT {defaultConstraintName}";
            //    command.ExecuteNonQuery();
            //    Console.WriteLine($"Dropped default constraint {defaultConstraintName} on {tableName}.{columnName}");
            //}

            //// Xóa Index
            //string indexQuery = @"
            //SELECT i.name
            //FROM sys.indexes i
            //INNER JOIN sys.index_columns ic ON i.index_id = ic.index_id AND i.object_id = ic.object_id
            //INNER JOIN sys.columns c ON ic.column_id = c.column_id AND ic.object_id = c.object_id
            //INNER JOIN sys.tables t ON t.object_id = c.object_id
            //WHERE t.name = @TableName AND c.name = @ColumnName AND i.is_primary_key = 0";

            //command.CommandText = indexQuery;
            //command.Parameters.Clear();
            //var param3 = command.CreateParameter();
            //param3.ParameterName = "@TableName";
            //param3.Value = tableName;
            //command.Parameters.Add(param3);
            //var param4 = command.CreateParameter();
            //param4.ParameterName = "@ColumnName";
            //param4.Value = columnName;
            //command.Parameters.Add(param4);

            //string? indexName = null;
            //using (var reader = command.ExecuteReader())
            //{
            //    if (reader.Read())
            //    {
            //        indexName = reader.GetString(0);
            //    }
            //}

            //if (!string.IsNullOrEmpty(indexName))
            //{
            //    command.Parameters.Clear();
            //    command.CommandText = $"DROP INDEX {indexName} ON {tableName}";
            //    command.ExecuteNonQuery();
            //    Console.WriteLine($"Dropped index {indexName} on {tableName}.{columnName}");
            //}
        }

        //private Dictionary<string, string> GetExistingColumns(IDbConnection connection, string tableName, IDbTransaction transaction)
        //{
        //    var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //    string query = @"
        //    SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
        //    FROM INFORMATION_SCHEMA.COLUMNS
        //    WHERE TABLE_NAME = @TableName";

        //    using var command = connection.CreateCommand();
        //    command.Transaction = transaction;
        //    command.CommandText = query;
        //    var parameter = command.CreateParameter();
        //    parameter.ParameterName = "@TableName";
        //    parameter.Value = tableName;
        //    command.Parameters.Add(parameter);

        //    try
        //    {
        //        using var reader = command.ExecuteReader();
        //        while (reader.Read())
        //        {
        //            string columnName = reader.GetString(0);
        //            string dataType = reader.GetString(1).ToLower();
        //            string maxLength = reader.IsDBNull(2) ? "" : $"({reader.GetInt32(2)})";
        //            columns[columnName] = $"{dataType}{maxLength}";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error getting columns for {tableName}: {ex.Message}");
        //    }

        //    return columns;
        //}

        //private HashSet<string> GetExistingIndexes(IDbConnection connection, string tableName, IDbTransaction transaction)
        //{
        //    var indexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //    string query = @"
        //    SELECT i.name
        //    FROM sys.indexes i
        //    INNER JOIN sys.tables t ON t.object_id = i.object_id
        //    WHERE t.name = @TableName AND i.is_primary_key = 0";

        //    using var command = connection.CreateCommand();
        //    command.Transaction = transaction;
        //    command.CommandText = query;
        //    var parameter = command.CreateParameter();
        //    parameter.ParameterName = "@TableName";
        //    parameter.Value = tableName;
        //    command.Parameters.Add(parameter);

        //    using var reader = command.ExecuteReader();
        //    while (reader.Read())
        //    {
        //        indexes.Add(reader.GetString(0));
        //    }

        //    return indexes;
        //}

        //private HashSet<string> GetExistingConstraints(IDbConnection connection, string tableName, IDbTransaction transaction)
        //{
        //    var constraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //    string query = @"
        //    SELECT name
        //    FROM sys.objects
        //    WHERE type IN ('D', 'F', 'PK') AND parent_object_id = OBJECT_ID(@TableName)";

        //    using var command = connection.CreateCommand();
        //    command.Transaction = transaction;
        //    command.CommandText = query;
        //    var parameter = command.CreateParameter();
        //    parameter.ParameterName = "@TableName";
        //    parameter.Value = tableName;
        //    command.Parameters.Add(parameter);

        //    using var reader = command.ExecuteReader();
        //    while (reader.Read())
        //    {
        //        constraints.Add(reader.GetString(0));
        //    }

        //    return constraints;
        //}

        //private string GetSqlDataType(IProperty property)
        //{
        //    // Lấy kiểu cơ bản nếu là Nullable<T>
        //    Type clrType = property.ClrType.IsGenericType && property.ClrType.GetGenericTypeDefinition() == typeof(Nullable<>)
        //        ? property.ClrType.GetGenericArguments()[0] // Lấy kiểu bên trong (ví dụ: DateTime từ DateTime?)
        //        : property.ClrType; // Giữ nguyên nếu không nullable

        //    string type = clrType.Name.ToLower() switch
        //    {
        //        "string" => "nvarchar",
        //        "int" => "int",
        //        "int32" => "int",
        //        "datetime" => "datetime2(7)",
        //        "bool" => "bit",
        //        "boolean" => "bit",
        //        "decimal" => "decimal(18,2)",
        //        "double" => "float",
        //        "guid" => "uniqueidentifier",
        //        _ => "nvarchar"
        //    };

        //    if (type == "nvarchar")
        //    {
        //        var maxLength = property.GetMaxLength();
        //        type += maxLength.HasValue ? $"({maxLength})" : "(max)";
        //    }

        //    // Thêm NULL nếu property có thể null và không phải nvarchar
        //    return property.IsNullable && type != "nvarchar" ? $"{type} NULL" : type;
        //}

        //private bool IsTypeCompatible(string existingType, string newType)
        //{
        //    existingType = existingType.ToLower().Replace(" ", "");
        //    newType = newType.ToLower().Replace(" ", "");

        //    if (existingType == newType) return true;

        //    if (existingType.StartsWith("nvarchar") && newType.StartsWith("nvarchar"))
        //    {
        //        int existingLength = existingType.Contains("(") ?
        //            int.Parse(existingType.Split('(')[1].TrimEnd(')')) : -1;
        //        int newLength = newType.Contains("(") ?
        //            int.Parse(newType.Split('(')[1].TrimEnd(')')) : -1;

        //        return existingLength == newLength || existingLength == -1;
        //    }

        //    return false;
        //}
    }
}

