using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Service.Identity
{
    public class AuthOptions
    {
        public const string ISSUER = "RcordatiCrmServer"; // издатель токена
        public const string AUDIENCE = "RecordatiUser"; // потребитель токена
        const string KEY = "Recordati_02092021!";   // ключ для шифрации
        public const int LIFETIME = 1440; // время жизни токена - 1 сутки
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}
