using System;

namespace TextCycler
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly
    public class FailException : Exception
    {
        public FailException(string message) : base(message)
        {
        }
    }
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
}
