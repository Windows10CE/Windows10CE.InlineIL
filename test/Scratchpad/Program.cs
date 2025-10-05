using System.IO.Compression;
using AsmResolver;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE;
using AsmResolver.PE.Debug;
using AsmResolver.PE.DotNet.Metadata;
using Windows10CE.InlineIL.PortablePdb;

var image = PEImage.FromFile(typeof(C).Assembly.Location);
var mod = new SerializedModuleDefinition(image, new ModuleReaderParameters());

ISegment pdbData = image.DebugData.Single(dd => dd.Contents?.Type == (DebugDataType)17).Contents!;
pdbData = ((CustomDebugDataSegment)pdbData).Contents!;
var reader = pdbData.ToReference().CreateReader();

reader.ReadUInt32();
var uncompressedSize = reader.ReadInt32();

var memoryStream = new MemoryStream(reader.ReadToEnd());
var compressStream = new DeflateStream(memoryStream, CompressionMode.Decompress);

var bytes = new byte[uncompressedSize];
compressStream.ReadExactly(bytes);

mod.ReaderContext.PdbDirectory = MetadataDirectory.FromBytes(bytes);

Console.WriteLine(string.Join('\n', mod.ManagedEntryPointMethod!.MethodDebugInformation!.SequencePoints));

class C;
