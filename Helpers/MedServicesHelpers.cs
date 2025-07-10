using System.Security.Cryptography;
using System.Text;

namespace MedServices.Helpers
{
    public class SecurityHelper
    {
        public static int GenerateRandomNumberInt(int length)
        {
            return RandomNumberGenerator.GetInt32(Math.Min(int.MaxValue, length));
        }
        public static string GenerateRandomString(int length)
        {
            byte[] arrBytes = RandomNumberGenerator.GetBytes(length);
            return Encoding.Default.GetString(arrBytes);
        }
    }
}
