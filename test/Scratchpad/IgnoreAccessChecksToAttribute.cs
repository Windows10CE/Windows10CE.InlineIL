using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

#pragma warning disable CS9113 // Parameter is unread.
internal sealed class IgnoresAccessChecksToAttribute(string assemblyName) : Attribute;
#pragma warning restore CS9113 // Parameter is unread.
