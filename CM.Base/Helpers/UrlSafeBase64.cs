using System;
using CodeContract = System.Diagnostics.Contracts.Contract;

namespace CM.Base.Helpers
{
    /// <summary>
    /// A Base64 encoding that uses '-' and '_' instead of '+' and '/', thus 
    /// making it safe for use in URLs
    /// </summary>
    public static class UrlSafeBase64
    {
        /// <summary>
        /// Encodes a byte array in a Base64 variant that is URL safe through 
        /// the characters ('-', '_') instead of ('+', '/')
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Encode(byte[] input)
        {
            CodeContract.Requires<ArgumentNullException>(input != null);
            return ToUrlSafeVariant(Convert.ToBase64String(input));
        }

        public static string EncodeWithoutPadding(byte[] input)
        {
            CodeContract.Requires<ArgumentNullException>(input != null);
            return Encode(input).Replace("=", "");
        }

        /// <summary>
        /// Decodes a "UrlSafeVariant Base64" string into a byte[]
        /// </summary>
        /// <param name="base64Encoded"></param>
        /// <returns></returns>
        public static byte[] Decode(string base64Encoded)
        {
            CodeContract.Requires<ArgumentNullException>(base64Encoded != null);
            CodeContract.Ensures(CodeContract.Result<byte[]>() != null);
            return Convert.FromBase64String(FromUrlSafeVariant(base64Encoded));
        }

        public static string ToUrlSafeVariant(string input)
        {
            CodeContract.Requires<ArgumentNullException>(input != null);
            return input.Replace('+', '-').Replace('/', '_');
        }

        public static string FromUrlSafeVariant(string input)
        {
            CodeContract.Requires<ArgumentNullException>(input != null);
            while (input.Length % 4 != 0)
                input += "=";
            return input.Replace('-', '+').Replace('_', '/');
        }
    }
}
