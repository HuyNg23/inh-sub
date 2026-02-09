using System.IdentityModel.Tokens.Jwt;

namespace IBox.Common.Security
{
    public interface IEncryption
    {
        /// <summary>
        /// Encode string to SHA string
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        string SHAEncode(string source);

        /// <summary>
        /// Encode JWT with param
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        string JWT(string tenantID, string userName, Action<string> onSignin, params KeyValuePair<string, string>[] data);

        /// <summary>
        /// Decode JWT to JwtSecurityToken
        /// </summary>
        /// <param name="jwt"></param>
        /// <returns></returns>
        JwtSecurityToken JWTDecode(string jwt);

        string Decrypt(string toDecrypt);

        string Encrypt(string toEncrypt);
    }
}