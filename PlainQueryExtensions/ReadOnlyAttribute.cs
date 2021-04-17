using System;

namespace PlainQueryExtensions
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ReadOnlyAttribute : Attribute
    {
    }
}