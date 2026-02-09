using System.Collections;

namespace IBox.Formatting
{
    public interface IString : IFormatting
    {
        /// <summary>
        /// Thêm vào 1 vị trí bất kỳ theo định dạng abc {0} xyz
        /// </summary>
        /// <param name="source"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        string AddEnd(string source, string material);

        /// <summary>
        /// Chuyển đổi danh sách kiểu nguyên thủy sang danh sách object
        /// </summary>
        /// <param name="values"></param>
        /// <param name="formatting"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        dynamic ListToObject(IList values, string formatting, Type type, string tenantId);

        /// <summary>
        /// Thay thế chuỗi
        /// </summary>
        /// <param name="source"></param>
        /// <param name="replateString"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        string Replace(string source, string replateString, string material);

        /// <summary>
        /// Cắt chuối sang danh sách object
        /// </summary>
        /// <param name="source"></param>
        /// <param name="character"></param>
        /// <param name="formatting"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        dynamic SplitToObject(string source, char character, string formatting, Type type, string tenantId);

        /// <summary>
        /// Lấy guid gán vào data
        /// </summary>
        /// <returns></returns>
		string GetGUID();

        /// <summary>
        /// Lấy random number từ 0->9
        /// </summary>
        /// <returns></returns>
		string RandomNumber();

        /// <summary>
        /// Get datetime theo fomatDate
        /// </summary>
        /// <param name="FomatDate"></param>
        /// <returns></returns>
		string GetDateTime(string FomatDate);
    }
}