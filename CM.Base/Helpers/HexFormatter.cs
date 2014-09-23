namespace System
{
    public static class HexFormatter
    {
        public static string ToHex(this byte[] data)
        {
            string hex = BitConverter.ToString(data).Replace("-", string.Empty).ToLowerInvariant();
            return hex;
        }
    }
}
