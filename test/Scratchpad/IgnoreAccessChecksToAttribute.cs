[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("System.Private.CoreLib")]

namespace System.Runtime.CompilerServices;

internal sealed class IgnoresAccessChecksToAttribute(string assemblyName) : Attribute;
