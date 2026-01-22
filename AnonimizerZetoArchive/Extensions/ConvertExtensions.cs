using System;
using System.Collections.Generic;
using System.Text;

namespace AnonimizerZetoArchive.Extensions
{
    public static class ConvertExtensions
    {
        public static AppMode ToAppModeEnum(this AppMode value, string str)
        {
            if (Enum.TryParse<AppMode>(str, true, out var result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException($"Invalid AppMode value: {str}");
            }
        }
    }

    public static class ConnectionStringValidator
    {
        public static bool Validate(string connectionString) =>
            connectionString.Contains("Server=") && connectionString.Contains("Database=");
    }
}
