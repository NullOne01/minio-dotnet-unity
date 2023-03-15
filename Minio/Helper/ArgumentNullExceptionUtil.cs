using System;

namespace Minio.Helper
{
    public static class ArgumentNullExceptionUtil
    {
        public static void ThrowIfNull(object checkObject)
        {
            if (checkObject == null)
            {
                throw new ArgumentNullException(nameof(checkObject));
            }
        }
    }
}