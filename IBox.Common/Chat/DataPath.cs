namespace IBox.Common.Chat.Configuration
{
    public class DataPath
    {
        public static string GetRuntimeRoot()
        {
            var configuredRoot = Environment.GetEnvironmentVariable("IBOX_DATA_ROOT");
            if (string.IsNullOrWhiteSpace(configuredRoot))
            {
                return Directory.GetCurrentDirectory();
            }

            return Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredRoot));
        }

        public static string CombineWithRuntimeRoot(params string[] segments)
        {
            var validSegments = segments
                .Where(ptr => !string.IsNullOrWhiteSpace(ptr))
                .ToArray();

            if (validSegments.Length == 0)
            {
                return GetRuntimeRoot();
            }

            return Path.Combine(new[] { GetRuntimeRoot() }.Concat(validSegments).ToArray());
        }

        //Thư mục chứa data base của chat bot
        public const string DataBaseChatBot = "DataBaseChatBot";

        //Thư mục lưu tin nhắn tạm và sẽ được xóa sau khi lấy dữ liệu
        public const string TemporaryFolder = "TemporaryFolderChatBot";

        //Thư mục lưu db sau khi giải nén và được xóa khi ngày mới
        public const string TemporaryFolderZip = "TemporaryFolderZip";

        //Thư mục tạm chứa data định danh kh
        public const string TemporaryFolderCustomer = "TemporaryFolderCustomerChatBot";

        //Thư mục chứa file data định danh khi bắt đầu chạy job
        public const string TemporaryMoveFolderCustomer = "TemporaryMoveFolderCustomerChatBot";

        //Thư mục chứa file data tin nhắn khi bắt đầu chạy job
        public const string TemporaryMoveFolder = "TemporaryMoveFolderChatBot";

        //Thư mục chứa database log của IBox
        public const string DataBaseLogIBox = "DatabaseLogIBox";

        //Giải nén ZipDay
        public const string PathBackUpDBLogDayUnZip = "BackUpDBLogDayIBoxUnZip";

        //Lưu file backup theo day
        public const string PathBackUpDBLogDay = "BackUpDBLogDayIBox";

        //Giải nén ZipMonth
        public const string PathBackUpDBLogMonthUnZip = "BackUpDBLogDayMonthUnZip";

        //Lưu file backup theo tháng
        public const string PathBackUpDBLogMonth = "BackUpDBLogDayMonth";

        //Thư mục chứa file tạm api log
        public const string TemporaryFolderLogAPI = "TemporaryFolderLogAPI";

        //Thư mục move file tạm api log để thực hiện đọc file
        public const string TemporaryMoveFolderLogAPI = "TemporaryMoveFolderLogAPI";

        //Thư mục chứa file tạm wf
        public const string TemporaryFolderLogWF = "TemporaryFolderLogWF";

        //Thư mục move file tạm wf để thực hiện đọc file lưu vào db
        public const string TemporaryMoveFolderLogWF = "TemporaryMoveFolderLogWF";

        //Thư mục chứa file tạm api log
        public const string TemporaryFolderLogQuerySQL = "TemporaryFolderLogQuerySQL";

        //Thư mục move file tạm api log để thực hiện đọc file
        public const string TemporaryMoveFolderLogQuerySQL = "TemporaryMoveFolderLogQuerySQL";
    }
}