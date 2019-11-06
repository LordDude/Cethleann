﻿using System;
using System.IO;
using System.Linq;
using Cethleann.G1;
using Cethleann.DataTables;
using Cethleann.G1.G1ModelSection;
using Cethleann.G1.G1ModelSection.G1MGSection;
using DragonLib.Imaging;
using DragonLib.Imaging.DXGI;

namespace Cethleann.Model
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = args.ElementAtOrDefault(0);
            var set = args.ElementAtOrDefault(1);

            if (container == null || !File.Exists(container))
            {
                Console.WriteLine("Usage: Cethleann.Model.exe E32.bin");
                Console.WriteLine("Usage: Cethleann.Model.exe model.g1m textureset.g1t");
                return;
            }

            if (set != null && !File.Exists(set))
            {
                Console.WriteLine($"File does not exist: {set}");
                set = null;
            }

            var destination = Path.ChangeExtension(container, "gltf");
            var texDestination = Path.Combine(Path.GetDirectoryName(container), Path.GetFileNameWithoutExtension(container) + "_textures");

            using var file = File.OpenRead(container);
            var containerData = new Span<byte>(new byte[file.Length]);
            file.Read(containerData);

            var g1m = default(G1Model);
            var g1t = default(G1TextureGroup);

            if (containerData.GetDataType() == DataType.Model)
            {
                if (set != null)
                {
                    using var fileset = File.OpenRead(set);
                    var setData = new Span<byte>(new byte[fileset.Length]);
                    fileset.Read(setData);
                    if (setData.GetDataType() == DataType.TextureGroup)
                    {
                        g1t = new G1TextureGroup(setData);
                    }
                }

                g1m = new G1Model(containerData);
            }
            else if (containerData.IsDataTable())
            {
                var dataTable = new DataTable(containerData);
                var g1mData = dataTable.Entries.FirstOrDefault(x => x.Span.GetDataType() == DataType.Model);
                var g1tData = dataTable.Entries.FirstOrDefault(x => x.Span.GetDataType() == DataType.TextureGroup);
                if (!g1mData.IsEmpty)
                {
                    g1m = new G1Model(g1mData.Span);
                }

                if (!g1tData.IsEmpty)
                {
                    g1t = new G1TextureGroup(g1tData.Span);
                }
            }

            if (g1m == null)
            {
                Console.WriteLine("Can't find G1M file");
                return;
            }

            if (g1t != null)
            {
                SaveTextures(texDestination, g1t);
            }

            SaveModel(destination, g1m, Path.GetFileName(texDestination));
        }

        private static void SaveTextures(string pathBase, G1TextureGroup group)
        {
            var i = 0;
            if (!Directory.Exists(pathBase)) Directory.CreateDirectory(pathBase);

            foreach (var (_, header, _, blob) in group.Textures)
            {
                var (width, height, mips, format) = G1TextureGroup.UnpackWHM(header);
                var data = DXGI.DecompressDXGIFormat(blob.Span, width, height, format);
                if (!TiffImage.WriteTiff($@"{pathBase}\{i:X4}.tif", data, width, height)) File.WriteAllBytes($@"{pathBase}\{i:X16}.dds", DXGI.BuildDDS(format, mips, width, height, blob.Span).ToArray());
                i += 1;
            }
        }

        private static void SaveModel(string pathBase, G1Model model, string texBase)
        {
            var geom = model.GetSection<G1MG>();
            var gltf = model.ExportMeshes(Path.ChangeExtension(pathBase, "bin"), $"{Path.GetFileNameWithoutExtension(pathBase)}.bin", 0, 0, texBase);
            using var file = File.OpenWrite(pathBase);
            file.SetLength(0);
            using var writer = new StreamWriter(file);
            gltf.Serialize(writer);
            using var materialInfo = File.OpenWrite(Path.ChangeExtension(pathBase, "material.txt"));
            materialInfo.SetLength(0);
            var materials = geom.GetSection<G1MGMaterial>();
            using var materialWriter = new StreamWriter(materialInfo);
            for (var index = 0; index < materials.Materials.Count; ++index)
            {
                var (material, textureSet) = materials.Materials[index];
                materialWriter.WriteLine($"Material {index} {{ Count = {material.Count}, Unknowns = [{material.Unknown1}, {material.Unknown2}, {material.Unknown3}] }}");
                foreach (var texture in textureSet) materialWriter.WriteLine($"\tTexture {{ Index = {texture.Index:X4}, Type = {texture.Kind:G}, AlternateType = {texture.AlternateKind:G}, UV Layer = {texture.TexCoord}, Unknowns = [{texture.Unknown4}, {texture.Unknown5}] }}");
            }
        }
    }
}