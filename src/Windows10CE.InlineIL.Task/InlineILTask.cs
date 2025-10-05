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
        
        [Required]
        public required string TargetFramework { get; set; }
        
        [Required]
        public required string DebugType { get; set; }
        
        public string? PdbFile { get; set; }
        
        public override bool Execute()
        {
            AssemblyProcessor.Process(InputPath.GetMetadata("FullPath"), References.Select(r => r.GetMetadata("FullPath")), OutputPath.GetMetadata("FullPath"), TargetFramework, DebugType, PdbFile);
            return true;
        }
    }
}
