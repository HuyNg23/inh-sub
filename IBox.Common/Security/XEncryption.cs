using IBox.Common.Objects;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IBox.Common.Security
{
    public class XEncryption : IEncryption
    {
        private readonly IConfiguration _configuration;

        public XEncryption(IConfiguration config)
        {
            this._configuration = config;
        }

        public string JWT(string tenantID, string userName, Action<string> onSignin, params KeyValuePair<string, string>[] data)
        {

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this._configuration.Config.Value.JWT.Key));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            var exp = DateTime.Now.AddMinutes(this._configuration.Config.Value.JWT.TotalMinuteAlive);
            DateTimeOffset dto = new DateTimeOffset(exp.ToLocalTime());
            var timeInSeconds = dto.ToUnixTimeSeconds();
            var securityCode = SHAEncode(tenantID + userName + timeInSeconds);
            var claims = new List<Claim> {
                new Claim("User", userName),
                new Claim("Security", securityCode)
            };

            onSignin(securityCode);

            foreach (var item in data)
            {
                claims.Add(new Claim(item.Key, item.Value));
            }

            var token = new JwtSecurityToken(
                  this._configuration.Config.Value.JWT.Issuer,
                  this._configuration.Config.Value.JWT.Issuer,
                  claims,
                  expires: exp,
                  signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

        public JwtSecurityToken? JWTDecode(string jwt)
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(jwt, GetValidationParameters(), out var validatedToken);
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase);

                    if (result == false)
                    {
                        return null;
                    }
                }
                return handler.ReadJwtToken(jwt);
            }
            catch
            {
                return null;
            }
        }

        private TokenValidationParameters GetValidationParameters()
        {

            return new TokenValidationParameters()
            {
                ValidateLifetime = false,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidIssuer = this._configuration.Config.Value.JWT.Issuer,
                ValidAudience = this._configuration.Config.Value.JWT.Issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this._configuration.Config.Value.JWT.Key))
            };

        }

        public string SHAEncode(string source)
        {
            try
            {
                using (SHA1 sha1 = SHA1.Create())
                {
                    var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(source));
                    var sb = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash)
                    {
                        sb.Append(b.ToString("X2"));
                    }

                    return sb.ToString();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private readonly string key = "aqHQMH7wU1&T0XIZVB[eUi%[D!{aI4[,";

        public string Encrypt(string toEncrypt)
        {
            byte[] Key = Encoding.UTF8.GetBytes(key);
            byte[] IV = Encoding.UTF8.GetBytes("Ex$[.@2:d*Qq}G{c");
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = aes.CreateEncryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return Convert.ToBase64String(resultArray, 0, resultArray.Length);
            }
        }

        public string Decrypt(string toDecrypt)
        {
            byte[] Key = Encoding.UTF8.GetBytes(key);
            byte[] IV = Encoding.UTF8.GetBytes("Ex$[.@2:d*Qq}G{c");
            byte[] cipherTextBytes = Convert.FromBase64String(toDecrypt);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor();

                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherTextBytes, 0, cipherTextBytes.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }
}