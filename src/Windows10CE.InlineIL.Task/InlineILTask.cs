using System.Collections.Immutable;
using Microsoft.Build.Framework;
using Windows10CE.InlineIL.Processor;

namespace Windows10CE.InlineIL.Task
{
    public sealed class InlineILTask : Microsoft.Build.Utilities.Task
    {
        [Required]
        public required ITaskItem InputPath { get; set; }
        
        [Required]
        public required ITaskItem[] References { get; set; }
        
        [Required]
        public required ITaskItem OutputPath { get; set; }
        
        public string? PdbFile { get; set; }
        
        public override bool Execute()
        {
            AssemblyProcessor.Process(InputPath.GetMetadata("FullPath"), References.Select(r => r.GetMetadata("FullPath")).ToImmutableArray(), OutputPath.GetMetadata("FullPath"), PdbFile);
            return true;
        }
    }
}
